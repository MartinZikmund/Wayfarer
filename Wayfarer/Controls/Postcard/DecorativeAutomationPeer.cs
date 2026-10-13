using Microsoft.UI.Xaml.Automation.Peers;

namespace Wayfarer.Controls.Postcard;

/// <summary>
/// Hides a decorative element and everything inside it from automation. AccessibilityView="Raw" alone only
/// hides the element itself: UIA promotes its children (each arc letter, the stamp label) to the parent.
/// </summary>
internal sealed partial class DecorativeAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    protected override IList<AutomationPeer> GetChildrenCore() => new List<AutomationPeer>();

    protected override bool IsControlElementCore() => false;

    protected override bool IsContentElementCore() => false;
}
