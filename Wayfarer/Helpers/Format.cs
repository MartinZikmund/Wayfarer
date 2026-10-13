using Wayfarer.Models;

namespace Wayfarer.Helpers;

/// <summary>x:Bind function helpers, e.g. <c>{x:Bind helpers:Format.DateRange(Trip.StartDate, Trip.EndDate)}</c>.</summary>
public static class Format
{
    public static Visibility VisibleIf(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility CollapsedIf(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility VisibleIfAny(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility VisibleIfNone(int count) => count > 0 ? Visibility.Collapsed : Visibility.Visible;

    public static bool Not(bool value) => !value;

    public static string Date(DateOnly date) => date.ToString("d MMM yyyy");

    public static string DateRange(DateOnly start, DateOnly end) =>
        start.Year != end.Year ? $"{start:d MMM yyyy} – {end:d MMM yyyy}"
        : start.Month != end.Month ? $"{start:d MMM} – {end:d MMM yyyy}"
        : $"{start.Day}–{end:d MMM yyyy}";

    public static string Days(int count) => count == 1 ? "1 day" : $"{count} days";

    public static string Photos(int count) => count == 1 ? "1 photo" : $"{count} photos";

    public static string Entries(int count) => count == 1 ? "1 entry" : $"{count} entries";

    public static string Distance(int km) => $"{km:N0} km";

    public static string AspectLabel(PhotoAspect aspect) => aspect switch
    {
        PhotoAspect.Panoramic => "Panorama",
        _ => aspect.ToString(),
    };
}
