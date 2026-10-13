namespace Wayfarer.Controls;

/// <summary>Centered glyph, title, message and optional action for empty or not-found content.</summary>
public sealed partial class EmptyState : UserControl
{
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(EmptyState), new PropertyMetadata(""));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata(""));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata(""));

    public static readonly DependencyProperty ArtProperty =
        DependencyProperty.Register(nameof(Art), typeof(object), typeof(EmptyState), new PropertyMetadata(null, OnArtChanged));

    public static readonly DependencyProperty ActionProperty =
        DependencyProperty.Register(nameof(Action), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    public EmptyState()
    {
        InitializeComponent();
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Decoration shown instead of the glyph, e.g. a postage stamp.</summary>
    public object? Art
    {
        get => GetValue(ArtProperty);
        set => SetValue(ArtProperty, value);
    }

    /// <summary>Optional content under the message, typically a Button.</summary>
    public object? Action
    {
        get => GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }

    private static void OnArtChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var state = (EmptyState)d;
        bool hasArt = e.NewValue is not null;
        state.ArtPresenter.Visibility = hasArt ? Visibility.Visible : Visibility.Collapsed;
        state.GlyphIcon.Visibility = hasArt ? Visibility.Collapsed : Visibility.Visible;
    }
}
