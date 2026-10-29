using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

namespace CascadiaUI.Tests;

public class ThemeTests
{
    [AvaloniaFact]
    public void Accent_DrivesFluentAccentResources()
    {
        var theme = CascadiaTheme.Current!;
        var original = theme.Accent;
        try
        {
            theme.Accent = Color.Parse("#7A4CD9");
            var window = new Window();
            window.Show();

            Assert.True(window.TryFindResource("SystemAccentColor", out var accent));
            Assert.Equal(Color.Parse("#7A4CD9"), accent);
            Assert.True(window.TryFindResource("CascadiaAccentSubtleColor", out var subtle));
            Assert.Equal(0x29, ((Color)subtle!).A);
        }
        finally
        {
            theme.Accent = original;
        }
    }

    [AvaloniaFact]
    public void DefaultAccent_IsGreen()
    {
        Assert.Equal(CascadiaTheme.DefaultAccent, CascadiaTheme.Current!.Accent);
        var hue = CascadiaTheme.DefaultAccent.ToHsl().H;
        Assert.InRange(hue, 100, 170);
    }

    [AvaloniaTheory]
    [InlineData("Light", "#FFF3F3F3")]
    [InlineData("Dark", "#FF1F1F1F")]
    public void WindowBackground_FollowsThemeVariant(string variant, string expected)
    {
        var window = new Window { RequestedThemeVariant = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light };
        window.Show();

        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(window.Background);
        Assert.Equal(Color.Parse(expected), brush.Color);
    }

    [AvaloniaFact]
    public void Density_Compact_ShrinksText()
    {
        var theme = CascadiaTheme.Current!;
        try
        {
            theme.Density = CascadiaDensity.Compact;
            Assert.True(Application.Current!.TryFindResource("ControlContentThemeFontSize", out var size));
            Assert.Equal(13d, size);
        }
        finally
        {
            theme.Density = CascadiaDensity.Comfortable;
        }
    }

    [Theory]
    [InlineData("#16834A", "#FFFFFFFF")]
    [InlineData("#D2600C", "#FFFFFFFF")]
    [InlineData("#FFD400", "#FF141414")]
    [InlineData("#0A72D1", "#FFFFFFFF")]
    public void AccentForeground_PicksReadableContrast(string accent, string expected)
    {
        Assert.Equal(Color.Parse(expected), AccentPalette.ContrastingForeground(Color.Parse(accent)));
    }
}
