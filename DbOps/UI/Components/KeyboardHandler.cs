using Terminal.Gui;

namespace DbOps.UI.Components;

public class KeyboardHandler {
    // Easy-to-modify key mappings - developers can change these easily
    // Example: To change quit from Q to Escape, replace the lines below with:
    // { Key.Escape, KeyAction.Quit },
    private readonly Dictionary<Key, UserAction> _keyMappings = new() {
        // Application control
        { Key.q, UserAction.Quit },
        { Key.Q, UserAction.Quit },
        
        // Connection management
        { Key.c, UserAction.ShowConnections },
        { Key.C, UserAction.ShowConnections },
        
        // Display modes
        { Key.w, UserAction.ShowWaitInfo },
        { Key.W, UserAction.ShowWaitInfo },
        { Key.s, UserAction.ShowSessionDetails },
        { Key.S, UserAction.ShowSessionDetails },
        { Key.l, UserAction.ShowLockingInfo },
        { Key.L, UserAction.ShowLockingInfo },
        
        // Actions
        { Key.Enter, UserAction.Refresh },
        { Key.F5, UserAction.Refresh }
    };

    // Events that the MainWindow can subscribe to
    public event Action? QuitRequested;
    public event Action? RefreshRequested;
    public event Action? ShowWaitInfoRequested;
    public event Action? ShowSessionDetailsRequested;
    public event Action? ShowLockingInfoRequested;
    public event Action? ShowConnectionsRequested;

    public bool HandleKeyPress(KeyEvent keyEvent) {
        if (_keyMappings.TryGetValue(keyEvent.Key, out var action)) {
            ExecuteAction(action);
            return true; // Key was handled
        }
        return false; // Key was not handled
    }

    private void ExecuteAction(UserAction action) {
        switch (action) {
            case UserAction.Quit:
                QuitRequested?.Invoke();
                break;
            case UserAction.Refresh:
                RefreshRequested?.Invoke();
                break;
            case UserAction.ShowWaitInfo:
                ShowWaitInfoRequested?.Invoke();
                break;
            case UserAction.ShowSessionDetails:
                ShowSessionDetailsRequested?.Invoke();
                break;
            case UserAction.ShowLockingInfo:
                ShowLockingInfoRequested?.Invoke();
                break;
            case UserAction.ShowConnections:
                ShowConnectionsRequested?.Invoke();
                break;
        }
    }

    // Helper method to get all mapped keys for a specific action (useful for help text)
    public IEnumerable<Key> GetKeysForAction(UserAction action) {
        return _keyMappings.Where(kvp => kvp.Value == action).Select(kvp => kvp.Key);
    }

    // Helper method to add or change key mappings programmatically
    public void SetKeyMapping(Key key, UserAction action) {
        _keyMappings[key] = action;
    }

    // Helper method to remove key mappings
    public void RemoveKeyMapping(Key key) {
        _keyMappings.Remove(key);
    }

    // Get display text for status bar - dynamically reflects current key mappings
    public string GetStatusBarKeyText(int terminalWidth) {
        var quitKeys = GetUniqueKeysForDisplay(UserAction.Quit);
        var refreshKeys = GetUniqueKeysForDisplay(UserAction.Refresh);
        var waitKeys = GetUniqueKeysForDisplay(UserAction.ShowWaitInfo);
        var sessionKeys = GetUniqueKeysForDisplay(UserAction.ShowSessionDetails);
        var lockKeys = GetUniqueKeysForDisplay(UserAction.ShowLockingInfo);
        var connectionKeys = GetUniqueKeysForDisplay(UserAction.ShowConnections);

        if (terminalWidth < 80) {
            // Very compact for narrow terminals
            return $"↑↓ Nav | {string.Join("/", refreshKeys)} Refresh | {string.Join("/", connectionKeys)} Conn | {string.Join("/", waitKeys)}/{string.Join("/", sessionKeys)}/{string.Join("/", lockKeys)} Mode | {string.Join("/", quitKeys)} Quit";
        } else if (terminalWidth < 120) {
            // Compact for medium terminals
            return $"[↑↓] Nav | [{string.Join("/", refreshKeys)}] Refresh | [{string.Join("/", connectionKeys)}] Connections | [{string.Join("/", waitKeys)}/{string.Join("/", sessionKeys)}/{string.Join("/", lockKeys)}] Mode | [{string.Join("/", quitKeys)}] Quit";
        } else {
            // Full text for wide terminals
            return $"[↑↓] Navigate | [{string.Join("/", refreshKeys)}] Refresh | [{string.Join("/", connectionKeys)}] Connections | [{string.Join("/", waitKeys)}] Wait | [{string.Join("/", sessionKeys)}] Session | [{string.Join("/", lockKeys)}] Lock | [{string.Join("/", quitKeys)}] Quit";
        }
    }

    // Get unique keys for display, avoiding duplicates like Q/Q
    private string[] GetUniqueKeysForDisplay(UserAction action) {
        var keys = GetKeysForAction(action).Select(FormatKeyForDisplay).ToHashSet();
        return keys.ToArray();
    }

    private static string FormatKeyForDisplay(Key key) {
        return key switch {
            Key.Enter => "Enter",
            Key.F5 => "F5",
            _ => key.ToString().ToUpper()
        };
    }
}

public enum UserAction {
    Quit,
    Refresh,
    ShowWaitInfo,
    ShowSessionDetails,
    ShowLockingInfo,
    ShowConnections
}