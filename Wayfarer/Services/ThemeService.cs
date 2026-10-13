using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace Wayfarer.Services;

public sealed class ThemeService : IThemeService
{
    private const string SettingKey = "AppTheme";

    private Window? _window;

    public event EventHandler<ElementTheme>? ThemeChanged;

    public ElementTheme Theme { get; private set; } = ElementTheme.Default;

    public void Initialize(Window window)
    {
        _window = window;
        if (ApplicationData.Current.LocalSettings.Values[SettingKey] is string stored
            && Enum.TryParse(stored, out ElementTheme theme))
        {
            Theme = theme;
        }

        Apply();
    }

    public void SetTheme(ElementTheme theme)
    {
        if (theme == Theme)
        {
            return;
        }

        Theme = theme;
        ApplicationData.Current.LocalSettings.Values[SettingKey] = theme.ToString();
        Apply();
        ThemeChanged?.Invoke(this, theme);
    }

    private void Apply()
    {
        if (_window?.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = Theme;

        if (root.IsLoaded)
        {
            ApplyToWindowChrome(root);
        }
        else
        {
            root.Loaded += OnRootLoaded;
        }
    }

    private void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        var root = (FrameworkElement)sender;
        root.Loaded -= OnRootLoaded;
        ApplyToWindowChrome(root);
    }

    // Uno draws the extended title bar's caption buttons in a chrome element above Window.Content,
    // and AppWindowTitleBar.ButtonForegroundColor isn't implemented, so theme that ancestor too.
    private void ApplyToWindowChrome(FrameworkElement root)
    {
        FrameworkElement topmost = root;
        while (VisualTreeHelper.GetParent(topmost) is FrameworkElement parent)
        {
            topmost = parent;
        }

        if (!ReferenceEquals(topmost, root))
        {
            topmost.RequestedTheme = Theme;
        }
    }
}
