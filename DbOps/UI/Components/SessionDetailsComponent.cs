using DbOps.Models;
using DbOps.UI.Components;
using Terminal.Gui;

namespace DbOps.UI.Components;

public class SessionDetailsComponent {
    private readonly Label _queryLabel;
    private readonly TextView _queryTextView;
    private ScrollBarView _queryTextScrollBar = null!;
    private readonly Label _currentQueryLabel;
    private readonly TextView _currentQueryTextView;
    private ScrollBarView _currentQueryScrollBar = null!;
    private readonly DisplayModeManager _displayModeManager;

    public SessionDetailsComponent(DisplayModeManager displayModeManager, View sessionListView) {
        _displayModeManager = displayModeManager;

        _queryLabel = new Label("Selected Session Details:") {
            X = 1,
            Y = Pos.Bottom(sessionListView) + 1
        };

        _queryTextView = new TextView() {
            X = 1,
            Y = Pos.Bottom(_queryLabel),
            Width = Dim.Percent(50) - 2,
            Height = Dim.Fill() - 3,
            ReadOnly = true,
            Text = "No session selected\n\nUse ↑↓ arrows to navigate sessions",
            WordWrap = true
        };

        _currentQueryLabel = new Label("Current Query:") {
            X = Pos.Right(_queryTextView) + 3,
            Y = Pos.Bottom(sessionListView) + 1
        };

        _currentQueryTextView = new TextView() {
            X = Pos.Right(_queryTextView) + 3,
            Y = Pos.Bottom(_currentQueryLabel),
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 3,
            ReadOnly = true,
            Text = "Select a session to view its current query",
            WordWrap = true
        };

        SetupEventHandlers();
    }

    private void SetupScrollBars() {
        _queryTextScrollBar = new ScrollBarView(_queryTextView, true) {
            X = Pos.Right(_queryTextView),
            Y = Pos.Top(_queryTextView),
            Height = Dim.Height(_queryTextView)
        };

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

        _currentQueryScrollBar = new ScrollBarView(_currentQueryTextView, true) {
            X = Pos.Right(_currentQueryTextView),
            Y = Pos.Top(_currentQueryTextView),
            Height = Dim.Height(_currentQueryTextView)
        };

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
    }

    private void SetupEventHandlers() {
        _displayModeManager.ModeChanged += UpdateQueryLabel;
    }

    public void UpdateSession(DatabaseSession? session) {
        if (session != null) {
            // Show loading message for locking information
            if (_displayModeManager.CurrentMode == DisplayModeManager.DisplayMode.LockingInformation) {
                _queryTextView.Text = "Loading locking information...";
                Application.Refresh();
            }

            _queryTextView.Text = _displayModeManager.GetDisplayContent(session);
            _currentQueryTextView.Text = string.IsNullOrWhiteSpace(session.CurrentQuery)
                ? "No active query"
                : session.CurrentQuery;

            RefreshViews();
        } else {
            _queryTextView.Text = "No session selected\n\nUse ↑↓ arrows to navigate sessions";
            _currentQueryTextView.Text = "Select a session to view its current query";
            RefreshViews();
        }
    }

    private void RefreshViews() {
        _currentQueryTextView.SetNeedsDisplay();
        _queryTextView.SetNeedsDisplay();
    }

    private void UpdateQueryLabel() {
        _queryLabel.Text = _displayModeManager.GetQueryLabelText();
    }
    public TextView QueryTextView => _queryTextView;
    public TextView CurrentQueryTextView => _currentQueryTextView;
    public DisplayModeManager DisplayModeManager => _displayModeManager;

    public void InitializeScrollBars() {
        SetupScrollBars();
    }

    public IEnumerable<View> GetViews() {
        yield return _queryLabel;
        yield return _queryTextView;
        yield return _currentQueryLabel;
        yield return _currentQueryTextView;
    }

    public IEnumerable<View> GetScrollBarViews() {
        if (_queryTextScrollBar != null)
            yield return _queryTextScrollBar;
        if (_currentQueryScrollBar != null)
            yield return _currentQueryScrollBar;
    }
}