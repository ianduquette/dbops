using DbOps.Models;
using DbOps.Services;
using Terminal.Gui;

namespace DbOps.UI.Dialogs;

public class ConnectionSelectionDialog : Dialog {
    private readonly ConnectionManager _connectionManager;
    private readonly ListView _connectionListView;
    private readonly Button _connectButton;
    private readonly Button _addButton;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private readonly Button _testButton;
    private readonly Button _cancelButton;
    private readonly Label _statusLabel;

    private List<DatabaseConnection> _connections = new();
    private DatabaseConnection? _selectedConnection;

    public DatabaseConnection? SelectedConnection => _selectedConnection;
    public bool ConnectionSelected { get; private set; } = false;

    public ConnectionSelectionDialog(ConnectionManager connectionManager) : base("Select Database Connection") {
        _connectionManager = connectionManager;

        // Set dialog size and position
        Width = Dim.Percent(80);
        Height = Dim.Percent(70);
        X = Pos.Center();
        Y = Pos.Center();

        // Create connection list view
        _connectionListView = new ListView {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1),
            Height = Dim.Fill(4),
            AllowsMarking = false,
            CanFocus = true
        };

        // Create buttons
        _connectButton = new Button("Open") {
            X = 1,
            Y = Pos.Bottom(_connectionListView) + 1
        };

        _addButton = new Button("Add New") {
            X = Pos.Right(_connectButton) + 2,
            Y = Pos.Top(_connectButton)
        };

        _editButton = new Button("Edit") {
            X = Pos.Right(_addButton) + 2,
            Y = Pos.Top(_connectButton)
        };

        _deleteButton = new Button("Delete") {
            X = Pos.Right(_editButton) + 2,
            Y = Pos.Top(_connectButton)
        };

        _testButton = new Button("Test") {
            X = Pos.Right(_deleteButton) + 2,
            Y = Pos.Top(_connectButton)
        };

        _cancelButton = new Button("Close") {
            X = Pos.Right(_testButton) + 2,
            Y = Pos.Top(_connectButton)
        };

        // Create status label
        _statusLabel = new Label("Select a connection and press Open, or Add New to create a connection") {
            X = 1,
            Y = Pos.Bottom(_connectButton) + 1,
            Width = Dim.Fill(1),
            Height = 1
        };

        // Add instruction label
        var instructionLabel = new Label("Use ↑↓ to navigate, Enter/O to open, A to add new, Del to delete, C to close") {
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
            Height = 1
        };

        // Add controls to dialog
        Add(instructionLabel);
        Add(_connectionListView);
        Add(_connectButton);
        Add(_addButton);
        Add(_editButton);
        Add(_deleteButton);
        Add(_testButton);
        Add(_cancelButton);
        Add(_statusLabel);

        // Set up event handlers
        SetupEventHandlers();

        // Load connections
        LoadConnections();

