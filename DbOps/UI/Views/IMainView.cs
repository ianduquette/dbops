using DbOps.Domain.Models;
using DbOps.Domain.Services;
using DbOps.UI.Components;

namespace DbOps.UI.Views;

public interface IMainView {
    // Events
    event Action<int>? SessionSelected;
    event Action<UserAction>? ActionRequested;
    event Action? ViewLoaded;
    event Action? ViewClosing;

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