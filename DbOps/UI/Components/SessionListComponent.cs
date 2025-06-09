using DbOps.Models;
using Terminal.Gui;

namespace DbOps.UI.Components;

public class SessionListComponent {
    private readonly ListView _sessionListView;
    private ScrollBarView _sessionListScrollBar = null!;
    private readonly Label _sessionCountLabel;
    private readonly Label _sessionHeaderLabel;
    private List<DatabaseSession> _sessions = new();

    public event Action<int>? SessionSelected;
    public ListView ListView => _sessionListView;
    public int SelectedIndex => _sessionListView.SelectedItem;
    public DatabaseSession? SelectedSession =>
        SelectedIndex >= 0 && SelectedIndex < _sessions.Count ? _sessions[SelectedIndex] : null;

    public SessionListComponent() {
        _sessionCountLabel = new Label("Active Sessions (0):") {
            X = 1,
            Y = 3
        };

        _sessionHeaderLabel = new Label(GenerateHeader()) {
            X = 1,
            Y = 4,
            ColorScheme = new ColorScheme {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray)
            }
        };

        _sessionListView = new ListView() {
            X = 1,
            Y = 5,
            Width = Dim.Fill() - 1,
            Height = Dim.Percent(25),
            CanFocus = true,
            TabStop = true
        };

        _sessionListView.SetSource(new List<string>());
        SetupColorScheme();
        SetupEventHandlers();
    }

    private void SetupColorScheme() {
        _sessionListView.ColorScheme = new ColorScheme {
            Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow),
            HotNormal = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow),
            HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow)
        };
    }

    private void SetupScrollBar() {
        _sessionListScrollBar = new ScrollBarView(_sessionListView, true) {
            X = Pos.Right(_sessionListView),
            Y = Pos.Top(_sessionListView),
            Height = Dim.Height(_sessionListView)
        };

        _sessionListScrollBar.ChangedPosition += () => {
            _sessionListView.TopItem = _sessionListScrollBar.Position;
            if (_sessionListView.TopItem != _sessionListScrollBar.Position) {
                _sessionListScrollBar.Position = _sessionListView.TopItem;
            }
            _sessionListView.SetNeedsDisplay();
        };

        _sessionListView.DrawContent += (e) => {
            if (_sessionListView.Source != null) {
                _sessionListScrollBar.Size = _sessionListView.Source.Count;
                _sessionListScrollBar.Position = _sessionListView.TopItem;
                _sessionListScrollBar.Refresh();
            }
        };
    }

    private void SetupEventHandlers() {
        _sessionListView.SelectedItemChanged += args => {
            if (args.Item >= 0 && args.Item < _sessions.Count) {
                SessionSelected?.Invoke(args.Item);
            }
        };
    }

    public void UpdateSessions(List<DatabaseSession> sessions) {
        var previousSelectedPid = SelectedSession?.Pid;
        var previousTopItem = _sessionListView.TopItem;

        _sessions = sessions;
        RefreshDisplay();
        _sessionCountLabel.Text = $"Active Sessions ({_sessions.Count}):";

        RestoreSelection(previousSelectedPid, previousTopItem);

        // Force refresh of all components
        _sessionCountLabel.SetNeedsDisplay();
        _sessionHeaderLabel.SetNeedsDisplay();
        _sessionListView.SetNeedsDisplay();
    }

    private void RefreshDisplay() {
        int terminalWidth = Application.Driver?.Cols ?? 120;

        if (_sessions.Count == 0) {
            _sessionListView.SetSource(new List<string>());
        } else {
            var sessionTexts = _sessions.Select(s => s.GetDisplayText(terminalWidth)).ToList();
            _sessionListView.SetSource(sessionTexts);
        }
    }

    private void RestoreSelection(int? previousSelectedPid, int previousTopItem) {
        int newSelectedIndex = 0;

        if (previousSelectedPid.HasValue && _sessions.Count > 0) {
            var matchingSessionIndex = _sessions.FindIndex(s => s.Pid == previousSelectedPid.Value);
            if (matchingSessionIndex >= 0) {
                newSelectedIndex = matchingSessionIndex;
            }
        }

        if (_sessions.Count > 0) {
            _sessionListView.SelectedItem = newSelectedIndex;

            if (previousTopItem < _sessions.Count) {
                _sessionListView.TopItem = previousTopItem;
            } else if (_sessions.Count > 0) {
                _sessionListView.TopItem = Math.Max(0, newSelectedIndex - 5);
            }
        }
    }

    public void OnTerminalResized() {
        _sessionHeaderLabel.Text = GenerateHeader();
        RefreshDisplay();
    }

    private string GenerateHeader() {
        int terminalWidth = Application.Driver?.Cols ?? 120;
        var widths = ColumnWidths.CalculateWidths(terminalWidth);

        if (widths == null)
            return "Terminal Too Small";

        var database = TruncateAndPad("Database", widths.Database);
        var program = TruncateAndPad("Program", widths.Application);
        var machine = TruncateAndPad("Machine", widths.Machine);
        var status = TruncateAndPad("Status", widths.Status);
        var type = TruncateAndPad("Type", widths.Type);

        return $"[{database}] {program} | {machine} | PID: {"PID",-6} | {status} | {type}";
    }

    private string TruncateAndPad(string text, int width) {
        if (text.Length > width)
            text = text[..Math.Min(width, text.Length)];
        return text.PadRight(width);
    }

    public void SetFocus() {
        _sessionListView.SetFocus();
    }
    public void InitializeScrollBar() {
        SetupScrollBar();
    }

    public IEnumerable<View> GetViews() {
        yield return _sessionCountLabel;
        yield return _sessionHeaderLabel;
        yield return _sessionListView;
    }

    public IEnumerable<View> GetScrollBarViews() {
        if (_sessionListScrollBar != null)
            yield return _sessionListScrollBar;
    }
}