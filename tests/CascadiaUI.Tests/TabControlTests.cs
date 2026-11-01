using TUnit.Assertions.Enums;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CascadiaUI.Controls;
using CommunityToolkit.Mvvm.Input;

namespace CascadiaUI.Tests;

public class TabControlTests
{
    private static CascadiaTabControl Show(CascadiaTabControl tabs)
    {
        var window = new Window { Width = 800, Height = 400, Content = tabs };
        window.Show();
        return tabs;
    }

    [Test]
    public async Task MoveTab_ReordersObservableCollection_AndKeepsSelection()
    {
        var items = new ObservableCollection<string> { "a", "b", "c" };
        var tabs = Show(new CascadiaTabControl { ItemsSource = items, SelectedIndex = 0 });
        TabReorderedEventArgs? raised = null;
        tabs.TabReordered += (_, e) => raised = e;

        await Assert.That(tabs.MoveTab(0, 2)).IsTrue();

        await Assert.That(items).IsEquivalentTo(["b", "c", "a"], CollectionOrdering.Matching);
        await Assert.That(tabs.SelectedIndex).IsEqualTo(2);
        await Assert.That(raised?.Item).IsEqualTo("a");
        await Assert.That((raised!.OldIndex, raised.NewIndex)).IsEqualTo((0, 2));
    }

    [Test]
    public async Task MoveTab_ReordersInlineTabItems()
    {
        var first = new TabItem { Header = "one" };
        var second = new TabItem { Header = "two" };
        var tabs = Show(new CascadiaTabControl { Items = { first, second } });

        await Assert.That(tabs.MoveTab(1, 0)).IsTrue();

        await Assert.That(tabs.Items[0]).IsSameReferenceAs(second);
        await Assert.That(tabs.Items[1]).IsSameReferenceAs(first);
    }

    [Test]
    public async Task MoveTab_RejectsReadOnlySourcesAndBadIndices()
    {
        var tabs = Show(new CascadiaTabControl { ItemsSource = new[] { "a", "b" } });

        await Assert.That(tabs.MoveTab(0, 1)).IsFalse();
        await Assert.That(tabs.MoveTab(0, 0)).IsFalse();
        await Assert.That(tabs.MoveTab(-1, 1)).IsFalse();
    }

    [Test]
    public async Task RequestClose_InvokesCommandWithItem()
    {
        var items = new ObservableCollection<string> { "a", "b" };
        object? closed = null;
        var tabs = Show(new CascadiaTabControl
        {
            ItemsSource = items,
            CloseTabCommand = new RelayCommand<object?>(item => closed = item),
        });

        tabs.RequestClose("b");

        await Assert.That(closed).IsEqualTo("b");
        await Assert.That(items.Count).IsEqualTo(2); // the command owns the removal
    }

    [Test]
    public async Task RequestClose_WithoutCommand_RemovesFromSource_UnlessHandled()
    {
        var items = new ObservableCollection<string> { "a", "b", "c" };
        var tabs = Show(new CascadiaTabControl { ItemsSource = items });
        tabs.TabCloseRequested += (_, e) => e.Handled = Equals(e.Item, "c");

        tabs.RequestClose("a");
        tabs.RequestClose("c");

        await Assert.That(items).IsEquivalentTo(["b", "c"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task CanCloseTabs_FlowsDownToTabItems_ButNotIntoNestedTabControls()
    {
        var item = new TabItem { Header = "x" };
        var tabs = Show(new CascadiaTabControl { CanCloseTabs = true, Items = { item } });

        await Assert.That(CascadiaTabControl.GetIsClosable(item)).IsTrue();

        var nested = new TabItem { Header = "inner" };
        tabs.Items.Add(new TabItem { Header = "host", Content = new TabControl { Items = { nested } } });
        tabs.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        await Assert.That(CascadiaTabControl.GetIsClosable(nested)).IsFalse();

        tabs.CanCloseTabs = false;
        await Assert.That(CascadiaTabControl.GetIsClosable(item)).IsFalse();
    }

    [Test]
    public async Task IsInTitleBar_TakesCaptionInsetsFromWindow()
    {
        var tabs = new CascadiaTabControl { IsInTitleBar = true, Items = { new TabItem { Header = "x" } } };
        var window = new CascadiaWindow { ExtendContentIntoTitleBar = true, Content = tabs, Width = 600, Height = 300 };
        window.Show();

        await Assert.That(tabs.StripInsets).IsEqualTo(CascadiaWindow.GetCaptionInsets(window));

        tabs.IsInTitleBar = false;
        await Assert.That(tabs.StripInsets).IsEqualTo(default(Thickness));
    }
}
