using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class MomentsPage : Page
{
    public MomentsPage()
    {
        InitializeComponent();
    }

    public MomentsViewModel ViewModel { get; } = App.GetService<MomentsViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
    }
}
