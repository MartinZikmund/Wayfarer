using Microsoft.UI.Xaml.Automation.Peers;
using Wayfarer.Helpers;
using Windows.Foundation;

namespace Wayfarer.Controls.Postcard;

/// <summary>
/// A dashed travel line arcing from a filled origin dot to a hollow destination dot, with an optional glyph
/// (a plane by default) riding the tip. Stretches horizontally; decorative, so Raw to automation.
/// </summary>
public sealed partial class RouteLine : UserControl
{
    public const string PlaneGlyph = "";
    public const string PinGlyph = "";

    private const double DotSize = 8;
    private const double MarkerSize = 24;

    // A plane parks this far short of the destination, so the hollow end dot still reads as "where it's headed".
    private const double PlaneParkingGap = 40;

    // Segoe Fluent's airplane points straight up; the path angle is measured from east.
    private const double PlaneHeadingOffset = 90;

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(RouteLine), new PropertyMetadata(PlaneGlyph, OnShapeChanged));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(RouteLine), new PropertyMetadata(1.0, OnShapeChanged));

    public static readonly DependencyProperty BendProperty =
        DependencyProperty.Register(nameof(Bend), typeof(double), typeof(RouteLine), new PropertyMetadata(0.7, OnShapeChanged));

    public static readonly DependencyProperty AnimateOnLoadProperty =
        DependencyProperty.Register(nameof(AnimateOnLoad), typeof(bool), typeof(RouteLine), new PropertyMetadata(false));

    public RouteLine()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) =>
        {
            if (AnimateOnLoad)
            {
                Replay();
            }
        };
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeAutomationPeer(this);

    /// <summary>Glyph at the tip of the line: <see cref="PlaneGlyph"/> (default), <see cref="PinGlyph"/>, or "" for none.</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>How much of the route is drawn, 0 to 1. The glyph sits at the tip.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Arc height as a share of the control's height: 0 is a straight line, 1 reaches the top.</summary>
    public double Bend
    {
        get => (double)GetValue(BendProperty);
        set => SetValue(BendProperty, value);
    }

    /// <summary>Draws the route in when loaded, unless animations are turned off.</summary>
    public bool AnimateOnLoad
    {
        get => (bool)GetValue(AnimateOnLoadProperty);
        set => SetValue(AnimateOnLoadProperty, value);
    }

    /// <summary>Draws the route in again from the origin; with animations off it just shows the full route.</summary>
    public void Replay()
    {
        if (Motion.Animate(this, nameof(Progress), 0, 1, 900, dependent: true) is null)
        {
            Progress = 1;
        }
    }

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RouteLine)d).Redraw();

    private void Redraw()
    {
        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= DotSize * 2 || height <= 0)
        {
            return;
        }

        bool hasMarker = !string.IsNullOrEmpty(Glyph);
        double baseline = height - DotSize / 2;
        double apex = baseline - Math.Clamp(Bend, 0, 1) * (baseline - (hasMarker ? MarkerSize / 2 : DotSize / 2));
        Point start = new(DotSize / 2, baseline);
        Point end = new(width - DotSize / 2, baseline);

        // A quadratic Bézier peaks halfway to its control point.
        Point control = new(width / 2, 2 * apex - baseline);

        bool isPlane = Glyph == PlaneGlyph;
        double planeStop = 1 - Math.Min(0.15, PlaneParkingGap / width);
        double progress = Math.Clamp(Progress, 0, 1) * (isPlane ? planeStop : 1);
        Trail.Data = PostcardGeometry.PartialArc(start, control, end, progress);
        Place(StartDot, start, DotSize);
        Place(EndDot, end, DotSize);

        Marker.Visibility = hasMarker ? Visibility.Visible : Visibility.Collapsed;
        if (hasMarker)
        {
            Point tip = PostcardGeometry.PointOnArc(start, control, end, progress);

            // A plane flies along the line; anything else (a pin) stands upright with its point on the tip.
            Place(Marker, isPlane ? tip : new Point(tip.X, tip.Y - MarkerSize / 2), MarkerSize);
            MarkerRotation.Angle = isPlane ? PostcardGeometry.AngleOnArc(start, control, end, progress) + PlaneHeadingOffset : 0;
        }
    }

    private static void Place(FrameworkElement element, Point centre, double size)
    {
        Canvas.SetLeft(element, centre.X - size / 2);
        Canvas.SetTop(element, centre.Y - size / 2);
    }
}
