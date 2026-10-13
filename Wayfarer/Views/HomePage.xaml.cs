using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; } = App.GetService<HomeViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
    }
}
