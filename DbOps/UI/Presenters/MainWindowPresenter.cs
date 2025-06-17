using DbOps.UI.Views;
using DbOps.Domain.Models;
using DbOps.Domain.Services;
using DbOps.UI.Components;

namespace DbOps.UI.Presenters;

public class MainWindowPresenter : IDisposable {
    private readonly IMainView _view;
    private readonly ConnectionManager _connectionManager;
    private readonly SessionManager _sessionManager;
    private bool _disposed = false;

    // Display mode tracking
    private UserAction _currentDisplayMode = UserAction.ShowSessionDetails;
    private DatabaseSession? _selectedSession = null;

    // Connection state tracking
    private bool _isConnecting = false;

    public MainWindowPresenter(IMainView view) {
        _view = view;
        _connectionManager = new ConnectionManager();
        _sessionManager = new SessionManager();

        SetupEventHandlers();
    }

    private void SetupEventHandlers() {
        // View events
        _view.SessionSelected += OnSessionSelected;
        _view.ActionRequested += OnActionRequested;
        _view.ConnectionSelected += OnConnectionSelected;
        _view.ViewLoaded += OnViewLoaded;
        _view.ViewClosing += OnViewClosing;

        // Session manager events
        _sessionManager.SessionsUpdated += OnSessionsUpdated;
        _sessionManager.ConnectionStateChanged += OnConnectionStateChanged;

        // Connection manager events
        _connectionManager.ConnectionDeleted += OnConnectionDeleted;
    }

    public void InitializeSync() {
        // Initialize UI first
        _view.UpdateConnectionStatus("Disconnected - Press 'c' to select connection");
        UpdateStatusBarMode();

        TryConnectToDefault();
    }

    private void TryConnectToDefault() {
        if (_isConnecting) {
            return; // Already connecting, ignore this request
        }

        // Check if we have any connections configured
        if (!_connectionManager.HasConnections) {
            _view.UpdateConnectionStatus("No database connections configured. Press 'c' to add a connection.");
            return;
        }

        // Try to connect using the default connection
        var defaultConnection = _connectionManager.DefaultConnection;
        if (defaultConnection == null) {
            _view.UpdateConnectionStatus("No default connection set. Press 'c' to select a connection.");
            return;
        }

        _isConnecting = true;
        _view.UpdateConnectionStatus($"Attempting to connect to {defaultConnection.DisplayName}...");

        try {
            var postgresService = _connectionManager.CreatePostgresService(defaultConnection);
            var success = _sessionManager.ConnectWithService(
                postgresService,
                defaultConnection
            );

            if (success) {
                _view.UpdateConnectionStatus($"Connected to {defaultConnection.DisplayName}");
                StartDataRefresh();
            } else {
                _view.UpdateConnectionStatus("Default connection failed - Press 'c' to select connection");
            }
        } catch (Exception ex) {
            _view.UpdateConnectionStatus($"Connection failed: {ex.Message} - Press 'c' to select connection");
        } finally {
            _isConnecting = false;
        }
    }

    private void StartDataRefresh() {
        // This method should start the data refresh process
        // For now, we'll just trigger an initial refresh
        try {
            _sessionManager.Refresh();
        } catch (Exception ex) {
            _view.ShowError("Refresh Error", $"Failed to start data refresh: {ex.Message}");
        }
    }




    // Event Handlers
    private void OnSessionSelected(int selectedIndex) {
        try {
            // Check if we have sessions and the index is valid
            var sessions = _sessionManager.CurrentSessions;
            if (sessions == null || sessions.Count == 0) {
                _selectedSession = null;
                _view.UpdateSessionDetails(null);
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= sessions.Count) {
                _selectedSession = null;
                _view.UpdateSessionDetails(null);
                return;
            }

            var session = _sessionManager.GetSession(selectedIndex);
            _selectedSession = session;

            if (session != null) {
                UpdateDisplayForCurrentMode();
            }
        } catch (Exception ex) {
            HandleError("Session Selection Error", ex);
        }
    }

