using Wayfarer.Models;

namespace Wayfarer.Views;

public sealed partial class GalleryPhotoDialog : ContentDialog
{
    public GalleryPhotoDialog(Photo photo, string tripTitle)
    {
        Photo = photo;
        TripTitle = tripTitle;
        InitializeComponent();
    }

    public Photo Photo { get; }

    public string TripTitle { get; }

    public string Credit => string.IsNullOrEmpty(Photo.Author) ? "" : $"Photo by {Photo.Author}";
}
