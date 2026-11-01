using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

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

    [Theory]
    [InlineData("#0A84FF")] // Apple system blue: 3.65:1 with white, so the fill must darken
    [InlineData("#16834A")] // the default green: already AA, so the fill is the accent itself
    [InlineData("#D2600C")]
    [InlineData("#FFD400")] // light accent: dark text, the fill lightens if needed
    public void AccentFill_KeepsTextAtWcagAA_InEveryState(string accent)
    {
        var palette = AccentPalette.Create(Color.Parse(accent));

        Assert.True(AccentPalette.Contrast(palette.Fill, palette.Foreground) >= AccentPalette.TextContrast);
        Assert.True(AccentPalette.Contrast(palette.FillHover, palette.Foreground) >= AccentPalette.TextContrast);
        Assert.True(AccentPalette.Contrast(palette.FillPressed, palette.Foreground) >= AccentPalette.TextContrast);
        Assert.Equal(Color.Parse(accent), palette.Accent); // controls without text keep the accent
    }

    [Fact]
    public void AccentFill_IsTheAccent_WhenItAlreadyPasses()
    {
        var palette = AccentPalette.Create(Color.Parse("#16834A"));
        Assert.Equal(palette.Accent, palette.Fill);
    }

    [AvaloniaFact]
    public void AccentButton_UsesTheTextSafeFill()
    {
        var theme = CascadiaTheme.Current!;
        var original = theme.Accent;
        try
        {
            theme.Accent = Color.Parse("#0A84FF");
            var window = new Window();
            window.Show();

            Assert.True(window.TryFindResource("AccentButtonBackground", window.ActualThemeVariant, out var brush));
            var fill = ((ISolidColorBrush)brush!).Color;
            Assert.NotEqual(Color.Parse("#0A84FF"), fill);
            Assert.True(AccentPalette.Contrast(fill, Colors.White) >= AccentPalette.TextContrast);
        }
        finally
        {
            theme.Accent = original;
        }
    }

    [AvaloniaFact]
    public void NumericUpDown_ShowsFocusOnTheWholeBox_NotOnItsInnerTextBox()
    {
        var nud = new NumericUpDown { Value = 10, Width = 200 };
        var plain = new TextBox { Width = 200 };
        var window = new Window { Content = new StackPanel { Children = { nud, plain } } };
        window.Show();

        var inner = nud.GetVisualDescendants().OfType<TextBox>().Single();
        inner.Focus();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var innerBorder = inner.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_BorderElement");
        var outerBorder = nud.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_SpinnerBorder");
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)innerBorder.BorderBrush!).Color);
        Assert.Equal(theme_Accent(), ((ISolidColorBrush)outerBorder.BorderBrush!).Color);

        // A plain TextBox keeps its own accent focus border.
        plain.Focus();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        var plainBorder = plain.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_BorderElement");
        Assert.Equal(theme_Accent(), ((ISolidColorBrush)plainBorder.BorderBrush!).Color);

        static Color theme_Accent() => CascadiaTheme.Current!.Accent;
    }
}
