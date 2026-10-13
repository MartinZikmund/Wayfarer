using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Wayfarer.Models;

namespace Wayfarer.Helpers;

/// <summary>Builds the magazine-style RichTextBlock content for a trip and finds the place names to highlight.</summary>
public static class StoryComposer
{
    public sealed record Story(IReadOnlyList<Paragraph> Blocks, string PlainText, IReadOnlyList<string> Places);

    public static string Byline(Trip? trip) => trip is null
        ? ""
        : $"{trip.Destination} · {Format.DateRange(trip.StartDate, trip.EndDate)} · {Format.Distance(trip.DistanceKm)}";

    private const double InlinePhotoWidth = 280;

    public static Story Compose(Trip trip)
    {
        List<Paragraph> blocks = new();
        StringBuilder plain = new();
        HashSet<string> places = new(StringComparer.OrdinalIgnoreCase);

        // Every paragraph contributes its text plus one separator to the highlight offsets.
        void Add(Paragraph paragraph, string text)
        {
            blocks.Add(paragraph);
            plain.Append(text).Append("\r\n");
        }

        Paragraph lede = new() { Margin = new Thickness(0, 0, 0, 12), LineHeight = 28 };
        lede.Inlines.Add(new Run { Text = trip.Summary, FontStyle = FontStyle.Italic });
        Add(lede, trip.Summary);

        foreach (JournalEntry entry in trip.Entries.OrderBy(e => e.Date))
        {
            string dateline = $"{Format.Date(entry.Date)} · {entry.Location}".ToUpperInvariant();
            Paragraph head = new() { Margin = new Thickness(0, 12, 0, 0) };
            head.Inlines.Add(new Run
            {
                Text = dateline,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
            });
            Add(head, dateline);

            Paragraph title = new() { Margin = new Thickness(0, 0, 0, 8) };
            title.Inlines.Add(new Run { Text = entry.Title, FontWeight = FontWeights.SemiBold });
            Add(title, entry.Title);

            places.Add(entry.Location);
            foreach (string phrase in entry.Highlights)
            {
                places.Add(phrase);
            }

            foreach (string text in entry.Paragraphs)
            {
                Paragraph body = new() { Margin = new Thickness(0, 0, 0, 8), LineHeight = 24 };
                body.Inlines.Add(new Run { Text = text });
                Add(body, text);
            }

            if (entry.Photos.Count > 0)
            {
                Add(PhotoParagraph(entry.Photos[0]), "");
            }
        }

        Photo? first = trip.Photos.FirstOrDefault();
        Add(CreditsParagraph(first), $"Photography: {first?.Author}. See CREDITS for sources.");
        return new Story(blocks, plain.ToString(), places.Where(p => p.Length > 0).ToList());
    }

    /// <summary>Sorted, non-overlapping ranges; the renderer expects them in text order.</summary>
    public static IReadOnlyList<TextRange> FindRanges(Story story)
    {
        List<TextRange> found = new();
        foreach (string place in story.Places)
        {
            int index = 0;
            while ((index = story.PlainText.IndexOf(place, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                found.Add(new TextRange { StartIndex = index, Length = place.Length });
                index += place.Length;
            }
        }

        List<TextRange> result = new();
        int end = 0;
        foreach (TextRange range in found.OrderBy(r => r.StartIndex))
        {
            if (range.StartIndex >= end)
            {
                result.Add(range);
                end = range.StartIndex + range.Length;
            }
        }

        return result;
    }

    private static Paragraph PhotoParagraph(Photo photo)
    {
        double height = InlinePhotoWidth / photo.AspectRatio;
        Grid card = new() { Width = InlinePhotoWidth, RowSpacing = 4, Margin = new Thickness(0, 4, 0, 4) };
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

    private static Paragraph CreditsParagraph(Photo? first)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0, 12, 0, 0) };
        paragraph.Inlines.Add(new Run { Text = "Photography: " });
        if (first is not null && Uri.TryCreate(first.SourceUrl, UriKind.Absolute, out Uri? uri))
        {
            Hyperlink link = new() { NavigateUri = uri };
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
