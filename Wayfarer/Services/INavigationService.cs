using Wayfarer.Models;

namespace Wayfarer.Services;

public interface INavigationService
{
    event EventHandler<AppPage>? Navigated;

    AppPage? CurrentPage { get; }

    bool CanGoBack { get; }

    /// <summary>Navigates the shell's content frame; the parameter arrives in Page.OnNavigatedTo.</summary>
    bool Navigate(AppPage page, object? parameter = null);

    bool GoBack();
}
