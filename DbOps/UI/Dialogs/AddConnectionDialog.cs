using DbOps.Models;
using DbOps.Services;
using Terminal.Gui;

namespace DbOps.UI.Dialogs;

public class AddConnectionDialog : Dialog {
    private readonly ConnectionManager _connectionManager;
    private readonly DatabaseConnection? _editingConnection;
    private readonly TextField _nameField;
    private readonly TextField _hostField;
    private readonly TextField _portField;
    private readonly TextField _databaseField;
    private readonly TextField _usernameField;
    private readonly TextField _passwordField;
    private readonly CheckBox _defaultCheckBox;
    private readonly Button _testButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly Label _statusLabel;

    public bool ConnectionAdded { get; private set; }
    public DatabaseConnection? NewConnection { get; private set; }

    public AddConnectionDialog(ConnectionManager connectionManager, DatabaseConnection? editingConnection = null)
        : base(editingConnection == null ? "Add New Connection" : "Edit Connection") {

        _connectionManager = connectionManager;
        _editingConnection = editingConnection;

        // Set dialog size and position
        Width = 60;
        Height = 20;
        X = Pos.Center();
        Y = Pos.Center();

        // Create form fields
        var y = 1;

        Add(new Label("Connection Name:") { X = 1, Y = y });
        _nameField = new TextField("") { X = 18, Y = y, Width = 40 };
        Add(_nameField);
        y += 2;

        Add(new Label("Host:") { X = 1, Y = y });
        _hostField = new TextField("localhost") { X = 18, Y = y, Width = 40 };
        Add(_hostField);
        y++;

        Add(new Label("Port:") { X = 1, Y = y });
        _portField = new TextField("5432") { X = 18, Y = y, Width = 10 };
        Add(_portField);
        y++;

        Add(new Label("Database:") { X = 1, Y = y });
        _databaseField = new TextField("postgres") { X = 18, Y = y, Width = 40 };
        Add(_databaseField);
        y++;

        Add(new Label("Username:") { X = 1, Y = y });
        _usernameField = new TextField("") { X = 18, Y = y, Width = 40 };
        Add(_usernameField);
        y++;

        Add(new Label("Password:") { X = 1, Y = y });
        _passwordField = new TextField("") { X = 18, Y = y, Width = 40, Secret = true };
        Add(_passwordField);
        y += 2;

        _defaultCheckBox = new CheckBox("Set as _default connection") { X = 1, Y = y };
        Add(_defaultCheckBox);
        y += 2;

        // Create buttons - enabled by default
        _testButton = new Button("Test Connection") { X = 1, Y = y, Enabled = true };
        _saveButton = new Button("Save") { X = Pos.Right(_testButton) + 2, Y = y, IsDefault = true, Enabled = true };
        _cancelButton = new Button("Cancel") { X = Pos.Right(_saveButton) + 2, Y = y, Enabled = true };

        Add(_testButton);
        Add(_saveButton);
        Add(_cancelButton);
        y++;

        // Status label
        _statusLabel = new Label("Fill in the connection details and click Save") {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Height = 1
        };
        Add(_statusLabel);

        // Set up event handlers
        SetupEventHandlers();

        // Load existing connection data if editing
        if (_editingConnection != null) {
            LoadConnectionData();
        }

        // Set initial focus
        _nameField.SetFocus();
    }

    private void SetupEventHandlers() {
        _testButton.Clicked += OnTestClicked;
        _saveButton.Clicked += OnSaveClicked;
        _cancelButton.Clicked += OnCancelClicked;

        // Add key press handlers for real-time validation
        _nameField.KeyPress += OnFieldKeyPress;
        _hostField.KeyPress += OnFieldKeyPress;
        _portField.KeyPress += OnFieldKeyPress;
        _databaseField.KeyPress += OnFieldKeyPress;
        _usernameField.KeyPress += OnFieldKeyPress;
        _passwordField.KeyPress += OnFieldKeyPress;

        // Initial validation
        ValidateForm();
    }

    private void OnFieldKeyPress(KeyEventEventArgs e) {
        // Trigger validation immediately after key press
        ValidateForm();
    }

    private void LoadConnectionData() {
        if (_editingConnection == null) return;

        try {
            _nameField.Text = _editingConnection.Name;
            _hostField.Text = _editingConnection.Host;
            _portField.Text = _editingConnection.Port.ToString();
            _databaseField.Text = _editingConnection.Database;
            _usernameField.Text = _editingConnection.Username;
            _defaultCheckBox.Checked = _editingConnection.IsDefault;

            // Decrypt and load password
            var password = _connectionManager.GetDecryptedPassword(_editingConnection);
            _passwordField.Text = password;

            _statusLabel.Text = "Editing existing connection";
            ValidateForm();
        } catch (Exception ex) {
            _statusLabel.Text = $"Error loading connection data: {ex.Message}";
        }
    }

