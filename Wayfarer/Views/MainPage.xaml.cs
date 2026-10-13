using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Wayfarer.Models;
using Wayfarer.Services;
using Wayfarer.ViewModels;

namespace Wayfarer.Views;

/// <summary>App shell: TitleBar with search on top, NavigationView with the content frame below.</summary>
public sealed partial class MainPage : Page
{
    private readonly NavigationService _navigation = (NavigationService)App.GetService<INavigationService>();

    public MainPage()
    {
        InitializeComponent();

        _navigation.Attach(ContentFrame);
        _navigation.Navigated += OnNavigated;
        Loaded += OnLoaded;
    }

    public ShellViewModel ViewModel { get; } = App.GetService<ShellViewModel>();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.IsDesktop && App.MainWindow is { } window)
        {
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(AppTitleBar);

            // Tall caption area, since the title bar hosts a search box.
            window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        if (_navigation.CurrentPage is null)
        {
            _navigation.Navigate(AppPage.Gallery);
        }
    }

    private void OnNavigated(object? sender, AppPage page)
    {
        AppTitleBar.IsBackButtonEnabled = _navigation.CanGoBack;

        object? item = page == AppPage.Settings
            ? NavView.SettingsItem
            : NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Tag == page.ToString());
        if (!ReferenceEquals(NavView.SelectedItem, item))
        {
            NavView.SelectedItem = item;
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            _navigation.Navigate(AppPage.Settings);
        }
        else if (args.SelectedItem is NavigationViewItem { Tag: string tag } && Enum.TryParse(tag, out AppPage page))
        {
            _navigation.Navigate(page);
        }
    }

    private void OnBackRequested(TitleBar sender, object args) => _navigation.GoBack();

    private void OnPaneToggleRequested(TitleBar sender, object args) => NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private async void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput
            && await ViewModel.GetSuggestionsAsync(sender.Text) is { } suggestions)
        {
            sender.ItemsSource = suggestions;
        }
    }

    private async void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await ViewModel.OpenSearchResultAsync(args.ChosenSuggestion as SearchResult, args.QueryText);
        sender.Text = "";
        sender.ItemsSource = null;
    }

    private void OnSearchAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = SearchBox.Focus(FocusState.Keyboard);
    }
}
