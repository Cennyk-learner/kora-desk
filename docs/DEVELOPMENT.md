# Development

Kora Desk targets .NET Framework 4.8 on Windows x64. Its C# source stays compatible with the framework's bundled compiler. It uses no external packages.

| Source | Responsibility |
|---|---|
| `src/Core.cs` | Weekly and one-off tasks, preset, per-date completion, preferences (window mode, theme, appearance), schema validation, mutex-protected atomic JSON storage, culture-independent date text |
| `src/Xaml.cs` | XAML text for the templated controls (button, check box, text box, scroll bar). No WPF types, so it can be linted anywhere |
| `src/Ui.cs` | Brush cache, text and icon helpers, buttons, window frame, check rows, and the drawn elements (progress ring, wave, cookie badge, dot grid) |
| `src/Controls.cs` | Themed segmented control, date field, popup menu, dialog, and the task editor |
| `src/Skin.cs` | `Skin` base class, palettes, the skin catalog, appearance detection, and the weekly plan and preferences pages shared by every skin |
| `src/SkinMac.cs` `SkinFluent.cs` `SkinMaterial.cs` `SkinCarbon.cs` `SkinDot.cs` | One file per interface style. Each builds the planner window (daily page and chrome) and the desktop widget, in light and dark |
| `src/DesktopHost.cs` | Native top-level window ordering, topmost switching, native coordinates and dragging |
| `src/Activation.cs` | Existing-process wake signals, startup coalescing, data-directory isolation |
| src/Manager.cs | Planner executable: state, navigation, editing, backup, and preference actions handed to the skin |
| `src/Widget.cs` | Independent widget executable: tray menu, daily rollover, live refresh, skin switching |
| `assets/fonts` | Dot-matrix font used by the dot skin (SIL OFL) |
| `tests/Tests.cs` | Behavior tests using isolated temporary storage |
| `tests/DesktopTests.cs` | Real HWND lifecycle, live widget synchronization, activation and polling error regressions |
| 	ests/SkinTests.cs | Builds every skin, page, widget, editor and dialog in a WPF session and lays them out |

The source stays compatible with the C# 5 compiler bundled with .NET Framework 4.8: no string interpolation, `?.`, `nameof`, expression-bodied members or auto-property initializers.

## Interface styles

A skin owns the whole look. `Skin.BuildPlanner` returns the content of the planner window for one page; `Skin.BuildWidget` returns the widget. The frame (radius, border, shadow, transparency) is added by `Manager` and `Widget`. To add a style:

1. Add a `Skin` subclass with light and dark palettes, fonts, corner radii and a check box template (`CheckXaml`).
2. Register it in `Skins.Create` and `Skins.Catalog`.
3. Add it to the list in `scripts/preview.ps1`.

The weekly plan and preferences pages come from `Pages` and only use the skin's tokens, so a new skin does not have to draw them.

The chosen style and appearance (`system`, `light`, `dark`) are stored in `plan.json` under `Settings`, so both executables follow a change within a second. Files written by 1.0 have neither field and open with the default style.

Native combo boxes, date pickers, context menus and message boxes ignore the skin and are unreadable in dark mode, so the app draws its own (`Controls`, `Dialogs`, `Pages`).

## Build and test

Run `scripts/build.ps1 -Test` on Windows. A nonzero compiler or test exit code fails the build. The suite checks the full weekly preset, date isolation, repeated checkoffs, one-off tasks, concurrent store instances, Unicode and identity preservation, import validation, backup roundtrips, corruption recovery, files saved before themes existed, and date text under non-Gregorian system calendars. The WPF part builds all five skins in light and dark on every page and lays them out at the real window size; it prints a NOTE when a five-task day no longer fits without scrolling.

`KORA_DATA_DIR` can redirect application storage for development and testing. Do not point a test run at a real user's data directory. `--preview` on either executable renders its actual WPF window into a PNG beside the executable and exits; the manager does not launch the widget in preview mode. `--theme=<mac|fluent|material|carbon|dot>`, `--appearance=<light|dark>`, `--page=<0|1|2>` (planner only) and `--out=<file>` choose what is rendered. `scripts/preview.ps1` renders every combination with the example plan in a throwaway data folder.

`KORA_DIAGNOSTICS=1` exposes a widget taskbar entry in both modes for UI inspection. This flag is off by default and is not needed for normal use.

## Process and data model

Each executable uses a named mutex for single-instance behavior and an event to reopen the running instance. The widget survives the planner's exit. Both read the same local store, refreshing when its timestamp changes. Mutations reload the current state under a path-specific named mutex, then replace the file atomically while retaining one previous save. A corrupt primary file is preserved separately before recovery from a valid backup.

Task occurrence is computed from the current rule and local calendar date. Completion keys contain `yyyy-MM-dd/task-id`; no time zone or UTC conversion is applied to a user's day. Recurrence edits apply to historical date views as well. A task editor's save replaces that task's current definition; simultaneous edits to the same task use the last saved version.

## Desktop integration

The widget keeps the same WPF layered top-level HWND for its lifetime. `DesktopHost` never calls `SetParent` or converts it to `WS_CHILD`. Native `SetWindowPos` ordering keeps desktop mode just above Explorer and below ordinary apps; foreground desktop transitions are observed on a 500 ms timer. Clicking a desktop-mode task does not activate and raise the entire widget. Always-on-top uses the normal WPF/native topmost flag.

The shell's `SHELLDLL_DefView`/`Progman` identity is still an Explorer convention, not a Windows widget API. Alternative shells, Show Desktop transitions, Explorer restarts, virtual desktops, and additional monitor/DPI combinations need interactive compatibility testing. The automated tests assert stable HWND identity, root-window style, visibility, topmost state, hide/show, and timer-driven synchronization. They do not prove on-screen composition or coverage of every shell configuration.

Activation sends a named event before launching an executable, with reentrancy/startup protection. Shell execution is retained so normal Windows download security prompts are not bypassed. Test data directories use separate mutex/event names. Repeated polling errors are shown once until an intervening successful refresh.

Native widget positions are persisted in physical screen pixels. Positions are clamped to the nearest monitor's work area when the mode is applied. The WPF interface declares per-monitor DPI awareness. Validation on additional multi-monitor/DPI arrangements is welcome; do not infer coverage from one machine.

## Release verification

- Build and core behavior suite pass.
- Run `scripts/preview.ps1` and inspect the screenshots of every style and appearance for clipping and readable task rows.
- Switch style and appearance in Preferences and confirm both windows follow; change the Windows app mode while set to follow the system.
- Check tasks in both windows and verify the shared file and displayed count.
- Edit a title and recurrence through the UI, then restart and verify persistence.
- Switch desktop/topmost mode repeatedly and visually verify the widget without changing theme; test Show Desktop separately.
- Close the planner and verify the widget process remains alive.
- Test extracted portable files in isolation.
- Scan tracked text and release contents; exclude private data, logs, test executables, and development paths.
- Verify public repository visibility, remote commit, release files and checksums after publication.

An optional GitHub Actions workflow is provided in `docs/windows-ci.yml`. To enable it, place it at `.github/workflows/windows.yml` using a credential with workflow permission. It builds and runs core tests on Windows. Native desktop interaction still requires an interactive Windows session; the workflow does not validate desktop composition.
