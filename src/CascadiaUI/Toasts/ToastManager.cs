using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace CascadiaUI.Toasts;

/// <summary>
/// Queue of toasts for a <see cref="ToastHost"/>. Lives in the view model, so status messages
/// can be raised without touching any view; safe to call from any thread.
/// </summary>
public class ToastManager
{
    private readonly ObservableCollection<Toast> _toasts = [];

    public ToastManager()
    {
        Toasts = new ReadOnlyObservableCollection<Toast>(_toasts);
    }

    /// <summary>Toasts currently on screen, oldest first.</summary>
    public ReadOnlyObservableCollection<Toast> Toasts { get; }

    /// <summary>At most this many are on screen; showing one more drops the oldest.</summary>
    public int MaxVisible { get; set; } = 4;

    public Toast Show(Toast toast)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _toasts.Add(toast);
            while (_toasts.Count > Math.Max(1, MaxVisible))
            {
                _toasts.RemoveAt(0);
            }

            if (toast.Duration is { } duration && duration > TimeSpan.Zero)
            {
                DispatcherTimer.RunOnce(() => Dismiss(toast), duration);
            }
        });

        return toast;
    }

    public Toast Show(string? title, string? message, ToastSeverity severity = ToastSeverity.Information, TimeSpan? duration = null) =>
        Show(new Toast
        {
            Title = title,
            Message = message,
            Severity = severity,
            Duration = duration ?? (severity == ToastSeverity.Error ? TimeSpan.FromSeconds(7) : TimeSpan.FromSeconds(4)),
        });

    public void Dismiss(Toast toast) => Dispatcher.UIThread.Post(() => _toasts.Remove(toast));

    public void DismissAll() => Dispatcher.UIThread.Post(_toasts.Clear);
}
