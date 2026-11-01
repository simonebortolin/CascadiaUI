using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using CascadiaUI.Gallery.ViewModels;
using CascadiaUI.Gallery.Views;

namespace CascadiaUI.Tests;

/// <summary>
/// Renders every gallery page in light and dark through real Skia. Doubles as a smoke test
/// (every template applies without throwing) and as the README's screenshots, written to
/// <c>artifacts/screenshots</c> (CI uploads them).
/// </summary>
public class ScreenshotTests
{
    private static string OutputDirectory
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("CASCADIA_SCREENSHOTS")
                ?? Path.Combine(FindRepoRoot(), "artifacts", "screenshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CascadiaUI.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }

    [Test]
    [Arguments("Light")]
    [Arguments("Dark")]
    public async Task Gallery_RendersEveryPage(string variant)
    {
        var app = Application.Current!;
        app.RequestedThemeVariant = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        try
        {
            var vm = new MainViewModel();
            var window = new MainWindow { DataContext = vm, Width = 1100, Height = 740 };
            window.Show();

            for (var i = 0; i < vm.Pages.Count; i++)
            {
                vm.SelectedPage = vm.Pages[i];
                if (vm.Pages[i] is FeedbackPageViewModel feedback && i == 3)
                {
                    feedback.ShowToastCommand.Execute("success");
                    feedback.ShowToastCommand.Execute("error");
                }

                Pump();
                var frame = window.CaptureRenderedFrame();
                await Assert.That(frame).IsNotNull();

                var name = vm.Pages[i].Header.ToLowerInvariant().Replace(" & ", "-").Replace(' ', '-');
                frame!.Save(Path.Combine(OutputDirectory, $"{i + 1:00}-{name}-{variant.ToLowerInvariant()}.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
        }
        finally
        {
            app.RequestedThemeVariant = ThemeVariant.Default;
        }
    }

    private static void Pump()
    {
        // Let bindings, layout, animations' first frame and posted work settle.
        for (var i = 0; i < 5; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
