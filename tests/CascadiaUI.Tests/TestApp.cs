using Avalonia;
using Avalonia.Headless;
using CascadiaUI.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace CascadiaUI.Tests;

public class TestApp : Application
{
    public override void Initialize() => Styles.Add(new CascadiaTheme());
}

public static class TestAppBuilder
{
    // Real Skia rendering (not the headless no-op drawing) so the screenshot tests produce
    // actual pixels.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
