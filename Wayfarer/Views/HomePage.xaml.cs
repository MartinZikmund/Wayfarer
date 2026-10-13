using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Wayfarer.Controls.Postcard;
using Wayfarer.Helpers;
using Wayfarer.Models;
using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class HomePage : Page
{
    public const string TripCoverAnimationKey = "TripCover";

    public HomePage()
    {
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; } = App.GetService<HomeViewModel>();

    public static ImageSource? Cover(Uri? uri) => uri is null ? null : new BitmapImage(uri);

    public static string TripStats(int days, int photos, int km) =>
        $"{Format.Days(days)} · {Format.Photos(photos)} · {Format.Distance(km)}";

    public static Visibility VisibleIfPresent(object? value) => Format.VisibleIf(value is not null);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnNavigatedToAsync(e.Parameter);
    }

    // The animation has to be prepared before navigating, while the cover is still on screen.
    private void OnTripClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Trip trip } button)
        {
            return;
        }

        if (Motion.IsEnabled && FindCover(button) is { } cover)
        {
            PrepareCoverAnimation(cover);
        }

        ViewModel.OpenTripCommand.Execute(trip);
    }

    // ConnectedAnimationService is still a stub in Uno 7.0 dev builds; the page simply navigates without it.
#pragma warning disable Uno0001
    private static void PrepareCoverAnimation(Image cover)
    {
        try
        {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(TripCoverAnimationKey, cover);
        }
        catch (NotImplementedException)
        {
        }
    }
#pragma warning restore Uno0001

    private static Image? FindCover(DependencyObject start)
    {
        DependencyObject? card = start;
        while (card is not null and not PostcardCard)
        {
            card = VisualTreeHelper.GetParent(card);
        }

        return card is null ? null : FindImage(card);
    }

    private static Image? FindImage(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is Image { Name: "CoverImage" } image)
            {
                return image;
            }

            if (FindImage(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
