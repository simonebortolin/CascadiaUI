using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CascadiaUI.Controls;

/// <summary>Attached properties the Cascadia tab templates read, so styles can tweak a tab's
/// look without a dedicated TabItem subclass.</summary>
public static class TabItemAssist
{
    /// <summary>Drop shadow under the tab - used by the raised "pill" of <c>Classes="pills"</c>.</summary>
    public static readonly AttachedProperty<BoxShadows> ShadowProperty =
        AvaloniaProperty.RegisterAttached<TabItem, BoxShadows>("Shadow", typeof(TabItemAssist));

    public static BoxShadows GetShadow(TabItem tab) => tab.GetValue(ShadowProperty);
    public static void SetShadow(TabItem tab, BoxShadows value) => tab.SetValue(ShadowProperty, value);
}
