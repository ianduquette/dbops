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
    public event Action<DatabaseConnection?>? ConnectionSelected;
    public event Action? ViewLoaded;
    public event Action? ViewClosing;

    // UI Components - not readonly to avoid assignment issues
    private SessionListComponent _sessionListComponent = null!;
    private SessionDetailsComponent _sessionDetailsComponent = null!;
    private StatusBarComponent _statusBar = null!;
    private Label _connectionLabel = null!;

    // Presenter
    private MainWindowPresenter? _presenter;

    // Keyboard handler
    private KeyboardHandler _keyboardHandler = null!;

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

        _keyboardHandler = new KeyboardHandler();
        _statusBar = new StatusBarComponent(_keyboardHandler);

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

            var action = _keyboardHandler.GetAction(keyEvent.KeyEvent.Key);
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

            var action = _keyboardHandler.GetAction(keyEvent.KeyEvent.Key);
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
        var action = _keyboardHandler.GetAction(keyEvent.Key);
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

    public void ShowConnectionDialog(ConnectionManager connectionManager) {
        try {
            var connectionDialog = new ConnectionSelectionDialog(connectionManager);
            Application.Run(connectionDialog);

            // Handle connection selection result
            if (connectionDialog.ConnectionSelected && connectionDialog.SelectedConnection != null) {
                // Fire the ConnectionSelected event to notify the presenter
                ConnectionSelected?.Invoke(connectionDialog.SelectedConnection);
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

            // Define the tab order: SessionList -> QueryTextView -> CurrentQueryTextView -> back to SessionList
            var focusableViews = new View[] {
                _sessionListComponent.ListView,
                _sessionDetailsComponent.QueryTextView,
                _sessionDetailsComponent.CurrentQueryTextView
            };

            // Find current focus index
            int currentIndex = -1;
            for (int i = 0; i < focusableViews.Length; i++) {
                if (currentFocus == focusableViews[i]) {
                    currentIndex = i;
                    break;
                }
            }

            // Calculate next focus index
            int nextIndex;
            if (reverse) {
                // Shift+Tab: Move backwards
                nextIndex = currentIndex <= 0 ? focusableViews.Length - 1 : currentIndex - 1;
            } else {
                // Tab: Move forwards
                nextIndex = currentIndex >= focusableViews.Length - 1 ? 0 : currentIndex + 1;
            }

            // Set focus to the next view
            if (nextIndex == 0) {
                _sessionListComponent.SetFocus();
            } else {
                focusableViews[nextIndex].SetFocus();
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