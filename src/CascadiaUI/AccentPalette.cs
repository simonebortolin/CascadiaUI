using Avalonia.Media;

namespace CascadiaUI;

/// <summary>Derives the full set of accent shades Fluent and Cascadia need from a single colour.</summary>
public readonly record struct AccentPalette(
    Color Accent,
    Color Light1, Color Light2, Color Light3,
    Color Dark1, Color Dark2, Color Dark3,
    Color Foreground,
    Color Fill, Color FillHover, Color FillPressed)
{
    /// <summary>WCAG AA contrast for normal-size text.</summary>
    public const double TextContrast = 4.5;

    public static AccentPalette Create(Color accent)
    {
        var hsl = accent.ToHsl();

        Color Shift(double delta) =>
            new HslColor(accent.A / 255d, hsl.H, hsl.S, Math.Clamp(hsl.L + delta, 0, 1)).ToRgb();

        var foreground = ContrastingForeground(accent);
        var fill = TextSafeFill(accent, foreground);
        var fillHsl = fill.ToHsl();

        // Hover and pressed move further away from the text colour, so they never lose contrast.
        var away = foreground == Colors.White ? -1 : 1;
        Color FillShift(double delta) =>
            new HslColor(fill.A / 255d, fillHsl.H, fillHsl.S, Math.Clamp(fillHsl.L + away * delta, 0, 1)).ToRgb();

        return new AccentPalette(
            accent,
            Shift(0.08), Shift(0.16), Shift(0.24),
            Shift(-0.07), Shift(-0.14), Shift(-0.21),
            foreground,
            fill, FillShift(0.04), FillShift(0.08));
    }

    /// <summary>
    /// The accent as a background for text (accent buttons, checked toggle buttons): the accent
    /// itself when <paramref name="foreground"/> already reads at <see cref="TextContrast"/> on it,
    /// otherwise the accent darkened (white text) or lightened (dark text) just enough to get there.
    /// Controls without text on the accent (focus ring, checkboxes, toggles) keep the accent as is.
    /// </summary>
    public static Color TextSafeFill(Color accent, Color foreground)
    {
        var hsl = accent.ToHsl();
        var step = foreground == Colors.White ? -0.005 : 0.005;
        var fill = accent;
        for (var l = hsl.L; Contrast(fill, foreground) < TextContrast && l is > 0 and < 1; l += step)
        {
            fill = new HslColor(accent.A / 255d, hsl.H, hsl.S, Math.Clamp(l, 0, 1)).ToRgb();
        }

        return fill;
    }

    /// <summary>WCAG contrast ratio between two opaque colours.</summary>
    public static double Contrast(Color a, Color b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color c)
    {
        static double Channel(byte v)
        {
            var x = v / 255d;
            return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    /// <summary>The accent at the given alpha - used for selection tints and the focus ring.</summary>
    public Color WithAlpha(byte alpha) => Color.FromArgb(alpha, Accent.R, Accent.G, Accent.B);

    /// <summary>Text colour for <paramref name="background"/>: white whenever it reaches the WCAG
    /// 3:1 contrast UI controls need (what Fluent and macOS do on saturated accents), near-black
    /// only for genuinely light accents such as yellow.</summary>
    public static Color ContrastingForeground(Color background) =>
        Contrast(background, Colors.White) >= 3 ? Colors.White : Color.Parse("#FF141414");
}
