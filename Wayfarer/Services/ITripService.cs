using Wayfarer.Models;

namespace Wayfarer.Services;

public interface ITripService
{
    event EventHandler<JournalEntry>? EntrySaved;

    event EventHandler<string>? EntryDeleted;

    /// <summary>All trips, newest first, with photos and entries resolved.</summary>
    Task<IReadOnlyList<Trip>> GetTripsAsync(CancellationToken ct = default);

    Task<Trip?> GetTripAsync(string tripId, CancellationToken ct = default);

    /// <summary>Journal entries, newest first, optionally filtered to one trip.</summary>
    Task<IReadOnlyList<JournalEntry>> GetEntriesAsync(string? tripId = null, CancellationToken ct = default);

    Task<JournalEntry?> GetEntryAsync(string entryId, CancellationToken ct = default);

    /// <summary>Photos in chronological order, optionally filtered to one trip.</summary>
    Task<IReadOnlyList<Photo>> GetPhotosAsync(string? tripId = null, CancellationToken ct = default);

    Task<Photo?> GetPhotoAsync(string photoId, CancellationToken ct = default);

    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>Adds or replaces an entry and saves it on this device.</summary>
    Task SaveEntryAsync(JournalEntry entry, CancellationToken ct = default);

    /// <summary>Removes an entry, saves that on this device and raises EntryDeleted with its id.</summary>
    Task DeleteEntryAsync(string entryId, CancellationToken ct = default);
}
