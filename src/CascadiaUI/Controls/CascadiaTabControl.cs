using System.Collections;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CascadiaUI.Controls;

/// <summary>
/// A <see cref="TabControl"/> with Windows Terminal-style tabs: closable, reorderable by
/// dragging, an optional "+" button and leading/trailing strip content, and able to live in a
/// <see cref="CascadiaWindow"/>'s title bar.
/// </summary>
/// <remarks>
/// Reordering moves the item in <see cref="ItemsControl.ItemsSource"/> when that is an
/// <see cref="IList"/> (an <c>ObservableCollection&lt;T&gt;</c>'s own <c>Move</c> is used when
/// available, so tab views aren't recreated), or in <see cref="ItemsControl.Items"/> otherwise.
/// Dragging a tab far enough above or below the strip raises <see cref="TabDraggedOutEvent"/>
/// instead, so the app can start a cross-window drag-and-drop of its own.
/// </remarks>
public class CascadiaTabControl : TabControl
{
    public static readonly StyledProperty<bool> CanCloseTabsProperty =
        AvaloniaProperty.Register<CascadiaTabControl, bool>(nameof(CanCloseTabs));

    public static readonly StyledProperty<bool> CanReorderTabsProperty =
        AvaloniaProperty.Register<CascadiaTabControl, bool>(nameof(CanReorderTabs), true);

    // The strip properties are attached ones hosted on TabControl, so the shared TabControl
    // template can TemplateBind them: a $parent[CascadiaTabControl] binding would wrongly pick up
    // an *outer* CascadiaTabControl when a plain TabControl is nested inside one.
    public static readonly AttachedProperty<bool> ShowAddButtonProperty =
        AvaloniaProperty.RegisterAttached<CascadiaTabControl, TabControl, bool>(nameof(ShowAddButton));

    public static readonly StyledProperty<bool> IsInTitleBarProperty =
        AvaloniaProperty.Register<CascadiaTabControl, bool>(nameof(IsInTitleBar));

    public static readonly StyledProperty<ICommand?> CloseTabCommandProperty =
        AvaloniaProperty.Register<CascadiaTabControl, ICommand?>(nameof(CloseTabCommand));

    public static readonly StyledProperty<ICommand?> AddTabCommandProperty =
        AvaloniaProperty.Register<CascadiaTabControl, ICommand?>(nameof(AddTabCommand));

    public static readonly AttachedProperty<object?> StripHeaderProperty =
        AvaloniaProperty.RegisterAttached<CascadiaTabControl, TabControl, object?>(nameof(StripHeader));

    public static readonly AttachedProperty<object?> StripFooterProperty =
        AvaloniaProperty.RegisterAttached<CascadiaTabControl, TabControl, object?>(nameof(StripFooter));

    public static readonly AttachedProperty<Thickness> StripInsetsProperty =
        AvaloniaProperty.RegisterAttached<CascadiaTabControl, TabControl, Thickness>(nameof(StripInsets));

    /// <summary>Whether a tab shows its close button. <see cref="CanCloseTabs"/> sets it on every tab
    /// at style priority, so a tab can still opt out locally (e.g. a pinned "Home" tab).</summary>
    public static readonly AttachedProperty<bool> IsClosableProperty =
        AvaloniaProperty.RegisterAttached<CascadiaTabControl, TabItem, bool>("IsClosable");

