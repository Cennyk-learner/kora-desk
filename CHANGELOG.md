# Changelog

## 2.0.0

### Architecture
- Rebuilt on Tauri 2 (Rust + Svelte). One exe, one process, two windows; closing the planner leaves the tray and widget running. Replaces the two-exe/one-dll design, the named mutex/event activation and the one-second file polling. Release exe is about 3.4 MB.
- `plan.json` is upgraded from version 1 to 2 on first open; the original is kept as `plan.json.v1.bak`. Fields are only added, never removed. Atomic writes, `.bak` and corruption recovery are preserved and covered by `cargo test`.
- The five interface styles are retained as CSS token sets, each with light and dark versions.
- The old .NET 1.x source was removed from the tree (it remains in git history and the 1.x releases).

### New
- Check-off feedback: animated tick, smooth progress, rolling numbers, confetti, a rising-pitch combo scale, and a day-complete celebration. All sounds are synthesised; mute, volume and reduced-motion are respected.
- Pomodoro: all choices (durations, task, auto-check, noise, strict, keep-awake, full-screen) are made on the focus page before starting. Full-screen focus and sleep prevention while a block runs. Restart-safe timing, widget mini timer, tray countdown, notifications and global shortcuts.
- Daily quote (420+ entries, shuffle bag, no repeat within a pass, favourites) and daily word.
- Automatic give-up settlement: on the next run, each task left undone the day before is recorded (0.1 day each) with a yearly projection, monthly chart, most-missed tasks and optional reasons. There is no give-up button.
- Leave: mark a day with a reason; leave days are excluded from settlement and streaks, are summarised on the give-up page, and 3+ leave days in a week warn.
- Streaks with monthly freeze cards, year heatmap, weekly/monthly completion, group share, focus history, estimate vs. actual, XP levels, 12 achievements, weekly summary image, CSV export.
- Exam countdown with a suggested daily amount, daily commitment and evening review, spaced review tasks (1/2/4/7/15 days).
- Care page: mood, three small joys, water, breathing exercises, rewards. Eye-care, sitting and bedtime reminders. Autostart.

## 1.1.1

### Fixed
- Switching from always-on-top to desktop mode could leave an invisible but clickable widget. Desktop mode now manages native top-level window ordering without reparenting the transparent WPF surface into Explorer. All five interface styles are retained.
- Show Widget now signals an existing process before considering a launch. Rapid repeated requests and requests during startup are coalesced. Windows first-launch security prompts remain under the user's control.
- Startup work runs once even if WPF raises Loaded again; polling errors cannot recursively open identical modal dialogs every second.
- Development data directories have isolated activation channels. Preview rendering no longer consumes a running app's wake signal.

### Verification
- Added native window lifecycle checks for every style in light and dark, live widget mode synchronization and wake tests, and polling error recovery tests.

## 1.1.0

### New
- Five interface styles: macOS (default), Windows 11 Fluent, Material 3 Expressive, IBM Carbon, and a dot-matrix instrument style. Each has a light and a dark version, and a matching desktop widget.
- Appearance setting: follow Windows, light, or dark. Follows changes made while the app is running.
- Preferences page lists the styles; the planner and the widget switch together.
- `scripts/preview.ps1` renders every style and appearance to images. `--theme`, `--appearance`, `--page` and `--out` work with `--preview`.
- Themed date field, choice control, menu and dialogs replace the native ones, which were unreadable in dark mode.

### Fixed
- The planner stopped following midnight after you had moved to another day or week; it now keeps following whenever you are looking at today.
- Saving a task whose group came from an imported backup silently moved it to 生活. Every group in use is now offered in the editor.
- The editor read a one-off task's saved date with the system calendar. On a Thai Buddhist or Hijri calendar the date picker showed the wrong year, and saving stored that wrong date. Dates now always use the Gregorian calendar.
- A check-off that failed to save left the box ticked. It is put back and the error is shown.
- Checking a task rebuilt the page and dropped keyboard focus and the scroll position. Both are kept.
- The widget height ignored group headings, so a short list scrolled for no reason.
- With no desktop layer available the widget was re-attached and moved every three seconds. It now retries at a slower pace and only when the desktop layer appears.
- Finishing a drag re-applied the whole window mode and made the widget flicker.
- Saving a task with an empty name showed a generic message. It now asks for a name.
- The planner drew itself twice at startup.
- Files written by 1.0 have no theme fields; they load with the default style.
