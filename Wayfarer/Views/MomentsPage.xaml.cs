using Microsoft.UI.Xaml.Controls.Primitives;
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

    private static string PlaybackGlyph(bool isPlaying) => isPlaying ? "" : "";

    private static string PlaybackName(bool isPlaying) => isPlaying ? "Pause animation" : "Play animation";

    private void OnPlayerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is AnimatedVisualPlayer player)
        {
            _ = player.PlayAsync(0, 1, true);
        }
    }

    private void OnPlaybackClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: AnimatedVisualPlayer player, IsChecked: bool isPlaying })
        {
            if (isPlaying)
            {
                player.Resume();
            }
            else
            {
                player.Pause();
            }
        }
    }
}
