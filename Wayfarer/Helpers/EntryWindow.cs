using Microsoft.UI.Xaml.Automation;
using Wayfarer.Services;
using Wayfarer.Views;

namespace Wayfarer.Helpers;

/// <summary>Opens a journal entry in its own window (a separate scene on iPad).</summary>
public static class EntryWindow
{
    private const int DefaultWidth = 720;
    private const int DefaultHeight = 800;

    private static readonly Dictionary<string, Window> OpenWindows = new();

    public static bool IsSupported =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS();

    public static void Open(string entryId)
    {
        if (OpenWindows.TryGetValue(entryId, out var existing))
        {
            existing.Activate();
            return;
        }

        var themeService = App.GetService<IThemeService>();
        JournalEntryEditor editor = new() { ShowOpenInNewWindow = false };
        Grid root = new()
        {
            Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Padding = (Thickness)Application.Current.Resources["WayfarerCompactPagePadding"],
            RequestedTheme = themeService.Theme,
            Children = { editor },
        };
        AutomationProperties.SetAutomationId(root, "EntryWindowRoot");

        Window window = new()
        {
            Title = "Journal entry - Wayfarer",
            Content = root,
        };

        void OnThemeChanged(object? sender, ElementTheme theme) => root.RequestedTheme = theme;

        themeService.ThemeChanged += OnThemeChanged;
        editor.Deleted += (_, _) => window.Close();
        window.Closed += async (_, _) =>
        {
            themeService.ThemeChanged -= OnThemeChanged;
            OpenWindows.Remove(entryId);
            await editor.SaveIfDirtyAsync();
        };

        OpenWindows[entryId] = window;
        root.Loaded += (_, _) => Resize(window, root);
        window.Activate();
        _ = editor.LoadAsync(entryId);
    }

    private static void Resize(Window window, FrameworkElement root)
    {
        if (!App.IsDesktop || window.AppWindow is not { } appWindow || root.XamlRoot is null)
        {
            return;
        }

        var scale = root.XamlRoot.RasterizationScale;
        appWindow.Resize(new Windows.Graphics.SizeInt32((int)(DefaultWidth * scale), (int)(DefaultHeight * scale)));
    }
}
