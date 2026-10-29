using Avalonia;
using Avalonia.Controls;

namespace CascadiaUI.Controls;

/// <summary>
/// Wraps content and, while <see cref="IsBusy"/>, dims it behind a scrim with a spinning
/// <see cref="ProgressRing"/> and an optional <see cref="BusyText"/> - for work with no
/// meaningful progress percentage (scans, renders, network round-trips).
/// </summary>
public class BusyArea : ContentControl
{
    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<BusyArea, bool>(nameof(IsBusy));

    public static readonly StyledProperty<string?> BusyTextProperty =
        AvaloniaProperty.Register<BusyArea, string?>(nameof(BusyText));

    public bool IsBusy
    {
        get => GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public string? BusyText
    {
        get => GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    static BusyArea()
    {
        IsBusyProperty.Changed.AddClassHandler<BusyArea>((area, e) => area.PseudoClasses.Set(":busy", e.GetNewValue<bool>()));
    }
}
