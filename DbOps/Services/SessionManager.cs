using DbOps.Models;

namespace DbOps.Services;

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


    public bool ConnectWithService(SyncPostgresService service, string displayName) {
        try {
            ConnectionStateChanged?.Invoke(ConnectionState.Connecting);

            // Test connection first - synchronous
            var isConnected = service.TestConnection();
            if (!isConnected) {
                ConnectionStateChanged?.Invoke(ConnectionState.Failed);
                // Don't invoke ErrorOccurred - let the presenter handle status updates
                return false;
            }

            _service = service;

            // Create a minimal DatabaseConnection for display purposes
            _currentConnection = new DatabaseConnection {
                Id = "hardcoded-connection",
                Name = displayName,
                Host = "127.0.0.1",
                Port = 5433,
                Database = "postgres",
                Username = "postgres",
                IsDefault = true
            };

            ConnectionStateChanged?.Invoke(ConnectionState.Connected);

            var refreshResult = Refresh();

            return true;
        } catch (Exception) {
            ConnectionStateChanged?.Invoke(ConnectionState.Failed);
            // Don't invoke ErrorOccurred - let the presenter handle status updates
            return false;
        }
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
