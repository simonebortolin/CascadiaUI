using Avalonia;
using Avalonia.Controls;

namespace CascadiaUI.Controls;

/// <summary>
/// Wraps content and, while <see cref="IsBusy"/>, dims it behind a scrim with a spinning
/// <see cref="ProgressRing"/> and an optional <see cref="BusyText"/> - for work with no
/// meaningful progress percentage (scans, renders, network round-trips).
/// </summary>
public partial class BusyArea : ContentControl
{
    [GeneratedStyledProperty]
    public partial bool IsBusy { get; set; }

    [GeneratedStyledProperty]
    public partial string? BusyText { get; set; }

    static BusyArea()
    {
        IsBusyProperty.Changed.AddClassHandler<BusyArea>((area, e) => area.PseudoClasses.Set(":busy", e.GetNewValue<bool>()));
    }
}
