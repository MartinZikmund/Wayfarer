using System.Text.Json.Serialization;

namespace Wayfarer.Models;

/// <summary>Shape of Assets/Data/trips.json.</summary>
public sealed record TripCatalog
{
    public IReadOnlyList<Trip> Trips { get; init; } = [];

    public IReadOnlyList<JournalEntry> Entries { get; init; } = [];

    public IReadOnlyList<Photo> Photos { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(TripCatalog))]
internal sealed partial class WayfarerJsonContext : JsonSerializerContext
{
}
