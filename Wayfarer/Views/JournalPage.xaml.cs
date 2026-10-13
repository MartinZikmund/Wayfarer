using System.ComponentModel;
using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class JournalPage : Page
{
    private const double NarrowBreakpoint = 900;

    private bool _isNarrow;
    private bool _isActive;

    public JournalPage()
    {
        InitializeComponent();
    }

    public JournalViewModel ViewModel { get; } = App.GetService<JournalViewModel>();

    public Visibility EditorVisibility(JournalEntryItem? item) => item is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility PromptVisibility(JournalEntryItem? item) => item is null ? Visibility.Visible : Visibility.Collapsed;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isActive = true;
        await ViewModel.OnNavigatedToAsync(e.Parameter);
        if (!_isActive)
        {
            return;
        }

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        if (ViewModel.SelectedItem is null && !_isNarrow && ViewModel.Items.Count > 0)
        {
            ViewModel.SelectedItem = ViewModel.Items[0];
        }
        else
        {
            await ShowSelectedAsync();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _isActive = false;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Detach();
        _ = EntryEditor.SaveIfDirtyAsync();
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JournalViewModel.SelectedItem))
        {
            await ShowSelectedAsync();
        }
    }

    // The editor still holds the previous entry here, so save it before loading the next.
    private async Task ShowSelectedAsync()
    {
        await EntryEditor.SaveIfDirtyAsync();
        if (ViewModel.SelectedItem is { } item && EntryEditor.EntryId != item.Entry.Id)
        {
            await EntryEditor.LoadAsync(item.Entry.Id);
        }

        UpdateLayoutState();
    }

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _isNarrow = e.NewSize.Width < NarrowBreakpoint;
        UpdateLayoutState();
    }

    private void UpdateLayoutState()
    {
        var state = !_isNarrow ? "Wide" : ViewModel.SelectedItem is null ? "NarrowList" : "NarrowDetail";
        VisualStateManager.GoToState(this, state, true);
    }

    private void OnEntryDeleted(object? sender, EventArgs e) => ViewModel.SelectedItem = null;

    // The window owns the entry now; unload it here so re-selecting it loads the window's latest save.
    private void OnEntryOpenedInNewWindow(object? sender, EventArgs e)
    {
        EntryEditor.Unload();
        ViewModel.SelectedItem = null;
    }
}
