using Wayfarer.Models;
using Wayfarer.Views;

namespace Wayfarer.Services;

public sealed class NavigationService : INavigationService
{
    private static readonly Dictionary<AppPage, Type> PageTypes = new()
    {
        [AppPage.Gallery] = typeof(GalleryPage),
        [AppPage.Journal] = typeof(JournalPage),
        [AppPage.Stories] = typeof(StoriesPage),
        [AppPage.Moments] = typeof(MomentsPage),
        [AppPage.Settings] = typeof(SettingsPage),
    };

    private Frame? _frame;

    public event EventHandler<AppPage>? Navigated;

    public AppPage? CurrentPage { get; private set; }

    public bool CanGoBack => _frame?.CanGoBack == true;

    /// <summary>Called by the shell to hand over its content frame.</summary>
    public void Attach(Frame frame)
    {
        if (_frame is not null)
        {
            _frame.Navigated -= OnFrameNavigated;
        }

        _frame = frame;
        _frame.Navigated += OnFrameNavigated;
    }

    public bool Navigate(AppPage page, object? parameter = null)
    {
        if (_frame is null || (CurrentPage == page && parameter is null))
        {
            return false;
        }

        return _frame.Navigate(PageTypes[page], parameter);
    }

    public bool GoBack()
    {
        if (_frame?.CanGoBack != true)
        {
            return false;
        }

        _frame.GoBack();
        return true;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        foreach (var (page, type) in PageTypes)
        {
            if (type == e.SourcePageType)
            {
                CurrentPage = page;
                Navigated?.Invoke(this, page);
                return;
            }
        }
    }
}
