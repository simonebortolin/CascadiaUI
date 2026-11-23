using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace CascadiaUI.Controls;

/// <summary>An indeterminate spinner: an accent arc sweeping round a faint track.</summary>
public partial class ProgressRing : TemplatedControl
{
    [GeneratedStyledProperty(DefaultValue = true)]
    public partial bool IsActive { get; set; }

    [GeneratedStyledProperty(DefaultValue = 3)]
    public partial double StrokeThickness { get; set; }

    static ProgressRing()
    {
        IsActiveProperty.Changed.AddClassHandler<ProgressRing>((ring, e) => ring.PseudoClasses.Set(":active", e.GetNewValue<bool>()));
    }

    public ProgressRing()
    {
        PseudoClasses.Set(":active", IsActive);
    }
}