        // Set initial focus
        _connectionListView.SetFocus();
    }

    private void SetupEventHandlers() {
        // Button events
        _connectButton.Clicked += OnConnectClicked;
        _addButton.Clicked += OnAddClicked;
        _editButton.Clicked += OnEditClicked;
        _deleteButton.Clicked += OnDeleteClicked;
        _testButton.Clicked += OnTestClicked;
        _cancelButton.Clicked += OnCancelClicked;

        // List view events
        _connectionListView.SelectedItemChanged += OnConnectionSelectionChanged;
        _connectionListView.OpenSelectedItem += (args) => OnConnectClicked();

        // Key events
        _connectionListView.KeyPress += OnListViewKeyPress;
    }

    private void OnListViewKeyPress(KeyEventEventArgs e) {
        switch (e.KeyEvent.Key) {
            case Key.Enter:
                OnConnectClicked();
                e.Handled = true;
                break;
            case Key.DeleteChar:
            case Key.Backspace:
                OnDeleteClicked();
                e.Handled = true;
                break;
            case Key.InsertChar:
            case Key.CursorRight: // Use CursorRight as alternative to Plus
                OnAddClicked();
                e.Handled = true;
                break;
            case Key.F2:
                OnEditClicked();
                e.Handled = true;
                break;
            case Key.F5:
                OnTestClicked();
                e.Handled = true;
                break;
            case Key.q:
            case Key.Q:
                OnCancelClicked();
                e.Handled = true;
                break;
        }
    }

    private void LoadConnections() {
        try {
            _connections = _connectionManager.GetConnectionsSortedByUsage();

            var displayItems = _connections.Select(conn => {
                var status = conn.IsDefault ? " [Default]" : "";
                var lastUsed = conn.LastUsed.ToString("yyyy-MM-dd HH:mm");
                return $"{conn.DisplayName}{status} - {conn.ConnectionSummary} (Last used: {lastUsed})";
            }).ToList();

            _connectionListView.SetSource(displayItems);

            // Select the default connection if available
            var defaultConnection = _connectionManager.DefaultConnection;
            if (defaultConnection != null) {
                var index = _connections.FindIndex(c => c.Id == defaultConnection.Id);
                if (index >= 0) {
                    _connectionListView.SelectedItem = index;
                }
            }

            UpdateButtonStates();
            UpdateStatusLabel();
        } catch (Exception ex) {
            _statusLabel.Text = $"Error loading connections: {ex.Message}";
        }
    }

    private void OnConnectionSelectionChanged(ListViewItemEventArgs e) {
        _selectedConnection = e.Item >= 0 && e.Item < _connections.Count ? _connections[e.Item] : null;
        UpdateButtonStates();
        UpdateStatusLabel();
    }

    private void UpdateButtonStates() {
        var hasSelection = _selectedConnection != null;
        var hasConnections = _connections.Count > 0;

        _connectButton.Enabled = hasSelection;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
        _testButton.Enabled = hasSelection;
    }

    private void UpdateStatusLabel() {
        if (_selectedConnection == null) {
            _statusLabel.Text = _connections.Count == 0
                ? "No connections configured. Press 'Add New' to create your first connection."
                : "Select a connection and press Open, or Add New to create a connection";
        } else {
            _statusLabel.Text = $"Selected: {_selectedConnection.DisplayName} ({_selectedConnection.ConnectionSummary})";
        }
    }

    private void OnConnectClicked() {
        if (_selectedConnection == null) {
            MessageBox.ErrorQuery("No Selection", "Please select a connection to connect to.", "OK");
            return;
        }

        try {
            // Test the connection before accepting
            _statusLabel.Text = "Testing connection...";
            Application.Refresh();

            if (_connectionManager.TestConnection(_selectedConnection)) {
                _connectionManager.UpdateConnectionLastUsed(_selectedConnection.Id);
                ConnectionSelected = true;
                Application.RequestStop();
            } else {
                MessageBox.ErrorQuery("Connection Failed",
                    $"Could not connect to {_selectedConnection.DisplayName}.\n\n" +
                    "Please check your connection settings and try again.", "OK");
                _statusLabel.Text = "Connection test failed";
            }
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Connection Error",
                $"Error testing connection: {ex.Message}", "OK");
            _statusLabel.Text = "Connection error occurred";
        }
    }

    private void OnAddClicked() {
        var addDialog = new AddConnectionDialog(_connectionManager);
        Application.Run(addDialog);

        if (addDialog.ConnectionAdded) {
            LoadConnections(); // Refresh the list

            // Select the newly added connection
            if (addDialog.NewConnection != null) {
                var index = _connections.FindIndex(c => c.Id == addDialog.NewConnection.Id);
                if (index >= 0) {
                    _connectionListView.SelectedItem = index;
                }
            }
        }
    }

    private void OnEditClicked() {
        if (_selectedConnection == null) return;

        var editDialog = new AddConnectionDialog(_connectionManager, _selectedConnection);
        Application.Run(editDialog);

        if (editDialog.ConnectionAdded) {
            LoadConnections(); // Refresh the list
        }
    }

    private void OnDeleteClicked() {
        if (_selectedConnection == null || _connections.Count == 0) return;

        var result = MessageBox.Query("Delete Connection",
            $"Are you sure you want to delete the connection '{_selectedConnection.DisplayName}'?\n\n" +
            "This action cannot be undone.", "Delete", "Cancel");

        if (result == 0) {
            try {
                _connectionManager.RemoveConnection(_selectedConnection.Id);
                LoadConnections(); // Refresh the list
                _statusLabel.Text = "Connection deleted successfully";
            } catch (Exception ex) {
                MessageBox.ErrorQuery("Delete Error",
                    $"Error deleting connection: {ex.Message}", "OK");
            }
        }
    }

    private void OnTestClicked() {
        if (_selectedConnection == null) return;

        try {
            _statusLabel.Text = "Testing connection...";
            Application.Refresh();

            if (_connectionManager.TestConnection(_selectedConnection)) {
                MessageBox.Query("Connection Test",
                    $"Connection to '{_selectedConnection.DisplayName}' was successful!", "OK");
                _statusLabel.Text = "Connection test successful";
            } else {
                MessageBox.ErrorQuery("Connection Test Failed",
                    $"Could not connect to '{_selectedConnection.DisplayName}'.\n\n" +
                    "Please check your connection settings.", "OK");
                _statusLabel.Text = "Connection test failed";
            }
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Connection Test Error",
                $"Error testing connection: {ex.Message}", "OK");
            _statusLabel.Text = "Connection test error";
        }
    }

    private void OnCancelClicked() {
        ConnectionSelected = false;
        Application.RequestStop();
    }

    public override bool ProcessKey(KeyEvent keyEvent) {
        // Handle global shortcuts
        switch (keyEvent.Key) {
            case Key.q:
            case Key.Q:
            case Key.Esc:
                OnCancelClicked();
                return true;
        }

        return base.ProcessKey(keyEvent);
    }
}