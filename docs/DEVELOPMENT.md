# Development

Kora Desk 2.0 is a Tauri 2 app: a Rust backend (`src-tauri/`) and a Svelte 5 + TypeScript front end (`src/`) compiled into one small exe.

## Prerequisites (Windows x64)

- Node.js 20+ and npm
- Rust stable (MSVC toolchain) and the Visual Studio Build Tools with the "Desktop development with C++" workload
- Microsoft Edge WebView2 runtime (already present on Windows 11)

## Commands

| Command | What it does |
|---|---|
| `npm install` | Install front-end dependencies |
| `npm run dev` | Vite dev server. Open `http://localhost:1420/?mock=rich&skin=dot&appearance=dark&page=stats` in a browser to view any page without Tauri (mock backend, browser localStorage). `&w=widget` renders the widget |
| `npm test` | Vitest: shuffle bag, give-up projection, streak and freeze cards, pomodoro settle/recovery, spaced review, XP, content checks |
| `npm run check` | `svelte-check` type check |
| `cargo test --manifest-path src-tauri/Cargo.toml --lib` | Rust store tests: v1 upgrade, atomic write, corruption recovery, import validation, concurrent writers |
| `npx tauri dev` | Run the real app with hot reload |
| `npx tauri build --no-bundle` | Release build. Output: `src-tauri/target/release/kora.exe` (about 3.4 MB) |

`KORA_DATA_DIR=<folder>` points the app at an isolated data folder for testing.

## Layout

| Path | Responsibility |
|---|---|
| `src-tauri/src/store.rs` | `plan.json` model (PascalCase, v1 compatible; new fields ride along in `extra`), validation and limits, atomic write, `.bak`, corruption recovery, v1→v2 upgrade, import/export |
| `src-tauri/src/desktop.rs` | Widget desktop-layer behaviour (Win32): stay above the desktop but below apps, no activation on click, topmost switching, monitor clamp |
| `src-tauri/src/lib.rs` | Commands, tray, single instance, window events, position saving, desktop watcher |
| `src/lib/*.ts` | Pure logic with tests: `bag`, `giveup`, `settle` (next-day settlement), `leave`, `streak`, `xp`, `achievements`, `pomodoro`, `review`, `exam`, `report` |
| `src/lib/app.svelte.ts` | Reactive state, serialised optimistic writes, event sync between the two windows |
| `src/lib/engine.svelte.ts` | Planner-window background duties: pomodoro settling, daily picks, reminders, achievements, tray text, global shortcuts |
| `src/lib/audio.ts`, `fx.ts` | WebAudio synthesis (no audio files) and the confetti canvas bus |
| `src/skins/base.css`, `skins.css` | Shared structure, and the five skins × light/dark as CSS tokens |
| `src/pages`, `src/components` | Pages (today, plan, focus, stats, give-up bank, care, favourites, settings), editor, give-up flow, widget |
| `src/data/quotes*.ts`, `words.ts` | 420+ quotes and daily words. Quote order is part of the shuffle bag's state: only append |

## Design notes

- One process, two windows (`main`, `widget`). Closing either hides it; the tray keeps the process alive. State changes are broadcast with the `state-changed` event; Rust preserves the stored widget position on every write.
- The pomodoro stores `EndsAt`, not a countdown, so restarts and sleep resolve correctly. Both windows disable WebView2 background throttling so a hidden planner still settles on time.
- XP, streaks, levels and the give-up projection are derived from history and never stored, so they cannot drift or double count.
- Give-ups are never created by a button: `settleDays` runs at startup/midnight and turns yesterday's (and any missed days') unfinished tasks into records, skipping leave days and tasks created later. The first run only sets the `Settled` marker.
- Sleep prevention uses `SetThreadExecutionState` on one dedicated thread (`keep_awake` command); full-screen uses the window API.
- Data version is 2. Files written by 1.x open unchanged; the original is kept once as `plan.json.v1.bak`.

## Adding a skin

1. Add `:root[data-skin='id'][data-mode='light'|'dark']` token blocks in `src/skins/skins.css` (colours, radii, fonts) and any small overrides.
2. Add it to `SKINS` in `src/pages/Settings.svelte`, to the allow-list in `src/lib/app.svelte.ts`, and to the progress mapping in `src/components/Progress.svelte`.

## Manual checklist (needs real Windows)

Desktop layer vs. topmost switching, dragging and position restore across monitors and DPI, click-through focus behaviour on the desktop layer, tray menu, toast notifications, global shortcuts, autostart, first run without WebView2.
