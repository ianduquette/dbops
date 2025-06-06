using Terminal.Gui;

namespace DbOps.UI.Components;

public class StatusBarComponent {
    public Label StatusLabel { get; private set; }
    private readonly KeyboardHandler? _keyboardHandler;

    public StatusBarComponent(KeyboardHandler? keyboardHandler = null) {
        _keyboardHandler = keyboardHandler;
        StatusLabel = new Label("") {
            X = 1,
            Y = Pos.AnchorEnd(1)
        };
    }

    public void UpdateStatus(string modeText) {
        // Get terminal width and create responsive status text
        int terminalWidth = Application.Driver?.Cols ?? 120;

        string statusText;
        if (_keyboardHandler != null) {
            // Use dynamic key mappings from KeyboardHandler
            var keyText = _keyboardHandler.GetStatusBarKeyText(terminalWidth);
            statusText = $"{keyText} | Mode: {modeText}";
        } else {
            // Fallback to hardcoded keys if no KeyboardHandler provided
            if (terminalWidth < 80) {
                statusText = $"↑↓ Nav | Enter Refresh | W/S/L Mode | Q Quit | {modeText}";
            } else if (terminalWidth < 100) {
                statusText = $"[↑↓] Nav | [Enter] Refresh | [W/S/L] Mode | [Q] Quit | Mode: {modeText}";
            } else {
                statusText = $"[↑↓] Navigate | [Enter] Refresh | [W] Wait | [S] Session | [L] Lock | [Q] Quit | Mode: {modeText}";
            }
        }

        StatusLabel.Text = statusText;
    }

    public void RefreshForTerminalSize() {
        // This method can be called when terminal is resized
        // For now, we'll need the current mode text to be passed in
        // In a future refactoring, we could make this component observe the DisplayModeManager
    }

    public void SetLoadingStatus(string message) {
        StatusLabel.Text = message;
    }

    public void SetErrorStatus(string errorMessage) {
        StatusLabel.Text = $"❌ Error: {errorMessage}";
    }
}