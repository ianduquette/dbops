using DbOps.Models;
using DbOps.Services;
using DbOps.UI.Components;
using Terminal.Gui;

namespace DbOps.UI;

public class MainWindow : Window {
    private readonly SyncPostgresService _postgresService;
    private readonly DisplayModeManager _displayModeManager;
    private readonly KeyboardHandler _keyboardHandler;
    private ListView _sessionListView = null!;
    private ScrollBarView _sessionListScrollBar = null!;

    private Label _connectionLabel = null!;
    private Label _sessionCountLabel = null!;
    private Label _sessionHeaderLabel = null!;
    private Label _queryLabel = null!;
    private TextView _queryTextView = null!;
    private ScrollBarView _queryTextScrollBar = null!;
    private TextView _currentQueryTextView = null!;
    private ScrollBarView _currentQueryScrollBar = null!;
    private StatusBarComponent _statusBar = null!;
    private List<DatabaseSession> _sessions = new();

    public MainWindow(SyncPostgresService postgresService) : base("PostgreSQL Database Monitor") {
        _postgresService = postgresService;
        _displayModeManager = new DisplayModeManager(postgresService);
        _keyboardHandler = new KeyboardHandler();

        InitializeComponents();
        SetupLayout();
        SetupEventHandlers();
    }

    private void InitializeComponents() {
        // Connection info label
        _connectionLabel = new Label($"Connected to: {_postgresService.GetConnectionInfo()}") {
            X = 1,
            Y = 1
        };

        // Session count label
        _sessionCountLabel = new Label("Active Sessions (0):") {
            X = 1,
            Y = 3
        };

        // Column headers for session list (dynamic sizing)
        _sessionHeaderLabel = new Label(GenerateHeader()) {
            X = 1,
            Y = 4,
            ColorScheme = new ColorScheme {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray)
            }
        };

        // Session list view - across the top, horizontal layout (full width)
        _sessionListView = new ListView() {
            X = 1,
            Y = 5,
            Width = Dim.Fill() - 1,  // Use full width minus just the right border
            Height = Dim.Percent(25),
            CanFocus = true,
            TabStop = true
        };

        // Initialize with empty source to prevent null reference exceptions
        _sessionListView.SetSource(new List<string>());

