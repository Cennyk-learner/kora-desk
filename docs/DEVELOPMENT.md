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
| `src/DesktopHost.cs` | Explorer desktop attachment, topmost switching, native coordinates and dragging |
| `src/Manager.cs` | Planner executable: state, navigation, editing, backup, and preference actions handed to the skin |
| `src/Widget.cs` | Independent widget executable: tray menu, daily rollover, live refresh, skin switching |
| `assets/fonts` | Dot-matrix font used by the dot skin (SIL OFL) |
| `tests/Tests.cs` | Behavior tests using isolated temporary storage |
| `tests/SkinTests.cs` | Builds every skin, page, widget, editor and dialog in a WPF session and lays them out |

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

`KORA_DIAGNOSTICS=1` enables desktop attachment diagnostics in the chosen data directory and exposes a taskbar entry in topmost mode for UI inspection. This flag is off by default and is not needed for normal use.

## Process and data model

Each executable uses a named mutex for single-instance behavior and an event to reopen the running instance. The widget survives the planner's exit. Both read the same local store, refreshing when its timestamp changes. Mutations reload the current state under a path-specific named mutex, then replace the file atomically while retaining one previous save. A corrupt primary file is preserved separately before recovery from a valid backup.

Task occurrence is computed from the current rule and local calendar date. Completion keys contain `yyyy-MM-dd/task-id`; no time zone or UTC conversion is applied to a user's day. Recurrence edits apply to historical date views as well. A task editor's save replaces that task's current definition; simultaneous edits to the same task use the last saved version.

## Desktop integration

The widget locates the Explorer top-level window containing `SHELLDLL_DefView` and attaches as a child. Switching back to a normal window restores the popup style; the documented Win32 `SetParent` and window-style rules are followed. The Explorer hierarchy itself is not a stable public API. Alternative shells and Explorer restarts therefore need compatibility testing. Unsupported attachment falls back to a non-topmost ordinary tool window and displays that state.

Native widget positions are persisted in physical screen pixels. Positions are clamped to the nearest monitor's work area when the mode is applied. The WPF interface declares per-monitor DPI awareness. Validation on additional multi-monitor/DPI arrangements is welcome; do not infer coverage from one machine.

## Release verification

- Build and core behavior suite pass.
- Run `scripts/preview.ps1` and inspect the screenshots of every style and appearance for clipping and readable task rows.
- Switch style and appearance in Preferences and confirm both windows follow; change the Windows app mode while set to follow the system.
- Check tasks in both windows and verify the shared file and displayed count.
- Edit a title and recurrence through the UI, then restart and verify persistence.
- Switch desktop/topmost mode and check native parent/style diagnostics.
- Close the planner and verify the widget process remains alive.
- Test extracted portable files in isolation.
- Scan tracked text and release contents; exclude private data, logs, test executables, and development paths.
- Verify public repository visibility, remote commit, release files and checksums after publication.

An optional GitHub Actions workflow is provided in `docs/windows-ci.yml`. To enable it, place it at `.github/workflows/windows.yml` using a credential with workflow permission. It builds and runs core tests on Windows. Native desktop interaction still requires an interactive Windows session; the workflow does not validate Explorer embedding.
