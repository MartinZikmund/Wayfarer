using Microsoft.UI.Xaml.Automation.Peers;
using System.Globalization;
using Wayfarer.Helpers;
using Windows.Foundation;

namespace Wayfarer.Controls.Postcard;

/// <summary>Which side of a <see cref="Postmark"/> its wavy cancellation lines run off to.</summary>
public enum PostmarkWaves
{
    None,
    Left,
    Right,
}

/// <summary>
/// A circular cancellation mark: the place lettered around the top of a double ring, day and month across the middle,
/// the year (or a caption) around the bottom, and wavy lines off to one side. Decorative and Raw to automation, so
/// pages show the same place and date as real text nearby.
/// </summary>
public sealed partial class Postmark : UserControl
{
    private const double WaveLength = 56;
    private const double WaveGap = 4;
    private const int WaveCount = 4;
    private const double WaveSpacing = 8;
    private const double WaveAmplitude = 2.5;
    private const double WavePeriod = 16;
    private const double LetterSpacing = 1.5;
    private const double WordGap = 4;

    // Distance from the outer edge to the middle of the lettering band.
    private const double TextInset = 13;

    public static readonly DependencyProperty PlaceProperty =
        DependencyProperty.Register(nameof(Place), typeof(string), typeof(Postmark), new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(Postmark), new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty DateProperty =
        DependencyProperty.Register(nameof(Date), typeof(DateOnly), typeof(Postmark), new PropertyMetadata(default(DateOnly), OnTextChanged));

    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(Postmark), new PropertyMetadata(88.0, OnShapeChanged));

    public static readonly DependencyProperty WavesProperty =
        DependencyProperty.Register(nameof(Waves), typeof(PostmarkWaves), typeof(Postmark), new PropertyMetadata(PostmarkWaves.Right, OnShapeChanged));

    public static readonly DependencyProperty TiltProperty =
        DependencyProperty.Register(nameof(Tilt), typeof(double), typeof(Postmark), new PropertyMetadata(-8.0, OnShapeChanged));

    public static readonly DependencyProperty AnimateOnLoadProperty =
        DependencyProperty.Register(nameof(AnimateOnLoad), typeof(bool), typeof(Postmark), new PropertyMetadata(false));

    public Postmark()
    {
        InitializeComponent();
        UpdateShape();
        Loaded += OnLoaded;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeAutomationPeer(this);

    /// <summary>Lettered around the top of the ring in capitals; trimmed with an ellipsis when it doesn't fit.</summary>
    public string Place
    {
        get => (string)GetValue(PlaceProperty);
        set => SetValue(PlaceProperty, value);
    }

    /// <summary>Lettering around the bottom of the ring instead of the year, e.g. a trip code.</summary>
    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    /// <summary>Postmark date; leave unset for an undated mark.</summary>
    public DateOnly Date
    {
        get => (DateOnly)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    /// <summary>Ring diameter in pixels, 88 or more so the lettering clears the date. Waves add to the width.</summary>
    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    public PostmarkWaves Waves
    {
        get => (PostmarkWaves)GetValue(WavesProperty);
        set => SetValue(WavesProperty, value);
    }

    /// <summary>Rotation in degrees around the ring's centre.</summary>
    public double Tilt
    {
        get => (double)GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    /// <summary>Stamps itself in (a quick press and fade) when loaded, unless animations are turned off.</summary>
    public bool AnimateOnLoad
    {
        get => (bool)GetValue(AnimateOnLoadProperty);
        set => SetValue(AnimateOnLoadProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Postmark)d).UpdateText();

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Postmark)d).UpdateShape();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateText();

        if (AnimateOnLoad)
        {
            Motion.Animate(Root, nameof(Opacity), 0, 1, 200);
            Motion.Animate(RootTransform, nameof(CompositeTransform.ScaleX), 1.3, 1, 360);
            Motion.Animate(RootTransform, nameof(CompositeTransform.ScaleY), 1.3, 1, 360);
        }
    }

    private void UpdateShape()
    {
        double diameter = Diameter;
        bool hasWaves = Waves != PostmarkWaves.None;
        double width = hasWaves ? diameter + WaveGap + WaveLength : diameter;
        double ringLeft = Waves == PostmarkWaves.Left ? width - diameter : 0;

        Root.Width = width;
        Root.Height = diameter;
        Ring.Width = diameter;
        Ring.Height = diameter;
        Ring.HorizontalAlignment = Waves == PostmarkWaves.Left ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        RootTransform.Rotation = Tilt;
        RootTransform.CenterX = ringLeft + diameter / 2;
        RootTransform.CenterY = diameter / 2;

        WaveLines.Visibility = hasWaves ? Visibility.Visible : Visibility.Collapsed;
        if (hasWaves)
        {
            double wavesLeft = Waves == PostmarkWaves.Left ? 0 : diameter + WaveGap;
            WaveLines.Data = BuildWaves(wavesLeft, diameter / 2);
        }

        UpdateText();
    }

