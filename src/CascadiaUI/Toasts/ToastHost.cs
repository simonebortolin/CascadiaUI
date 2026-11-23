using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace CascadiaUI.Toasts;

/// <summary>
/// Overlay that shows a <see cref="ToastManager"/>'s toasts stacked in a corner. Put it last in
/// a <c>Panel</c> spanning the view so it floats above everything; it lets clicks through
/// everywhere except on the toasts themselves.
/// </summary>
public partial class ToastHost : TemplatedControl
{
    [GeneratedStyledProperty]
    public partial ToastManager? Manager { get; set; }

    [GeneratedStyledProperty(DefaultValue = HorizontalAlignment.Right)]
    public partial HorizontalAlignment ToastHorizontalAlignment { get; set; }

    [GeneratedStyledProperty(DefaultValue = VerticalAlignment.Bottom)]
    public partial VerticalAlignment ToastVerticalAlignment { get; set; }

    public ToastHost()
    {
        AddHandler(PointerReleasedEvent, OnToastReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnToastReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Manager is not { } manager || e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        for (var v = e.Source as Visual; v != null && v != this; v = v.GetVisualParent())
        {
            if (v is Control { DataContext: Toast toast } control && control.Classes.Contains("cascadia-toast"))
            {
                if (toast.DismissOnClick)
                {
                    manager.Dismiss(toast);
                }

                return;
            }
        }
    }
}
