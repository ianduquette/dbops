using DbOps.Domain.Models;

namespace DbOps.Domain.Services;

public class SessionManager : IDisposable {
    private SyncPostgresService? _service;
    private DatabaseConnection? _currentConnection;
    private List<DatabaseSession> _sessions = new();
    private readonly object _sessionsLock = new object();
    private bool _disposed = false;

    public event Action<List<DatabaseSession>>? SessionsUpdated;
    public event Action<string>? ErrorOccurred;
    public event Action<ConnectionState>? ConnectionStateChanged;

    public SessionManager() {
    }

    public List<DatabaseSession> CurrentSessions {
        get {
            lock (_sessionsLock) {
                return new List<DatabaseSession>(_sessions);
            }
        }
    }
    public bool IsConnected => _service != null && _currentConnection != null;
    public DatabaseConnection? CurrentConnection => _currentConnection;


    public bool ConnectWithService(SyncPostgresService service, DatabaseConnection connection) {
        try {
            ConnectionStateChanged?.Invoke(ConnectionState.Connecting);

            var isConnected = service.TestConnection();
            if (!isConnected) {
                ConnectionStateChanged?.Invoke(ConnectionState.Failed);
                // Don't invoke ErrorOccurred - let the presenter handle status updates
                return false;
            }

            _service = service;
            _currentConnection = connection;

            ConnectionStateChanged?.Invoke(ConnectionState.Connected);

            var refreshResult = Refresh();

            return true;
        } catch (Exception) {
            ConnectionStateChanged?.Invoke(ConnectionState.Failed);
            // Don't invoke ErrorOccurred - let the presenter handle status updates
            return false;
        }
    }

    // Overload for backward compatibility with display name only
    public bool ConnectWithService(SyncPostgresService service, string displayName) {
        // This method should not be used anymore, but keeping for compatibility
        // Create a temporary connection for display purposes
        var tempConnection = new DatabaseConnection {
            Id = "temp-connection",
            Name = displayName,
            Host = "unknown",
            Port = 0,
            Database = "unknown",
            Username = "unknown"
        };

        return ConnectWithService(service, tempConnection);
    }


    public bool Refresh() {
        if (_service == null || _currentConnection == null) {
            ErrorOccurred?.Invoke("No active connection");
            return false;
        }

        try {
            var sessions = _service.GetActiveSessions(); // Synchronous call
            var newSessions = sessions ?? new List<DatabaseSession>();

            lock (_sessionsLock) {
                _sessions = newSessions;
            }

            SessionsUpdated?.Invoke(CurrentSessions);
            return true;
        } catch (Exception ex) {
            // Handle connection failures gracefully
            ErrorOccurred?.Invoke($"Failed to refresh sessions: {ex.Message}");

            // If it's a connection error, mark as disconnected
            if (ex.Message.Contains("connection") || ex.Message.Contains("network") ||
                ex.Message.Contains("timeout") || ex.Message.Contains("database")) {

                // Clear current connection state
                ConnectionStateChanged?.Invoke(ConnectionState.Failed);

                // Clear sessions but don't dispose the service yet - let the presenter handle it
                lock (_sessionsLock) {
                    _sessions.Clear();
                }
                SessionsUpdated?.Invoke(CurrentSessions);
            }

            return false;
        }
    }


    public DatabaseSession? GetSession(int index) {
        lock (_sessionsLock) {
            return index >= 0 && index < _sessions.Count ? _sessions[index] : null;
        }
    }


    public void Disconnect() {
        _service = null;
        _currentConnection = null;

        lock (_sessionsLock) {
            _sessions.Clear();
        }

        ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
        SessionsUpdated?.Invoke(CurrentSessions);
    }

    public void Dispose() {
        if (!_disposed) {
            Disconnect();
            _disposed = true;
        }
    }
}
