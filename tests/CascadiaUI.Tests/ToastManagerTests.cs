using TUnit.Assertions.Enums;
using Avalonia.Threading;
using CascadiaUI.Toasts;

namespace CascadiaUI.Tests;

public class ToastManagerTests
{
    [Test]
    public async Task Show_AddsToast_AndCapsVisibleCount()
    {
        var manager = new ToastManager { MaxVisible = 2 };

        manager.Show("1", null);
        manager.Show("2", null);
        manager.Show("3", null);
        Dispatcher.UIThread.RunJobs();

        await Assert.That(manager.Toasts.Select(t => t.Title!)).IsEquivalentTo(["2", "3"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Dismiss_RemovesToast()
    {
        var manager = new ToastManager();
        var toast = manager.Show(new Toast { Title = "x", Duration = null });
        Dispatcher.UIThread.RunJobs();

        manager.Dismiss(toast);
        Dispatcher.UIThread.RunJobs();

        await Assert.That(manager.Toasts).IsEmpty();
    }

    [Test]
    public async Task Errors_StayUpLonger()
    {
        var manager = new ToastManager();
        var error = manager.Show("e", "boom", ToastSeverity.Error);
        var info = manager.Show("i", "fyi");

        await Assert.That(error.Duration > info.Duration).IsTrue();
        await Assert.That(error.IsError).IsTrue();
    }
}
