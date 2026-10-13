using System.Collections.ObjectModel;
using Wayfarer.Models;
using Wayfarer.Services;

namespace Wayfarer.ViewModels;

/// <summary>A journal list row: the entry plus the title of the trip it belongs to.</summary>
public sealed partial class JournalEntryItem : ObservableObject
{
    public JournalEntryItem(JournalEntry entry, string tripTitle)
    {
        Entry = entry;
        TripTitle = tripTitle;
    }

    [ObservableProperty]
    public partial JournalEntry Entry { get; set; }

    [ObservableProperty]
    public partial string TripTitle { get; set; }
}

public sealed partial class JournalViewModel(ITripService tripService) : PageViewModel
{
    private readonly ITripService _tripService = tripService;
    private IReadOnlyList<Trip> _trips = [];

    public ObservableCollection<JournalEntryItem> Items { get; } = new();

    [ObservableProperty]
    public partial JournalEntryItem? SelectedItem { get; set; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        // Subscribe before any await, so a Detach() that runs mid-load always wins.
        Detach();
        _tripService.EntrySaved += OnEntrySaved;
        _tripService.EntryDeleted += OnEntryDeleted;

        IsLoading = true;
        try
        {
            _trips = await _tripService.GetTripsAsync();
            var entries = await _tripService.GetEntriesAsync();

            Items.Clear();
            foreach (var entry in entries)
            {
                Items.Add(new JournalEntryItem(entry, TripTitleOf(entry.TripId)));
            }

            if (parameter is string id)
            {
                SelectedItem = Items.FirstOrDefault(i => i.Entry.Id == id);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Detach()
    {
        _tripService.EntrySaved -= OnEntrySaved;
        _tripService.EntryDeleted -= OnEntryDeleted;
    }

    [RelayCommand]
    private async Task NewEntryAsync()
    {
        var trip = _trips.FirstOrDefault(t => t.Id == SelectedItem?.Entry.TripId) ?? _trips.FirstOrDefault();
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
        SelectedItem = Items.FirstOrDefault(i => i.Entry.Id == entry.Id);
    }

    [RelayCommand]
    private void CloseEntry() => SelectedItem = null;

    private string TripTitleOf(string tripId) => _trips.FirstOrDefault(t => t.Id == tripId)?.Title ?? "";

    private void OnEntrySaved(object? sender, JournalEntry entry)
    {
        var item = Items.FirstOrDefault(i => i.Entry.Id == entry.Id);
        if (item is not null)
        {
            item.Entry = entry;
            item.TripTitle = TripTitleOf(entry.TripId);
            return;
        }

        var index = Items.TakeWhile(i => i.Entry.Date > entry.Date).Count();
        Items.Insert(index, new JournalEntryItem(entry, TripTitleOf(entry.TripId)));
    }

    private void OnEntryDeleted(object? sender, string entryId)
    {
        var item = Items.FirstOrDefault(i => i.Entry.Id == entryId);
        if (item is null)
        {
            return;
        }

        if (SelectedItem == item)
        {
            SelectedItem = null;
        }

        Items.Remove(item);
    }
}
