namespace Wayfarer.Models;

public enum SearchResultKind
{
    Trip,
    Entry,
    Photo,
}

/// <param name="TargetId">Trip, entry or photo id, passed as the navigation parameter.</param>
public sealed record SearchResult(SearchResultKind Kind, string Title, string Subtitle, string Glyph, AppPage TargetPage, string TargetId);
