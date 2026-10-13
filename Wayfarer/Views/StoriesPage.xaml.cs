using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class StoriesPage : Page
{
    public StoriesPage()
    {
        InitializeComponent();
    }

    public StoriesViewModel ViewModel { get; } = App.GetService<StoriesViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
    }
}
