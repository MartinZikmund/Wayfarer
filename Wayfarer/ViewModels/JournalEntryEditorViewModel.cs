using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>State behind one journal entry editor: title, date, trip and the save/delete plumbing.</summary>
public sealed partial class JournalEntryEditorViewModel(ITripService tripService) : ObservableObject
{
    private readonly ITripService _tripService = tripService;
    private bool _loading;

    public JournalEntry? Entry { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<Trip> Trips { get; set; } = [];

    [ObservableProperty]
    public partial string EntryTitle { get; set; } = "";

    [ObservableProperty]
    public partial DateTimeOffset? EntryDate { get; set; }

    [ObservableProperty]
    public partial Trip? SelectedTrip { get; set; }

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    public async Task<JournalEntry?> LoadAsync(string entryId)
    {
        var entry = await _tripService.GetEntryAsync(entryId);
        var trips = await _tripService.GetTripsAsync();

        _loading = true;
        Entry = entry;
        Trips = trips;
        EntryTitle = entry?.Title ?? "";
        EntryDate = entry is null ? null : new DateTimeOffset(entry.Date.ToDateTime(TimeOnly.MinValue));
        SelectedTrip = trips.FirstOrDefault(t => t.Id == entry?.TripId);
        IsDirty = false;
        _loading = false;
        return entry;
    }

    public void MarkDirty() => Touch();

    public void Unload()
    {
        _loading = true;
        Entry = null;
        EntryTitle = "";
        EntryDate = null;
        SelectedTrip = null;
        IsDirty = false;
        _loading = false;
    }

    public async Task SaveAsync(string rtf, string plainText)
    {
        if (Entry is not { } entry)
        {
            return;
        }

        var paragraphs = plainText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        JournalEntry updated = entry with
        {
            Title = string.IsNullOrWhiteSpace(EntryTitle) ? "Untitled entry" : EntryTitle.Trim(),
            Date = EntryDate is { } date ? DateOnly.FromDateTime(date.DateTime) : entry.Date,
            TripId = SelectedTrip?.Id ?? entry.TripId,
            Location = SelectedTrip is { } trip && trip.Id != entry.TripId ? trip.Destination : entry.Location,
            Paragraphs = paragraphs,
            Rtf = rtf,
        };

        await _tripService.SaveEntryAsync(updated);
        Entry = updated;
        IsDirty = false;
    }

    public async Task DeleteAsync()
    {
        if (Entry is { } entry)
        {
            Entry = null;
            IsDirty = false;
            await _tripService.DeleteEntryAsync(entry.Id);
        }
    }

    partial void OnEntryTitleChanged(string value) => Touch();

    partial void OnEntryDateChanged(DateTimeOffset? value) => Touch();

    partial void OnSelectedTripChanged(Trip? value) => Touch();

    private void Touch()
    {
        if (!_loading)
        {
            IsDirty = true;
        }
    }
}
