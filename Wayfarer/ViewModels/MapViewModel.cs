using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>Routes: one trip at a time, with its stops in travel order.</summary>
public sealed partial class MapViewModel(ITripService tripService, INavigationService navigation) : PageViewModel
{
    private readonly ITripService _tripService = tripService;
    private readonly INavigationService _navigation = navigation;

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

    /// <summary>The stop whose details are showing; cleared when the trip changes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedStop), nameof(HasSelectedEntry), nameof(SelectedPhoto), nameof(HasSelectedPhoto), nameof(SelectedStopCaption))]
    public partial RouteStop? SelectedStop { get; set; }

    public bool HasSelectedStop => SelectedStop is not null;

    public bool HasSelectedEntry => SelectedStop?.Entry is not null;

    public Photo? SelectedPhoto => SelectedStop?.Photos.FirstOrDefault();

    public bool HasSelectedPhoto => SelectedPhoto is not null;

    public string SelectedStopCaption => SelectedStop is { } stop
        ? $"Stop {IndexOf(stop) + 1} · Day {stop.Day} · {stop.Date:d MMM yyyy}"
        : "";

    [RelayCommand]
    private void SelectStop(RouteStop? stop) => SelectedStop = stop;

    [RelayCommand]
    private void ReadEntry()
    {
        if (SelectedStop?.EntryId is { } entryId)
        {
            _navigation.Navigate(AppPage.Journal, entryId);
        }
    }

    private int IndexOf(RouteStop stop)
    {
        for (int i = 0; i < Stops.Count; i++)
        {
            if (ReferenceEquals(Stops[i], stop))
            {
                return i;
            }
        }

        return -1;
    }

    partial void OnSelectedTripChanged(Trip? value)
    {
        SelectedStop = null;
        Stops = value?.Route ?? [];
    }
}
