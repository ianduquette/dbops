using DbOps.Domain.Models;
using DbOps.Domain.Services;
using DbOps.Domain.Logic;
using DbOps.UI.Components;
using DbOps.UI.Dialogs;
using DbOps.UI.Presenters;
using Terminal.Gui;

namespace DbOps.UI.Views;

public class MainWindowView : Window, IMainView {
    // Events from IMainView
    public event Action<int>? SessionSelected;
    public event Action<UserAction>? ActionRequested;
    public event Action? ViewLoaded;
    public event Action? ViewClosing;

    // UI Components - not readonly to avoid assignment issues
    private SessionListComponent _sessionListComponent = null!;
    private SessionDetailsComponent _sessionDetailsComponent = null!;
    private StatusBarComponent _statusBar = null!;
    private Label _connectionLabel = null!;

    // Presenter
    private MainWindowPresenter? _presenter;

    // State
    private bool _initialized = false;

    public MainWindowView() : base("PostgreSQL Database Monitor") {
        InitializeComponents();
        InitializeLayout();
        SetupEventHandlers();
    }

    public new bool IsInitialized => _initialized;

    private void InitializeComponents() {
        _sessionListComponent = new SessionListComponent();

        // Create DisplayModeManager without service - will be updated when connection is established
        var displayModeManager = new DisplayModeManager();
        _sessionDetailsComponent = new SessionDetailsComponent(displayModeManager, _sessionListComponent.ListView);

        var keyboardHandler = new KeyboardHandler();
        _statusBar = new StatusBarComponent(keyboardHandler);

        _connectionLabel = new Label("Initializing...") {
            X = 1,
            Y = 1,
            ColorScheme = new ColorScheme {
                Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
                Focus = new Terminal.Gui.Attribute(Color.White, Color.Black)
            }
        };

        // Setup key event forwarding for text views
        SetupTextViewKeyHandling();
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

        // Initialize status bar with commands
        _statusBar.UpdateStatus("Session Details");

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
        // Session selection events - forward to presenter
        _sessionListComponent.SessionSelected += (index) => SessionSelected?.Invoke(index);

        // Terminal resize events
        Application.Resized += OnTerminalResized;
    }

    private void SetupTextViewKeyHandling() {
        // Setup key event handlers for text views to forward application keys
        _sessionDetailsComponent.QueryTextView.KeyPress += (keyEvent) => {
            // Don't intercept Tab keys - let them be handled by the main window for navigation
            if (keyEvent.KeyEvent.Key == Key.Tab || keyEvent.KeyEvent.Key == (Key.ShiftMask | Key.Tab)) {
                return;
            }

            var action = MapKeyToAction(keyEvent.KeyEvent.Key);
            if (action.HasValue) {
                // Handle quit action with confirmation dialog directly
                if (action.Value == UserAction.Quit) {
                    if (ShowQuitConfirmation()) {
                        ActionRequested?.Invoke(action.Value);
                    }
                    keyEvent.Handled = true;
                    return;
                }

                // Forward other application keys to main window
                ActionRequested?.Invoke(action.Value);
                keyEvent.Handled = true;
            }
        };

        _sessionDetailsComponent.CurrentQueryTextView.KeyPress += (keyEvent) => {
            // Don't intercept Tab keys - let them be handled by the main window for navigation
            if (keyEvent.KeyEvent.Key == Key.Tab || keyEvent.KeyEvent.Key == (Key.ShiftMask | Key.Tab)) {
                return;
            }

            var action = MapKeyToAction(keyEvent.KeyEvent.Key);
            if (action.HasValue) {
                // Handle quit action with confirmation dialog directly
                if (action.Value == UserAction.Quit) {
                    if (ShowQuitConfirmation()) {
                        ActionRequested?.Invoke(action.Value);
                    }
                    keyEvent.Handled = true;
                    return;
                }

                // Forward other application keys to main window
                ActionRequested?.Invoke(action.Value);
                keyEvent.Handled = true;
            }
        };
    }

    public override bool ProcessKey(KeyEvent keyEvent) {
        var action = MapKeyToAction(keyEvent.Key);
        if (action.HasValue) {
            // Handle quit action with confirmation dialog
            if (action.Value == UserAction.Quit) {
                if (ShowQuitConfirmation()) {
                    ActionRequested?.Invoke(action.Value);
                }
                return true;
            }
            ActionRequested?.Invoke(action.Value);
            return true;
        }

        // Handle Tab navigation between components
        if (keyEvent.Key == Key.Tab || keyEvent.Key == (Key.ShiftMask | Key.Tab)) {
            HandleTabNavigation(keyEvent.Key == (Key.ShiftMask | Key.Tab));
            return true;
        }

        return base.ProcessKey(keyEvent);
    }

