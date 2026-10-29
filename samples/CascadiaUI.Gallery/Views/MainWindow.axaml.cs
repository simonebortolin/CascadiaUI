using Avalonia.Interactivity;
using CascadiaUI.Controls;

namespace CascadiaUI.Gallery.Views;

public partial class MainWindow : CascadiaWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();
}
