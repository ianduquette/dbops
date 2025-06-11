namespace DbOps.Domain.Models;

public enum ConnectionState {
    Disconnected,
    Connecting,
    Connected,
    Failed
}

public class ConnectionStateChangedEventArgs : EventArgs {
    public ConnectionState OldState { get; }
    public ConnectionState NewState { get; }
    public DatabaseConnection? Connection { get; }
    public string? ErrorMessage { get; }
    public DateTime Timestamp { get; }

    public ConnectionStateChangedEventArgs(
        ConnectionState oldState,
        ConnectionState newState,
        DatabaseConnection? connection = null,
        string? errorMessage = null) {
        OldState = oldState;
        NewState = newState;
        Connection = connection;
        ErrorMessage = errorMessage;
        Timestamp = DateTime.UtcNow;
    }
}