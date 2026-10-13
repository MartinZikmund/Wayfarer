using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Wayfarer.Models;

namespace Wayfarer.Helpers;

/// <summary>Builds the magazine-style RichTextBlock content for a trip.</summary>
public static class StoryComposer
{
    public static string Byline(Trip? trip) => trip is null
        ? ""
        : $"{trip.Destination} · {Format.DateRange(trip.StartDate, trip.EndDate)} · {Format.Distance(trip.DistanceKm)}";

    private const double InlinePhotoWidth = 280;

    /// <param name="accent">Brush for datelines and highlighted places, resolved for the page's theme.</param>
    /// <param name="theme">Theme for inline UI, which otherwise resolves against the app theme.</param>
    public static IReadOnlyList<Paragraph> Compose(Trip trip, Brush accent, ElementTheme theme, bool highlightPlaces)
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

            if (entry.Photos.Count > 0)
            {
                blocks.Add(PhotoParagraph(entry.Photos[0], theme));
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

    private static Paragraph PhotoParagraph(Photo photo, ElementTheme theme)
    {
        double height = InlinePhotoWidth / photo.AspectRatio;
        Grid card = new()
        {
            Width = InlinePhotoWidth,
            RowSpacing = 4,
            Margin = new Thickness(0, 4, 0, 4),
            RequestedTheme = theme,
        };
        card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Border frame = new()
        {
            Style = (Style)Application.Current.Resources["WayfarerPhotoFrameStyle"],
            Width = InlinePhotoWidth,
            Height = height,
            Child = new Image
            {
                Source = new BitmapImage(photo.ImageUri),
                Stretch = Stretch.UniformToFill,
            },
        };
        AutomationProperties.SetName(frame, photo.Caption);

        TextBlock caption = new()
        {
            Text = photo.Caption,
            Style = (Style)Application.Current.Resources["WayfarerCaptionSecondaryTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(caption, 1);
        card.Children.Add(frame);
        card.Children.Add(caption);

        Paragraph paragraph = new();
        paragraph.Inlines.Add(new InlineUIContainer { Child = card });
        return paragraph;
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
