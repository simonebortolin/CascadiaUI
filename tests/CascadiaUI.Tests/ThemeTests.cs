using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace CascadiaUI.Tests;

public class ThemeTests
{
    [Test]
    public async Task Accent_DrivesFluentAccentResources()
    {
        var theme = CascadiaTheme.Current!;
        var original = theme.Accent;
        try
        {
            theme.Accent = Color.Parse("#7A4CD9");
            var window = new Window();
            window.Show();

            await Assert.That(window.TryFindResource("SystemAccentColor", out var accent)).IsTrue();
            await Assert.That(accent).IsEqualTo(Color.Parse("#7A4CD9"));
            await Assert.That(window.TryFindResource("CascadiaAccentSubtleColor", out var subtle)).IsTrue();
            await Assert.That(((Color)subtle!).A).IsEqualTo((byte)0x29);
        }
        finally
        {
            theme.Accent = original;
        }
    }

    [Test]
    public async Task DefaultAccent_IsGreen()
    {
        await Assert.That(CascadiaTheme.Current!.Accent).IsEqualTo(CascadiaTheme.DefaultAccent);
        await Assert.That(CascadiaTheme.DefaultAccent.ToHsl().H).IsBetween(100, 170);
    }

    [Test]
    [Arguments("Light", "#FFF3F3F3")]
    [Arguments("Dark", "#FF1F1F1F")]
    public async Task WindowBackground_FollowsThemeVariant(string variant, string expected)
    {
        var window = new Window { RequestedThemeVariant = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light };
        window.Show();

        var brush = window.Background as ISolidColorBrush;
        await Assert.That(brush).IsNotNull();
        await Assert.That(brush!.Color).IsEqualTo(Color.Parse(expected));
    }

    [Test]
    public async Task Density_Compact_ShrinksText()
    {
        var theme = CascadiaTheme.Current!;
        try
        {
            theme.Density = CascadiaDensity.Compact;
            await Assert.That(Application.Current!.TryFindResource("ControlContentThemeFontSize", out var size)).IsTrue();
            await Assert.That(size).IsEqualTo(13d);
        }
        finally
        {
            theme.Density = CascadiaDensity.Comfortable;
        }
    }

    [Test]
    [Arguments("#16834A", "#FFFFFFFF")]
    [Arguments("#D2600C", "#FFFFFFFF")]
    [Arguments("#FFD400", "#FF141414")]
    [Arguments("#0A72D1", "#FFFFFFFF")]
    public async Task AccentForeground_PicksReadableContrast(string accent, string expected)
    {
        await Assert.That(AccentPalette.ContrastingForeground(Color.Parse(accent))).IsEqualTo(Color.Parse(expected));
    }

    [Test]
    [Arguments("#0A84FF")] // Apple system blue: 3.65:1 with white, so the fill must darken
    [Arguments("#16834A")] // the default green: already AA, so the fill is the accent itself
    [Arguments("#D2600C")]
    [Arguments("#FFD400")] // light accent: dark text, the fill lightens if needed
    public async Task AccentFill_KeepsTextAtWcagAA_InEveryState(string accent)
    {
        var palette = AccentPalette.Create(Color.Parse(accent));

        await Assert.That(AccentPalette.Contrast(palette.Fill, palette.Foreground)).IsGreaterThanOrEqualTo(AccentPalette.TextContrast);
        await Assert.That(AccentPalette.Contrast(palette.FillHover, palette.Foreground)).IsGreaterThanOrEqualTo(AccentPalette.TextContrast);
        await Assert.That(AccentPalette.Contrast(palette.FillPressed, palette.Foreground)).IsGreaterThanOrEqualTo(AccentPalette.TextContrast);
        await Assert.That(palette.Accent).IsEqualTo(Color.Parse(accent)); // controls without text keep the accent
    }

    [Test]
    public async Task AccentFill_IsTheAccent_WhenItAlreadyPasses()
    {
        var palette = AccentPalette.Create(Color.Parse("#16834A"));
        await Assert.That(palette.Fill).IsEqualTo(palette.Accent);
    }

    [Test]
    public async Task AccentButton_UsesTheTextSafeFill()
    {
        var theme = CascadiaTheme.Current!;
        var original = theme.Accent;
        try
        {
            theme.Accent = Color.Parse("#0A84FF");
            var window = new Window();
            window.Show();

            await Assert.That(window.TryFindResource("AccentButtonBackground", window.ActualThemeVariant, out var brush)).IsTrue();
            var fill = ((ISolidColorBrush)brush!).Color;
            await Assert.That(fill).IsNotEqualTo(Color.Parse("#0A84FF"));
            await Assert.That(AccentPalette.Contrast(fill, Colors.White)).IsGreaterThanOrEqualTo(AccentPalette.TextContrast);
        }
        finally
        {
            theme.Accent = original;
        }
    }

    [Test]
    public async Task NumericUpDown_ShowsFocusOnTheWholeBox_NotOnItsInnerTextBox()
    {
        var accent = CascadiaTheme.Current!.Accent;
        var nud = new NumericUpDown { Value = 10, Width = 200 };
        var plain = new TextBox { Width = 200 };
        var window = new Window { Content = new StackPanel { Children = { nud, plain } } };
        window.Show();

        var inner = nud.GetVisualDescendants().OfType<TextBox>().Single();
        inner.Focus();
        Dispatcher.UIThread.RunJobs();

        var innerBorder = inner.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_BorderElement");
        var outerBorder = nud.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_SpinnerBorder");
        await Assert.That(((ISolidColorBrush)innerBorder.BorderBrush!).Color).IsEqualTo(Colors.Transparent);
        await Assert.That(((ISolidColorBrush)outerBorder.BorderBrush!).Color).IsEqualTo(accent);

        // A plain TextBox keeps its own accent focus border.
        plain.Focus();
        Dispatcher.UIThread.RunJobs();
        var plainBorder = plain.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_BorderElement");
        await Assert.That(((ISolidColorBrush)plainBorder.BorderBrush!).Color).IsEqualTo(accent);
    }
}
