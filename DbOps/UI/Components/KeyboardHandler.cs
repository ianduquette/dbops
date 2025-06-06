using Terminal.Gui;

namespace DbOps.UI.Components;

public class KeyboardHandler {
    // Easy-to-modify key mappings - developers can change these easily
    // Example: To change quit from Q to Escape, replace the lines below with:
    // { Key.Escape, KeyAction.Quit },
    private readonly Dictionary<Key, KeyAction> _keyMappings = new() {
        // Application control
        { Key.q, KeyAction.Quit },
        { Key.Q, KeyAction.Quit },
        
        // Display modes
        { Key.w, KeyAction.ShowWaitInfo },
        { Key.W, KeyAction.ShowWaitInfo },
        { Key.s, KeyAction.ShowSessionDetails },
        { Key.S, KeyAction.ShowSessionDetails },
        { Key.l, KeyAction.ShowLockingInfo },
        { Key.L, KeyAction.ShowLockingInfo },
        
        // Actions
        { Key.Enter, KeyAction.Refresh },
        { Key.F5, KeyAction.Refresh }
    };

    // Events that the MainWindow can subscribe to
    public event Action? QuitRequested;
    public event Action? RefreshRequested;
    public event Action? ShowWaitInfoRequested;
    public event Action? ShowSessionDetailsRequested;
    public event Action? ShowLockingInfoRequested;

    public bool HandleKeyPress(KeyEvent keyEvent) {
        if (_keyMappings.TryGetValue(keyEvent.Key, out var action)) {
            ExecuteAction(action);
            return true; // Key was handled
        }
        return false; // Key was not handled
    }

    private void ExecuteAction(KeyAction action) {
        switch (action) {
            case KeyAction.Quit:
                QuitRequested?.Invoke();
                break;
            case KeyAction.Refresh:
                RefreshRequested?.Invoke();
                break;
            case KeyAction.ShowWaitInfo:
                ShowWaitInfoRequested?.Invoke();
                break;
            case KeyAction.ShowSessionDetails:
                ShowSessionDetailsRequested?.Invoke();
                break;
            case KeyAction.ShowLockingInfo:
                ShowLockingInfoRequested?.Invoke();
                break;
        }
    }

    // Helper method to get all mapped keys for a specific action (useful for help text)
    public IEnumerable<Key> GetKeysForAction(KeyAction action) {
        return _keyMappings.Where(kvp => kvp.Value == action).Select(kvp => kvp.Key);
    }

    // Helper method to add or change key mappings programmatically
    public void SetKeyMapping(Key key, KeyAction action) {
        _keyMappings[key] = action;
    }

    // Helper method to remove key mappings
    public void RemoveKeyMapping(Key key) {
        _keyMappings.Remove(key);
    }

    // Get display text for status bar - dynamically reflects current key mappings
    public string GetStatusBarKeyText(int terminalWidth) {
        var quitKeys = GetUniqueKeysForDisplay(KeyAction.Quit);
        var refreshKeys = GetUniqueKeysForDisplay(KeyAction.Refresh);
        var waitKeys = GetUniqueKeysForDisplay(KeyAction.ShowWaitInfo);
        var sessionKeys = GetUniqueKeysForDisplay(KeyAction.ShowSessionDetails);
        var lockKeys = GetUniqueKeysForDisplay(KeyAction.ShowLockingInfo);

        if (terminalWidth < 80) {
            // Very compact for narrow terminals
            return $"↑↓ Nav | {string.Join("/", refreshKeys)} Refresh | {string.Join("/", waitKeys)}/{string.Join("/", sessionKeys)}/{string.Join("/", lockKeys)} Mode | {string.Join("/", quitKeys)} Quit";
        } else if (terminalWidth < 100) {
            // Compact for medium terminals
            return $"[↑↓] Nav | [{string.Join("/", refreshKeys)}] Refresh | [{string.Join("/", waitKeys)}/{string.Join("/", sessionKeys)}/{string.Join("/", lockKeys)}] Mode | [{string.Join("/", quitKeys)}] Quit";
        } else {
            // Full text for wide terminals
            return $"[↑↓] Navigate | [{string.Join("/", refreshKeys)}] Refresh | [{string.Join("/", waitKeys)}] Wait | [{string.Join("/", sessionKeys)}] Session | [{string.Join("/", lockKeys)}] Lock | [{string.Join("/", quitKeys)}] Quit";
        }
    }

    // Get unique keys for display, avoiding duplicates like Q/Q
    private string[] GetUniqueKeysForDisplay(KeyAction action) {
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

public enum KeyAction {
    Quit,
    Refresh,
    ShowWaitInfo,
    ShowSessionDetails,
    ShowLockingInfo
}