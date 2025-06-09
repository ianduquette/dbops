using DbOps.Models;
using DbOps.Services;
using DbOps.UI.Components;
using DbOps.UI.Dialogs;
using Terminal.Gui;

namespace DbOps.UI;

public class MainWindow : Window {
    private readonly SessionListComponent _sessionListComponent;
    private readonly SessionDetailsComponent _sessionDetailsComponent;
    private readonly StatusBarComponent _statusBar;
    private readonly KeyboardHandler _keyboardHandler;
    private readonly DisplayModeManager _displayModeManager;
    private readonly DataRefreshService _dataRefreshService;
    private readonly Label _connectionLabel;
    private readonly ConnectionManager _connectionManager;

    private List<DatabaseSession> _sessions = new();
    private SyncPostgresService _postgresService;
    private DatabaseConnection? _currentConnection;

    public MainWindow(SyncPostgresService postgresService, ConnectionManager connectionManager, DatabaseConnection? currentConnection = null) : base("PostgreSQL Database Monitor") {
        _postgresService = postgresService;
        _connectionManager = connectionManager;
        _currentConnection = currentConnection;
        _displayModeManager = new DisplayModeManager(_postgresService);
        _keyboardHandler = new KeyboardHandler();
        _statusBar = new StatusBarComponent(_keyboardHandler);
        _dataRefreshService = new DataRefreshService(_postgresService, _statusBar);

        _sessionListComponent = new SessionListComponent();
        _sessionDetailsComponent = new SessionDetailsComponent(_displayModeManager, _sessionListComponent.ListView);

        _connectionLabel = new Label(GetConnectionDisplayText()) {
            X = 1,
            Y = 1,
            ColorScheme = new ColorScheme {
                Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
                Focus = new Terminal.Gui.Attribute(Color.White, Color.Black)
            }
        };

        InitializeLayout();
        SetupEventHandlers();
    }

    private void InitializeLayout() {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Add connection label
        Add(_connectionLabel);

        // Add session list components
        foreach (var view in _sessionListComponent.GetViews()) {
            Add(view);
        }

        // Add session details components
        foreach (var view in _sessionDetailsComponent.GetViews()) {
            Add(view);
        }

        // Add status bar
        Add(_statusBar.StatusLabel);

        // Initialize scrollbars after components are added to parent
        _sessionListComponent.InitializeScrollBar();
        _sessionDetailsComponent.InitializeScrollBars();

        // Add scrollbars
        foreach (var view in _sessionListComponent.GetScrollBarViews()) {
            Add(view);
        }

        foreach (var view in _sessionDetailsComponent.GetScrollBarViews()) {
            Add(view);
        }
    }

