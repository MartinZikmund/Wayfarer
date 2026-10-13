using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;

namespace Wayfarer.Controls.Postcard;

/// <summary>The slanted red and blue stripes of an airmail envelope, filling the element's bounds.</summary>
public sealed partial class AirmailEdge : UserControl
{
    private const double StripeWidth = 12;
    private const double Gap = 8;

    public AirmailEdge()
    {
        InitializeComponent();
        SizeChanged += (_, e) => Redraw(e.NewSize);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeAutomationPeer(this);

    private void Redraw(Size size)
    {
        PathGeometry red = new();
        PathGeometry blue = new();
        double slant = size.Height;
        bool isRed = true;
        for (double x = -slant; x < size.Width; x += StripeWidth + Gap, isRed = !isRed)
        {
            PathFigure stripe = new() { StartPoint = new Point(x + slant, 0), IsClosed = true, IsFilled = true };
            stripe.Segments.Add(new LineSegment { Point = new Point(x + slant + StripeWidth, 0) });
            stripe.Segments.Add(new LineSegment { Point = new Point(x + StripeWidth, size.Height) });
            stripe.Segments.Add(new LineSegment { Point = new Point(x, size.Height) });
            (isRed ? red : blue).Figures.Add(stripe);
        }

        RedStripes.Data = red;
        BlueStripes.Data = blue;
    }
}
