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
        if (_window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = Theme;
        }
    }
}
