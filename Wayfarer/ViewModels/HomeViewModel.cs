using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>A journal entry resurfaced on the home page.</summary>
public sealed record HomeMemory(JournalEntry Entry, string TripTitle, bool IsToday)
{
    public Photo? Photo => Entry.Photos.FirstOrDefault();

    public string Heading => IsToday ? "On this day" : "Closest to today";
}

/// <summary>The start page: a greeting, a memory, the entry to pick up and the trips as postcards.</summary>
public sealed partial class HomeViewModel(ITripService tripService, INavigationService navigation) : PageViewModel
{
    private static readonly string[] Quips =
    [
        "Your passport is feeling neglected.",
        "Somewhere, a mountain is waiting to be underwhelmed by your snacks.",
        "The best souvenirs are the stories you didn't plan.",
        "Pack light. Write heavy.",
        "Not all who wander are lost. Some just have no signal.",
        "Today's forecast: a strong chance of postcards.",
        "Every journal starts with a single, slightly smudged sentence.",
        "Your carry-on called. It wants adventures.",
    ];

    private static int s_lastQuip = -1;

    private readonly ITripService _tripService = tripService;
    private readonly INavigationService _navigation = navigation;

    [ObservableProperty]
    public partial string Title { get; set; } = "Welcome back, wanderer";

    [ObservableProperty]
    public partial string Subtitle { get; set; } = Quips[0];

    [ObservableProperty]
    public partial IReadOnlyList<Trip> Trips { get; set; } = [];

    /// <summary>Newest first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<JournalEntry> RecentEntries { get; set; } = [];

    /// <summary>The trip with the newest start date, or null when there are none.</summary>
    [ObservableProperty]
    public partial Trip? LatestTrip { get; set; }

    /// <summary>The newest entry, or null when the journal is empty.</summary>
    [ObservableProperty]
    public partial JournalEntry? LatestEntry { get; set; }

    [ObservableProperty]
    public partial string? LatestEntryTripTitle { get; set; }

    /// <summary>An entry from today's date in a past year, or else the closest one.</summary>
    [ObservableProperty]
    public partial HomeMemory? Memory { get; set; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;
        try
        {
            DateTime now = DateTime.Now;
            Title = $"{GreetingFor(now.Hour)}, wanderer";
            Subtitle = NextQuip();

            Trips = await _tripService.GetTripsAsync();
            var entries = await _tripService.GetEntriesAsync();
            RecentEntries = entries.Take(3).ToList();
            LatestTrip = Trips.FirstOrDefault();
            LatestEntry = entries.FirstOrDefault();
            LatestEntryTripTitle = TripTitle(LatestEntry);
            Memory = FindMemory(entries, DateOnly.FromDateTime(now));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public static string GreetingFor(int hour) => hour switch
    {
        >= 5 and < 12 => "Good morning",
        >= 12 and < 18 => "Good afternoon",
        >= 18 and < 22 => "Good evening",
        _ => "Still up",
    };

    /// <summary>Entries on today's month and day win; otherwise the one nearest to it, wrapping around the year.</summary>
    public static IEnumerable<JournalEntry> RankByNearness(IEnumerable<JournalEntry> entries, DateOnly today) =>
        entries.OrderBy(e => Distance(e.Date, today)).ThenByDescending(e => e.Date);

    private static int Distance(DateOnly a, DateOnly b)
    {
        int da = DayOfYear(a);
        int db = DayOfYear(b);
        int diff = Math.Abs(da - db);
        return Math.Min(diff, 365 - diff);
    }

    // Pinned to a non-leap year so 29 Feb doesn't shift every later date.
    private static int DayOfYear(DateOnly date) => new DateOnly(2023, date.Month, Math.Min(date.Day, DateTime.DaysInMonth(2023, date.Month))).DayOfYear;

    private static string NextQuip()
    {
        int index;
        do
        {
            index = Random.Shared.Next(Quips.Length);
        }
        while (index == s_lastQuip);

        s_lastQuip = index;
        return Quips[index];
    }

    private HomeMemory? FindMemory(IReadOnlyList<JournalEntry> entries, DateOnly today)
    {
        JournalEntry? entry = RankByNearness(entries, today).FirstOrDefault();
        return entry is null
            ? null
            : new HomeMemory(entry, TripTitle(entry) ?? "", Distance(entry.Date, today) == 0);
    }

    private string? TripTitle(JournalEntry? entry) => entry is null ? null : Trips.FirstOrDefault(t => t.Id == entry.TripId)?.Title;

    [RelayCommand]
    private void OpenTrip(Trip? trip)
    {
        if (trip is not null)
        {
            _navigation.Navigate(AppPage.Map, trip.Id);
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

    [RelayCommand]
    private async Task StartNewEntryAsync()
    {
        Trip? trip = LatestTrip;
        if (trip is null)
        {
            return;
        }

        JournalEntry entry = new()
        {
            Id = $"e{Guid.NewGuid():N}"[..9],
            TripId = trip.Id,
            Title = "Untitled entry",
            Date = DateOnly.FromDateTime(DateTime.Today),
            Location = trip.Destination,
        };

        await _tripService.SaveEntryAsync(entry);
        _navigation.Navigate(AppPage.Journal, entry.Id);
    }
}