    private void OnActionRequested(UserAction action) {
        try {
            switch (action) {
                case UserAction.Refresh:
                    HandleRefreshRequest();
                    break;
                case UserAction.ShowConnections:
                    if (!_isConnecting) {
                        _view.ShowConnectionDialog(_connectionManager);
                    }
                    break;
                case UserAction.Quit:
                    HandleQuitRequest();
                    break;
                case UserAction.ShowSessionDetails:
                case UserAction.ShowWaitInfo:
                case UserAction.ShowLockingInfo:
                    HandleDisplayModeChange(action);
                    break;
            }
        } catch (Exception ex) {
            HandleError("User Action Error", ex);
        }
    }

    private void HandleRefreshRequest() {
        if (_isConnecting) {
            _view.UpdateConnectionStatus("Connection in progress - Please wait...");
            return;
        }

        if (!_sessionManager.IsConnected) {
            _view.UpdateConnectionStatus("No connection - Press 'c' to select connection");
            return;
        }

        try {
            _view.UpdateConnectionStatus("Refreshing...");
            var result = _sessionManager.Refresh();

            if (result) {
                _view.UpdateConnectionStatus($"Connected to {_sessionManager.CurrentConnection?.DisplayName} - Last refresh: {DateTime.Now:HH:mm:ss}");

                // Update the current display mode after refresh
                UpdateDisplayForCurrentMode();
            } else {
                _view.UpdateConnectionStatus("Refresh failed - Press 'c' to select connection");
            }
        } catch (Exception) {
            // Handle refresh errors gracefully - don't crash the app
            ClearCurrentSessionState();
            _sessionManager.Disconnect();
            _view.UpdateConnectionStatus("Connection lost - Press 'c' to reconnect");
        }
    }

    private void HandleDisplayModeChange(UserAction newMode) {
        try {
            // Always update the current mode, even if no session is selected
            _currentDisplayMode = newMode;

            // Set the display mode in the UI (this will update the DisplayModeManager)
            _view.SetDisplayMode(newMode);

            // Update status bar to show current mode
            UpdateStatusBarMode();

            // If we have a selected session, update the display
            if (_selectedSession != null) {
                UpdateDisplayForCurrentMode();
            } else {
                // Clear the session details but keep the mode change
                _view.UpdateSessionDetails(null);
            }
        } catch (Exception ex) {
            HandleError("Display Mode Error", ex);
        }
    }

    private void UpdateDisplayForCurrentMode() {
        if (_selectedSession == null) return;

        try {
            // Load locking information if in locking mode and we have a connection
            if (_currentDisplayMode == UserAction.ShowLockingInfo && _sessionManager.IsConnected) {
                try {
                    // Get the current service and load locking information
                    var service = GetCurrentPostgresService();
                    if (service != null) {
                        service.LoadLockingInformation(_selectedSession);
                    }
                } catch (Exception) {
                    // If loading fails, clear the locking info so the session shows an error message
                    _selectedSession.Locks = new List<DatabaseLock>();
                    _selectedSession.BlockingRelationships = new List<BlockingRelationship>();
                }
            }

            // Update the session details - the DisplayModeManager will format appropriately
            _view.UpdateSessionDetails(_selectedSession);
        } catch (Exception ex) {
            HandleError("Display Update Error", ex);
        }
    }

    private SyncPostgresService? GetCurrentPostgresService() {
        // Get the current connection from session manager
        var currentConnection = _sessionManager.CurrentConnection;
        if (currentConnection != null && _sessionManager.IsConnected) {
            try {
                return _connectionManager.CreatePostgresService(currentConnection);
            } catch (Exception) {
                // If we can't create the service, return null
                return null;
            }
        }
        return null;
    }

    private void UpdateStatusBarMode() {
        var modeText = _currentDisplayMode switch {
            UserAction.ShowSessionDetails => "Session Details",
            UserAction.ShowWaitInfo => "Wait Information",
            UserAction.ShowLockingInfo => "Locking Information",
            _ => "Unknown"
        };

        // Note: This would need to be implemented in the view interface
        // For now, we'll update the connection status to include mode
        var currentStatus = _sessionManager.IsConnected
            ? $"Connected to {_sessionManager.CurrentConnection?.DisplayName}"
            : "Disconnected";
        _view.UpdateConnectionStatus($"{currentStatus} | Mode: {modeText}");
    }


