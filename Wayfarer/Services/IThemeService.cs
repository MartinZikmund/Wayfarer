namespace Wayfarer.Services;

public interface IThemeService
{
    event EventHandler<ElementTheme>? ThemeChanged;

    /// <summary>Default means "follow the system".</summary>
    ElementTheme Theme { get; }

    /// <summary>Applies the persisted theme to the window content; called once at startup.</summary>
    void Initialize(Window window);

    void SetTheme(ElementTheme theme);
}
