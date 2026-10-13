using System.Text.Json.Serialization;

namespace Wayfarer.Models;

public sealed record Trip
{
    public string Id { get; init; } = "";

    public string Title { get; init; } = "";

    public string Region { get; init; } = "";

    public string Country { get; init; } = "";

    public string Summary { get; init; } = "";

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string CoverPhotoId { get; init; } = "";

    public int DistanceKm { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Three-letter code of the nearest airport, printed on the trip's stamps, e.g. "LKN".</summary>
    public string Code { get; init; } = "";

    public StampTone StampTone { get; init; }

    /// <summary>Stops in travel order; resolved by the trip service.</summary>
    public IReadOnlyList<RouteStop> Route { get; init; } = [];

    /// <summary>Resolved by the trip service.</summary>
    [JsonIgnore]
    public IReadOnlyList<Photo> Photos { get; init; } = [];

    /// <summary>Resolved by the trip service, newest first.</summary>
    [JsonIgnore]
    public IReadOnlyList<JournalEntry> Entries { get; init; } = [];

    [JsonIgnore]
    public Photo? CoverPhoto => Photos.FirstOrDefault(p => p.Id == CoverPhotoId) ?? Photos.FirstOrDefault();

    [JsonIgnore]
    public int DayCount => EndDate.DayNumber - StartDate.DayNumber + 1;

    [JsonIgnore]
    public string Destination => string.IsNullOrEmpty(Region) ? Country : $"{Region}, {Country}";
}
