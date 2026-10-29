using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CascadiaUI.Toasts;

namespace CascadiaUI.Tests;

public class ToastManagerTests
{
    [AvaloniaFact]
    public void Show_AddsToast_AndCapsVisibleCount()
    {
        var manager = new ToastManager { MaxVisible = 2 };

        manager.Show("1", null);
        manager.Show("2", null);
        manager.Show("3", null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["2", "3"], manager.Toasts.Select(t => t.Title));
    }

    [AvaloniaFact]
    public void Dismiss_RemovesToast()
    {
        var manager = new ToastManager();
        var toast = manager.Show(new Toast { Title = "x", Duration = null });
        Dispatcher.UIThread.RunJobs();

        manager.Dismiss(toast);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(manager.Toasts);
    }

    [AvaloniaFact]
    public void Errors_StayUpLonger()
    {
        var manager = new ToastManager();
        var error = manager.Show("e", "boom", ToastSeverity.Error);
        var info = manager.Show("i", "fyi");

        Assert.True(error.Duration > info.Duration);
        Assert.True(error.IsError);
    }
}
