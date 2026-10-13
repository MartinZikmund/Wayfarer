using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Documents;
using Wayfarer.Models;
using Windows.UI.Text;

namespace Wayfarer.Helpers;

/// <summary>Builds the magazine-style RichTextBlock content for a trip.</summary>
public static class StoryComposer
{
    /// <summary>Destination, dates and distance, led by an inline map pin.</summary>
    /// <param name="accent">Brush for the pin, resolved for the page's theme.</param>
    public static Paragraph Byline(Trip trip, Brush accent)
    {
        FontIcon pin = new()
        {
            Glyph = Models.Glyphs.Trip,
            FontSize = 14,
            Foreground = accent,
            Margin = new Thickness(0, 0, 4, -2),
        };
        AutomationProperties.SetAccessibilityView(pin, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);

        Paragraph paragraph = new();
        paragraph.Inlines.Add(new InlineUIContainer { Child = pin });
        paragraph.Inlines.Add(new Run
        {
            Text = $"{trip.Destination} · {Format.DateRange(trip.StartDate, trip.EndDate)} · {Format.Distance(trip.DistanceKm)}",
        });
        return paragraph;
    }

    /// <summary>The first photo of each entry, in story order.</summary>
    public static IReadOnlyList<Photo> Photos(Trip? trip) => trip is null
        ? []
        : trip.Entries.OrderBy(e => e.Date).SelectMany(e => e.Photos.Take(1)).ToList();

    /// <summary>Text-only story blocks; inline photos in an overflow chain render unreliably on Uno, so photos sit below.</summary>
    /// <param name="accent">Brush for datelines and highlighted places, resolved for the page's theme.</param>
    public static IReadOnlyList<Paragraph> Compose(Trip trip, Brush accent, bool highlightPlaces)
    {
        List<Paragraph> blocks = new();
        List<string> places = highlightPlaces
            ? trip.Entries
                .SelectMany(e => e.Highlights.Append(e.Location))
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(p => p.Length)
                .ToList()
            : [];

        Paragraph lede = new() { Margin = new Thickness(0, 0, 0, 12), LineHeight = 28 };
        lede.Inlines.Add(new Run { Text = trip.Summary, FontStyle = FontStyle.Italic });
        blocks.Add(lede);

        foreach (JournalEntry entry in trip.Entries.OrderBy(e => e.Date))
        {
            Paragraph head = new() { Margin = new Thickness(0, 12, 0, 0) };
            head.Inlines.Add(new Run
            {
                Text = $"{Format.Date(entry.Date)} · {entry.Location}".ToUpperInvariant(),
                FontWeight = FontWeights.SemiBold,
                Foreground = accent,
            });
            blocks.Add(head);

            Paragraph title = new() { Margin = new Thickness(0, 0, 0, 8) };
            title.Inlines.Add(new Run { Text = entry.Title, FontWeight = FontWeights.SemiBold });
            blocks.Add(title);

            foreach (string text in entry.Paragraphs)
            {
                Paragraph body = new() { Margin = new Thickness(0, 0, 0, 8), LineHeight = 24 };
                AddRuns(body, text, places, accent);
                blocks.Add(body);
            }
        }

        blocks.Add(CreditsParagraph(trip.Photos.FirstOrDefault(), accent));
        return blocks;
    }

    // Places are emphasised with styled runs rather than TextHighlighters, which drift in overflow columns on Uno.
    private static void AddRuns(Paragraph paragraph, string text, IReadOnlyList<string> places, Brush accent)
    {
        int index = 0;
        while (index < text.Length)
        {
            (int start, string place) = places
                .Select(p => (Start: text.IndexOf(p, index, StringComparison.OrdinalIgnoreCase), Place: p))
                .Where(m => m.Start >= 0)
                .OrderBy(m => m.Start)
                .FirstOrDefault((-1, ""));

            if (start < 0)
            {
                paragraph.Inlines.Add(new Run { Text = text[index..] });
                return;
            }

            if (start > index)
            {
                paragraph.Inlines.Add(new Run { Text = text[index..start] });
            }

            paragraph.Inlines.Add(new Run
            {
                Text = text.Substring(start, place.Length),
                FontWeight = FontWeights.SemiBold,
                Foreground = accent,
            });
            index = start + place.Length;
        }
    }

    private static Paragraph CreditsParagraph(Photo? first, Brush accent)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0, 12, 0, 0) };
        paragraph.Inlines.Add(new Run { Text = "Photography: " });
        if (first is not null && Uri.TryCreate(first.SourceUrl, UriKind.Absolute, out Uri? uri))
        {
            Hyperlink link = new() { NavigateUri = uri, Foreground = accent };
            link.Inlines.Add(new Run { Text = first.Author });
            paragraph.Inlines.Add(link);
        }
        else
        {
            paragraph.Inlines.Add(new Run { Text = first?.Author ?? "" });
        }

        paragraph.Inlines.Add(new Run { Text = ". See CREDITS for sources." });
        return paragraph;
    }
}
