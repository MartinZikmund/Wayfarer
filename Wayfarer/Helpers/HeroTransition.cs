#if HAS_UNO
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
#endif
using Microsoft.UI.Xaml.Media.Animation;

namespace Wayfarer.Helpers;

/// <summary>
/// Connected animation for a photo travelling from one page to the next. WinAppSDK uses
/// <see cref="ConnectedAnimationService"/>; Uno's Skia targets still stub that service, so there a copy of
/// the photo flies from the old spot to the new one in a popup above the window.
/// </summary>
public static class HeroTransition
{
#if HAS_UNO
    private const double DurationMs = 520;
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(2);

    private static Flight? _pending;
#endif

    /// <summary>Call while the source is still on screen, right before navigating.</summary>
    public static void Prepare(string key, Image source)
    {
        if (!Motion.IsEnabled || source.Source is null || source.XamlRoot is null)
        {
            return;
        }

#if HAS_UNO
        _pending?.Land();
        Rect bounds = source.TransformToVisual(null).TransformBounds(new Rect(0, 0, source.ActualWidth, source.ActualHeight));
        Flight flight = new(key, source.Source, bounds, source.XamlRoot);
        _pending = flight;

        // Nobody picked it up (navigation failed or the page has no target): put the photo down.
        DispatcherQueueTimer timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        timer.Interval = MaxWait;
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            if (ReferenceEquals(_pending, flight))
            {
                _pending = null;
                flight.Land();
            }
        };
        timer.Start();
#else
        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(key, source);
#endif
    }

    /// <summary>
    /// Lands a prepared animation on <paramref name="destination"/>. <paramref name="reveal"/> (default: the
    /// destination) stays hidden until the photo touches down. Returns false when nothing was prepared.
    /// </summary>
    public static bool TryStart(string key, FrameworkElement destination, UIElement? reveal = null)
    {
#if HAS_UNO
        if (_pending is not { } flight || flight.Key != key)
        {
            return false;
        }

        _pending = null;
        reveal ??= destination;
        reveal.Opacity = 0;
        WhenLaidOut(destination, () => flight.FlyTo(destination, reveal));
        return true;
#else
        return ConnectedAnimationService.GetForCurrentView().GetAnimation(key)?.TryStart(destination) == true;
#endif
    }

#if HAS_UNO
    private static void WhenLaidOut(FrameworkElement element, Action action)
    {
        if (element.ActualWidth > 0 && element.XamlRoot is not null)
        {
            action();
            return;
        }

        void OnLayoutUpdated(object? sender, object e)
        {
            if (element.ActualWidth > 0 && element.XamlRoot is not null)
            {
                element.LayoutUpdated -= OnLayoutUpdated;
                action();
            }
        }

        element.LayoutUpdated += OnLayoutUpdated;
    }

    /// <summary>A copy of the photo, held where it was while the next page loads, then flown to its target.</summary>
    private sealed class Flight
    {
        private readonly Rect _from;
        private readonly Border _photo;
        private readonly RotateTransform _tilt = new();
        private readonly Popup _popup;

        public Flight(string key, ImageSource source, Rect from, XamlRoot root)
        {
            Key = key;
            _from = from;
            _photo = new()
            {
                Width = from.Width,
                Height = from.Height,
                CornerRadius = (CornerRadius)Application.Current.Resources["ControlCornerRadius"],
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _tilt,
                IsHitTestVisible = false,
                Child = new Image { Source = source, Stretch = Stretch.UniformToFill },
            };
            AutomationProperties.SetAccessibilityView(_photo, AccessibilityView.Raw);
            _popup = new()
            {
                XamlRoot = root,
                Child = _photo,
                HorizontalOffset = from.X,
                VerticalOffset = from.Y,
                IsHitTestVisible = false,
                IsOpen = true,
            };
        }

        public string Key { get; }

        public void Land() => _popup.IsOpen = false;

        public void FlyTo(FrameworkElement destination, UIElement reveal)
        {
            CubicEase ease = new() { EasingMode = EasingMode.EaseInOut };
            DateTimeOffset start = DateTimeOffset.UtcNow;
            double endAngle = TotalRotation(destination);

            void OnRendering(object? sender, object e)
            {
                double t = Math.Min(1, (DateTimeOffset.UtcNow - start).TotalMilliseconds / DurationMs);
                double k = ease.Ease(t);

                // Re-read the target every frame: the new page may still be settling into place.
                Rect target = UnrotatedBounds(destination);
                _photo.Width = Lerp(_from.Width, target.Width, k);
                _photo.Height = Lerp(_from.Height, target.Height, k);
                _popup.HorizontalOffset = Lerp(_from.X, target.X, k);
                _popup.VerticalOffset = Lerp(_from.Y, target.Y, k);
                _tilt.Angle = endAngle * k;

                if (t >= 1)
                {
                    CompositionTarget.Rendering -= OnRendering;
                    reveal.Opacity = 1;
                    Land();
                }
            }

            CompositionTarget.Rendering += OnRendering;
        }
    }
    // Position of the element as if its ancestors' rotation were undone around its centre; the flying copy
    // applies the rotation itself so the photo lands square on a tilted stamp.
    private static Rect UnrotatedBounds(FrameworkElement element)
    {
        Point center = element.TransformToVisual(null).TransformPoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
        return new Rect(center.X - element.ActualWidth / 2, center.Y - element.ActualHeight / 2, element.ActualWidth, element.ActualHeight);
    }

    private static double TotalRotation(FrameworkElement element)
    {
        double angle = 0;
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { RenderTransform: RotateTransform rotate })
            {
                angle += rotate.Angle;
            }
        }

        return angle;
    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

#endif
}