    private void ValidateForm() {
        try {
            // Simple validation - just check if basic fields have content
            var hasName = !string.IsNullOrWhiteSpace(_nameField.Text.ToString());
            var hasHost = !string.IsNullOrWhiteSpace(_hostField.Text.ToString());
            var hasDatabase = !string.IsNullOrWhiteSpace(_databaseField.Text.ToString());
            var hasUsername = !string.IsNullOrWhiteSpace(_usernameField.Text.ToString());

            if (hasName && hasHost && hasDatabase && hasUsername) {
                _statusLabel.Text = "Ready to save or test connection";
                _saveButton.Enabled = true;
                _testButton.Enabled = true;
            } else {
                var missing = new List<string>();
                if (!hasName) missing.Add("name");
                if (!hasHost) missing.Add("host");
                if (!hasDatabase) missing.Add("database");
                if (!hasUsername) missing.Add("username");

                _statusLabel.Text = $"Please fill in: {string.Join(", ", missing)}";
                _saveButton.Enabled = false;
                _testButton.Enabled = hasHost && hasDatabase && hasUsername; // Allow test without name
            }
        } catch (Exception ex) {
            _statusLabel.Text = $"Form error: {ex.Message}";
            _saveButton.Enabled = true; // Default to enabled
            _testButton.Enabled = true;
        }
    }

    private DatabaseConnection CreateConnectionFromForm() {
        var connection = new DatabaseConnection {
            Id = _editingConnection?.Id ?? DatabaseConnection.GenerateId(),
            Name = _nameField.Text.ToString() ?? "",
            Host = _hostField.Text.ToString() ?? "",
            Database = _databaseField.Text.ToString() ?? "",
            Username = _usernameField.Text.ToString() ?? "",
            IsDefault = _defaultCheckBox.Checked,
            CreatedAt = _editingConnection?.CreatedAt ?? DateTime.UtcNow,
            LastUsed = _editingConnection?.LastUsed ?? DateTime.UtcNow
        };

        // Parse port
        if (int.TryParse(_portField.Text.ToString(), out var port)) {
            connection.Port = port;
        } else {
            connection.Port = 5432; // Default PostgreSQL port
        }

        return connection;
    }

    private void OnTestClicked() {
        try {
            _statusLabel.Text = "Testing connection...";
            Application.Refresh();

            var connection = CreateConnectionFromForm();
            var password = _passwordField.Text.ToString() ?? "";

            if (_connectionManager.TestConnection(connection, password)) {
                MessageBox.Query("Connection Test", "Connection successful!", "OK");
                _statusLabel.Text = "Connection test successful";
            } else {
                MessageBox.ErrorQuery("Connection Test Failed",
                    "Could not connect to the database.\n\n" +
                    "Please check your connection settings and try again.", "OK");
                _statusLabel.Text = "Connection test failed";
            }
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Connection Test Error",
                $"Error testing connection: {ex.Message}", "OK");
            _statusLabel.Text = "Connection test error";
        }
    }

    private void OnSaveClicked() {
        try {
            var connection = CreateConnectionFromForm();
            var password = _passwordField.Text.ToString() ?? "";

            // Validate the connection
            var errors = connection.GetValidationErrors();
            if (errors.Count > 0) {
                MessageBox.ErrorQuery("Validation Error",
                    $"Please fix the following errors:\n\n{string.Join("\n", errors)}", "OK");
                return;
            }

            // Check for duplicates (unless editing the same connection)
            if (_editingConnection == null || _editingConnection.UniqueKey != connection.UniqueKey) {
                if (_connectionManager.Connections.Any(c => c.UniqueKey == connection.UniqueKey)) {
                    MessageBox.ErrorQuery("Duplicate Connection",
                        "A connection with the same host, port, database, and username already exists.", "OK");
                    return;
                }
            }

            // Test connection before saving
            _statusLabel.Text = "Testing connection before saving...";
            Application.Refresh();

            if (!_connectionManager.TestConnection(connection, password)) {
                var result = MessageBox.Query("Connection Test Failed",
                    "The connection test failed. Do you want to save it anyway?", "Save Anyway", "Cancel");
                if (result != 0) {
                    _statusLabel.Text = "Save cancelled";
                    return;
                }
            }

            // Save the connection
            if (_editingConnection != null) {
                // Remove the old connection and add the updated one
                _connectionManager.RemoveConnection(_editingConnection.Id);
            }

            _connectionManager.AddConnection(connection, password);

            NewConnection = connection;
            ConnectionAdded = true;
            _statusLabel.Text = "Connection saved successfully";

            Application.RequestStop();
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Save Error",
                $"Error saving connection: {ex.Message}", "OK");
            _statusLabel.Text = "Save error occurred";
        }
    }

    private void OnCancelClicked() {
        ConnectionAdded = false;
        Application.RequestStop();
    }

    public override bool ProcessKey(KeyEvent keyEvent) {
        switch (keyEvent.Key) {
            case Key.F5:
                OnTestClicked();
                return true;
            case Key.CtrlMask | Key.S:
                OnSaveClicked();
                return true;
        }

        return base.ProcessKey(keyEvent);
    }
}