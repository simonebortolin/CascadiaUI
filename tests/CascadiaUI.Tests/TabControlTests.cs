using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CascadiaUI.Controls;
using CommunityToolkit.Mvvm.Input;

namespace CascadiaUI.Tests;

public class TabControlTests
{
    private static (Window Window, CascadiaTabControl Tabs) Show(CascadiaTabControl tabs)
    {
        var window = new Window { Width = 800, Height = 400, Content = tabs };
        window.Show();
        return (window, tabs);
    }

    [AvaloniaFact]
    public void MoveTab_ReordersObservableCollection_AndKeepsSelection()
    {
        var items = new ObservableCollection<string> { "a", "b", "c" };
        var (_, tabs) = Show(new CascadiaTabControl { ItemsSource = items, SelectedIndex = 0 });
        TabReorderedEventArgs? raised = null;
        tabs.TabReordered += (_, e) => raised = e;

        Assert.True(tabs.MoveTab(0, 2));

        Assert.Equal(["b", "c", "a"], items);
        Assert.Equal(2, tabs.SelectedIndex);
        Assert.Equal("a", raised?.Item);
        Assert.Equal((0, 2), (raised!.OldIndex, raised.NewIndex));
    }

    [AvaloniaFact]
    public void MoveTab_ReordersInlineTabItems()
    {
        var first = new TabItem { Header = "one" };
        var second = new TabItem { Header = "two" };
        var (_, tabs) = Show(new CascadiaTabControl { Items = { first, second } });

        Assert.True(tabs.MoveTab(1, 0));

        Assert.Same(second, tabs.Items[0]);
        Assert.Same(first, tabs.Items[1]);
    }

    [AvaloniaFact]
    public void MoveTab_RejectsReadOnlySourcesAndBadIndices()
    {
        var (_, tabs) = Show(new CascadiaTabControl { ItemsSource = new[] { "a", "b" } });

        Assert.False(tabs.MoveTab(0, 1));
        Assert.False(tabs.MoveTab(0, 0));
        Assert.False(tabs.MoveTab(-1, 1));
    }

    [AvaloniaFact]
    public void RequestClose_InvokesCommandWithItem()
    {
        var items = new ObservableCollection<string> { "a", "b" };
        object? closed = null;
        var (_, tabs) = Show(new CascadiaTabControl
        {
            ItemsSource = items,
            CloseTabCommand = new RelayCommand<object?>(item => closed = item),
        });

        tabs.RequestClose("b");

        Assert.Equal("b", closed);
        Assert.Equal(2, items.Count); // the command owns the removal
    }

    [AvaloniaFact]
    public void RequestClose_WithoutCommand_RemovesFromSource_UnlessHandled()
    {
        var items = new ObservableCollection<string> { "a", "b", "c" };
        var (_, tabs) = Show(new CascadiaTabControl { ItemsSource = items });
        tabs.TabCloseRequested += (_, e) => e.Handled = Equals(e.Item, "c");

        tabs.RequestClose("a");
        tabs.RequestClose("c");

        Assert.Equal(["b", "c"], items);
    }

    [AvaloniaFact]
    public void CanCloseTabs_FlowsDownToTabItems()
    {
        var item = new TabItem { Header = "x" };
        var (_, tabs) = Show(new CascadiaTabControl { CanCloseTabs = true, Items = { item } });

        Assert.True(CascadiaTabControl.GetIsClosable(item));
        var nested = new TabItem { Header = "inner" };
        tabs.Items.Add(new TabItem { Header = "host", Content = new TabControl { Items = { nested } } });
        tabs.SelectedIndex = 1;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.False(CascadiaTabControl.GetIsClosable(nested));
        tabs.CanCloseTabs = false;
        Assert.False(CascadiaTabControl.GetIsClosable(item));
    }

    [AvaloniaFact]
    public void IsInTitleBar_TakesCaptionInsetsFromWindow()
    {
        var tabs = new CascadiaTabControl { IsInTitleBar = true, Items = { new TabItem { Header = "x" } } };
        var window = new CascadiaWindow { ExtendContentIntoTitleBar = true, Content = tabs, Width = 600, Height = 300 };
        window.Show();

        Assert.True(tabs.Classes.Contains(":titlebar") || ((IPseudoClasses)tabs.Classes).Contains(":titlebar"));
        Assert.Equal(CascadiaWindow.GetCaptionInsets(window), tabs.StripInsets);

        tabs.IsInTitleBar = false;
        Assert.Equal(default(Thickness), tabs.StripInsets);
    }
}
