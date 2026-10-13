using Wayfarer.Services;
using Wayfarer.Views;
#if __IOS__
using UIKit;
#endif

namespace Wayfarer.Helpers;

/// <summary>Opens a journal entry in its own window (a separate scene on iPad).</summary>
public static class EntryWindow
{
    private const int DefaultWidth = 720;
    private const int DefaultHeight = 800;

    private static readonly Dictionary<string, Window> OpenWindows = new();

    /// <summary>Desktop heads always allow it; iOS only where UIKit supports multiple scenes (iPad, not iPhone).</summary>
    public static bool IsSupported { get; private set; } = DetectSupport();

    /// <returns>False if the platform refused to create a window; the caller keeps editing inline.</returns>
    public static bool TryOpen(string entryId)
    {
        if (OpenWindows.TryGetValue(entryId, out var existing))
        {
            existing.Activate();
            return true;
        }

        Window window;
        try
        {
            window = new Window { Title = "Journal entry - Wayfarer" };
        }
        catch (InvalidOperationException)
        {
            // Uno throws when the platform can't host secondary windows; don't offer it again.
            IsSupported = false;
            return false;
        }

        var themeService = App.GetService<IThemeService>();
        EntryWindowContent content = new() { RequestedTheme = themeService.Theme };
        window.Content = content;

        void OnThemeChanged(object? sender, ElementTheme theme) => content.RequestedTheme = theme;

        themeService.ThemeChanged += OnThemeChanged;
        content.Editor.Deleted += (_, _) => window.Close();
        window.Closed += async (_, _) =>
        {
            themeService.ThemeChanged -= OnThemeChanged;
            OpenWindows.Remove(entryId);
            await content.Editor.SaveIfDirtyAsync();
        };

        OpenWindows[entryId] = window;
        content.Loaded += (_, _) => Resize(window, content);
        window.Activate();
        _ = content.Editor.LoadAsync(entryId);
        return true;
    }

    private static bool DetectSupport()
    {
#if __IOS__
        return UIApplication.SharedApplication.SupportsMultipleScenes;
#else
        return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();
#endif
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