    public static readonly RoutedEvent<TabEventArgs> TabCloseRequestedEvent =
        RoutedEvent.Register<CascadiaTabControl, TabEventArgs>(nameof(TabCloseRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> AddTabRequestedEvent =
        RoutedEvent.Register<CascadiaTabControl, RoutedEventArgs>(nameof(AddTabRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<TabReorderedEventArgs> TabReorderedEvent =
        RoutedEvent.Register<CascadiaTabControl, TabReorderedEventArgs>(nameof(TabReordered), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<TabDraggedOutEventArgs> TabDraggedOutEvent =
        RoutedEvent.Register<CascadiaTabControl, TabDraggedOutEventArgs>(nameof(TabDraggedOut), RoutingStrategies.Bubble);

    private const double DragThreshold = 6;
    private const double TearOutDistance = 36;

    private TabItem? _pressedTab;
    private Point _pressPoint;
    private bool _isDragging;
    private ScrollViewer? _stripScroller;
    private Button? _addButton;


    public CascadiaTabControl()
    {
        AddHandler(PointerPressedEvent, OnTabPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnTabPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnTabPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, (_, _) => EndDrag(), RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble);
    }

    /// <summary>Shows a close button on every tab (middle-click closes too).</summary>
    public bool CanCloseTabs
    {
        get => GetValue(CanCloseTabsProperty);
        set => SetValue(CanCloseTabsProperty, value);
    }

    /// <summary>Lets the user drag tabs to reorder them.</summary>
    public bool CanReorderTabs
    {
        get => GetValue(CanReorderTabsProperty);
        set => SetValue(CanReorderTabsProperty, value);
    }

    /// <summary>Shows a "+" button right after the last tab.</summary>
    public bool ShowAddButton
    {
        get => GetValue(ShowAddButtonProperty);
        set => SetValue(ShowAddButtonProperty, value);
    }

    /// <summary>
    /// Puts the tab strip in the title bar of the hosting <see cref="CascadiaWindow"/> (which must
    /// have <see cref="CascadiaWindow.ExtendContentIntoTitleBar"/> set): the strip gets the title bar
    /// height, steps out of the caption buttons' way and doubles as window-drag area.
    /// </summary>
    public bool IsInTitleBar
    {
        get => GetValue(IsInTitleBarProperty);
        set => SetValue(IsInTitleBarProperty, value);
    }

    /// <summary>Invoked with the tab's item (its data item, or the <see cref="TabItem"/> itself)
    /// when its close button is clicked. <see cref="TabCloseRequested"/> is raised first and can
    /// cancel it by setting Handled.</summary>
    public ICommand? CloseTabCommand
    {
        get => GetValue(CloseTabCommandProperty);
        set => SetValue(CloseTabCommandProperty, value);
    }

    /// <summary>Invoked when the "+" button is clicked (after <see cref="AddTabRequested"/>).</summary>
    public ICommand? AddTabCommand
    {
        get => GetValue(AddTabCommandProperty);
        set => SetValue(AddTabCommandProperty, value);
    }

    /// <summary>Content placed before the tabs (an app menu, a logo...).</summary>
    public object? StripHeader
    {
        get => GetValue(StripHeaderProperty);
        set => SetValue(StripHeaderProperty, value);
    }

    /// <summary>Content placed at the far end of the strip (settings button, search...).</summary>
    public object? StripFooter
    {
        get => GetValue(StripFooterProperty);
        set => SetValue(StripFooterProperty, value);
    }

    /// <summary>Room the strip leaves for the window's caption buttons - the inherited
    /// <see cref="CascadiaWindow.CaptionInsetsProperty"/> while <see cref="IsInTitleBar"/>, else zero.</summary>
    public Thickness StripInsets
    {
        get => GetValue(StripInsetsProperty);
        private set => SetValue(StripInsetsProperty, value);
    }

    public static bool GetIsClosable(TabItem tab) => tab.GetValue(IsClosableProperty);
    public static void SetIsClosable(TabItem tab, bool value) => tab.SetValue(IsClosableProperty, value);

    public event EventHandler<TabEventArgs>? TabCloseRequested
    {
        add => AddHandler(TabCloseRequestedEvent, value);
        remove => RemoveHandler(TabCloseRequestedEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? AddTabRequested
    {
        add => AddHandler(AddTabRequestedEvent, value);
        remove => RemoveHandler(AddTabRequestedEvent, value);
    }

    public event EventHandler<TabReorderedEventArgs>? TabReordered
    {
        add => AddHandler(TabReorderedEvent, value);
        remove => RemoveHandler(TabReorderedEvent, value);
    }

    public event EventHandler<TabDraggedOutEventArgs>? TabDraggedOut
    {
        add => AddHandler(TabDraggedOutEvent, value);
        remove => RemoveHandler(TabDraggedOutEvent, value);
    }

    protected override Type StyleKeyOverride => typeof(TabControl);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_stripScroller != null)
        {
            _stripScroller.PointerWheelChanged -= OnStripWheel;
        }

        _stripScroller = e.NameScope.Find<ScrollViewer>("PART_StripScroller");
        _addButton = e.NameScope.Find<Button>("PART_AddButton");

        if (_stripScroller != null)
        {
            _stripScroller.PointerWheelChanged += OnStripWheel;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CanCloseTabsProperty)
        {
            for (var i = 0; i < ItemCount; i++)
            {
                if (ContainerFromIndex(i) is TabItem tab)
                {
                    tab.SetValue(IsClosableProperty, CanCloseTabs, BindingPriority.Style);
                }
            }
        }
        else if (change.Property == IsInTitleBarProperty || change.Property == CascadiaWindow.CaptionInsetsProperty)
        {
            PseudoClasses.Set(":titlebar", IsInTitleBar);
            StripInsets = IsInTitleBar ? CascadiaWindow.GetCaptionInsets(this) : default;
        }
        else if (change.Property == SelectedIndexProperty && SelectedIndex >= 0)
        {
            // Keep the active tab in view when the strip overflows (e.g. a newly added tab).
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                (ContainerFromIndex(SelectedIndex) as Control)?.BringIntoView());
        }
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        container.SetValue(IsClosableProperty, CanCloseTabs, BindingPriority.Style);
    }

    /// <summary>Moves the tab at <paramref name="oldIndex"/> to <paramref name="newIndex"/>, keeping it
    /// selected, and raises <see cref="TabReordered"/>. Returns false if the items can't be moved.</summary>
    public bool MoveTab(int oldIndex, int newIndex)
    {
        var count = ItemCount;
        if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0 || oldIndex >= count || newIndex >= count)
        {
            return false;
        }

        var wasSelected = SelectedIndex == oldIndex;
        object? item;

        if (ItemsSource is { } source)
        {
            if (source is not IList list || list.IsReadOnly || list.IsFixedSize)
            {
                return false;
            }

            item = list[oldIndex];
            if (FindMoveMethod(list) is { } move)
            {
                move.Invoke(list, [oldIndex, newIndex]);
            }
            else
            {
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);
            }
        }
        else
        {
            item = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, item);
        }

        if (wasSelected)
        {
            SelectedIndex = newIndex;
        }

        RaiseEvent(new TabReorderedEventArgs(TabReorderedEvent, item, oldIndex, newIndex));
        return true;
    }

    /// <summary>Asks for the tab holding <paramref name="item"/> to close (same as clicking its ×).</summary>
    public void RequestClose(object? item)
    {
        var args = new TabEventArgs(TabCloseRequestedEvent, item);
        RaiseEvent(args);
        if (args.Handled)
        {
            return;
        }

        if (CloseTabCommand is { } command)
        {
            if (command.CanExecute(item))
            {
                command.Execute(item);
            }

            return;
        }

        // No command and nobody handled the event: close it ourselves when we can.
        if (ItemsSource is IList { IsReadOnly: false, IsFixedSize: false } list)
        {
            list.Remove(item);
        }
        else if (ItemsSource is null && item is not null)
        {
            Items.Remove(item);
        }
    }

    private static MethodInfo? FindMoveMethod(IList list) =>
        list.GetType().GetMethod("Move", BindingFlags.Public | BindingFlags.Instance, [typeof(int), typeof(int)]);

    private object? ItemFromContainer(TabItem tab)
    {
        var index = IndexFromContainer(tab);
        return index >= 0 && index < ItemCount ? Items[index] : null;
    }

    private TabItem? FindTab(object? source)
    {
        for (var v = source as Visual; v != null && v != this; v = v.GetVisualParent())
        {
            if (v is TabItem tab && IndexFromContainer(tab) >= 0)
            {
                return tab;
            }
        }

        return null;
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button button)
        {
            return;
        }

        if (button.Name == "PART_CloseButton" && FindTab(button) is { } tab)
        {
            e.Handled = true;
            RequestClose(ItemFromContainer(tab));
        }
        else if (button == _addButton)
        {
            e.Handled = true;
            var args = new RoutedEventArgs(AddTabRequestedEvent);
            RaiseEvent(args);
            if (!args.Handled && AddTabCommand is { } command && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }

    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Route == RoutingStrategies.Tunnel)
        {
            // Middle-click closes, like every browser and terminal out there.
            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsMiddleButtonPressed && FindTab(e.Source) is { } middle && GetIsClosable(middle))
            {
                e.Handled = true;
                RequestClose(ItemFromContainer(middle));
            }

            return;
        }

        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed || !CanReorderTabs || e.Source is Button || IsInsideButton(e.Source))
        {
            return;
        }

        _pressedTab = FindTab(e.Source);
        _pressPoint = e.GetPosition(this);
        _isDragging = false;
    }

    private static bool IsInsideButton(object? source)
    {
        for (var v = source as Visual; v != null; v = v.GetVisualParent())
        {
            if (v is Button)
            {
                return true;
            }

            if (v is TabItem)
            {
                return false;
            }
        }

        return false;
    }

    private void OnTabPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedTab is null)
        {
            return;
        }

        var position = e.GetPosition(this);

        if (!_isDragging)
        {
            if (Math.Abs(position.X - _pressPoint.X) < DragThreshold && Math.Abs(position.Y - _pressPoint.Y) < DragThreshold)
            {
                return;
            }

            _isDragging = true;
            e.Pointer.Capture(this);
            _pressedTab.Classes.Add("dragging");
        }

        // Far enough off the strip: hand the tab over to the app for a cross-window drag.
        var strip = _stripScroller ?? (Visual)this;
        var stripPos = e.GetPosition(strip);
        if ((stripPos.Y < -TearOutDistance || stripPos.Y > strip.Bounds.Height + TearOutDistance)
            && HasTearOutHandlers())
        {
            var tab = _pressedTab;
            var item = ItemFromContainer(tab);
            EndDrag();
            e.Pointer.Capture(null);
            RaiseEvent(new TabDraggedOutEventArgs(TabDraggedOutEvent, item, e));
            return;
        }

        var from = IndexFromContainer(_pressedTab);
        var target = IndexAt(position.X);
        if (from >= 0 && target >= 0 && target != from)
        {
            var tab = _pressedTab;
            if (MoveTab(from, target))
            {
                // The container may have been recycled by the move; track the one now holding it.
                _pressedTab = ContainerFromIndex(target) as TabItem ?? tab;
                _pressedTab.Classes.Add("dragging");
                if (!ReferenceEquals(tab, _pressedTab))
                {
                    tab.Classes.Remove("dragging");
                }
            }
        }
    }

    private bool HasTearOutHandlers()
    {
        // RoutedEvent has no public "any subscribers?" query, so ask the route instead.
        var route = BuildEventRoute(TabDraggedOutEvent);
        return route.HasHandlers;
    }

    private int IndexAt(double x)
    {
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is not Control container || !container.IsVisible)
            {
                continue;
            }

            var topLeft = container.TranslatePoint(default, this);
            if (topLeft is null)
            {
                continue;
            }

            var left = topLeft.Value.X;
            var right = left + container.Bounds.Width;
            if (x >= left && x < right)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnTabPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        EndDrag();
    }

    private void EndDrag()
    {
        _pressedTab?.Classes.Remove("dragging");
        _pressedTab = null;
        _isDragging = false;
    }

    private void OnStripWheel(object? sender, PointerWheelEventArgs e)
    {
        // A plain vertical wheel scrolls the (horizontal) strip, like Windows Terminal.
        if (_stripScroller is { } scroller && e.Delta.X == 0 && e.Delta.Y != 0)
        {
            scroller.Offset = scroller.Offset.WithX(scroller.Offset.X - e.Delta.Y * 48);
            e.Handled = true;
        }
    }
}

/// <summary>Event data naming the tab (its data item, or the <see cref="TabItem"/>) concerned.</summary>
public class TabEventArgs(RoutedEvent routedEvent, object? item) : RoutedEventArgs(routedEvent)
{
    public object? Item { get; } = item;
}

public class TabReorderedEventArgs(RoutedEvent routedEvent, object? item, int oldIndex, int newIndex)
    : TabEventArgs(routedEvent, item)
{
    public int OldIndex { get; } = oldIndex;
    public int NewIndex { get; } = newIndex;
}

/// <summary>Raised when a tab is dragged off the strip. <see cref="PointerEventArgs"/> is the
/// live pointer event, suitable for <c>DragDrop.DoDragDropAsync</c>.</summary>
public class TabDraggedOutEventArgs(RoutedEvent routedEvent, object? item, PointerEventArgs pointerEventArgs)
    : TabEventArgs(routedEvent, item)
{
    public PointerEventArgs PointerEventArgs { get; } = pointerEventArgs;
}
