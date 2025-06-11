using DbOps.Models;
using DbOps.Services;
using DbOps.UI.Components;

namespace DbOps.Interfaces;

public interface IMainView {
    // Events
    event Action<int>? SessionSelected;
    event Action<UserAction>? ActionRequested;
    event Action? ViewLoaded;
    event Action? ViewClosing;

    // UI Updates (simplified - just sync for now)
    void UpdateConnectionStatus(string status);
    void UpdateSessions(List<DatabaseSession> sessions);
    void UpdateSessionDetails(DatabaseSession? session);
    void SetDisplayMode(UserAction mode);
    void ShowError(string title, string message);
    void ShowMessage(string title, string message);
    void ShowConnectionDialog();

    // Lifecycle
    void Initialize();
    void Cleanup();
    bool IsInitialized { get; }
}