    private UserAction? MapKeyToAction(Key key) => key switch {
        Key.F5 => UserAction.Refresh,
        Key.CtrlMask | Key.Q => UserAction.Quit,
        Key.q => UserAction.Quit,
        Key.Q => UserAction.Quit,
        Key.c => UserAction.ShowConnections,
        Key.C => UserAction.ShowConnections,
        Key.w => UserAction.ShowWaitInfo,
        Key.W => UserAction.ShowWaitInfo,
        Key.s => UserAction.ShowSessionDetails,
        Key.S => UserAction.ShowSessionDetails,
        Key.l => UserAction.ShowLockingInfo,
        Key.L => UserAction.ShowLockingInfo,
        Key.Enter => UserAction.Refresh,
        _ => null
    };

    // IMainView Implementation
    public void UpdateConnectionStatus(string status) {
        _connectionLabel.Text = status;
        _connectionLabel.SetNeedsDisplay();
    }

    public void UpdateSessions(List<DatabaseSession> sessions) {
        _sessionListComponent.UpdateSessions(sessions);
    }

    public void UpdateSessionDetails(DatabaseSession? session) {
        _sessionDetailsComponent.UpdateSession(session);
    }

    public void SetDisplayMode(UserAction mode) {
        // Map UserAction to DisplayModeManager.DisplayMode
        var displayMode = mode switch {
            UserAction.ShowSessionDetails => DisplayModeManager.DisplayMode.SessionDetails,
            UserAction.ShowWaitInfo => DisplayModeManager.DisplayMode.WaitInformation,
            UserAction.ShowLockingInfo => DisplayModeManager.DisplayMode.LockingInformation,
            _ => DisplayModeManager.DisplayMode.SessionDetails
        };

        // Get the DisplayModeManager from the SessionDetailsComponent
        var displayModeManager = GetDisplayModeManager();
        if (displayModeManager != null) {
            displayModeManager.SetMode(displayMode);

            // Update status bar with current mode
            var modeText = displayModeManager.GetModeDisplayName();
            _statusBar.UpdateStatus(modeText);
        }
    }

    private DisplayModeManager? GetDisplayModeManager() {
        return _sessionDetailsComponent.DisplayModeManager;
    }


    public void ShowError(string title, string message) {
        MessageBox.ErrorQuery(title, message, "OK");
    }

    public void ShowMessage(string title, string message) {
        MessageBox.Query(title, message, "OK");
    }

    public void ShowConnectionDialog() {
        try {
            var connectionManager = new ConnectionManager();
            var connectionDialog = new ConnectionSelectionDialog(connectionManager);
            Application.Run(connectionDialog);

            // Handle connection selection result
            if (connectionDialog.ConnectionSelected && connectionDialog.SelectedConnection != null) {
                // For now, we'll need to handle this through the presenter
                // This is a temporary implementation - ideally we'd have a callback or event
                ShowMessage("Connection Selected", $"Selected: {connectionDialog.SelectedConnection.DisplayName}");
            }
        } catch (Exception ex) {
            ShowError("Connection Error", $"Error showing connection dialog: {ex.Message}");
        }
    }

    private bool ShowQuitConfirmation() {
        var result = MessageBox.Query("Confirm Exit",
            "Are you sure you want to quit DbOps?",
            "Yes", "No");
        return result == 0; // 0 = Yes, 1 = No
    }

    private void HandleTabNavigation(bool reverse) {
        try {
            // Get current focused view
            var currentFocus = Application.Top.MostFocused;

            if (reverse) {
                // Shift+Tab: Move focus backwards
                if (currentFocus == _sessionDetailsComponent.QueryTextView ||
                    currentFocus == _sessionDetailsComponent.CurrentQueryTextView) {
                    _sessionListComponent.SetFocus();
                } else {
                    // Focus on the first text view in session details
                    _sessionDetailsComponent.QueryTextView.SetFocus();
                }
            } else {
                // Tab: Move focus forwards
                if (currentFocus == _sessionListComponent.ListView) {
                    // Focus on the first text view in session details
                    _sessionDetailsComponent.QueryTextView.SetFocus();
                } else if (currentFocus == _sessionDetailsComponent.QueryTextView) {
                    // Move to the second text view
                    _sessionDetailsComponent.CurrentQueryTextView.SetFocus();
                } else {
                    // Go back to session list
                    _sessionListComponent.SetFocus();
                }
            }
        } catch {
            // Fallback to session list if navigation fails
            _sessionListComponent.SetFocus();
        }
    }

    // Lifecycle methods
    public void Initialize() {
        if (!_initialized) {
            // Create presenter
            _presenter = new MainWindowPresenter(this);

            _sessionListComponent.SetFocus();
            _initialized = true;
            ViewLoaded?.Invoke();
        }
    }

    public void Cleanup() {
        ViewClosing?.Invoke();
        _presenter?.Dispose();
    }

    private void OnTerminalResized(Application.ResizedEventArgs args) {
        _sessionListComponent.OnTerminalResized();
        SetNeedsDisplay();
    }
}