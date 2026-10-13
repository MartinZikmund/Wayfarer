using Microsoft.UI.Xaml.Media.Imaging;
using Wayfarer.Controls.Postcard;
using Wayfarer.Helpers;
using Wayfarer.Models;
using Wayfarer.ViewModels;

namespace Wayfarer.Views;

public sealed partial class HomePage : Page
{
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

    // The cover has to be captured before navigating, while it is still on screen.
    private void OnTripClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Trip trip } button)
        {
            return;
        }

        if (FindCover(button) is { } cover)
        {
            HeroTransition.Prepare(MapPage.TripHeroAnimationKey, cover);
        }

        ViewModel.OpenTripCommand.Execute(trip);
    }

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