    private void HandleError(string title, Exception ex) {
        // Log the full exception details (in a real app, use proper logging)
        System.Diagnostics.Debug.WriteLine($"Error in {title}: {ex}");

        // Check if this is a connection-related error
        bool isConnectionError = ex is InvalidOperationException ||
                                ex.Message.Contains("connection") ||
                                ex.Message.Contains("database") ||
                                ex.Message.Contains("network") ||
                                ex.Message.Contains("timeout");

        if (isConnectionError) {
            _view.UpdateConnectionStatus("Connection error - Press 'c' to reconnect");

            // Disconnect the current session to prevent further errors
            if (_sessionManager.IsConnected) {
                _sessionManager.Disconnect();
            }
        } else {
            // For non-connection errors, just update status
            _view.UpdateConnectionStatus("Error occurred - Press 'c' for connection options");
        }
    }

    private void HandleQuitRequest() {
        CleanupSync();
        Terminal.Gui.Application.RequestStop();
    }

    private void OnViewLoaded() {
        InitializeSync();
    }

    private void OnViewClosing() {
        CleanupSync();
    }

    private void OnSessionsUpdated(List<DatabaseSession> sessions) {
        try {
            _view.UpdateSessions(sessions);
        } catch (Exception ex) {
            _view.ShowError("UI Update Error", ex.Message);
        }
    }


    private void OnConnectionStateChanged(ConnectionState state) {
        var statusMessage = state switch {
            ConnectionState.Disconnected => "Disconnected",
            ConnectionState.Connecting => "Connecting...",
            ConnectionState.Connected => $"Connected to {_sessionManager.CurrentConnection?.DisplayName}",
            ConnectionState.Failed => "Connection failed",
            _ => "Unknown state"
        };

        _view.UpdateConnectionStatus(statusMessage);
    }


    private void OnConnectionDeleted(string connectionId) {
        if (_sessionManager.CurrentConnection?.Id == connectionId) {
            // Clear UI state before disconnecting
            ClearCurrentSessionState();
            _sessionManager.Disconnect();

            _view.ShowMessage("Connection Deleted",
                "The active connection was deleted. Please select a new connection.");
            _view.ShowConnectionDialog(_connectionManager);
        }
    }

    private void OnConnectionSelected(DatabaseConnection? selectedConnection) {
        if (selectedConnection == null) {
            return;
        }

        try {
            // Clear current UI state when switching connections
            ClearCurrentSessionState();

            // Disconnect current connection if any
            if (_sessionManager.IsConnected) {
                _sessionManager.Disconnect();
            }

            _isConnecting = true;
            _view.UpdateConnectionStatus($"Connecting to {selectedConnection.DisplayName}...");

            // Create postgres service and connect
            var postgresService = _connectionManager.CreatePostgresService(selectedConnection);
            var success = _sessionManager.ConnectWithService(postgresService, selectedConnection);

            if (success) {
                _view.UpdateConnectionStatus($"Connected to {selectedConnection.DisplayName}");

                // Update the connection's last used timestamp
                _connectionManager.UpdateConnectionLastUsed(selectedConnection.Id);

                // Start data refresh
                StartDataRefresh();
            } else {
                _view.UpdateConnectionStatus("Connection failed - Press 'c' to select connection");
            }
        } catch (Exception ex) {
            _view.UpdateConnectionStatus($"Connection failed: {ex.Message} - Press 'c' to select connection");
        } finally {
            _isConnecting = false;
        }
    }

    private void ClearCurrentSessionState() {
        // Clear selected session
        _selectedSession = null;

        // Clear session details in the view
        _view.UpdateSessionDetails(null);

        // Clear the session list (will be repopulated after connection)
        _view.UpdateSessions(new List<DatabaseSession>());
    }

    public void CleanupSync() {
        try {
            _sessionManager?.Dispose();
        } catch (Exception ex) {
            _view.ShowError("Cleanup Error", ex.Message);
        }
    }


    public void Dispose() {
        if (!_disposed) {
            CleanupSync();
            _disposed = true;
        }
    }
}