using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; } = App.GetService<SettingsViewModel>();
}
