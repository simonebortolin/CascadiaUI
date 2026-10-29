using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace CascadiaUI;

/// <summary>How much breathing room controls get.</summary>
public enum CascadiaDensity
{
    /// <summary>Touch-friendly sizes (14px text, 32px controls) - the default, and the right
    /// pick for anything that also runs on Android.</summary>
    Comfortable,

    /// <summary>Desktop-dense sizes (13px text, ~28px controls), SourceGit / macOS style.</summary>
    Compact,
}

/// <summary>
/// The Avalonia Cascadia theme: a Fluent × macOS look with neutral light/dark palettes, a
/// customisable accent and Windows Terminal-style tabs.
/// </summary>
/// <remarks>
/// It builds on <see cref="FluentTheme"/> rather than replacing it: every control keeps Fluent's
/// battle-tested templates, while Cascadia's own resources (looked up first, since they live in
/// this <see cref="Styles"/>' own <see cref="Styles.Resources"/>) re-skin them, and Cascadia's own
/// control themes take over where the look differs too much to get there with colours alone
/// (tabs, window chrome, busy overlay, toasts).
/// <code>
/// &lt;Application.Styles&gt;
///   &lt;cascadia:CascadiaTheme Accent="#16834A" Density="Compact" /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// </remarks>
public class CascadiaTheme : Styles
{
    /// <summary>The default accent: a deep, slightly teal green that keeps white text on accent
    /// buttons at WCAG AA (4.8:1).</summary>
    public static readonly Color DefaultAccent = Color.Parse("#16834A");

    public static readonly StyledProperty<Color> AccentProperty =
        AvaloniaProperty.Register<CascadiaTheme, Color>(nameof(Accent), DefaultAccent);

    public static readonly StyledProperty<CascadiaDensity> DensityProperty =
        AvaloniaProperty.Register<CascadiaTheme, CascadiaDensity>(nameof(Density));

    private static readonly Uri BaseUri = new("avares://CascadiaUI/");

    private readonly FluentTheme _fluent;

    public CascadiaTheme() : this(null)
    {
    }

    public CascadiaTheme(IServiceProvider? serviceProvider)
    {
        _fluent = new FluentTheme(serviceProvider);
        Add(_fluent);
        Add(new StyleInclude(BaseUri) { Source = new Uri("avares://CascadiaUI/Themes/CascadiaStyles.axaml") });

        Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = new Uri("avares://CascadiaUI/Themes/Tokens.axaml") });
        Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = new Uri("avares://CascadiaUI/Themes/Controls.axaml") });

        Resources["CascadiaRaisedShadow"] = BoxShadows.Parse("0 1 2 0 #26000000");
        Resources["CascadiaOverlayShadow"] = BoxShadows.Parse("0 8 24 0 #2E000000");
        ApplyAccent(Accent);
    }

    /// <summary>The accent colour. Every accent shade (hover, pressed, selection tints, focus
    /// ring) is derived from it, so it can be swapped at runtime.</summary>
    public Color Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Control sizing, see <see cref="CascadiaDensity"/>.</summary>
    public CascadiaDensity Density
    {
        get => GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    /// <summary>The first <see cref="CascadiaTheme"/> registered on the current application, if any -
    /// handy for runtime accent/density switches from code.</summary>
    public static CascadiaTheme? Current =>
        Application.Current?.Styles.OfType<CascadiaTheme>().FirstOrDefault();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AccentProperty)
        {
            ApplyAccent(change.GetNewValue<Color>());
        }
        else if (change.Property == DensityProperty)
        {
            var compact = change.GetNewValue<CascadiaDensity>() == CascadiaDensity.Compact;
            _fluent.DensityStyle = compact ? DensityStyle.Compact : DensityStyle.Normal;
            Resources["ControlContentThemeFontSize"] = compact ? 13d : 14d;
            Resources["CascadiaTabItemHeight"] = compact ? 30d : 34d;
            Resources["CascadiaTitleBarHeight"] = compact ? 38d : 42d;
        }
    }

    private void ApplyAccent(Color accent)
    {
        var shades = AccentPalette.Create(accent);

        Resources["SystemAccentColor"] = shades.Accent;
        Resources["SystemAccentColorLight1"] = shades.Light1;
        Resources["SystemAccentColorLight2"] = shades.Light2;
        Resources["SystemAccentColorLight3"] = shades.Light3;
        Resources["SystemAccentColorDark1"] = shades.Dark1;
        Resources["SystemAccentColorDark2"] = shades.Dark2;
        Resources["SystemAccentColorDark3"] = shades.Dark3;

        Resources["CascadiaAccentSubtleColor"] = shades.WithAlpha(0x29);
        Resources["CascadiaAccentSubtleHoverColor"] = shades.WithAlpha(0x38);
        Resources["CascadiaAccentSubtlePressedColor"] = shades.WithAlpha(0x47);
        Resources["CascadiaFocusRingColor"] = shades.WithAlpha(0x59);
        Resources["CascadiaAccentForegroundColor"] = shades.Foreground;
        Resources["CascadiaFocusRingShadow"] = new BoxShadows(new BoxShadow { Spread = 3, Color = shades.WithAlpha(0x4D) });
    }
}
