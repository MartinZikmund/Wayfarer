using Windows.Foundation;

namespace Wayfarer.Controls.Postcard;

/// <summary>Geometry for the postcard motif, rebuilt whenever an element resizes.</summary>
internal static class PostcardGeometry
{
    /// <summary>A rectangle whose edges are bitten by evenly spaced semicircular perforations.</summary>
    public static PathGeometry PerforatedRectangle(double width, double height, double holeRadius, double pitch)
    {
        // Half a pixel in, so the 1px edge stroke stays inside the element.
        Point topLeft = new(0.5, 0.5);
        Point topRight = new(width - 0.5, 0.5);
        Point bottomRight = new(width - 0.5, height - 0.5);
        Point bottomLeft = new(0.5, height - 0.5);

        PathFigure figure = new() { StartPoint = topLeft, IsClosed = true, IsFilled = true };
        AddPerforatedEdge(figure, topLeft, topRight, holeRadius, pitch);
        AddPerforatedEdge(figure, topRight, bottomRight, holeRadius, pitch);
        AddPerforatedEdge(figure, bottomRight, bottomLeft, holeRadius, pitch);
        AddPerforatedEdge(figure, bottomLeft, topLeft, holeRadius, pitch);

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        return geometry;
    }

    /// <summary>A quadratic arc from start to end, cut off at <paramref name="progress"/> (0 to 1).</summary>
    public static PathGeometry PartialArc(Point start, Point control, Point end, double progress)
    {
        // De Casteljau: the first part of a split quadratic Bézier is itself a quadratic Bézier.
        double t = Math.Clamp(progress, 0, 1);
        Point c1 = Lerp(start, control, t);
        Point c2 = Lerp(control, end, t);
        Point tip = Lerp(c1, c2, t);

        PathFigure figure = new() { StartPoint = start, IsFilled = false };
        figure.Segments.Add(new QuadraticBezierSegment { Point1 = c1, Point2 = tip });

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        return geometry;
    }

    public static Point PointOnArc(Point start, Point control, Point end, double t) =>
        Lerp(Lerp(start, control, t), Lerp(control, end, t), t);

    /// <summary>Direction of travel along the arc at <paramref name="t"/>, in degrees clockwise from east.</summary>
    public static double AngleOnArc(Point start, Point control, Point end, double t)
    {
        double dx = 2 * (1 - t) * (control.X - start.X) + 2 * t * (end.X - control.X);
        double dy = 2 * (1 - t) * (control.Y - start.Y) + 2 * t * (end.Y - control.Y);
        return Math.Atan2(dy, dx) * 180 / Math.PI;
    }

    private static void AddPerforatedEdge(PathFigure figure, Point from, Point to, double radius, double pitch)
    {
        double length = Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
        double dx = (to.X - from.X) / length;
        double dy = (to.Y - from.Y) / length;
        int count = Math.Max(1, (int)Math.Round(length / pitch));
        double step = length / count;

        for (int i = 0; i < count; i++)
        {
            double centre = (i + 0.5) * step;
            figure.Segments.Add(new LineSegment { Point = new(from.X + dx * (centre - radius), from.Y + dy * (centre - radius)) });

            // Walking the outline clockwise, a counterclockwise arc bites into the paper.
            figure.Segments.Add(new ArcSegment
            {
                Point = new(from.X + dx * (centre + radius), from.Y + dy * (centre + radius)),
                Size = new(radius, radius),
                SweepDirection = SweepDirection.Counterclockwise,
            });
        }

        figure.Segments.Add(new LineSegment { Point = to });
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
