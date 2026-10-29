using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Media;

namespace CascadiaUI.Controls;

/// <summary>
/// A window whose content extends into the title bar, Windows Terminal / SourceGit style.
/// </summary>
/// <remarks>
/// The caption buttons stay the platform's own (DWM on Windows, the traffic lights on macOS, the
/// theme-drawn ones on Linux), so snap layouts and accessibility keep working; the window only
/// tells its content how much room they take via the inherited <see cref="CaptionInsetsProperty"/>.
/// <para>
/// With <see cref="ExtendContentIntoTitleBar"/> off (the default) the window draws its own title bar
/// (icon, <see cref="Window.Title"/>, then <see cref="TitleBarContent"/>). Turn it on to let the
/// content own that strip - a <see cref="CascadiaTabControl"/> with
/// <see cref="CascadiaTabControl.IsInTitleBar"/> set then puts its tabs up there.
/// </para>
/// </remarks>
public class CascadiaWindow : Window
{
    public static readonly StyledProperty<bool> ExtendContentIntoTitleBarProperty =
        AvaloniaProperty.Register<CascadiaWindow, bool>(nameof(ExtendContentIntoTitleBar));

    public static readonly StyledProperty<object?> TitleBarContentProperty =
        AvaloniaProperty.Register<CascadiaWindow, object?>(nameof(TitleBarContent));

    public static readonly StyledProperty<bool> ShowTitleProperty =
        AvaloniaProperty.Register<CascadiaWindow, bool>(nameof(ShowTitle), true);

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<CascadiaWindow, double>(nameof(TitleBarHeight), 42);

    /// <summary>
    /// Room taken by the platform caption buttons on each side of the title bar (left on macOS,
    /// right elsewhere). Inherited, so anything inside the window - a title-bar tab strip in
    /// particular - can pad itself out of their way.
    /// </summary>
    public static readonly AttachedProperty<Thickness> CaptionInsetsProperty =
        AvaloniaProperty.RegisterAttached<CascadiaWindow, Control, Thickness>("CaptionInsets", inherits: true);

    /// <summary>
    /// Marks an element (and everything inside it that doesn't say otherwise) as window-drag area.
    /// Interactive children inside it should set <see cref="IsTitleBarInteractiveProperty"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTitleBarDragAreaProperty =
        AvaloniaProperty.RegisterAttached<CascadiaWindow, Control, bool>("IsTitleBarDragArea");

    /// <summary>
    /// Keeps an element clickable even though it sits in a <see cref="IsTitleBarDragAreaProperty"/> area.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTitleBarInteractiveProperty =
        AvaloniaProperty.RegisterAttached<CascadiaWindow, Control, bool>("IsTitleBarInteractive");

    static CascadiaWindow()
    {
        IsTitleBarDragAreaProperty.Changed.AddClassHandler<Control>((c, e) =>
            WindowDecorationProperties.SetElementRole(c, e.GetNewValue<bool>()
                ? WindowDecorationsElementRole.TitleBar
                : WindowDecorationsElementRole.None));

        IsTitleBarInteractiveProperty.Changed.AddClassHandler<Control>((c, e) =>
            WindowDecorationProperties.SetElementRole(c, e.GetNewValue<bool>()
                ? WindowDecorationsElementRole.User
                : WindowDecorationsElementRole.None));
    }

    public CascadiaWindow()
    {
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = TitleBarHeight;
        UpdateCaptionInsets();
    }

    /// <summary>Lets the content draw into the title bar strip (no Cascadia title bar is drawn).</summary>
    public bool ExtendContentIntoTitleBar
    {
        get => GetValue(ExtendContentIntoTitleBarProperty);
        set => SetValue(ExtendContentIntoTitleBarProperty, value);
    }

    /// <summary>Extra content placed in the title bar after the title (search box, buttons...).
    /// Interactive elements in it are made clickable automatically.</summary>
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    public bool ShowTitle
    {
        get => GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public static Thickness GetCaptionInsets(Control control) => control.GetValue(CaptionInsetsProperty);
    public static void SetCaptionInsets(Control control, Thickness value) => control.SetValue(CaptionInsetsProperty, value);
    public static bool GetIsTitleBarDragArea(Control control) => control.GetValue(IsTitleBarDragAreaProperty);
    public static void SetIsTitleBarDragArea(Control control, bool value) => control.SetValue(IsTitleBarDragAreaProperty, value);
    public static bool GetIsTitleBarInteractive(Control control) => control.GetValue(IsTitleBarInteractiveProperty);
    public static void SetIsTitleBarInteractive(Control control, bool value) => control.SetValue(IsTitleBarInteractiveProperty, value);

    /// <summary>
    /// Width reserved for the caption buttons on the side the platform puts them. Windows' and the
    /// Linux drawn decorations' three 46px buttons; macOS' traffic lights plus their margin.
    /// </summary>
    public static Thickness PlatformCaptionInsets =>
        OperatingSystem.IsMacOS() ? new Thickness(76, 0, 0, 0)
        : OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() ? new Thickness(0, 0, 140, 0)
        : default;

    protected override Type StyleKeyOverride => typeof(CascadiaWindow);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleBarHeightProperty)
        {
            ExtendClientAreaTitleBarHeightHint = change.GetNewValue<double>();
        }
        else if (change.Property == WindowStateProperty || change.Property == WindowDecorationsProperty)
        {
            UpdateCaptionInsets();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Platforms that honour WindowDecorationProperties.ElementRole start the drag natively
        // before we ever see the press; this fallback only covers those that don't (e.g. an
        // X11 window manager without _NET_WM_MOVERESIZE), and only in the drag area itself.
        if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.ClickCount != 1)
        {
            return;
        }

        if (e.Source is Visual source && IsInDragArea(source) && !OperatingSystem.IsWindows())
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    protected override void OnDoubleTapped(TappedEventArgs e)
    {
        base.OnDoubleTapped(e);

        if (!e.Handled && e.Source is Visual source && IsInDragArea(source) && !OperatingSystem.IsWindows() && CanResize)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }
    }

    private static bool IsInDragArea(Visual source)
    {
        for (Visual? v = source; v != null; v = v.GetVisualParentOrNull())
        {
            if (v is Control c)
            {
                if (GetIsTitleBarInteractive(c))
                {
                    return false;
                }

                if (GetIsTitleBarDragArea(c))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateCaptionInsets()
    {
        var insets = WindowDecorations == WindowDecorations.Full && WindowState != WindowState.FullScreen
            ? PlatformCaptionInsets
            : default;
        SetCaptionInsets(this, insets);
    }
}

internal static class VisualExtensions
{
    public static Visual? GetVisualParentOrNull(this Visual visual) => Avalonia.VisualTree.VisualExtensions.GetVisualParent(visual);
}
