namespace CascadiaUI.Toasts;

public enum ToastSeverity
{
    Information,
    Success,
    Warning,
    Error,
}

/// <summary>One notification shown by a <see cref="ToastHost"/>.</summary>
public sealed class Toast
{
    public string? Title { get; init; }

    public string? Message { get; init; }

    public ToastSeverity Severity { get; init; }

    /// <summary>How long it stays up; <c>null</c> (or zero) keeps it until dismissed.</summary>
    public TimeSpan? Duration { get; init; } = TimeSpan.FromSeconds(4);

    /// <summary>Whether clicking the toast dismisses it.</summary>
    public bool DismissOnClick { get; init; } = true;

    public bool IsInformation => Severity == ToastSeverity.Information;
    public bool IsSuccess => Severity == ToastSeverity.Success;
    public bool IsWarning => Severity == ToastSeverity.Warning;
    public bool IsError => Severity == ToastSeverity.Error;
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public bool HasMessage => !string.IsNullOrEmpty(Message);
}
