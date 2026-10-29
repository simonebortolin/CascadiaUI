using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CascadiaUI.Toasts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadiaUI.Gallery.ViewModels;

/// <summary>A page of the gallery, shown as one tab.</summary>
public abstract partial class PageViewModel(string header) : ObservableObject
{
    public string Header { get; } = header;
}

public sealed class ButtonsPageViewModel() : PageViewModel("Buttons & inputs");
public sealed class ListsPageViewModel() : PageViewModel("Lists & trees")
{
    public ObservableCollection<string> Devices { get; } =
        ["LETRA-200B-4F2A", "LETRA-200B-91C0", "Office printer", "Kitchen labeller"];

    public IReadOnlyList<ReportNode> Report { get; } =
    [
        new("General", [new("Format", "Matroska"), new("Duration", "1 h 42 min"), new("Overall bit rate", "8 412 kb/s")]),
        new("Video", [new("Format", "AVC / High@L4.1"), new("Width", "1 920 pixels"), new("Height", "1 080 pixels"), new("Frame rate", "23.976 FPS")]),
        new("Audio #1", [new("Format", "E-AC-3"), new("Channels", "6 channels"), new("Language", "Italian")]),
        new("Text", [new("Format", "UTF-8"), new("Language", "English")]),
    ];

    public IReadOnlyList<string> Chips { get; } =
        ["12 mm", "Bold", "Inverted", "QR", "Code 128", "DIN rail", "Frame", "Auto-cut", "2 copies"];
}

public sealed record ReportNode(string Name, IReadOnlyList<ReportNode> Children, string? Value = null)
{
    public ReportNode(string name, string value) : this(name, [], value) { }
}

public sealed partial class TabsPageViewModel() : PageViewModel("Tabs")
{
    private int _counter = 3;

    public ObservableCollection<DocumentTab> Documents { get; } =
        [new("movie.mkv"), new("trailer.mp4"), new("podcast.flac")];

    [ObservableProperty]
    public partial DocumentTab? SelectedDocument { get; set; }

    [RelayCommand]
    private void AddDocument()
    {
        var tab = new DocumentTab($"untitled-{++_counter}.wav");
        Documents.Add(tab);
        SelectedDocument = tab;
    }

    [RelayCommand]
    private void CloseDocument(DocumentTab? tab)
    {
        if (tab != null)
        {
            Documents.Remove(tab);
        }
    }
}

public sealed record DocumentTab(string Header);

public sealed partial class FeedbackPageViewModel(ToastManager toasts) : PageViewModel("Feedback")
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; } = true;

    [ObservableProperty]
    public partial bool IsStatsVisible { get; set; } = true;

    public string Stats { get; } = "packets  42 / 42\nretries  0\nrtt      18 ms\nbytes    3 712";

    [RelayCommand]
    private void ShowToast(string? kind)
    {
        switch (kind)
        {
            case "success": toasts.Show("Printed", "Label sent to LETRA-200B-4F2A.", ToastSeverity.Success); break;
            case "warning": toasts.Show("Low tape", "About 40 cm of tape left.", ToastSeverity.Warning); break;
            case "error": toasts.Show("Bluetooth unavailable", "Turn Bluetooth on and refresh the device list.", ToastSeverity.Error); break;
            default: toasts.Show("Heads up", "This is an informational toast; click it to dismiss.", ToastSeverity.Information); break;
        }
    }
}

public sealed class TypographyPageViewModel() : PageViewModel("Text & surfaces");

public sealed class ScratchPageViewModel(int number) : PageViewModel($"Scratch {number}")
{
    public string Body { get; } = $"A tab added with the + button (#{number}). Drag it around, middle-click or × to close it.";
}

public sealed record AccentChoice(string Name, Color Color)
{
    public IBrush Brush { get; } = new SolidColorBrush(Color);
}

public sealed partial class MainViewModel : ObservableObject
{
    private int _scratchCounter;

    public MainViewModel()
    {
        Pages =
        [
            new ButtonsPageViewModel(),
            new ListsPageViewModel(),
            new TabsPageViewModel(),
            new FeedbackPageViewModel(Toasts),
            new TypographyPageViewModel(),
        ];
        SelectedPage = Pages[0];
    }

    public ToastManager Toasts { get; } = new();

    public ObservableCollection<PageViewModel> Pages { get; }

    [ObservableProperty]
    public partial PageViewModel? SelectedPage { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Ready";

    public IReadOnlyList<AccentChoice> Accents { get; } =
    [
        new("Green", CascadiaTheme.DefaultAccent),
        new("Blue", Color.Parse("#0A72D1")),
        new("Teal", Color.Parse("#0E8A8A")),
        new("Purple", Color.Parse("#7A4CD9")),
        new("Pink", Color.Parse("#C2367A")),
        new("Orange", Color.Parse("#D2600C")),
        new("Graphite", Color.Parse("#5A5F66")),
    ];

    partial void OnSelectedPageChanged(PageViewModel? value) =>
        Status = value is null ? "Ready" : $"Showing “{value.Header}” — {Pages.Count} tabs open";

    [RelayCommand]
    private void AddPage()
    {
        var page = new ScratchPageViewModel(++_scratchCounter);
        Pages.Add(page);
        SelectedPage = page;
    }

    [RelayCommand]
    private void ClosePage(PageViewModel? page)
    {
        if (page != null && Pages.Count > 1)
        {
            Pages.Remove(page);
        }
    }

    [RelayCommand]
    private void SetTheme(string? variant)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = variant switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }

    [RelayCommand]
    private void SetAccent(AccentChoice? accent)
    {
        if (accent != null && CascadiaTheme.Current is { } theme)
        {
            theme.Accent = accent.Color;
            Status = $"Accent: {accent.Name} ({accent.Color})";
        }
    }

    [RelayCommand]
    private void SetDensity(string? density)
    {
        if (CascadiaTheme.Current is { } theme)
        {
            theme.Density = density == "Compact" ? CascadiaDensity.Compact : CascadiaDensity.Comfortable;
        }
    }
}
