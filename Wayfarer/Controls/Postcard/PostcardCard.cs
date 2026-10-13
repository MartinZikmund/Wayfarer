namespace Wayfarer.Controls.Postcard;

/// <summary>
/// A card dressed as a postcard: warm paper sheen, an optional airmail-striped top edge and a stamp slot
/// in the top-right corner. Styled by <c>WayfarerPostcardCardStyle</c> (applied implicitly).
/// </summary>
public sealed partial class PostcardCard : ContentControl
{
    public static readonly DependencyProperty StampProperty =
        DependencyProperty.Register(nameof(Stamp), typeof(object), typeof(PostcardCard), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowAirmailEdgeProperty =
        DependencyProperty.Register(nameof(ShowAirmailEdge), typeof(bool), typeof(PostcardCard), new PropertyMetadata(true, OnShowAirmailEdgeChanged));

    private FrameworkElement? _airmailEdge;

    public PostcardCard()
    {
        DefaultStyleKey = typeof(PostcardCard);
    }

    /// <summary>Decoration for the top-right slot, typically a <see cref="PostageStamp"/> or <see cref="Postmark"/>.</summary>
    public object? Stamp
    {
        get => GetValue(StampProperty);
        set => SetValue(StampProperty, value);
    }

    /// <summary>Red and blue airmail stripes along the top edge. They vanish in High Contrast.</summary>
    public bool ShowAirmailEdge
    {
        get => (bool)GetValue(ShowAirmailEdgeProperty);
        set => SetValue(ShowAirmailEdgeProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _airmailEdge = GetTemplateChild("AirmailEdge") as FrameworkElement;
        UpdateAirmailEdge();
    }

    private static void OnShowAirmailEdgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PostcardCard)d).UpdateAirmailEdge();

    private void UpdateAirmailEdge()
    {
        if (_airmailEdge is not null)
        {
            _airmailEdge.Visibility = ShowAirmailEdge ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