    private void SetupEventHandlers() {
        // Session selection events
        _sessionListComponent.SessionSelected += OnSessionSelected;

        // Data refresh events
        _dataRefreshService.SessionsRefreshed += OnSessionsRefreshed;
        _dataRefreshService.ErrorOccurred += OnDataRefreshError;

        // Display mode events
        _displayModeManager.ModeChanged += OnModeChanged;

        // Connection management events
        _connectionManager.ConnectionDeleted += OnConnectionDeleted;

        // Keyboard events
        _keyboardHandler.QuitRequested += PromptToQuit;
        _keyboardHandler.RefreshRequested += HandleRefreshRequest;
        _keyboardHandler.ShowWaitInfoRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.WaitInformation);
        _keyboardHandler.ShowSessionDetailsRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.SessionDetails);
        _keyboardHandler.ShowLockingInfoRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.LockingInformation);
        _keyboardHandler.ShowConnectionsRequested += ShowConnectionSelectionDialog;

        // Terminal resize events
        Application.Resized += OnTerminalResized;

        // Add key event handlers to text views
        _sessionDetailsComponent.QueryTextView.KeyPress += OnTextViewKeyPress;
        _sessionDetailsComponent.CurrentQueryTextView.KeyPress += OnTextViewKeyPress;
    }

    private void OnSessionSelected(int selectedIndex) {
        if (selectedIndex >= 0 && selectedIndex < _sessions.Count) {
            var selectedSession = _sessions[selectedIndex];
            _sessionDetailsComponent.UpdateSession(selectedSession);
        } else {
            _sessionDetailsComponent.UpdateSession(null);
        }
    }

    private void OnSessionsRefreshed(List<DatabaseSession> sessions) {
        _sessions = sessions;
        _sessionListComponent.UpdateSessions(sessions);
        UpdateStatusLabel();

        // Update session details if a session is selected
        var selectedSession = _sessionListComponent.SelectedSession;
        _sessionDetailsComponent.UpdateSession(selectedSession);
    }

    private void OnDataRefreshError(string errorMessage) {
        _sessionDetailsComponent.UpdateSession(null);
        _sessionDetailsComponent.QueryTextView.Text = errorMessage;
        _sessionDetailsComponent.CurrentQueryTextView.Text = "Error occurred - no query data available";
    }

    private void OnModeChanged() {
        var selectedSession = _sessionListComponent.SelectedSession;
        _sessionDetailsComponent.UpdateSession(selectedSession);
        UpdateStatusLabel();
    }

    private void OnTerminalResized(Application.ResizedEventArgs args) {
        _sessionListComponent.OnTerminalResized();
        UpdateStatusLabel();
    }

    private void OnConnectionDeleted(string deletedConnectionId) {
        // Check if the deleted connection is the currently active one
        if (_currentConnection != null && _currentConnection.Id == deletedConnectionId) {
            HandleActiveConnectionDeletion();
        }
    }

    private void HandleActiveConnectionDeletion() {
        try {
            // Get available connections sorted by usage
            var availableConnections = _connectionManager.GetConnectionsSortedByUsage();

            if (availableConnections.Any()) {
                // Try to auto-switch to each available connection until one works
                bool switchSuccessful = false;
                Exception? lastException = null;

                foreach (var connection in availableConnections) {
                    try {
                        SwitchConnection(connection, isAutoSwitch: true);
                        switchSuccessful = true;
                        break; // Successfully switched, exit loop
                    } catch (Exception ex) {
                        lastException = ex;
                        // Continue to try next connection
                    }
                }

                if (!switchSuccessful) {
                    // All connections failed
                    MessageBox.ErrorQuery("Auto-Switch Failed",
                        $"Failed to connect to any available connections.\n\n" +
                        $"Last error: {lastException?.Message}\n\n" +
                        "Please check your connections and try manually.", "OK");
                    HandleNoConnectionsAvailable();
                }
            } else {
                // No connections left - enter disconnected state
                HandleNoConnectionsAvailable();
            }
        } catch (Exception ex) {
            // Unexpected error in the auto-switch process
            MessageBox.ErrorQuery("Auto-Switch Error",
                $"Unexpected error during auto-switch: {ex.Message}\n\n" +
                "Please select a connection manually.", "OK");
            HandleNoConnectionsAvailable();
        }
    }

    private void HandleNoConnectionsAvailable() {
        try {
            // Clear session data
            _sessions.Clear();
            _sessionListComponent.UpdateSessions(_sessions);
            _sessionDetailsComponent.UpdateSession(null);

            // Update connection label to show disconnected state
            _currentConnection = null;
            _connectionLabel.Text = "No connections configured";
            _connectionLabel.SetNeedsDisplay();

            // Update status
            UpdateStatusLabel();
            SetNeedsDisplay();

            // Show message and auto-open connection manager
            MessageBox.Query("No Connections",
                "Last connection deleted. Please add a new connection to continue.", "OK");

            // Auto-open connection manager
            ShowConnectionSelectionDialog();
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Error",
                $"Error handling disconnected state: {ex.Message}", "OK");
        }
    }

    private void UpdateStatusLabel() {
        var modeText = _displayModeManager.GetModeDisplayName();
        _statusBar.UpdateStatus(modeText);
    }

    public override bool ProcessKey(KeyEvent keyEvent) {
        if (_keyboardHandler.HandleKeyPress(keyEvent)) {
            return true;
        }
        return base.ProcessKey(keyEvent);
    }

    private void OnTextViewKeyPress(KeyEventEventArgs keyEvent) {
        if (_keyboardHandler.HandleKeyPress(keyEvent.KeyEvent)) {
            keyEvent.Handled = true;
        }
    }

    private void PromptToQuit() {
        var result = MessageBox.Query("Quit Application",
            "Are you sure you want to quit the PostgreSQL Database Monitor?",
            "Yes", "No");

        if (result == 0) {
            Application.RequestStop();
        }
    }

    private void HandleRefreshRequest() {
        // Only allow refresh if there's an active connection
        if (_currentConnection != null) {
            _dataRefreshService.RefreshSessions();
        } else {
            // Show message that no connection is available
            MessageBox.Query("No Connection",
                "Cannot refresh sessions - no active database connection.\n\n" +
                "Please select a connection first.", "OK");
        }
    }

    public void Initialize() {
        _dataRefreshService.RefreshSessions();
        _sessionListComponent.SetFocus();
    }

    private string GetConnectionDisplayText() {
        if (_currentConnection != null) {
            return $"Connected to: {_currentConnection.DisplayName} ({_currentConnection.ConnectionSummary})";
        }
        return $"Connected to: {_dataRefreshService.GetConnectionInfo()}";
    }

    private void ShowConnectionSelectionDialog() {
        try {
            var connectionDialog = new ConnectionSelectionDialog(_connectionManager);
            Application.Run(connectionDialog);

            // Only switch connection if user actually selected one (not cancelled)
            if (connectionDialog.ConnectionSelected && connectionDialog.SelectedConnection != null) {
                // Switch to the selected connection
                SwitchConnection(connectionDialog.SelectedConnection);
            }
            // If cancelled, do nothing - just return to main window
        } catch (Exception ex) {
            MessageBox.ErrorQuery("Connection Error",
                $"Error showing connection dialog: {ex.Message}", "OK");
        }
    }

    private void SwitchConnection(DatabaseConnection newConnection, bool isAutoSwitch = false) {
        try {
            // Create new PostgreSQL service for the selected connection
            var newPostgresService = _connectionManager.CreatePostgresService(newConnection);

            // Test the connection
            if (!newPostgresService.TestConnection()) {
                if (isAutoSwitch) {
                    // For auto-switch, throw exception to try next connection
                    throw new InvalidOperationException($"Connection test failed for {newConnection.DisplayName}");
                } else {
                    MessageBox.ErrorQuery("Connection Failed",
                        $"Could not connect to {newConnection.DisplayName}.\n\n" +
                        "The connection may be unavailable or the credentials may have changed.", "OK");
                    return;
                }
            }

            // Update the current connection and service
            _currentConnection = newConnection;
            _postgresService = newPostgresService;

            // Update connection manager usage tracking
            _connectionManager.UpdateConnectionLastUsed(newConnection.Id);

            // Update the display
            _connectionLabel.Text = GetConnectionDisplayText();
            _connectionLabel.SetNeedsDisplay();

            // Update all services with the new PostgreSQL service
            _displayModeManager.UpdatePostgresService(_postgresService);
            _dataRefreshService.UpdatePostgresService(_postgresService);

            // Clear current session details since we're switching databases
            _sessionDetailsComponent.UpdateSession(null);

            // Refresh sessions with new connection
            _dataRefreshService.RefreshSessions();

            // Update status
            UpdateStatusLabel();

            // Force refresh of the main window
            SetNeedsDisplay();

            if (isAutoSwitch) {
                // Show brief notification for auto-switch
                ShowBriefNotification($"Active connection deleted. Switched to {newConnection.DisplayName}");
            } else {
                // Show full dialog for manual switch
                MessageBox.Query("Connection Switched",
                    $"Successfully switched to {newConnection.DisplayName}!\n\n" +
                    "The application is now connected to the new database.", "OK");
            }
        } catch (Exception ex) {
            if (isAutoSwitch) {
                // Re-throw for auto-switch to try next connection
                throw;
            } else {
                MessageBox.ErrorQuery("Connection Switch Error",
                    $"Error switching connection: {ex.Message}", "OK");
            }
        }
    }

    private void ShowBriefNotification(string message) {
        // For now, use a simple message box that auto-dismisses quickly
        // In a more advanced implementation, this could be a toast notification
        var result = MessageBox.Query("Connection Auto-Switched", message, "OK");
    }

    // Public method to update connection from external sources
    public void UpdateConnection(DatabaseConnection connection, SyncPostgresService postgresService) {
        _currentConnection = connection;
        _postgresService = postgresService;
        _connectionLabel.Text = GetConnectionDisplayText();

        // Update all services with the new PostgreSQL service
        _displayModeManager.UpdatePostgresService(_postgresService);
        _dataRefreshService.UpdatePostgresService(_postgresService);

        // Clear current session details since we're switching databases
        _sessionDetailsComponent.UpdateSession(null);

        // Refresh data with new connection
        _dataRefreshService.RefreshSessions();
        UpdateStatusLabel();
    }
}