    private void UpdateText()
    {
        bool hasDate = Date != default;
        DateText.Text = hasDate ? Date.ToString("d MMM", CultureInfo.InvariantCulture).ToUpperInvariant() : "";
        Centre.Visibility = hasDate ? Visibility.Visible : Visibility.Collapsed;

        if (!IsLoaded)
        {
            return;
        }

        ArcText.Children.Clear();
        double radius = Diameter / 2 - TextInset;
        string bottom = !string.IsNullOrEmpty(Caption) ? Caption : hasDate ? Date.Year.ToString(CultureInfo.InvariantCulture) : "";
        AddArcText(Place, radius, top: true, maxSweepDegrees: bottom.Length > 0 ? 200 : 250);
        AddArcText(bottom, radius, top: false, maxSweepDegrees: 120);
    }

    private void AddArcText(string text, double radius, bool top, double maxSweepDegrees)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Style style = (Style)Application.Current.Resources["WayfarerPostmarkTextBlockStyle"];
        List<TextBlock> letters = new();
        foreach (char c in text.Trim().ToUpperInvariant())
        {
            TextBlock letter = new() { Text = c.ToString(), Style = style, RenderTransformOrigin = new Point(0.5, 0.5) };
            if (c == ' ')
            {
                // A lone space measures as zero width, which would glue the words together.
                letter.Width = WordGap;
            }

            ArcText.Children.Add(letter);
            letter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            letters.Add(letter);
        }

        double available = radius * maxSweepDegrees * Math.PI / 180;
        if (Length(letters) > available)
        {
            TextBlock ellipsis = new() { Text = "…", Style = style, RenderTransformOrigin = new Point(0.5, 0.5) };
            ArcText.Children.Add(ellipsis);
            ellipsis.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            while (letters.Count > 1 && Length(letters) + LetterSpacing + ellipsis.DesiredSize.Width > available)
            {
                ArcText.Children.Remove(letters[^1]);
                letters.RemoveAt(letters.Count - 1);
            }

            // Don't leave a dangling space before the ellipsis.
            if (letters[^1].Text == " ")
            {
                ArcText.Children.Remove(letters[^1]);
                letters.RemoveAt(letters.Count - 1);
            }

            letters.Add(ellipsis);
        }

        double centre = Diameter / 2;
        double baseAngle = top ? -Math.PI / 2 : Math.PI / 2;
        double offset = -Length(letters) / 2;
        foreach (TextBlock letter in letters)
        {
            Size size = letter.DesiredSize;
            double along = offset + size.Width / 2;
            offset += size.Width + LetterSpacing;

            // Top lettering runs clockwise, bottom lettering counterclockwise, so both read left to right.
            double angle = top ? baseAngle + along / radius : baseAngle - along / radius;
            Canvas.SetLeft(letter, centre + radius * Math.Cos(angle) - size.Width / 2);
            Canvas.SetTop(letter, centre + radius * Math.Sin(angle) - size.Height / 2);
            letter.RenderTransform = new RotateTransform { Angle = angle * 180 / Math.PI + (top ? 90 : -90) };
        }
    }

    private static double Length(List<TextBlock> letters) =>
        letters.Sum(l => l.DesiredSize.Width) + LetterSpacing * Math.Max(0, letters.Count - 1);

    private static PathGeometry BuildWaves(double left, double middle)
    {
        PathGeometry geometry = new();
        for (int line = 0; line < WaveCount; line++)
        {
            double y = middle + (line - (WaveCount - 1) / 2.0) * WaveSpacing;
            PathFigure figure = new() { StartPoint = new Point(left, y), IsFilled = false };
            double half = WavePeriod / 2;
            for (double x = left, sign = -1; x + half <= left + WaveLength + 0.01; x += half, sign = -sign)
            {
                figure.Segments.Add(new QuadraticBezierSegment
                {
                    Point1 = new Point(x + half / 2, y + sign * 2 * WaveAmplitude),
                    Point2 = new Point(x + half, y),
                });
            }

            geometry.Figures.Add(figure);
        }

        return geometry;
    }
}
