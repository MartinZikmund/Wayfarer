using System.Text.Json;
using Wayfarer.Models;
using Windows.Storage;

namespace Wayfarer.Services;

/// <summary>
/// Loads the bundled trips.json once and serves it from memory. Journal edits are kept in
/// LocalFolder/journal.json and replayed over the catalog on the next launch.
/// </summary>
public sealed class TripService(ILogger<TripService> logger) : ITripService
{
    private const string CatalogUri = "ms-appx:///Assets/Data/trips.json";
    private const string EditsFileName = "journal.json";

    private readonly ILogger _logger = logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Dictionary<string, JournalEntry> _savedEdits = new();
    private readonly HashSet<string> _deletedIds = new();
    private Dictionary<string, JournalEntry> _entries = new();
    private Dictionary<string, Photo> _photos = new();
    private List<Trip> _rawTrips = new();
    private IReadOnlyList<Trip>? _trips;

    public event EventHandler<JournalEntry>? EntrySaved;

    public event EventHandler<string>? EntryDeleted;

    public async Task<IReadOnlyList<Trip>> GetTripsAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _trips!;
    }

    public async Task<Trip?> GetTripAsync(string tripId, CancellationToken ct = default) =>
        (await GetTripsAsync(ct)).FirstOrDefault(t => t.Id == tripId);

    public async Task<IReadOnlyList<JournalEntry>> GetEntriesAsync(string? tripId = null, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _entries.Values
            .Where(e => tripId is null || e.TripId == tripId)
            .OrderByDescending(e => e.Date)
            .ToList();
    }

    public async Task<JournalEntry?> GetEntryAsync(string entryId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _entries.GetValueOrDefault(entryId);
    }

    public async Task<IReadOnlyList<Photo>> GetPhotosAsync(string? tripId = null, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _photos.Values
            .Where(p => tripId is null || p.TripId == tripId)
            .OrderBy(p => p.TakenOn)
            .ThenBy(p => p.Id, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Photo?> GetPhotoAsync(string photoId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _photos.GetValueOrDefault(photoId);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        var text = query.Trim();
        if (text.Length == 0)
        {
            return [];
        }

        bool Matches(IEnumerable<string> fields) => fields.Any(f => f.Contains(text, StringComparison.CurrentCultureIgnoreCase));

        List<SearchResult> results = new();
        results.AddRange(_trips!
            .Where(t => Matches([t.Title, t.Region, t.Country, t.Summary]))
            .Select(t => new SearchResult(SearchResultKind.Trip, t.Title, t.Destination, Glyphs.Trip, AppPage.Stories, t.Id)));
        results.AddRange(_entries.Values
            .Where(e => Matches([e.Title, e.Location, .. e.Paragraphs]))
            .OrderByDescending(e => e.Date)
            .Select(e => new SearchResult(SearchResultKind.Entry, e.Title, $"{e.Location} · {e.Date:d MMM yyyy}", Glyphs.Journal, AppPage.Journal, e.Id)));
        results.AddRange(_photos.Values
            .Where(p => Matches([p.Caption, p.Location]))
            .Select(p => new SearchResult(SearchResultKind.Photo, p.Caption, p.Location, Glyphs.Gallery, AppPage.Gallery, p.Id)));

        return results.Take(8).ToList();
    }

    public async Task SaveEntryAsync(JournalEntry entry, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        JournalEntry resolved = entry with { Photos = ResolvePhotos(entry.PhotoIds) };
        _entries[entry.Id] = resolved;
        _savedEdits[entry.Id] = entry;
        _deletedIds.Remove(entry.Id);
        BuildTrips();
        EntrySaved?.Invoke(this, resolved);
        await WriteEditsAsync();
    }

    public async Task DeleteEntryAsync(string entryId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        if (_entries.Remove(entryId))
        {
            _savedEdits.Remove(entryId);
            _deletedIds.Add(entryId);
            BuildTrips();
            EntryDeleted?.Invoke(this, entryId);
            await WriteEditsAsync();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_trips is not null)
        {
            return;
        }

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_trips is not null)
            {
                return;
            }

            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(CatalogUri));
            var json = await FileIO.ReadTextAsync(file);
            var catalog = JsonSerializer.Deserialize(json, WayfarerJsonContext.Default.TripCatalog)
                ?? throw new InvalidOperationException("trips.json is empty.");

            _photos = catalog.Photos.ToDictionary(p => p.Id);
            _entries = catalog.Entries.ToDictionary(e => e.Id, e => e with { Photos = ResolvePhotos(e.PhotoIds) });
            _rawTrips = catalog.Trips.ToList();
            await ApplyEditsAsync();
            BuildTrips();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task ApplyEditsAsync()
    {
        try
        {
            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(EditsFileName) is not StorageFile file)
            {
                return;
            }

            var json = await FileIO.ReadTextAsync(file);
            if (JsonSerializer.Deserialize(json, WayfarerJsonContext.Default.JournalEdits) is not { } edits)
            {
                return;
            }

            foreach (var id in edits.Deleted)
            {
                _deletedIds.Add(id);
                _entries.Remove(id);
            }

            foreach (var entry in edits.Saved.Where(e => !_deletedIds.Contains(e.Id)))
            {
                _savedEdits[entry.Id] = entry;
                _entries[entry.Id] = entry with { Photos = ResolvePhotos(entry.PhotoIds) };
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A corrupt or unreadable edits file shouldn't keep the bundled journal from loading.
            _logger.LogWarning(ex, "Ignoring saved journal edits that could not be read.");
        }
    }

    private async Task WriteEditsAsync()
    {
        JournalEdits edits = new() { Saved = _savedEdits.Values.ToList(), Deleted = _deletedIds.ToList() };
        var json = JsonSerializer.Serialize(edits, WayfarerJsonContext.Default.JournalEdits);

        await _writeLock.WaitAsync();
        try
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(EditsFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Could not save journal edits.");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void BuildTrips() =>
        _trips = _rawTrips
            .Select(t => t with
            {
                Photos = _photos.Values.Where(p => p.TripId == t.Id).OrderBy(p => p.TakenOn).ToList(),
                Entries = _entries.Values.Where(e => e.TripId == t.Id).OrderByDescending(e => e.Date).ToList(),
            })
            .OrderByDescending(t => t.StartDate)
            .ToList();

    private List<Photo> ResolvePhotos(IEnumerable<string> ids) =>
        ids.Select(id => _photos.GetValueOrDefault(id)).OfType<Photo>().ToList();
}
