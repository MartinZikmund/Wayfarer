using Wayfarer.Models;

namespace Wayfarer.Views;

public sealed partial class GalleryPhotoDialog : ContentDialog
{
    public GalleryPhotoDialog(Photo photo, Trip? trip)
    {
        Photo = photo;
        Trip = trip;
        InitializeComponent();
    }

    public Photo Photo { get; }

    public Trip? Trip { get; }

    public string TripTitle => Trip?.Title ?? "";

    public string TripCode => Trip?.Code ?? "";

    public string Credit => string.IsNullOrEmpty(Photo.Author) ? "" : $"Photo by {Photo.Author}";
}
