using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class JournalPage : Page
{
    public JournalPage()
    {
        InitializeComponent();
    }

    public JournalViewModel ViewModel { get; } = App.GetService<JournalViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
    }
}
