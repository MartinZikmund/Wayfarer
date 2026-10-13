using Foundation;
using Uno.UI.Runtime.AppleUIKit;

namespace Wayfarer.iOS;

// Registered as UISceneDelegateClassName in Info.plist; gives every activated Window its own iPad scene.
[Register("SceneDelegate")]
public class SceneDelegate : UnoUISceneDelegate
{
}
