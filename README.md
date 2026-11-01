# Avalonia Cascadia Theme

**CascadiaUI** is a theme for [Avalonia 12](https://avaloniaui.net) that mixes Fluent and macOS
into one look that feels at home on Windows, macOS, Linux and Android:

- **Neutral light and dark palettes.** Light uses soft greys around white surfaces; dark uses deep greys, never pure black.
- **One customisable accent.** Every hover, pressed, selection, focus-ring and link shade is derived from it, and it can be changed at runtime. The default is a deep green.
- **Accessible accent fills.** Accent backgrounds that carry text (the `accent` button, checked toggle buttons) use the accent adjusted just enough to keep that text at WCAG AA (4.5:1). Text-free uses (focus ring, checkboxes, toggle switches, selection tints) keep the exact brand colour. So a bright accent such as `#0A84FF` still gives accessible buttons.
- **Windows Terminal / SourceGit-style tabs.** The tabs sit in the title bar. They can be closed (× or middle click), added (+) and reordered by dragging. A tab dragged off the strip raises an event, so the app can move it to another window.
- **macOS touches.** Segmented "pill" tabs, rounded inset list rows, a soft focus ring on text fields, flat cards.
- **Native caption buttons.** Windows keeps its own (snap layouts work), macOS keeps the traffic lights, and on Linux the theme draws flat Terminal-like ones.
- **Feedback controls.** `BusyArea`, `ProgressRing` and toast notifications (`ToastHost` + `ToastManager`).

It is built on top of Avalonia's `FluentTheme`. Every stock control keeps Fluent's tested templates,
and Cascadia re-skins them through resources. Cascadia's own templates replace only the controls whose look
differs too much (tabs, window, busy overlay, toasts).

| Light | Dark |
| --- | --- |
| ![Buttons, light](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/01-buttons-inputs-light.png) | ![Buttons, dark](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/01-buttons-inputs-dark.png) |
| ![Lists, light](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/02-lists-trees-light.png) | ![Lists, dark](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/02-lists-trees-dark.png) |
| ![Tabs, light](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/03-tabs-light.png) | ![Tabs, dark](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/03-tabs-dark.png) |
| ![Feedback, light](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/04-feedback-light.png) | ![Feedback, dark](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/04-feedback-dark.png) |
| ![Text and surfaces, light](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/05-text-surfaces-light.png) | ![Text and surfaces, dark](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/05-text-surfaces-dark.png) |

### In use

| [SharpMediaInfo](https://github.com/simonebortolin/SharpMediaInfo) | [Label Studio 200B](https://github.com/simonebortolin/Letra200bSharp) |
| --- | --- |
| ![SharpMediaInfo](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/app-sharpmediainfo-light.png) | ![Label Studio 200B](https://raw.githubusercontent.com/simonebortolin/CascadiaUI/main/docs/screenshots/app-labelstudio-dark.png) |

## Getting started

```xml
<!-- App.axaml -->
<Application xmlns:cascadia="https://github.com/simonebortolin/CascadiaUI"
             RequestedThemeVariant="Default">
  <Application.Styles>
    <cascadia:CascadiaTheme Accent="#16834A" Density="Comfortable" />
  </Application.Styles>
</Application>
```

`CascadiaTheme` replaces `<FluentTheme />` (it includes it), so remove that line.
The font defaults to Inter: call `.WithInterFont()` on your `AppBuilder`, or
override `ContentControlThemeFontFamily`.

Change the accent, density or light/dark at runtime:

```csharp
CascadiaTheme.Current!.Accent = Color.Parse("#0A72D1");
CascadiaTheme.Current!.Density = CascadiaDensity.Compact;   // 13px text, SourceGit-dense
Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
```

## Tabs in the title bar

```xml
<cascadia:CascadiaWindow ExtendContentIntoTitleBar="True" Title="My app">
  <cascadia:CascadiaTabControl IsInTitleBar="True"
                               CanCloseTabs="True"
                               ShowAddButton="True"
                               ItemsSource="{Binding Tabs}"
                               SelectedItem="{Binding SelectedTab}"
                               AddTabCommand="{Binding NewTabCommand}"
                               CloseTabCommand="{Binding CloseTabCommand}">
    <cascadia:CascadiaTabControl.StripHeader>
      <Menu> … </Menu>              <!-- optional: lives left of the tabs -->
    </cascadia:CascadiaTabControl.StripHeader>
  </cascadia:CascadiaTabControl>
</cascadia:CascadiaWindow>
```

- **Reordering** moves the item in your `ItemsSource`. `ObservableCollection<T>.Move` is used, so tab
  views aren't recreated. `TabReordered` reports the move.
- **Closing**: `TabCloseRequested` is raised first (set `Handled` to veto), then `CloseTabCommand`
  runs with the item. With neither, the item is removed from the collection.
  `CascadiaTabControl.IsClosable="False"` on a `TabItem` pins it.
- **Tear-out**: dragging a tab well above or below the strip raises `TabDraggedOut` with the
  `PointerPressedEventArgs` that started the drag, ready for `DragDrop.DoDragDropAsync`.
- Without `ExtendContentIntoTitleBar`, `CascadiaWindow` draws its own title bar (title plus
  `TitleBarContent`). Anything in your own title-bar content that needs clicks should set
  `cascadia:CascadiaWindow.IsTitleBarInteractive="True"`. Mark extra drag areas with
  `IsTitleBarDragArea`.

A plain `TabControl` gets the same Terminal look. Use `Classes="pills"` for the macOS segmented
look used for in-page navigation.

## Feedback

```xml
<Panel>
  <cascadia:BusyArea IsBusy="{Binding IsScanning}" BusyText="Scanning…">
    <ListBox ItemsSource="{Binding Devices}" />
  </cascadia:BusyArea>

  <!-- last in the panel so it floats above everything -->
  <cascadia:ToastHost Manager="{Binding Toasts}" />
</Panel>
```

```csharp
public ToastManager Toasts { get; } = new();
Toasts.Show("Printed", "Label sent.", ToastSeverity.Success);    // auto-dismisses; click to dismiss
```

## Style classes

| Class | On | Effect |
| --- | --- | --- |
| `accent`, `subtle`, `danger`, `icon` | `Button` (`subtle`/`icon` also `ToggleButton`) | primary, borderless, destructive, square |
| `h1` `h2` `h3` `caption` `secondary` `mono` `link` | `TextBlock` | type ramp, secondary text, monospace (Cascadia Mono → Consolas → SF Mono → DejaVu), link |
| `card`, `statusbar`, `overlay` | `Border` | grouping surface, bottom status bar, floating panel |
| `pills` | `TabControl` | macOS segmented tabs |
| `flat` | `ListBox` | no frame |

Every colour is a resource you can override in `Application.Resources`, e.g.
`CascadiaWindowBackgroundBrush`, `CascadiaSurfaceBrush`, `CascadiaTitleBarBackgroundBrush`,
`CascadiaCardBrush`, `CascadiaTextSecondaryBrush`, `CascadiaAccentSubtleBrush`. See
[`Tokens.axaml`](https://github.com/simonebortolin/CascadiaUI/blob/main/src/CascadiaUI/Themes/Tokens.axaml).

## Gallery

`samples/CascadiaUI.Gallery` shows every control
[SharpMediaInfo](https://github.com/simonebortolin/SharpMediaInfo) and
[Label Studio 200B](https://github.com/simonebortolin/Letra200bSharp) use:

```sh
dotnet run --project samples/CascadiaUI.Gallery
```

The test suite (`dotnet test`) renders every gallery page in light and dark through real Skia into
`artifacts/screenshots`. CI uploads those images on every run.

## Releases

The version lives in [`BuildVersion.props`](https://github.com/simonebortolin/CascadiaUI/blob/main/BuildVersion.props) (`<VersionPrefix>`). CI does the rest:

- **Pull request**: builds, tests, packs `X.Y.Z-pr.<PR>.<run>` and attaches the package plus the
  Windows/Linux/macOS gallery builds to a pre-release, which is replaced on every push and deleted
  when the PR closes. It warns if `vX.Y.Z` is already released.
- **Merge to `main`**: if `vX.Y.Z` doesn't exist yet, CI tags it, creates the GitHub Release (with the
  package, the gallery builds and checksums) and publishes `CascadiaUI X.Y.Z` to GitHub Packages and
  to nuget.org via [Trusted Publishing](https://www.nuget.org/account/trustedpublishing), which needs
  the `NUGET_USER` repository variable. A change that should not release, such as a docs or CI
  change, carries `[no-new-version]` in its commit message.

## License

MIT. Window-chrome ideas come from [SourceGit](https://github.com/sourcegit-scm/sourcegit) (MIT) and
Windows Terminal. "Cascadia" is the theme's name only; this project is not affiliated with Microsoft.
