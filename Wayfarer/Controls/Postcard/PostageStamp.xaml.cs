using Wayfarer.Models;
using Windows.Foundation;

namespace Wayfarer.Controls.Postcard;

/// <summary>
/// A perforated postage stamp around a photo or glyph, with an optional caption printed under the art.
/// Purely decorative: Raw to automation and transparent to input.
/// </summary>
public sealed partial class PostageStamp : UserControl
{
    private const double HoleRadius = 2.5;
    private const double HolePitch = 8;

    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(PostageStamp), new PropertyMetadata(null, OnArtChanged));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(PostageStamp), new PropertyMetadata("", OnArtChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PostageStamp), new PropertyMetadata("", OnLabelChanged));

    public static readonly DependencyProperty TintProperty =
        DependencyProperty.Register(nameof(Tint), typeof(Brush), typeof(PostageStamp), new PropertyMetadata(null));

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(nameof(Tone), typeof(StampTone), typeof(PostageStamp), new PropertyMetadata(StampTone.Accent, OnToneChanged));

    public static readonly DependencyProperty TiltProperty =
        DependencyProperty.Register(nameof(Tilt), typeof(double), typeof(PostageStamp), new PropertyMetadata(0.0, OnTiltChanged));

    public PostageStamp()
    {
        InitializeComponent();
        SizeChanged += (_, e) => Redraw(e.NewSize);
        Loaded += (_, _) => VisualStateManager.GoToState(this, Tone.ToString(), false);
        UpdateArt();
        UpdateLabel();
    }

    /// <summary>Photo printed on the stamp; wins over <see cref="Glyph"/>.</summary>
    public ImageSource? ImageSource
    {
        get => (ImageSource?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    /// <summary>Segoe Fluent Icons glyph printed on the tone when there is no image.</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>Short caption under the art, shown in capitals, e.g. a trip code like "KIX".</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Optional brush laid over the tone. Pass a {ThemeResource} so it follows the theme.</summary>
    public Brush? Tint
    {
        get => (Brush?)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    /// <summary>Theme-aware ink colour behind the art.</summary>
    public StampTone Tone
    {
        get => (StampTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    /// <summary>Rotation in degrees; a few degrees either way reads as "stuck on by hand".</summary>
    public double Tilt
    {
        get => (double)GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    private static void OnArtChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PostageStamp)d).UpdateArt();

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PostageStamp)d).UpdateLabel();

    private static void OnToneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        VisualStateManager.GoToState((PostageStamp)d, e.NewValue.ToString(), false);

    private static void OnTiltChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PostageStamp)d).TiltTransform.Angle = (double)e.NewValue;

    private void UpdateArt()
    {
        bool hasImage = ImageSource is not null;
        ArtImage.Visibility = hasImage ? Visibility.Visible : Visibility.Collapsed;
        ArtGlyph.Visibility = !hasImage && !string.IsNullOrEmpty(Glyph) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLabel()
    {
        LabelText.Text = Label?.ToUpperInvariant() ?? "";
        LabelText.Visibility = string.IsNullOrEmpty(Label) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Redraw(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        // Small stamps keep more of their face for the art.
        double inset = size.Width < 56 ? 4 : 8;
        Face.Margin = new Thickness(inset);
        Paper.Data = PostcardGeometry.PerforatedRectangle(size.Width, size.Height, HoleRadius, HolePitch);
        ArtGlyph.FontSize = Math.Max(12, Math.Round((Math.Min(size.Width, size.Height) - 2 * inset) * 0.4 / 4) * 4);
    }
}
