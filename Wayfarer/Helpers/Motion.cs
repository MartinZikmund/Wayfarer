using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace Wayfarer.Helpers;

/// <summary>Decorative motion that steps aside when the user turns animations off.</summary>
public static class Motion
{
    private static readonly UISettings Settings = new();

    // One running storyboard per target and property, so a new animation replaces the last instead of fighting it.
    private static readonly ConditionalWeakTable<DependencyObject, Dictionary<string, Storyboard>> Running = new();

    /// <summary>Windows' "Animation effects" setting (and its equivalent on other platforms).</summary>
    public static bool IsEnabled => Settings.AnimationsEnabled;

    /// <summary>
    /// Animates a double property, replacing any earlier <see cref="Animate"/> on the same property. Returns null
    /// when animations are off; the earlier animation is still stopped, so the caller can set the value directly.
    /// </summary>
    public static Storyboard? Animate(DependencyObject target, string property, double from, double to, int milliseconds, bool dependent = false)
    {
        Stop(target, property);
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
        Running.GetOrCreateValue(target)[property] = storyboard;
        storyboard.Begin();
        return storyboard;
    }

    /// <summary>Stops the animation <see cref="Animate"/> started on the property, so a local value shows again.</summary>
    public static void Stop(DependencyObject target, string property)
    {
        if (Running.TryGetValue(target, out Dictionary<string, Storyboard>? storyboards)
            && storyboards.Remove(property, out Storyboard? storyboard))
        {
            storyboard.Stop();
        }
    }
}
