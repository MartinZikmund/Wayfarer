using Wayfarer.Helpers;
using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>A photo in the gallery grid; <see cref="Key"/> is its position in the current filter.</summary>
public sealed record GalleryItem(Photo Photo, int Key, Trip? Trip);

public sealed partial class GalleryViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;
    private IReadOnlyList<Photo> _allPhotos = [];

    public string Title => "Gallery";

    public IReadOnlyList<Trip> Trips { get; private set; } = [];

    /// <summary>Photo id requested through navigation (e.g. from search); consumed by the page.</summary>
    public string? RequestedPhotoId { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<GalleryItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial string Summary { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        RequestedPhotoId = parameter as string;

        if (_allPhotos.Count > 0)
        {
            return;
        }

        IsLoading = true;
        try
        {
            Trips = await _tripService.GetTripsAsync();
            _allPhotos = await _tripService.GetPhotosAsync();
            Filter(null);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearRequest() => RequestedPhotoId = null;

    public Trip? TripOf(Photo photo) => Trips.FirstOrDefault(t => t.Id == photo.TripId);

    /// <param name="tripId">Trip to show, or null for all trips.</param>
    public void Filter(string? tripId)
    {
        List<Photo> photos = _allPhotos.Where(p => tripId is null || p.TripId == tripId).ToList();
        List<GalleryItem> items = photos.Select((photo, index) => new GalleryItem(photo, index, TripOf(photo))).ToList();

        Items = items;
        IsEmpty = items.Count == 0;
        Summary = tripId is null
            ? $"{Format.Photos(photos.Count)} from {Trips.Count} trips. Postcards you never had to write."
            : $"{Format.Photos(photos.Count)} from {Trips.FirstOrDefault(t => t.Id == tripId)?.Title}";
    }
}
