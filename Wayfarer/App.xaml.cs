using Microsoft.Extensions.Logging.Abstractions;
using Uno.Resizetizer;
using Wayfarer.Services;
using Wayfarer.ViewModels;
using Wayfarer.Views;
using Windows.Graphics;

namespace Wayfarer;

public partial class App : Application
{
    // Multi-pane rubric: 280 pane + content with 24px padding; title bar 48.
    private const int DefaultWindowWidth = 1200;
    private const int DefaultWindowHeight = 800;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    public static IServiceProvider Services { get; private set; } = null!;

    public static Window? MainWindow { get; private set; }

    /// <summary>Set by <see cref="InitializeLogging"/> in Debug builds; app services log through it too.</summary>
    private static ILoggerFactory? AppLoggerFactory { get; set; }

    /// <summary>True on Skia desktop (Win32, macOS, X11) and WinAppSDK, where the app owns the window chrome.</summary>
    public static bool IsDesktop { get; } =
        !OperatingSystem.IsBrowser() && !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS();

    public static T GetService<T>()
        where T : notnull => Services.GetRequiredService<T>();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window { Title = "Wayfarer" };
#if DEBUG && HAS_UNO
        MainWindow.UseStudio();
#endif
        MainWindow.SystemBackdrop = new MicaBackdrop();

        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
            rootFrame.NavigationFailed += OnNavigationFailed;

            if (IsDesktop)
            {
                rootFrame.Loaded += OnRootLoaded;
            }
        }

        GetService<IThemeService>().Initialize(MainWindow);

        if (rootFrame.Content is null)
        {
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        MainWindow.SetWindowIcon();
        MainWindow.Activate();
    }

    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        services.AddSingleton(AppLoggerFactory ?? NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        services.AddSingleton<ITripService, TripService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();

        services.AddTransient<ShellViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MapViewModel>();
        services.AddTransient<GalleryViewModel>();
        services.AddTransient<JournalViewModel>();
        services.AddTransient<StoriesViewModel>();
        services.AddTransient<MomentsViewModel>();

        return services.BuildServiceProvider();
    }

    private static void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        var root = (FrameworkElement)sender;
        root.Loaded -= OnRootLoaded;

        if (MainWindow?.AppWindow is not { } appWindow || root.XamlRoot is null)
        {
            return;
        }

        // AppWindow sizes are physical pixels. DisplayArea isn't implemented on Uno, so no centering.
        var scale = root.XamlRoot.RasterizationScale;
        appWindow.Resize(new SizeInt32((int)(DefaultWindowWidth * scale), (int)(DefaultWindowHeight * scale)));
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e) =>
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup.
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        AppLoggerFactory = factory;
        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
