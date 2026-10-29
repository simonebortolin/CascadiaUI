using Avalonia.Media;

namespace CascadiaUI;

/// <summary>Derives the full set of accent shades Fluent and Cascadia need from a single colour.</summary>
public readonly record struct AccentPalette(
    Color Accent,
    Color Light1, Color Light2, Color Light3,
    Color Dark1, Color Dark2, Color Dark3,
    Color Foreground)
{
    public static AccentPalette Create(Color accent)
    {
        var hsl = accent.ToHsl();

        Color Shift(double delta) =>
            new HslColor(accent.A / 255d, hsl.H, hsl.S, Math.Clamp(hsl.L + delta, 0, 1)).ToRgb();

        return new AccentPalette(
            accent,
            Shift(0.08), Shift(0.16), Shift(0.24),
            Shift(-0.07), Shift(-0.14), Shift(-0.21),
            ContrastingForeground(accent));
    }

    /// <summary>The accent at the given alpha - used for selection tints and the focus ring.</summary>
    public Color WithAlpha(byte alpha) => Color.FromArgb(alpha, Accent.R, Accent.G, Accent.B);

    /// <summary>Text colour for <paramref name="background"/>: white whenever it reaches the WCAG
    /// 3:1 contrast UI controls need (what Fluent and macOS do on saturated accents), near-black
    /// only for genuinely light accents such as yellow.</summary>
    public static Color ContrastingForeground(Color background)
    {
        static double Channel(byte c)
        {
            var v = c / 255d;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        var luminance = 0.2126 * Channel(background.R) + 0.7152 * Channel(background.G) + 0.0722 * Channel(background.B);
        var contrastWithWhite = 1.05 / (luminance + 0.05);
        return contrastWithWhite >= 3 ? Colors.White : Color.Parse("#FF141414");
    }
}