        // Set up color scheme - yellow background highlighting for selected items
        _sessionListView.ColorScheme = new ColorScheme {
            Normal = Application.Driver.MakeAttribute(Color.White, Color.Black), // Non-selected items
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow), // Selected item - YELLOW BACKGROUND
            HotNormal = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow), // Selected item when not focused - YELLOW BACKGROUND
            HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow) // Selected item when focused - YELLOW BACKGROUND
        };

        // Session details label - bottom left
        _queryLabel = new Label("Selected Session Details:") {
            X = 1,
            Y = Pos.Bottom(_sessionListView) + 1
        };

        // Session details view - bottom left half
        _queryTextView = new TextView() {
            X = 1,
            Y = Pos.Bottom(_queryLabel),
            Width = Dim.Percent(50) - 2,  // Standard padding for scrollbar
            Height = Dim.Fill() - 3,
            ReadOnly = true,
            Text = "No session selected\n\nUse ↑↓ arrows to navigate sessions",
            WordWrap = true
        };

        // Current Query label - bottom right
        var currentQueryLabel = new Label("Current Query:") {
            X = Pos.Right(_queryTextView) + 3,  // Increased spacing to prevent blending
            Y = Pos.Bottom(_sessionListView) + 1
        };

        // Current Query view - bottom right half
        _currentQueryTextView = new TextView() {
            X = Pos.Right(_queryTextView) + 3,  // Increased spacing to prevent blending
            Y = Pos.Bottom(currentQueryLabel),
            Width = Dim.Fill() - 2,  // Standard padding for scrollbar
            Height = Dim.Fill() - 3,
            ReadOnly = true,
            Text = "Select a session to view its current query",
            WordWrap = true
        };

        // Status bar component
        _statusBar = new StatusBarComponent(_keyboardHandler);

        // Add all components first
        Add(_connectionLabel, _sessionCountLabel, _sessionHeaderLabel, _sessionListView, _queryLabel,
            _queryTextView, currentQueryLabel, _currentQueryTextView, _statusBar.StatusLabel);

        // Create scrollbar for session list view
        _sessionListScrollBar = new ScrollBarView(_sessionListView, true, false) {
            X = Pos.Right(_sessionListView),
            Y = Pos.Top(_sessionListView),
            Height = Dim.Height(_sessionListView)
        };

        // Set up session list scrollbar event handlers
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

        // Create scrollbar for query text view
        _queryTextScrollBar = new ScrollBarView(_queryTextView, true, false) {
            X = Pos.Right(_queryTextView),
            Y = Pos.Top(_queryTextView),
            Height = Dim.Height(_queryTextView)
        };

        // Set up query text scrollbar event handlers
        _queryTextScrollBar.ChangedPosition += () => {
            _queryTextView.TopRow = _queryTextScrollBar.Position;
            if (_queryTextView.TopRow != _queryTextScrollBar.Position) {
                _queryTextScrollBar.Position = _queryTextView.TopRow;
            }
            _queryTextView.SetNeedsDisplay();
        };

        _queryTextView.DrawContent += (e) => {
            if (_queryTextView.Text != null) {
                _queryTextScrollBar.Size = _queryTextView.Lines;
                _queryTextScrollBar.Position = _queryTextView.TopRow;
                _queryTextScrollBar.Refresh();
            }
        };

        // Create scrollbar for current query text view
        _currentQueryScrollBar = new ScrollBarView(_currentQueryTextView, true, false) {
            X = Pos.Right(_currentQueryTextView),
            Y = Pos.Top(_currentQueryTextView),
            Height = Dim.Height(_currentQueryTextView)
        };

        // Set up current query scrollbar event handlers
        _currentQueryScrollBar.ChangedPosition += () => {
            _currentQueryTextView.TopRow = _currentQueryScrollBar.Position;
            if (_currentQueryTextView.TopRow != _currentQueryScrollBar.Position) {
                _currentQueryScrollBar.Position = _currentQueryTextView.TopRow;
            }
            _currentQueryTextView.SetNeedsDisplay();
        };

        _currentQueryTextView.DrawContent += (e) => {
            if (_currentQueryTextView.Text != null) {
                _currentQueryScrollBar.Size = _currentQueryTextView.Lines;
                _currentQueryScrollBar.Position = _currentQueryTextView.TopRow;
                _currentQueryScrollBar.Refresh();
            }
        };

        // Add all scrollbars
        Add(_sessionListScrollBar, _queryTextScrollBar, _currentQueryScrollBar);

        // Add key event handlers to TextViews so custom keys work when they have focus
        _queryTextView.KeyPress += OnTextViewKeyPress;
        _currentQueryTextView.KeyPress += OnTextViewKeyPress;
    }

    private void SetupLayout() {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    private void SetupEventHandlers() {
        // Handle session selection
        _sessionListView.SelectedItemChanged += OnSessionSelected;

        // Handle display mode changes
        _displayModeManager.ModeChanged += OnModeChanged;

        // Handle keyboard events through KeyboardHandler
        _keyboardHandler.QuitRequested += PromptToQuit;
        _keyboardHandler.RefreshRequested += RefreshSessions;
        _keyboardHandler.ShowWaitInfoRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.WaitInformation);
        _keyboardHandler.ShowSessionDetailsRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.SessionDetails);
        _keyboardHandler.ShowLockingInfoRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.LockingInformation);

        // Handle terminal resize
        Application.Resized += OnTerminalResized;
    }

    private void OnTerminalResized(Application.ResizedEventArgs args) {
        // Update display for new terminal size
        UpdateDisplayForTerminalSize();
    }

    private void OnSessionSelected(ListViewItemEventArgs args) {
        if (args.Item >= 0 && args.Item < _sessions.Count) {
            UpdateSessionDisplay();
        }
    }

    private void OnModeChanged() {
        UpdateSessionDisplay();
        UpdateStatusLabel();
    }

    private void UpdateSessionDisplay() {
        var selectedIndex = _sessionListView.SelectedItem;
        if (selectedIndex >= 0 && selectedIndex < _sessions.Count) {
            var session = _sessions[selectedIndex];

            // Show loading message for locking information
            if (_displayModeManager.CurrentMode == DisplayModeManager.DisplayMode.LockingInformation) {
                _queryTextView.Text = "Loading locking information...";
                Application.Refresh();
            }

            // Get content from the display mode manager
            _queryTextView.Text = _displayModeManager.GetDisplayContent(session);

            // Always update the current query in the separate view
            _currentQueryTextView.Text = string.IsNullOrWhiteSpace(session.CurrentQuery)
                ? "No active query"
                : session.CurrentQuery;

            // Force refresh to update scrollbars
            _currentQueryTextView.SetNeedsDisplay();
            _currentQueryScrollBar.Refresh();
            _queryTextView.SetNeedsDisplay();
            _queryTextScrollBar.Refresh();
        } else {
            _queryTextView.Text = "No session selected\n\nUse ↑↓ arrows to navigate sessions";
            _currentQueryTextView.Text = "Select a session to view its current query";
        }
    }

    public override bool ProcessKey(KeyEvent keyEvent) {
        // First, try to handle the key through our KeyboardHandler
        if (_keyboardHandler.HandleKeyPress(keyEvent)) {
            return true; // Key was handled by our custom handler
        }

        // If our handler didn't handle it, let the base class handle it normally
        // This ensures normal Terminal.Gui functionality (navigation, etc.) still works
        return base.ProcessKey(keyEvent);
    }

    private void OnTextViewKeyPress(KeyEventEventArgs keyEvent) {
        // Handle custom keys when TextViews have focus
        if (_keyboardHandler.HandleKeyPress(keyEvent.KeyEvent)) {
            keyEvent.Handled = true;
        }
        // If not handled, let the TextView handle it normally
    }

    private void OnKeyPress(KeyEventEventArgs keyEvent) {
        // This method is kept for any additional key handling if needed
        // Currently empty as ProcessKey handles everything
    }

    private void PromptToQuit() {
        var result = MessageBox.Query("Quit Application",
            "Are you sure you want to quit the PostgreSQL Database Monitor?",
            "Yes", "No");

        if (result == 0) { // User clicked "Yes"
            Application.RequestStop();
        }
        // If result == 1 (No) or dialog was cancelled, do nothing
    }

    private void UpdateStatusLabel() {
        var modeText = _displayModeManager.GetModeDisplayName();
        _statusBar.UpdateStatus(modeText);

        // Update the query label to match the mode
        _queryLabel.Text = _displayModeManager.GetQueryLabelText();
    }

    private string GenerateHeader() {
        int terminalWidth = Application.Driver?.Cols ?? 120;
        var widths = ColumnWidths.CalculateWidths(terminalWidth);

        if (widths == null)
            return "Terminal Too Small";

        // Truncate header text if needed to fit exactly in calculated widths
        var database = "Database".Length > widths.Database
            ? "Database"[..Math.Min(widths.Database, "Database".Length)]
            : "Database";
        var program = "Program".Length > widths.Application
            ? "Program"[..Math.Min(widths.Application, "Program".Length)]
            : "Program";
        var machine = "Machine".Length > widths.Machine
            ? "Machine"[..Math.Min(widths.Machine, "Machine".Length)]
            : "Machine";
        var status = "Status".Length > widths.Status
            ? "Status"[..Math.Min(widths.Status, "Status".Length)]
            : "Status";
        var type = "Type".Length > widths.Type
            ? "Type"[..Math.Min(widths.Type, "Type".Length)]
            : "Type";

        // Pad to exact widths to use all available space
        database = database.PadRight(widths.Database);
        program = program.PadRight(widths.Application);
        machine = machine.PadRight(widths.Machine);
        status = status.PadRight(widths.Status);
        type = type.PadRight(widths.Type);

        return $"[{database}] {program} | {machine} | PID: {"PID",-6} | {status} | {type}";
    }

    private void UpdateDisplayForTerminalSize() {
        // Update header for current terminal size
        _sessionHeaderLabel.Text = GenerateHeader();

        // Update status label for current terminal size
        UpdateStatusLabel();

        // Refresh session display with new widths
        RefreshSessionDisplay();
    }

    private void RefreshSessionDisplay() {
        if (_sessions.Count == 0) return;

        int terminalWidth = Application.Driver?.Cols ?? 120;
        var sessionTexts = _sessions.Select(s => s.GetDisplayText(terminalWidth)).ToList();
        _sessionListView.SetSource(sessionTexts);
    }

    public void RefreshSessions() {
        try {
            _statusBar.SetLoadingStatus("Refreshing...");
            Application.Refresh();

            // Store current selection and scroll position BEFORE refresh
            int? selectedSessionPid = null;
            int previousSelectedIndex = _sessionListView.SelectedItem;
            int previousTopItem = _sessionListView.TopItem;

            if (previousSelectedIndex >= 0 && previousSelectedIndex < _sessions.Count) {
                // Use PID as unique identifier for the session
                selectedSessionPid = _sessions[previousSelectedIndex].Pid;
            }

            // Refresh the data
            _sessions = _postgresService.GetActiveSessions();

            // Create session display list with dynamic widths
            RefreshSessionDisplay();

            _sessionCountLabel.Text = $"Active Sessions ({_sessions.Count}):";
            UpdateStatusLabel();

            // Restore selection or default to top session
            int newSelectedIndex = 0; // Default to top session

            if (selectedSessionPid.HasValue && _sessions.Count > 0) {
                // Try to find the same session by PID
                var matchingSessionIndex = _sessions.FindIndex(s => s.Pid == selectedSessionPid.Value);

                if (matchingSessionIndex >= 0) {
                    newSelectedIndex = matchingSessionIndex;
                }
                // If not found, newSelectedIndex remains 0 (top session)
            }

            // Set the selection and restore scroll position
            if (_sessions.Count > 0) {
                _sessionListView.SelectedItem = newSelectedIndex;

                // Restore scroll position if possible
                if (previousTopItem < _sessions.Count) {
                    _sessionListView.TopItem = previousTopItem;
                } else if (_sessions.Count > 0) {
                    // If previous scroll position is beyond new list, scroll to show selected item
                    _sessionListView.TopItem = Math.Max(0, newSelectedIndex - 5);
                }

                UpdateSessionDisplay();
            } else {
                _queryTextView.Text = "No active sessions found";
                _currentQueryTextView.Text = "No sessions available";
            }
        } catch (Exception ex) {
            _statusBar.SetErrorStatus(ex.Message);
            _queryTextView.Text = $"❌ Failed to refresh sessions\n\n" +
                                 $"Error Details:\n{ex.Message}\n\n" +
                                 $"Possible causes:\n" +
                                 $"• Database connection lost\n" +
                                 $"• Network connectivity issues\n" +
                                 $"• PostgreSQL server stopped\n" +
                                 $"• Insufficient permissions\n\n" +
                                 $"Press [Enter] to retry connection";
            _currentQueryTextView.Text = "Error occurred - no query data available";
        }

        Application.Refresh();
    }

    public void Initialize() {
        // Load initial data synchronously
        RefreshSessions();

        // Ensure the session list has focus for highlighting to work
        _sessionListView.SetFocus();
    }
}
