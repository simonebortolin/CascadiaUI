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
public class ToastHost : TemplatedControl
{
    public static readonly StyledProperty<ToastManager?> ManagerProperty =
        AvaloniaProperty.Register<ToastHost, ToastManager?>(nameof(Manager));

    public static readonly StyledProperty<HorizontalAlignment> ToastHorizontalAlignmentProperty =
        AvaloniaProperty.Register<ToastHost, HorizontalAlignment>(nameof(ToastHorizontalAlignment), HorizontalAlignment.Right);

    public static readonly StyledProperty<VerticalAlignment> ToastVerticalAlignmentProperty =
        AvaloniaProperty.Register<ToastHost, VerticalAlignment>(nameof(ToastVerticalAlignment), VerticalAlignment.Bottom);

    public ToastManager? Manager
    {
        get => GetValue(ManagerProperty);
        set => SetValue(ManagerProperty, value);
    }

    public HorizontalAlignment ToastHorizontalAlignment
    {
        get => GetValue(ToastHorizontalAlignmentProperty);
        set => SetValue(ToastHorizontalAlignmentProperty, value);
    }

    public VerticalAlignment ToastVerticalAlignment
    {
        get => GetValue(ToastVerticalAlignmentProperty);
        set => SetValue(ToastVerticalAlignmentProperty, value);
    }

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
