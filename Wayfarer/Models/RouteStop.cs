using System.Text.Json.Serialization;

namespace Wayfarer.Models;

/// <summary>A stop on a trip's route, placed on the trip's own map frame.</summary>
public sealed record RouteStop
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>1-based trip day; day 1 is <see cref="Trip.StartDate"/>.</summary>
    public int Day { get; init; }

    /// <summary>Horizontal position, 0 (west) to 1 (east), normalized to the trip's map frame.</summary>
    public double X { get; init; }

    /// <summary>Vertical position, 0 (north) to 1 (south), normalized to the trip's map frame.</summary>
    public double Y { get; init; }

    public string? EntryId { get; init; }

    public IReadOnlyList<string> PhotoIds { get; init; } = [];

    /// <summary>Resolved by the trip service from <see cref="Day"/>.</summary>
    [JsonIgnore]
    public DateOnly Date { get; init; }

    /// <summary>Resolved by the trip service; null when the stop has no entry (or it was deleted).</summary>
    [JsonIgnore]
    public JournalEntry? Entry { get; init; }

    /// <summary>Resolved by the trip service from <see cref="PhotoIds"/>.</summary>
    [JsonIgnore]
    public IReadOnlyList<Photo> Photos { get; init; } = [];
}
