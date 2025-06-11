using DbOps.Domain.Models;
namespace DbOps.Domain.Logic;

public class DisplayModeManager {
    public enum DisplayMode { SessionDetails, WaitInformation, LockingInformation }

    private DisplayMode _currentMode = DisplayMode.SessionDetails;

    public DisplayMode CurrentMode => _currentMode;
    public event Action? ModeChanged;

    public DisplayModeManager() { }

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
            DisplayMode.LockingInformation => session.GetLockingInformation(),
            _ => throw new ArgumentOutOfRangeException()
        };
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