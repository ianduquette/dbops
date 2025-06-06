using DbOps.Models;
using DbOps.Services;
namespace DbOps.UI.Components;

public class DisplayModeManager {
    private readonly SyncPostgresService _postgresService;
    public enum DisplayMode { SessionDetails, WaitInformation, LockingInformation }

    private DisplayMode _currentMode = DisplayMode.SessionDetails;

    public DisplayMode CurrentMode => _currentMode;
    public event Action? ModeChanged;

    public DisplayModeManager(SyncPostgresService postgresService) {
        _postgresService = postgresService;
    }

    public void SetMode(DisplayMode mode) {
        if (_currentMode != mode) {
            _currentMode = mode;
            ModeChanged?.Invoke();
        }
    }

    public string GetDisplayContent(DatabaseSession session) {
        return _currentMode switch {
            DisplayMode.SessionDetails => session.GetSessionDetails(),
            DisplayMode.WaitInformation => session.GetWaitInformation(),
            DisplayMode.LockingInformation => GetLockingInformation(session),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string GetLockingInformation(DatabaseSession session) {
        try {
            _postgresService.LoadLockingInformation(session);
            return session.GetLockingInformation();
        } catch (Exception ex) {
            return $"❌ Failed to load locking information\n\n" +
                   $"Error: {ex.Message}\n\n" +
                   $"This could be due to:\n" +
                   $"• Insufficient database permissions\n" +
                   $"• Connection issues\n" +
                   $"• PostgreSQL version compatibility\n\n" +
                   $"Try switching to another view mode.";
        }
    }

    public string GetModeDisplayName() => _currentMode switch {
        DisplayMode.WaitInformation => "Wait",
        DisplayMode.LockingInformation => "Lock",
        _ => "Session"
    };

    public string GetQueryLabelText() => _currentMode switch {
        DisplayMode.WaitInformation => "Selected Wait Information:",
        DisplayMode.LockingInformation => "Selected Locking Information:",
        _ => "Selected Session Details:"
    };
}