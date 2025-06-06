using DbOps.Models;
using DbOps.Services;
using DbOps.UI.Components;
using Terminal.Gui;

namespace DbOps.UI;

public class MainWindow : Window {
    private readonly SessionListComponent _sessionListComponent;
    private readonly SessionDetailsComponent _sessionDetailsComponent;
    private readonly StatusBarComponent _statusBar;
    private readonly KeyboardHandler _keyboardHandler;
    private readonly DisplayModeManager _displayModeManager;
    private readonly DataRefreshService _dataRefreshService;
    private readonly Label _connectionLabel;

    private List<DatabaseSession> _sessions = new();

    public MainWindow(SyncPostgresService postgresService) : base("PostgreSQL Database Monitor") {
        _displayModeManager = new DisplayModeManager(postgresService);
        _keyboardHandler = new KeyboardHandler();
        _statusBar = new StatusBarComponent(_keyboardHandler);
        _dataRefreshService = new DataRefreshService(postgresService, _statusBar);

        _sessionListComponent = new SessionListComponent();
        _sessionDetailsComponent = new SessionDetailsComponent(_displayModeManager, _sessionListComponent.ListView);

        _connectionLabel = new Label($"Connected to: {_dataRefreshService.GetConnectionInfo()}") {
            X = 1,
            Y = 1
        };

        InitializeLayout();
        SetupEventHandlers();
    }

    private void InitializeLayout() {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Add connection label
        Add(_connectionLabel);

        // Add session list components
        foreach (var view in _sessionListComponent.GetViews()) {
            Add(view);
        }

        // Add session details components
        foreach (var view in _sessionDetailsComponent.GetViews()) {
            Add(view);
        }

        // Add status bar
        Add(_statusBar.StatusLabel);

        // Initialize scrollbars after components are added to parent
        _sessionListComponent.InitializeScrollBar();
        _sessionDetailsComponent.InitializeScrollBars();

        // Add scrollbars
        foreach (var view in _sessionListComponent.GetScrollBarViews()) {
            Add(view);
        }

        foreach (var view in _sessionDetailsComponent.GetScrollBarViews()) {
            Add(view);
        }
    }

    private void SetupEventHandlers() {
        // Session selection events
        _sessionListComponent.SessionSelected += OnSessionSelected;

        // Data refresh events
        _dataRefreshService.SessionsRefreshed += OnSessionsRefreshed;
        _dataRefreshService.ErrorOccurred += OnDataRefreshError;

        // Display mode events
        _displayModeManager.ModeChanged += OnModeChanged;

        // Keyboard events
        _keyboardHandler.QuitRequested += PromptToQuit;
        _keyboardHandler.RefreshRequested += () => _dataRefreshService.RefreshSessions();
        _keyboardHandler.ShowWaitInfoRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.WaitInformation);
        _keyboardHandler.ShowSessionDetailsRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.SessionDetails);
        _keyboardHandler.ShowLockingInfoRequested += () => _displayModeManager.SetMode(DisplayModeManager.DisplayMode.LockingInformation);

        // Terminal resize events
        Application.Resized += OnTerminalResized;

        // Add key event handlers to text views
        _sessionDetailsComponent.QueryTextView.KeyPress += OnTextViewKeyPress;
        _sessionDetailsComponent.CurrentQueryTextView.KeyPress += OnTextViewKeyPress;
    }

    private void OnSessionSelected(int selectedIndex) {
        if (selectedIndex >= 0 && selectedIndex < _sessions.Count) {
            var selectedSession = _sessions[selectedIndex];
            _sessionDetailsComponent.UpdateSession(selectedSession);
        } else {
            _sessionDetailsComponent.UpdateSession(null);
        }
    }

    private void OnSessionsRefreshed(List<DatabaseSession> sessions) {
        _sessions = sessions;
        _sessionListComponent.UpdateSessions(sessions);
        UpdateStatusLabel();

        // Update session details if a session is selected
        var selectedSession = _sessionListComponent.SelectedSession;
        _sessionDetailsComponent.UpdateSession(selectedSession);
    }

    private void OnDataRefreshError(string errorMessage) {
        _sessionDetailsComponent.UpdateSession(null);
        _sessionDetailsComponent.QueryTextView.Text = errorMessage;
        _sessionDetailsComponent.CurrentQueryTextView.Text = "Error occurred - no query data available";
    }

    private void OnModeChanged() {
        var selectedSession = _sessionListComponent.SelectedSession;
        _sessionDetailsComponent.UpdateSession(selectedSession);
        UpdateStatusLabel();
    }

    private void OnTerminalResized(Application.ResizedEventArgs args) {
        _sessionListComponent.OnTerminalResized();
        UpdateStatusLabel();
    }

    private void UpdateStatusLabel() {
        var modeText = _displayModeManager.GetModeDisplayName();
        _statusBar.UpdateStatus(modeText);
    }

    public override bool ProcessKey(KeyEvent keyEvent) {
        if (_keyboardHandler.HandleKeyPress(keyEvent)) {
            return true;
        }
        return base.ProcessKey(keyEvent);
    }

    private void OnTextViewKeyPress(KeyEventEventArgs keyEvent) {
        if (_keyboardHandler.HandleKeyPress(keyEvent.KeyEvent)) {
            keyEvent.Handled = true;
        }
    }

    private void PromptToQuit() {
        var result = MessageBox.Query("Quit Application",
            "Are you sure you want to quit the PostgreSQL Database Monitor?",
            "Yes", "No");

        if (result == 0) {
            Application.RequestStop();
        }
    }

    public void Initialize() {
        _dataRefreshService.RefreshSessions();
        _sessionListComponent.SetFocus();
    }
}
