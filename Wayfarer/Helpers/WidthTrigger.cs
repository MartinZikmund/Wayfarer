namespace Wayfarer.Helpers;

/// <summary>
/// Like <see cref="AdaptiveTrigger"/>, but measures an element instead of the window, so an open navigation pane
/// counts against the room a page actually has.
/// </summary>
public sealed partial class WidthTrigger : StateTriggerBase
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(WidthTrigger), new PropertyMetadata(null, OnTargetChanged));

    public static readonly DependencyProperty MinWidthProperty =
        DependencyProperty.Register(nameof(MinWidth), typeof(double), typeof(WidthTrigger), new PropertyMetadata(0.0, (d, _) => ((WidthTrigger)d).Update()));

    public static readonly DependencyProperty MaxWidthProperty =
        DependencyProperty.Register(nameof(MaxWidth), typeof(double), typeof(WidthTrigger), new PropertyMetadata(double.PositiveInfinity, (d, _) => ((WidthTrigger)d).Update()));

    public FrameworkElement? Target
    {
        get => (FrameworkElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public double MinWidth
    {
        get => (double)GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    /// <summary>Exclusive upper bound.</summary>
    public double MaxWidth
    {
        get => (double)GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        WidthTrigger trigger = (WidthTrigger)d;
        if (e.OldValue is FrameworkElement oldTarget)
        {
            oldTarget.SizeChanged -= trigger.OnTargetSizeChanged;
        }

        if (e.NewValue is FrameworkElement newTarget)
        {
            newTarget.SizeChanged += trigger.OnTargetSizeChanged;
        }

        trigger.Update();
    }

    private void OnTargetSizeChanged(object sender, SizeChangedEventArgs e) => Update();

    // Inactive until the target has been measured, so the page starts in its default layout.
    private void Update() =>
        SetActive(Target is { ActualWidth: > 0 and var width } && width >= MinWidth && width < MaxWidth);
}
