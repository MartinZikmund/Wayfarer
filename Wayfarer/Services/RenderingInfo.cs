using System.Reflection;
using System.Runtime.InteropServices;

namespace Wayfarer.Services;

/// <summary>What the app is running on, for the Settings page.</summary>
public sealed record RenderingInfo(string Platform, string Renderer, string Runtime, string UnoVersion)
{
    public static RenderingInfo Current { get; } = Detect();

    private static RenderingInfo Detect()
    {
        var platform =
            OperatingSystem.IsBrowser() ? "WebAssembly"
            : OperatingSystem.IsAndroid() ? "Android"
            : OperatingSystem.IsTvOS() ? "tvOS"
            : OperatingSystem.IsIOS() ? "iOS / iPadOS"
            : OperatingSystem.IsMacOS() ? "macOS"
            : OperatingSystem.IsWindows() ? "Windows"
            : OperatingSystem.IsLinux() ? "Linux"
            : RuntimeInformation.OSDescription;

        return new(
            $"{platform} ({RuntimeInformation.ProcessArchitecture})",
            DetectRenderer(),
            RuntimeInformation.FrameworkDescription,
            GetUnoVersion());
    }

    private static string DetectRenderer()
    {
#if WINDOWS
        return "WinUI (Windows App SDK)";
#else
        var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToHashSet();
        return loaded.Contains("Uno.UI.Composition.WebGpu") ? "WebGPU"
            : loaded.Contains("Uno.UI.Composition.Skia") ? "Skia"
            : "Skia (default)";
#endif
    }

    private static string GetUnoVersion()
    {
        var version = typeof(Microsoft.UI.Xaml.Controls.Page).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        // Drop the "+commit" build metadata suffix.
        var plus = version.IndexOf('+');
        return plus > 0 ? version[..plus] : version;
    }
}
