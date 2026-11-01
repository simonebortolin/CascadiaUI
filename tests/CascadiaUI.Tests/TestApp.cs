using Avalonia;
using Avalonia.Headless;
using CascadiaUI.Tests;
using TUnit.Core.Executors;
using TUnit.Core.Interfaces;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
[assembly: TestExecutor<AvaloniaTestExecutor>]
// Every test runs on the one headless UI thread and some change app-wide state (accent,
// density, light/dark), so they must not interleave at their awaits.
[assembly: NotInParallel]

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

/// <summary>
/// Runs every test on the Avalonia headless session's UI thread - TUnit's counterpart of
/// Avalonia.Headless.XUnit's [AvaloniaFact].
/// </summary>
public sealed class AvaloniaTestExecutor : ITestExecutor
{
    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) =>
        new(HeadlessUnitTestSession.GetOrStartForAssembly(typeof(AvaloniaTestExecutor).Assembly)
            .Dispatch(async () =>
            {
                await action();
                return true;
            }, CancellationToken.None));
}
