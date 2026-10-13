using System.ComponentModel;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Hosting;
using Wayfarer.Helpers;
using Wayfarer.Models;
using Wayfarer.ViewModels;
using Windows.UI.ViewManagement;

namespace Wayfarer.Views;

public sealed partial class MapPage : Page
{
    /// <summary>Name Home gives its connected animation when it opens a trip here.</summary>
    public const string TripHeroAnimationKey = "TripHero";

    private const double WideLayoutWidth = 840;
    private const double StampWidth = 56;
    private const double StampHeight = 64;
    private const double EdgeInset = 40;
    private const double GridStep = 48;
    private const double MinSpan = 0.4;
    private const int SamplesPerLeg = 14;
    private const float DashLength = 8;
    private const float DashPeriod = 15;
    private const int DrawMilliseconds = 1800;
    private const float PlaneSize = 24;

    // Uno's Skia renderer treats TrimStart = TrimEnd = 0 as "no trim" and draws the whole dash, so a hidden
    // dash is trimmed to almost nothing instead. Flat caps keep that sliver invisible.
    private const float HiddenTrim = 0.001f;

    private readonly AccessibilitySettings _accessibility = new();
    private bool _isWide = true;
    private bool _animateNextBuild;
    private EventHandler<object>? _drawIn;

    public MapPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => Rebuild(false));
        ElementCompositionPreview.SetIsTranslationEnabled(Plane, true);
    }

    public MapViewModel ViewModel { get; } = App.GetService<MapViewModel>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string)
        {
            CompleteHeroAnimation();
        }

        await ViewModel.OnNavigatedToAsync(e.Parameter);
        BuildSelector();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OnUnloaded(sender, e);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        BuildSelector();
        Rebuild(true);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        StopDrawIn();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MapViewModel.SelectedTrip):
                SyncSelector();
                Rebuild(true);
                break;
            case nameof(MapViewModel.SelectedStop):
                UpdateStopStyles();
                break;
        }
    }

    private void BuildSelector()
    {
        if (TripSelector.Items.Count == ViewModel.Trips.Count)
        {
            SyncSelector();
            return;
        }

        TripSelector.Items.Clear();
        foreach (Trip trip in ViewModel.Trips)
        {
            TripSelector.Items.Add(new SelectorBarItem { Text = trip.Title, Tag = trip.Id });
        }

        SyncSelector();
    }

    private void SyncSelector()
    {
        SelectorBarItem? item = TripSelector.Items.FirstOrDefault(i => (string?)i.Tag == ViewModel.SelectedTrip?.Id);
        if (item is not null && TripSelector.SelectedItem != item)
        {
            TripSelector.SelectedItem = item;
        }
    }

    private void OnTripSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is string id && ViewModel.Trips.FirstOrDefault(t => t.Id == id) is { } trip)
        {
            ViewModel.SelectedTrip = trip;
        }
    }

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool wide = e.NewSize.Width >= WideLayoutWidth;
        if (wide == _isWide)
        {
            return;
        }

        _isWide = wide;
        PanelColumn.Width = wide ? new GridLength(320) : new GridLength(0);
        Grid.SetColumn(DetailCard, wide ? 1 : 0);
        Grid.SetRow(DetailCard, wide ? 0 : 1);
        ContentGrid.ColumnSpacing = wide ? 16 : 0;
    }

    private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double available = e.NewSize.Height - ContentGrid.Margin.Bottom;
        MapCard.Height = Math.Max(_isWide ? 360 : 320, _isWide ? available : available * 0.7);
    }

    private void OnMapSizeChanged(object sender, SizeChangedEventArgs e) => Rebuild(false);

    private void Rebuild(bool animate)
    {
        _animateNextBuild |= animate;
        double width = MapArea.ActualWidth;
        double height = MapArea.ActualHeight;
        IReadOnlyList<RouteStop> stops = ViewModel.Stops;
        StopsLayer.Children.Clear();
        if (width < 200 || height < 160 || stops.Count == 0)
        {
            return;
        }

        bool animateNow = _animateNextBuild && Motion.IsEnabled;
        _animateNextBuild = false;

        DrawDecoration(width, height);

        Vector2[] centers = Layout(stops, (float)width, (float)height);

        for (int i = 0; i < stops.Count; i++)
        {
            Button button = CreateStopButton(stops[i], i + 1);
            Canvas.SetLeft(button, centers[i].X - StampWidth / 2);
            Canvas.SetTop(button, centers[i].Y - StampHeight / 2);
            StopsLayer.Children.Add(button);
        }

        DrawRoute(centers, new Vector2((float)width, (float)height), animateNow);
    }

    // Stops are framed by their own bounding box (at least MinSpan of the map, so a tight cluster still spreads out),
    // then nudged apart so no two stamps sit on top of each other.
    private static Vector2[] Layout(IReadOnlyList<RouteStop> stops, float width, float height)
    {
        (double minX, double spanX) = Span(stops.Select(s => s.X));
        (double minY, double spanY) = Span(stops.Select(s => s.Y));
        float usableWidth = width - (float)EdgeInset * 2;
        float usableHeight = height - (float)EdgeInset * 2;
        Vector2[] centers = stops
            .Select(s => new Vector2(
                (float)(EdgeInset + (s.X - minX) / spanX * usableWidth),
                (float)(EdgeInset + (s.Y - minY) / spanY * usableHeight)))
            .ToArray();

        const float GapX = (float)StampWidth + 8;
        const float GapY = (float)StampHeight + 8;
        for (int pass = 0; pass < 40; pass++)
        {
            bool moved = false;
            for (int i = 0; i < centers.Length; i++)
            {
                for (int j = i + 1; j < centers.Length; j++)
                {
                    Vector2 delta = centers[j] - centers[i];
                    float overlapX = GapX - Math.Abs(delta.X);
                    float overlapY = GapY - Math.Abs(delta.Y);
                    if (overlapX <= 0 || overlapY <= 0)
                    {
                        continue;
                    }

                    // Push along the axis that needs the smaller shove.
                    Vector2 push = overlapX < overlapY
                        ? new Vector2(overlapX / 2 * (delta.X < 0 ? -1 : 1), 0)
                        : new Vector2(0, overlapY / 2 * (delta.Y < 0 ? -1 : 1));
                    centers[i] -= push;
                    centers[j] += push;
                    moved = true;
                }
            }

            for (int i = 0; i < centers.Length; i++)
            {
                centers[i] = new Vector2(
                    Math.Clamp(centers[i].X, (float)EdgeInset, width - (float)EdgeInset),
                    Math.Clamp(centers[i].Y, (float)EdgeInset, height - (float)EdgeInset));
            }

            if (!moved)
            {
                break;
            }
        }

        return centers;
    }

    private static (double Min, double Span) Span(IEnumerable<double> values)
    {
        double min = values.Min();
        double max = values.Max();
        double span = max - min;
        if (span >= MinSpan)
        {
            return (min, span);
        }

        return ((min + max) / 2 - MinSpan / 2, MinSpan);
    }

    private Button CreateStopButton(RouteStop stop, int order)
    {
        Button button = new()
        {
            Content = new Wayfarer.Controls.Postcard.PostageStamp
            {
                Width = 48,
                Height = 56,
                Glyph = Glyphs.Routes,
                Label = $"Day {stop.Day}",
                Tone = ViewModel.SelectedTrip?.StampTone ?? StampTone.Accent,
                Tilt = stop.Day % 2 == 0 ? 3 : -3,
            },
            Command = ViewModel.SelectStopCommand,
            CommandParameter = stop,
            Tag = stop,
        };
        AutomationProperties.SetName(button, $"Day {stop.Day}, {stop.Name}");
        AutomationProperties.SetAutomationId(button, $"RouteStop{order}");
        ToolTipService.SetToolTip(button, stop.Name);
        ApplyStopStyle(button);
        return button;
    }

    private void ApplyStopStyle(Button button) =>
        button.Style = (Style)Resources[ReferenceEquals(button.Tag, ViewModel.SelectedStop)
            ? "WayfarerSelectedStopButtonStyle"
            : "WayfarerStopButtonStyle"];

    private void UpdateStopStyles()
    {
        foreach (Button button in StopsLayer.Children.OfType<Button>())
        {
            ApplyStopStyle(button);
        }
    }

    // Faint contour rings and a dotted survey grid; both are ornament, so High Contrast drops them.
    private void DrawDecoration(double width, double height)
    {
        bool show = !_accessibility.HighContrast;
        Contours.Visibility = GridLines.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show)
        {
            return;
        }

        PathGeometry grid = new();
        for (double x = GridStep; x < width; x += GridStep)
        {
            grid.Figures.Add(Segment(new(x, 0), new(x, height)));
        }

        for (double y = GridStep; y < height; y += GridStep)
        {
            grid.Figures.Add(Segment(new(0, y), new(width, y)));
        }

        GridLines.Data = grid;

        // Rings are seeded by the trip so each map has its own terrain.
        // string.GetHashCode is randomized per process, so fold the id by hand to keep the terrain stable.
        int seed = (ViewModel.SelectedTrip?.Id ?? "").Aggregate(17, (hash, ch) => unchecked(hash * 31 + ch)) & int.MaxValue;
        double cx = width * (0.25 + seed % 50 / 100.0);
        double cy = height * (0.3 + seed / 50 % 40 / 100.0);
        GeometryGroup rings = new();
        for (int i = 1; i <= 4; i++)
        {
            rings.Children.Add(new EllipseGeometry
            {
                Center = new(cx, cy),
                RadiusX = i * width * 0.09,
                RadiusY = i * height * 0.11,
            });
        }

        Contours.Data = rings;
    }

    private static PathFigure Segment(Windows.Foundation.Point from, Windows.Foundation.Point to)
    {
        PathFigure figure = new() { StartPoint = from };
        figure.Segments.Add(new LineSegment { Point = to });
        return figure;
    }

    private void DrawRoute(Vector2[] centers, Vector2 size, bool animate)
    {
        StopDrawIn();
        Visual hostVisual = ElementCompositionPreview.GetElementVisual(RouteHost);
        Compositor compositor = hostVisual.Compositor;
        ShapeVisual shapes = compositor.CreateShapeVisual();
        shapes.Size = size;
        ElementCompositionPreview.SetElementChildVisual(RouteHost, shapes);

        Visual planeVisual = ElementCompositionPreview.GetElementVisual(Plane);
        planeVisual.StopAnimation("Translation");
        planeVisual.StopAnimation("RotationAngleInDegrees");
        planeVisual.StopAnimation("Opacity");
        planeVisual.Opacity = 1;
        Plane.Visibility = animate ? Visibility.Visible : Visibility.Collapsed;

        List<Vector2> line = Sample(centers);
        float[] along = new float[line.Count];
        for (int i = 1; i < line.Count; i++)
        {
            along[i] = along[i - 1] + Vector2.Distance(line[i - 1], line[i]);
        }

        float total = along[^1];
        if (total <= 0)
        {
            return;
        }

        Windows.UI.Color color = ((SolidColorBrush)BrushProbe.Background).Color;
        CompositionColorBrush stroke = compositor.CreateColorBrush(color);
        LinearEasingFunction linear = compositor.CreateLinearEasingFunction();

        List<(CompositionLineGeometry Dash, float From)> dashes = new();
        for (float start = 0; start < total; start += DashPeriod)
        {
            CompositionLineGeometry dash = compositor.CreateLineGeometry();
            dash.Start = PointAt(line, along, start, out _);
            dash.End = PointAt(line, along, Math.Min(start + DashLength, total), out _);
            CompositionSpriteShape shape = compositor.CreateSpriteShape(dash);
            shape.StrokeBrush = stroke;
            shape.StrokeThickness = 2;
            shapes.Shapes.Add(shape);

            if (!animate)
            {
                dash.TrimEnd = 1;
                continue;
            }

            dash.TrimEnd = HiddenTrim;
            dashes.Add((dash, start / total));
        }

        if (animate)
        {
            DrawIn(dashes, DashLength / total);
            FlyPlane(planeVisual, compositor, line, along, total, linear);
        }
    }

    // Each dash's TrimEnd is stepped per frame, in step with the plane. A ScalarKeyFrameAnimation on TrimEnd
    // would be the natural fit, but Uno's Skia compositor doesn't render geometry trim animations yet.
    private void DrawIn(List<(CompositionLineGeometry Dash, float From)> dashes, float span)
    {
        StopDrawIn();
        DateTimeOffset start = DateTimeOffset.UtcNow;

        void OnRendering(object? sender, object e)
        {
            float t = (float)Math.Min(1, (DateTimeOffset.UtcNow - start).TotalMilliseconds / DrawMilliseconds);
            foreach ((CompositionLineGeometry dash, float from) in dashes)
            {
                dash.TrimEnd = Math.Clamp((t - from) / span, HiddenTrim, 1);
            }

            if (t >= 1)
            {
                StopDrawIn();
            }
        }

        _drawIn = OnRendering;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopDrawIn()
    {
        if (_drawIn is not null)
        {
            CompositionTarget.Rendering -= _drawIn;
            _drawIn = null;
        }
    }

    private static void FlyPlane(Visual plane, Compositor compositor, List<Vector2> line, float[] along, float total, CompositionEasingFunction easing)
    {
        const int Samples = 48;
        Vector3KeyFrameAnimation move = compositor.CreateVector3KeyFrameAnimation();
        ScalarKeyFrameAnimation turn = compositor.CreateScalarKeyFrameAnimation();
        float previousAngle = 0;
        for (int i = 0; i <= Samples; i++)
        {
            float t = i / (float)Samples;
            Vector2 point = PointAt(line, along, total * t, out float angle);

            // The plane glyph points up; keep the angle continuous so it never spins the long way round.
            float degrees = angle * 180 / MathF.PI + 90;
            while (degrees - previousAngle > 180)
            {
                degrees -= 360;
            }

            while (degrees - previousAngle < -180)
            {
                degrees += 360;
            }

            previousAngle = degrees;
            move.InsertKeyFrame(t, new Vector3(point.X - PlaneSize / 2, point.Y - PlaneSize / 2, 0), easing);
            turn.InsertKeyFrame(t, degrees, easing);
        }

        move.Duration = turn.Duration = TimeSpan.FromMilliseconds(DrawMilliseconds);
        plane.CenterPoint = new Vector3(PlaneSize / 2, PlaneSize / 2, 0);
        plane.StartAnimation("Translation", move);
        plane.StartAnimation("RotationAngleInDegrees", turn);

        // It lands on the last stop, so fade out rather than sit under the stamp.
        ScalarKeyFrameAnimation fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0.9f, 1, easing);
        fade.InsertKeyFrame(1, 0, easing);
        fade.Duration = TimeSpan.FromMilliseconds(DrawMilliseconds);
        plane.StartAnimation("Opacity", fade);
    }

    // Centripetal Catmull-Rom through the stops: curves like a hand-drawn line, but never loops or overshoots
    // when the route doubles back (the uniform variant does).
    private static List<Vector2> Sample(Vector2[] points)
    {
        List<Vector2> result = new() { points[0] };
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 p0 = i > 0 ? points[i - 1] : 2 * p1 - p2;
            Vector2 p3 = i + 2 < points.Length ? points[i + 2] : 2 * p2 - p1;
            for (int s = 1; s <= SamplesPerLeg; s++)
            {
                result.Add(CatmullRom(p0, p1, p2, p3, s / (float)SamplesPerLeg));
            }
        }

        return result;
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
    {
        static float Knot(float t, Vector2 a, Vector2 b) => t + MathF.Max(MathF.Sqrt(Vector2.Distance(a, b)), 1e-3f);

        float t0 = 0;
        float t1 = Knot(t0, p0, p1);
        float t2 = Knot(t1, p1, p2);
        float t3 = Knot(t2, p2, p3);
        float t = t1 + (t2 - t1) * u;

        Vector2 a1 = ((t1 - t) * p0 + (t - t0) * p1) / (t1 - t0);
        Vector2 a2 = ((t2 - t) * p1 + (t - t1) * p2) / (t2 - t1);
        Vector2 a3 = ((t3 - t) * p2 + (t - t2) * p3) / (t3 - t2);
        Vector2 b1 = ((t2 - t) * a1 + (t - t0) * a2) / (t2 - t0);
        Vector2 b2 = ((t3 - t) * a2 + (t - t1) * a3) / (t3 - t1);
        return ((t2 - t) * b1 + (t - t1) * b2) / (t2 - t1);
    }

    private static Vector2 PointAt(List<Vector2> line, float[] along, float distance, out float angle)
    {
        int index = Array.BinarySearch(along, distance);
        if (index < 0)
        {
            index = ~index;
        }

        index = Math.Clamp(index, 1, line.Count - 1);
        Vector2 a = line[index - 1];
        Vector2 b = line[index];
        float span = along[index] - along[index - 1];
        float f = span <= 0 ? 0 : Math.Clamp((distance - along[index - 1]) / span, 0, 1);
        angle = MathF.Atan2(b.Y - a.Y, b.X - a.X);
        return Vector2.Lerp(a, b, f);
    }

    private void CompleteHeroAnimation()
    {
        if (!HeroTransition.TryStart(TripHeroAnimationKey, TripStamp.ArtArea, TripStamp))
        {
            Motion.Animate(TripStamp, nameof(Opacity), 0, 1, 400);
        }
    }
}
