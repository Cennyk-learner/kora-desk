# Development

Kora Desk targets .NET Framework 4.8 on Windows x64. Its C# source stays compatible with the framework's bundled compiler. It uses no external packages.

| Source | Responsibility |
|---|---|
| `src/Core.cs` | Weekly and one-off tasks, preset, per-date completion, schema validation, mutex-protected atomic JSON storage |
| `src/Ui.cs` | Shared WPF styles, task rows, accessible checkboxes, task editor, preview rendering |
| `src/DesktopHost.cs` | Explorer desktop attachment, topmost switching, native coordinates and dragging |
| `src/Manager.cs` | Planner executable, day/week navigation, editing, backup UI, preferences |
| `src/Widget.cs` | Independent widget executable, tray menu, daily rollover, live refresh |
| `tests/Tests.cs` | Behavior tests using isolated temporary storage |

## Build and test

Run `scripts/build.ps1 -Test` on Windows. A nonzero compiler or test exit code fails the build. The suite checks the full weekly preset, date isolation, repeated checkoffs, one-off tasks, concurrent store instances, Unicode and identity preservation, import validation, backup roundtrips, and corruption recovery.

`KORA_DATA_DIR` can redirect application storage for development and testing. Do not point a test run at a real user's data directory. `--preview` on either executable renders its actual WPF window into a PNG beside the executable and exits; the manager does not launch the widget in preview mode.

`KORA_DIAGNOSTICS=1` enables desktop attachment diagnostics in the chosen data directory and exposes a taskbar entry in topmost mode for UI inspection. This flag is off by default and is not needed for normal use.

## Process and data model

Each executable uses a named mutex for single-instance behavior and an event to reopen the running instance. The widget survives the planner's exit. Both read the same local store, refreshing when its timestamp changes. Mutations reload the current state under a path-specific named mutex, then replace the file atomically while retaining one previous save. A corrupt primary file is preserved separately before recovery from a valid backup.

Task occurrence is computed from the current rule and local calendar date. Completion keys contain `yyyy-MM-dd/task-id`; no time zone or UTC conversion is applied to a user's day. Recurrence edits apply to historical date views as well. A task editor's save replaces that task's current definition; simultaneous edits to the same task use the last saved version.

## Desktop integration

The widget locates the Explorer top-level window containing `SHELLDLL_DefView` and attaches as a child. Switching back to a normal window restores the popup style; the documented Win32 `SetParent` and window-style rules are followed. The Explorer hierarchy itself is not a stable public API. Alternative shells and Explorer restarts therefore need compatibility testing. Unsupported attachment falls back to a non-topmost ordinary tool window and displays that state.

Native widget positions are persisted in physical screen pixels. Positions are clamped to the nearest monitor's work area when the mode is applied. The WPF interface declares per-monitor DPI awareness. Validation on additional multi-monitor/DPI arrangements is welcome; do not infer coverage from one machine.

## Release verification

- Build and core behavior suite pass.
- Inspect actual WPF screenshots for clipping and readable task rows.
- Check tasks in both windows and verify the shared file and displayed count.
- Edit a title and recurrence through the UI, then restart and verify persistence.
- Switch desktop/topmost mode and check native parent/style diagnostics.
- Close the planner and verify the widget process remains alive.
- Test extracted portable files in isolation.
- Scan tracked text and release contents; exclude private data, logs, test executables, and development paths.
- Verify public repository visibility, remote commit, release files and checksums after publication.

An optional GitHub Actions workflow is provided in `docs/windows-ci.yml`. To enable it, place it at `.github/workflows/windows.yml` using a credential with workflow permission. It builds and runs core tests on Windows. Native desktop interaction still requires an interactive Windows session; the workflow does not validate Explorer embedding.
