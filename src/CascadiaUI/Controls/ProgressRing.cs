using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace CascadiaUI.Controls;

/// <summary>An indeterminate spinner: an accent arc sweeping round a faint track.</summary>
public class ProgressRing : TemplatedControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsActive), true);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(StrokeThickness), 3);

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    static ProgressRing()
    {
        IsActiveProperty.Changed.AddClassHandler<ProgressRing>((ring, e) => ring.PseudoClasses.Set(":active", e.GetNewValue<bool>()));
    }

    public ProgressRing()
    {
        PseudoClasses.Set(":active", IsActive);
    }
}
