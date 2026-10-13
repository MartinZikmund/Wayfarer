using System.Text.Json.Serialization;

namespace Wayfarer.Models;

public sealed record Photo
{
    public string Id { get; init; } = "";

    public string TripId { get; init; } = "";

    public string FileName { get; init; } = "";

    public string Caption { get; init; } = "";

    public string Location { get; init; } = "";

    public DateOnly TakenOn { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public PhotoAspect Aspect { get; init; }

    public string Author { get; init; } = "";

    public string SourceUrl { get; init; } = "";

    /// <summary>Width divided by height, e.g. 1.5 for a 3:2 landscape shot.</summary>
    [JsonIgnore]
    public double AspectRatio => Height == 0 ? 1 : (double)Width / Height;

    [JsonIgnore]
    public Uri ImageUri => new($"ms-appx:///Assets/Photos/{FileName}");
}
