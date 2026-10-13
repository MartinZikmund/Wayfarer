using System.Text.Json.Serialization;

namespace Wayfarer.Models;

/// <summary>Shape of Assets/Data/trips.json.</summary>
public sealed record TripCatalog
{
    public IReadOnlyList<Trip> Trips { get; init; } = [];

    public IReadOnlyList<JournalEntry> Entries { get; init; } = [];

    public IReadOnlyList<Photo> Photos { get; init; } = [];
}

/// <summary>Journal changes saved on this device, applied over the bundled catalog.</summary>
public sealed record JournalEdits
{
    public IReadOnlyList<JournalEntry> Saved { get; init; } = [];

    public IReadOnlyList<string> Deleted { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(TripCatalog))]
[JsonSerializable(typeof(JournalEdits))]
internal sealed partial class WayfarerJsonContext : JsonSerializerContext
{
}
