using Microsoft.UI.Xaml.Automation.Peers;
using Wayfarer.Models;
using Windows.Foundation;

namespace Wayfarer.Controls.Postcard;

/// <summary>
/// A thumbnail of a trip's actual route: a dashed, gently smoothed line through its stops, from a filled origin dot
/// to a hollow destination ring. Decorative; the trip's stats carry the same information as text.
/// </summary>
public sealed partial class RouteSketch : UserControl
{
    private const double DotSize = 8;

    public static readonly DependencyProperty StopsProperty =
        DependencyProperty.Register(nameof(Stops), typeof(IReadOnlyList<RouteStop>), typeof(RouteSketch), new PropertyMetadata(null, (d, _) => ((RouteSketch)d).Redraw()));

    public RouteSketch()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    public IReadOnlyList<RouteStop>? Stops
    {
        get => (IReadOnlyList<RouteStop>?)GetValue(StopsProperty);
        set => SetValue(StopsProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeAutomationPeer(this);

    private void Redraw()
    {
        IReadOnlyList<RouteStop>? stops = Stops;
        double width = ActualWidth - DotSize;
        double height = ActualHeight - DotSize;
        Visibility = stops is { Count: > 1 } ? Visibility.Visible : Visibility.Collapsed;
        if (stops is not { Count: > 1 } || width <= 0 || height <= 0)
        {
            return;
        }

        double minX = stops.Min(s => s.X);
        double minY = stops.Min(s => s.Y);
        double spanX = Math.Max(stops.Max(s => s.X) - minX, 1e-3);
        double spanY = Math.Max(stops.Max(s => s.Y) - minY, 1e-3);
        Point[] points = stops
            .Select(s => new Point(DotSize / 2 + (s.X - minX) / spanX * width, DotSize / 2 + (s.Y - minY) / spanY * height))
            .ToArray();

        // Each stop pulls the line towards it and the curve passes through the midpoints between stops.
        PathFigure figure = new() { StartPoint = points[0], IsFilled = false };
        for (int i = 1; i < points.Length - 1; i++)
        {
            Point next = points[i + 1];
            figure.Segments.Add(new QuadraticBezierSegment
            {
                Point1 = points[i],
                Point2 = new Point((points[i].X + next.X) / 2, (points[i].Y + next.Y) / 2),
            });
        }

        figure.Segments.Add(new LineSegment { Point = points[^1] });
        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        Trail.Data = geometry;

        Place(StartDot, points[0]);
        Place(EndDot, points[^1]);
    }

    private static void Place(FrameworkElement element, Point centre)
    {
        Canvas.SetLeft(element, centre.X - DotSize / 2);
        Canvas.SetTop(element, centre.Y - DotSize / 2);
    }
}
