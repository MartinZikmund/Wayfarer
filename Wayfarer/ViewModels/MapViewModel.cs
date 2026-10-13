using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>Routes: one trip at a time, with its stops in travel order.</summary>
public sealed partial class MapViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;

    public string Title => "Routes";

    public string Subtitle => "Every dashed line is a story waiting to be retold.";

    [ObservableProperty]
    public partial IReadOnlyList<Trip> Trips { get; set; } = [];

    [ObservableProperty]
    public partial Trip? SelectedTrip { get; set; }

    /// <summary>The selected trip's stops, with <see cref="RouteStop.X"/>/<see cref="RouteStop.Y"/> normalized to 0..1.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<RouteStop> Stops { get; set; } = [];

    /// <param name="parameter">Optional trip id to show first.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;
        try
        {
            Trips = await _tripService.GetTripsAsync();
            SelectedTrip = Trips.FirstOrDefault(t => t.Id == parameter as string) ?? SelectedTrip ?? Trips.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedTripChanged(Trip? value) => Stops = value?.Route ?? [];
}
