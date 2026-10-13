using System.Text;
using Wayfarer.Models;

namespace Wayfarer.Helpers;

/// <summary>Builds RTF suitable for RichEditBox.Document.SetText(TextSetOptions.FormatRtf, ...).</summary>
public static class RtfBuilder
{
    /// <summary>Entry body as RTF, with its highlight phrases in bold. Prefers the edited <see cref="JournalEntry.Rtf"/>.</summary>
    public static string FromEntry(JournalEntry entry)
    {
        if (entry.Rtf is { Length: > 0 } rtf)
        {
            return rtf;
        }

        // No font table, so every head renders in the editor's own platform font.
        StringBuilder sb = new(@"{\rtf1\ansi\fs21\sa180 ");
        for (int i = 0; i < entry.Paragraphs.Count; i++)
        {
            // No \par after the last paragraph, or typing at the end lands in an empty one.
            if (i > 0)
            {
                sb.Append(@"\par ");
            }

            AppendWithBold(sb, entry.Paragraphs[i], entry.Highlights);
        }

        return sb.Append('}').ToString();
    }

    private static void AppendWithBold(StringBuilder sb, string text, IReadOnlyList<string> phrases)
    {
        var index = 0;
        while (index < text.Length)
        {
            var (start, length) = FindNext(text, index, phrases);
            if (start < 0)
            {
                AppendEscaped(sb, text.AsSpan(index));
                return;
            }

            AppendEscaped(sb, text.AsSpan(index, start - index));
            sb.Append(@"{\b ");
            AppendEscaped(sb, text.AsSpan(start, length));
            sb.Append('}');
            index = start + length;
        }
    }

    private static (int Start, int Length) FindNext(string text, int from, IReadOnlyList<string> phrases)
    {
        (int Start, int Length) best = (-1, 0);
        foreach (var phrase in phrases)
        {
            var at = text.IndexOf(phrase, from, StringComparison.Ordinal);
            if (at >= 0 && (best.Start < 0 || at < best.Start))
            {
                best = (at, phrase.Length);
            }
        }

        return best;
    }

    private static void AppendEscaped(StringBuilder sb, ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            switch (c)
            {
                case '\\' or '{' or '}':
                    sb.Append('\\').Append(c);
                    break;
                case > '\x7f':
                    // RTF \u takes a signed 16-bit value, followed by an ASCII fallback char.
                    sb.Append(@"\u").Append((short)c).Append('?');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
    }
}
