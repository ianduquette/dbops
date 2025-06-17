using DbOps.UI.Views;
using DbOps.Domain.Models;
using DbOps.Domain.Services;
using DbOps.UI.Components;

namespace DbOps.Testing;

public class MockMainView : IMainView {
    // Events
    public event Action<int>? SessionSelected;
    public event Action<UserAction>? ActionRequested;
    public event Action<DatabaseConnection?>? ConnectionSelected;
    public event Action? ViewLoaded;
    public event Action? ViewClosing;

    // Captured data for testing
    public List<string> StatusUpdates { get; } = new();
    public List<List<DatabaseSession>> SessionUpdates { get; } = new();
    public List<DatabaseSession?> SessionDetailUpdates { get; } = new();
    public List<(string title, string message)> ErrorMessages { get; } = new();
    public List<(string title, string message)> InfoMessages { get; } = new();
    public int ConnectionDialogShownCount { get; private set; } = 0;

    // State
    public bool IsInitialized { get; private set; } = false;

    // IMainView Implementation
    public void UpdateConnectionStatus(string status) {
        StatusUpdates.Add(status);
    }

    public void UpdateSessions(List<DatabaseSession> sessions) {
        SessionUpdates.Add(new List<DatabaseSession>(sessions));
    }

    public void UpdateSessionDetails(DatabaseSession? session) {
        SessionDetailUpdates.Add(session);
    }

    public void SetDisplayMode(UserAction mode) {
        // Mock implementation - just store the mode for testing
    }

    public void ShowError(string title, string message) {
        ErrorMessages.Add((title, message));
    }

    public void ShowMessage(string title, string message) {
        InfoMessages.Add((title, message));
    }

    public void ShowConnectionDialog(ConnectionManager connectionManager) {
        ConnectionDialogShownCount++;
    }

    // Lifecycle
    public void Initialize() {
        IsInitialized = true;
        ViewLoaded?.Invoke();
    }

    public void Cleanup() {
        ViewClosing?.Invoke();
    }

    // Test helper methods to trigger events
    public void TriggerSessionSelected(int index) => SessionSelected?.Invoke(index);
    public void TriggerActionRequested(UserAction action) => ActionRequested?.Invoke(action);
    public void TriggerConnectionSelected(DatabaseConnection? connection) => ConnectionSelected?.Invoke(connection);
    public void TriggerViewLoaded() => ViewLoaded?.Invoke();
    public void TriggerViewClosing() => ViewClosing?.Invoke();

    // Test helper methods for assertions
    public string? GetLastStatusUpdate() => StatusUpdates.LastOrDefault();
    public List<DatabaseSession>? GetLastSessionUpdate() => SessionUpdates.LastOrDefault();
    public DatabaseSession? GetLastSessionDetailUpdate() => SessionDetailUpdates.LastOrDefault();
    public (string title, string message)? GetLastErrorMessage() => ErrorMessages.LastOrDefault();
    public (string title, string message)? GetLastInfoMessage() => InfoMessages.LastOrDefault();

    public void ClearCapturedData() {
        StatusUpdates.Clear();
        SessionUpdates.Clear();
        SessionDetailUpdates.Clear();
        ErrorMessages.Clear();
        InfoMessages.Clear();
        ConnectionDialogShownCount = 0;
    }
}