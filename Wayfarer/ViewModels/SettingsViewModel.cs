using Wayfarer.Services;

namespace Wayfarer.ViewModels;

public sealed record FeatureInfo(string Glyph, string Title, string Description);

public sealed partial class SettingsViewModel : PageViewModel
{
    private static readonly ElementTheme[] Themes = [ElementTheme.Default, ElementTheme.Light, ElementTheme.Dark];

    private readonly IThemeService _themeService;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        ThemeIndex = Math.Max(0, Array.IndexOf(Themes, themeService.Theme));
    }

    /// <summary>0 = System, 1 = Light, 2 = Dark; matches the RadioButtons order.</summary>
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    public RenderingInfo Rendering => RenderingInfo.Current;

    public string AppVersion { get; } = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public IReadOnlyList<FeatureInfo> Features { get; } =
    [
        new("", "LinedFlowLayout", "The Gallery lays out photos of mixed aspect ratios in justified rows."),
        new("", "RichEditBox", "Journal entries support formatting, undo/redo and RTF import/export."),
        new("", "RichTextBlock", "Stories flow across overflow columns, with an inline map pin and highlighted places."),
        new("", "AnimatedVisualPlayer", "Moments play Lottie animations with no extra packages."),
        new("", "TitleBar", "The WinUI TitleBar hosts search, back and pane toggle in the window chrome."),
        new("", "Smooth scrolling", "Native flings and smooth wheel scrolling in ScrollViewer and ScrollView."),
        new("", "Multi-window on iPad", "Each activated window gets its own UIKit scene."),
        new("", "Accessibility", "TalkBack and VoiceOver through automation peers."),
        new("", "Theme-aware splash", "The WebAssembly splash follows the browser's light or dark theme."),
    ];

    partial void OnThemeIndexChanged(int value)
    {
        if (value >= 0 && value < Themes.Length)
        {
            _themeService.SetTheme(Themes[value]);
        }
    }
}
