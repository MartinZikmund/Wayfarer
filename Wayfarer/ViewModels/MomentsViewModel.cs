using System.Collections.ObjectModel;
using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed partial class MomentsViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;

    public string Title => "Moments";

    public ObservableCollection<TripMoment> Moments { get; } = new();

    [ObservableProperty]
    public partial bool ShowEmpty { get; set; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        if (Moments.Count > 0)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var trips = await _tripService.GetTripsAsync();
            foreach (var trip in trips)
            {
                Moments.Add(new(trip));
            }

            ShowEmpty = Moments.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>One trip's dashboard card: animation, stats, completion and itinerary.</summary>
public sealed partial class TripMoment : ObservableObject
{
    public TripMoment(Trip trip)
    {
        Trip = trip;
        AnimationSource = new($"ms-appx:///Assets/Lottie/{AnimationName(trip.Id)}.json");

        var photoDays = trip.Photos.Select(p => p.TakenOn).ToHashSet();
        var entryDays = trip.Entries.Select(e => e.Date).ToHashSet();
        photoDays.UnionWith(entryDays);
        DaysCaptured = photoDays.Count(d => d >= trip.StartDate && d <= trip.EndDate);
        CapturedPercent = Math.Round(100.0 * DaysCaptured / trip.DayCount);
        JournalPercent = Math.Round(100.0 * Math.Min(trip.Entries.Count, trip.DayCount) / trip.DayCount);

        Itinerary = BuildItinerary(trip);
    }

    public Trip Trip { get; }

    public Uri AnimationSource { get; }

    public IReadOnlyList<ItineraryStop> Itinerary { get; }

    public int DaysCaptured { get; }

    public double CapturedPercent { get; }

    public double JournalPercent { get; }

    public string CapturedSummary => $"{CapturedPercent:0}% of the trip captured";

    public string CapturedDetail => $"{DaysCaptured} of {Trip.DayCount} days have photos or a journal entry";

    public string JournalSummary => $"{Trip.Entries.Count} of {Trip.DayCount} days journaled";

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = true;

    private static string AnimationName(string tripId) => tripId switch
    {
        "kyoto" => "plane",
        "patagonia" => "mountain",
        _ => "sunrise",
    };

    private static List<ItineraryStop> BuildItinerary(Trip trip)
    {
        List<ItineraryStop> stops = new()
        {
            new(trip.StartDate, "Arrival", trip.Destination, ""),
        };

        foreach (var entry in trip.Entries.OrderBy(e => e.Date))
        {
            stops.Add(new(entry.Date, entry.Title, entry.Location, ""));
        }

        stops.Add(new(trip.EndDate, "Heading home", $"{trip.DayCount} days and {trip.DistanceKm:N0} km later", ""));

        var ordered = stops.OrderBy(s => s.Date).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i] = ordered[i] with
            {
                Day = $"Day {ordered[i].Date.DayNumber - trip.StartDate.DayNumber + 1}",
                IsLast = i == ordered.Count - 1,
            };
        }

        return ordered;
    }
}

public sealed record ItineraryStop(DateOnly Date, string Title, string Subtitle, string Glyph)
{
    public string Day { get; init; } = "";

    public bool IsLast { get; init; }
}
