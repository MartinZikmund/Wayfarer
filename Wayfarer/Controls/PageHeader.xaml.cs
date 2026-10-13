using Microsoft.UI.Xaml.Automation;

namespace Wayfarer.Controls;

/// <summary>
/// Page title and subtitle, a decorative accent (a postmark or stamp) and optional actions on the right.
/// The accent steps aside on narrow widths so the title keeps its room.
/// </summary>
public sealed partial class PageHeader : UserControl
{
    private const double AccentMinWidth = 520;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PageHeader), new PropertyMetadata("", OnSubtitleChanged));

    public static readonly DependencyProperty SubtitleAutomationIdProperty =
        DependencyProperty.Register(nameof(SubtitleAutomationId), typeof(string), typeof(PageHeader), new PropertyMetadata("", OnSubtitleAutomationIdChanged));

    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(nameof(Accent), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

    public static readonly DependencyProperty ActionsProperty =
        DependencyProperty.Register(nameof(Actions), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

    public PageHeader()
    {
        InitializeComponent();
        SubtitleText.Visibility = Visibility.Collapsed;
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>One warm line under the title; hidden when empty.</summary>
    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>AutomationId for the subtitle text, for UI tests that read it.</summary>
    public string SubtitleAutomationId
    {
        get => (string)GetValue(SubtitleAutomationIdProperty);
        set => SetValue(SubtitleAutomationIdProperty, value);
    }

    /// <summary>Decoration beside the title, e.g. a <see cref="Postcard.Postmark"/>. Raw to automation.</summary>
    public object? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Commands for the page, e.g. a "New entry" button.</summary>
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PageHeader)d).SubtitleText.Visibility = string.IsNullOrEmpty(e.NewValue as string) ? Visibility.Collapsed : Visibility.Visible;

    private static void OnSubtitleAutomationIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        AutomationProperties.SetAutomationId(((PageHeader)d).SubtitleText, e.NewValue as string ?? "");

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        AccentPresenter.Visibility = e.NewSize.Width < AccentMinWidth ? Visibility.Collapsed : Visibility.Visible;
}
