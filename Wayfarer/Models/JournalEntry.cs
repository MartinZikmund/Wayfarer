using System.Text.Json.Serialization;

namespace Wayfarer.Models;

public sealed record JournalEntry
{
    public required string Id { get; init; }

    public required string TripId { get; init; }

    public required string Title { get; init; }

    public DateOnly Date { get; init; }

    public string Location { get; init; } = "";

    public string Mood { get; init; } = "";

    /// <summary>Plain-text body, one item per paragraph.</summary>
    public IReadOnlyList<string> Paragraphs { get; init; } = [];

    /// <summary>Phrases from <see cref="Paragraphs"/> worth highlighting (e.g. via TextHighlighter).</summary>
    public IReadOnlyList<string> Highlights { get; init; } = [];

    public IReadOnlyList<string> PhotoIds { get; init; } = [];

    /// <summary>Edited rich text; when null, use <see cref="Helpers.RtfBuilder.FromEntry"/>.</summary>
    public string? Rtf { get; init; }

    /// <summary>Resolved from <see cref="PhotoIds"/> by the trip service.</summary>
    [JsonIgnore]
    public IReadOnlyList<Photo> Photos { get; init; } = [];

    [JsonIgnore]
    public string Excerpt => Paragraphs.Count > 0 ? Paragraphs[0] : "";
}
