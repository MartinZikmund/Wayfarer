using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace Wayfarer.Helpers;

/// <summary>Decorative motion that steps aside when the user turns animations off.</summary>
public static class Motion
{
    private static readonly UISettings Settings = new();

    /// <summary>Windows' "Animation effects" setting (and its equivalent on other platforms).</summary>
    public static bool IsEnabled => Settings.AnimationsEnabled;

    /// <summary>Animates a double property; returns null (and does nothing) when animations are off.</summary>
    public static Storyboard? Animate(DependencyObject target, string property, double from, double to, int milliseconds, bool dependent = false)
    {
        if (!IsEnabled)
        {
            return null;
        }

        DoubleAnimation animation = new()
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 },
            EnableDependentAnimation = dependent,
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);

        Storyboard storyboard = new();
        storyboard.Children.Add(animation);
        storyboard.Begin();
        return storyboard;
    }
}
