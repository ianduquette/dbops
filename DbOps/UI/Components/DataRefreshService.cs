using DbOps.Models;
using DbOps.Services;
using Terminal.Gui;

namespace DbOps.UI.Components;

public class DataRefreshService {
    private SyncPostgresService _postgresService;
    private readonly StatusBarComponent _statusBar;

    public event Action<List<DatabaseSession>>? SessionsRefreshed;
    public event Action<string>? ErrorOccurred;

    public DataRefreshService(SyncPostgresService postgresService, StatusBarComponent statusBar) {
        _postgresService = postgresService;
        _statusBar = statusBar;
    }

    public void UpdatePostgresService(SyncPostgresService newPostgresService) {
        _postgresService = newPostgresService;
    }

    public void RefreshSessions() {
        try {
            _statusBar.SetLoadingStatus("Refreshing...");
            Application.Refresh();

            var sessions = _postgresService.GetActiveSessions();
            SessionsRefreshed?.Invoke(sessions);
        } catch (Exception ex) {
            _statusBar.SetErrorStatus(ex.Message);
            var errorMessage = $"❌ Failed to refresh sessions\n\n" +
                              $"Error Details:\n{ex.Message}\n\n" +
                              $"Possible causes:\n" +
                              $"• Database connection lost\n" +
                              $"• Network connectivity issues\n" +
                              $"• PostgreSQL server stopped\n" +
                              $"• Insufficient permissions\n\n" +
                              $"Press [Enter] to retry connection";

            ErrorOccurred?.Invoke(errorMessage);
        }

        Application.Refresh();
    }

    public string GetConnectionInfo() {
        return _postgresService.GetConnectionInfo();
    }
}