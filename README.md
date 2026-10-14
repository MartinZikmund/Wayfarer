# Wayfarer

**A travel journal built with [Uno Platform](https://platform.uno) 7.0.** It's one C#/XAML codebase on the WinUI API, running on Windows, macOS, Linux, the web, Android, iOS and Apple TV.

Wayfarer keeps three trips (Lofoten, Kyoto and Patagonia) as a home dashboard of postcards, a photo gallery, a rich-text journal, magazine-style stories, an animated trip dashboard and a map of each route. It's small enough to read in an afternoon, and every screen leans on something new in Uno Platform 7.0.

It dresses up as a box of postcards (perforated stamps, postmarks, airmail edges and dashed routes) while staying a Fluent app: type-ramp text, theme brushes with Light, Dark and High Contrast, and built-in controls for everything you can click.

<p align="center">
  <img src="docs/screenshots/home-light.png" alt="Home in the light theme: a greeting with a postmark, a postcard from this day in past trips, and trip postcards with stamps and dashed routes" width="49%" />
  <img src="docs/screenshots/map-dark.png" alt="Routes in the dark theme: numbered postage stamps for each stop joined by a dashed route on a postcard map" width="49%" />
</p>

## What Uno Platform 7.0 features this shows

| Feature | Where to see it | Where it lives |
| --- | --- | --- |
| **Composition shapes and animation**: the route is a `ShapeVisual` of `CompositionLineGeometry` dashes drawn in by stepping `TrimEnd`, along a centripetal Catmull-Rom curve with a chevron on each leg, and a plane flown along it by `Vector3KeyFrameAnimation` and `ScalarKeyFrameAnimation` | Routes | [`Views/MapPage.xaml.cs`](Wayfarer/Views/MapPage.xaml.cs) |
| **Connected animation** from a trip postcard's cover to the stamp on its route. WinAppSDK uses `ConnectedAnimationService`; Uno Platform still stubs it, so the Skia heads fly a copy of the photo in a `Popup` | Home → Routes | [`Helpers/HeroTransition.cs`](Wayfarer/Helpers/HeroTransition.cs), [`Views/HomePage.xaml.cs`](Wayfarer/Views/HomePage.xaml.cs) |
| **Postcard motif kit**: perforated `PostageStamp`, `Postmark` lettered on an arc, `RouteLine`, a `RouteSketch` of each trip's real route and `PostcardCard`. All decorative: `AccessibilityView="Raw"` plus an automation peer with no children, so screen readers skip the lettering. Quiet when animations are off | Every page | [`Controls/Postcard`](Wayfarer/Controls/Postcard), [`Styles/Postcard.xaml`](Wayfarer/Styles/Postcard.xaml) |
| **ItemsRepeater** with `UniformGridLayout` and `AdaptiveTrigger` reflow | Home | [`Views/HomePage.xaml`](Wayfarer/Views/HomePage.xaml) |
| **LinedFlowLayout** in an `ItemsView`, using `LineHeight`, `LineSpacing`, `ItemsJustification`, `ItemsStretch` and `ItemsInfoRequested` aspect ratios | Gallery | [`Views/GalleryPage.xaml`](Wayfarer/Views/GalleryPage.xaml), [`GalleryPage.xaml.cs`](Wayfarer/Views/GalleryPage.xaml.cs) |
| **RichEditBox** with bold, italic, underline, strikethrough, font colour, links, undo/redo, and RTF import/export through `Document.GetText`/`SetText` | Journal | [`Views/JournalEntryEditor.xaml`](Wayfarer/Views/JournalEntryEditor.xaml), [`JournalEntryEditor.xaml.cs`](Wayfarer/Views/JournalEntryEditor.xaml.cs), [`Helpers/RtfBuilder.cs`](Wayfarer/Helpers/RtfBuilder.cs) |
| **RichTextBlock** flowing through **RichTextBlockOverflow** columns, with an `InlineUIContainer` map pin, styled place names and hyperlinks | Stories | [`Views/StoriesPage.xaml`](Wayfarer/Views/StoriesPage.xaml), [`Helpers/StoryComposer.cs`](Wayfarer/Helpers/StoryComposer.cs) |
| **AnimatedVisualPlayer** playing Lottie JSON with no extra package or UnoFeature | Moments | [`Views/MomentCard.xaml`](Wayfarer/Views/MomentCard.xaml), [`Assets/Lottie`](Wayfarer/Assets/Lottie) |
| **ProgressRing** using WinUI's own animation, as a determinate ring with a track | Moments | [`Views/MomentCard.xaml`](Wayfarer/Views/MomentCard.xaml) |
| **TitleBar** with `ExtendsContentIntoTitleBar`/`SetTitleBar`, hosting search, back and the pane toggle | Every page | [`Views/MainPage.xaml`](Wayfarer/Views/MainPage.xaml), [`MainPage.xaml.cs`](Wayfarer/Views/MainPage.xaml.cs) |
| **Smooth scrolling** with native flings and smooth wheel scrolling in `ScrollViewer` and `ScrollView` | Gallery, Stories, Moments | No code needed |
| **Multi-window**: "Open in new window" gives each entry its own window, and on iPad each window is its own UIKit scene | Journal, ⋯ menu | [`Helpers/EntryWindow.cs`](Wayfarer/Helpers/EntryWindow.cs), [`Platforms/iOS/SceneDelegate.iOS.cs`](Wayfarer/Platforms/iOS/SceneDelegate.iOS.cs), [`Platforms/iOS/Info.plist`](Wayfarer/Platforms/iOS/Info.plist) |
| **tvOS** head (`net10.0-tvos`) with `XYFocusKeyboardNavigation="Enabled"` for the Siri Remote | The whole shell | [`Views/MainPage.xaml`](Wayfarer/Views/MainPage.xaml), [`Platforms/tvOS`](Wayfarer/Platforms/tvOS) |
| **Accessibility**: TalkBack and VoiceOver through automation peers, with names on icon-only controls and labelled value pairs | Everywhere, e.g. Settings › Rendering | `AutomationProperties` throughout [`Views`](Wayfarer/Views) |
| **Theme-aware WebAssembly splash** that follows the browser's light or dark theme | Web | `UnoSplashScreenBackgroundColor` / `UnoSplashScreenDarkBackgroundColor` in [`Wayfarer.csproj`](Wayfarer/Wayfarer.csproj) |
| **WebGPU** rendering backend (opt-in) | Any page | See [Opt into WebGPU](#opt-into-webgpu) |

It also follows WinUI's Fluent guidance throughout: type-ramp styles, `{ThemeResource}` brushes with Light, Dark and High Contrast support, a 4px spacing grid, and `x:Bind` with explicit modes.

## Screenshots

| | Light | Dark |
| --- | --- | --- |
| Home | ![Home, light](docs/screenshots/home-light.png) | ![Home, dark](docs/screenshots/home-dark.png) |
| Routes | ![Routes, light](docs/screenshots/map-light.png) | ![Routes, dark](docs/screenshots/map-dark.png) |
| Gallery | ![Gallery, light](docs/screenshots/gallery-light.png) | ![Gallery, dark](docs/screenshots/gallery-dark.png) |
| Journal | ![Journal, light](docs/screenshots/journal-light.png) | ![Journal, dark](docs/screenshots/journal-dark.png) |
| Stories | ![Stories, light](docs/screenshots/stories-light.png) | ![Stories, dark](docs/screenshots/stories-dark.png) |
| Moments | ![Moments, light](docs/screenshots/moments-light.png) | ![Moments, dark](docs/screenshots/moments-dark.png) |
| Settings | ![Settings, light](docs/screenshots/settings-light.png) | ![Settings, dark](docs/screenshots/settings-dark.png) |

<p align="center">
  <img src="docs/screenshots/home-to-map-dark.png" alt="Halfway through the connected animation: the Lofoten cover photo flies from Home towards the stamp on the Routes page while the route draws in" width="70%" /><br />
  <em>Open the route from a postcard and its cover flies to the stamp on the map.</em>
</p>

<p align="center">
  <img src="docs/screenshots/journal-entry-window-dark.png" alt="A journal entry open in its own window" width="40%" />
  <img src="docs/screenshots/home-narrow-dark.png" alt="Home in a narrow window, with the cards stacked" width="18%" />
  <img src="docs/screenshots/map-narrow-dark.png" alt="Routes in a narrow window, with the stop details under the map" width="18%" />
  <img src="docs/screenshots/stories-narrow-dark.png" alt="A story in a narrow window, in one column" width="18%" />
</p>

<p align="center">
  <img src="docs/screenshots/wasm-home-light.png" alt="Home running in the browser on WebAssembly" width="49%" />
  <img src="docs/screenshots/wasm-map-light.png" alt="Routes running in the browser on WebAssembly" width="49%" /><br />
  <em>The same app in the browser, on WebAssembly.</em>
</p>

## Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). The Uno.Sdk version is pinned in [`global.json`](global.json) and restores on its own. Run `uno-check` from [Uno Platform's setup guide](https://platform.uno/docs/articles/get-started.html) to install the workloads for mobile and web.

| Target | Command | Notes |
| --- | --- | --- |
| **Desktop** (Windows, macOS, Linux) | `dotnet run --project Wayfarer -f net10.0-desktop` | Skia renderer, native window chrome |
| **Web** (WebAssembly) | `dotnet run --project Wayfarer -f net10.0-browserwasm` | Opens at http://localhost:5000 |
| **Android** | `dotnet build Wayfarer -f net10.0-android -t:Run` | With an emulator running or a device attached |
| **iOS / iPadOS** | `dotnet build Wayfarer -f net10.0-ios -t:Run` | On a Mac with Xcode. Use an iPad to try multi-window |
| **Apple TV** | `dotnet build Wayfarer -f net10.0-tvos -t:Run` | On a Mac with Xcode |
| **Windows** (WinAppSDK) | Open `Wayfarer.sln` in Visual Studio and run the *Wayfarer (WinAppSDK Packaged)* profile | Native WinUI 3, no Uno renderer |

Visual Studio, Rider and VS Code (with the Uno Platform extension) also pick up the launch profiles in [`Properties/launchSettings.json`](Wayfarer/Properties/launchSettings.json).

## Opt into WebGPU

Skia renders by default. To try the WebGPU backend:

1. Add `WebGpu` to the features in [`Wayfarer.csproj`](Wayfarer/Wayfarer.csproj). Skia stays in the app, and the runtime picks a backend when the host starts:

   ```xml
   <UnoFeatures>
     Mvvm;
     SkiaRenderer;
     WebGpu;
   </UnoFeatures>
   ```

2. Register the backend on the host builder in the head's `Program.cs` (for example [`Platforms/Desktop/Program.cs`](Wayfarer/Platforms/Desktop/Program.cs) or [`Platforms/WebAssembly/Program.cs`](Wayfarer/Platforms/WebAssembly/Program.cs)), before `Build()`:

   ```csharp
   var builder = UnoPlatformHostBuilder.Create()
       .App(() => new App())
       .UseWebAssembly();

   builder.GraphicsBackend(Uno.UI.Composition.WebGpu.WebGpuBackend.CreateGraphicsProvider());
   builder.GeometryFactory(Uno.UI.Composition.Managed.ManagedBackend.CreateGeometryFactory());

   await builder.Build().RunAsync();
   ```

On the web you need a browser with WebGPU enabled. The WebAssembly build links Dawn on its own once the feature is on.

## Good to know

- **Your journal stays on your device.** The trips, photos and starting entries ship in [`Assets/Data/trips.json`](Wayfarer/Assets/Data/trips.json). New, edited and deleted entries are saved to the app's local folder (IndexedDB in the browser) and replayed over that catalog. Nothing syncs between devices.
- **Multi-window needs an iPad on Apple devices.** iPhone has no multi-scene support, so the "Open in new window" command is hidden there. Desktop heads always offer it. Android and the web don't offer it at all.
- **The trips are made up.** The photos are stock images from Unsplash, so captions describe what's in the frame rather than naming landmarks.

## Credits and license

Photos come from [Unsplash](https://unsplash.com) via [Lorem Picsum](https://picsum.photos). The animations and app artwork were made for Wayfarer. Each photographer is credited in [CREDITS.md](CREDITS.md).

The code is under the [MIT License](LICENSE). Copyright © 2026 Martin Zikmund.
