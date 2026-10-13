using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>The start page: a welcome postcard, the trips and the latest journal entries.</summary>
public sealed partial class HomeViewModel(ITripService tripService, INavigationService navigation) : PageViewModel
{
    private readonly ITripService _tripService = tripService;
    private readonly INavigationService _navigation = navigation;

    public string Title => "Welcome back";

    public string Subtitle => "Where to next? Your postcards from the road are all here.";

    [ObservableProperty]
    public partial IReadOnlyList<Trip> Trips { get; set; } = [];

    /// <summary>Newest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<JournalEntry> RecentEntries { get; set; } = [];

    /// <summary>The trip with the newest start date, or null when there are none.</summary>
    [ObservableProperty]
    public partial Trip? LatestTrip { get; set; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;
        try
        {
            Trips = await _tripService.GetTripsAsync();
            RecentEntries = (await _tripService.GetEntriesAsync()).Take(3).ToList();
            LatestTrip = Trips.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenTrip(Trip? trip)
    {
        if (trip is not null)
        {
            _navigation.Navigate(AppPage.Stories, trip.Id);
        }
    }

    [RelayCommand]
    private void OpenEntry(JournalEntry? entry)
    {
        if (entry is not null)
        {
            _navigation.Navigate(AppPage.Journal, entry.Id);
        }
    }
}
