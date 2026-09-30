mod desktop;
mod store;

use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::Mutex;
use std::time::Duration;
use store::{State as PlanState, Store};
use tauri::menu::{MenuBuilder, MenuItemBuilder, PredefinedMenuItem};
use tauri::tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent};
use tauri::{AppHandle, Emitter, LogicalSize, Manager, WebviewWindow, WindowEvent};

pub struct Shared {
    store: Store,
    /// Last (pinned, x, y) pushed to the desktop layer.
    applied: Mutex<(bool, i32, i32)>,
    move_gen: AtomicU64,
    tray_tip: Mutex<String>,
}

type Res<T> = Result<T, String>;

fn widget(app: &AppHandle) -> Option<WebviewWindow> {
    app.get_webview_window("widget")
}

fn hwnd_of(w: &WebviewWindow) -> isize {
    w.hwnd().map(|h| h.0 as isize).unwrap_or(0)
}

fn show_main(app: &AppHandle, page: Option<String>) {
    if let Some(w) = app.get_webview_window("main") {
        let _ = w.show();
        let _ = w.unminimize();
        let _ = w.set_focus();
        if let Some(p) = page {
            let _ = app.emit("navigate", p);
        }
    }
}

fn place_widget(app: &AppHandle, state: &PlanState) {
    let Some(w) = widget(app) else { return };
    let h = hwnd_of(&w);
    if h == 0 {
        return;
    }
    let shared = app.state::<Shared>();
    let (x, y) = desktop::apply(h, state.settings.always_on_top, state.settings.x, state.settings.y);
    *shared.applied.lock().unwrap() = (state.settings.always_on_top, x, y);
}

fn broadcast(app: &AppHandle, state: &PlanState) {
    let _ = app.emit("state-changed", state);
}

#[tauri::command]
fn read_state(shared: tauri::State<Shared>) -> Res<serde_json::Value> {
    let state = shared.store.read()?;
    Ok(serde_json::json!({ "state": state, "notice": shared.store.take_notice(), "dir": shared.store.dir() }))
}

#[tauri::command]
fn write_state(app: AppHandle, shared: tauri::State<Shared>, state: PlanState) -> Res<PlanState> {
    // Window position belongs to the backend (it is saved when the widget is dragged).
    let saved = shared.store.read()?;
    let mut state = state;
    state.settings.x = saved.settings.x;
    state.settings.y = saved.settings.y;
    let pin_changed = state.settings.always_on_top != saved.settings.always_on_top;
    let state = shared.store.replace(state)?;
    broadcast(&app, &state);
    if pin_changed {
        place_widget(&app, &state);
    }
    Ok(state)
}

#[tauri::command]
fn import_state(app: AppHandle, shared: tauri::State<Shared>, path: String) -> Res<PlanState> {
    let state = shared.store.import(&PathBuf::from(path))?;
    broadcast(&app, &state);
    place_widget(&app, &state);
    Ok(state)
}

#[tauri::command]
fn export_state(shared: tauri::State<Shared>, path: String) -> Res<()> {
    shared.store.export(&PathBuf::from(path))
}

#[tauri::command]
fn write_text_file(path: String, text: String) -> Res<()> {
    std::fs::write(path, text.as_bytes()).map_err(|e| e.to_string())
}

#[tauri::command]
fn write_binary_file(path: String, bytes: Vec<u8>) -> Res<()> {
    std::fs::write(path, bytes).map_err(|e| e.to_string())
}

#[tauri::command]
fn open_folder(shared: tauri::State<Shared>) -> Res<()> {
    std::process::Command::new("explorer.exe").arg(shared.store.dir()).spawn().map(|_| ()).map_err(|e| e.to_string())
}

#[tauri::command]
fn open_main(app: AppHandle, page: Option<String>) {
    show_main(&app, page);
}

#[tauri::command]
fn widget_visible(app: AppHandle) -> bool {
    widget(&app).and_then(|w| w.is_visible().ok()).unwrap_or(false)
}

#[tauri::command]
fn widget_show(app: AppHandle, shared: tauri::State<Shared>) -> Res<()> {
    let state = shared.store.read()?;
    if let Some(w) = widget(&app) {
        desktop::show_no_activate(hwnd_of(&w));
    }
    place_widget(&app, &state);
    let _ = app.emit("widget-visibility", true);
    Ok(())
}

#[tauri::command]
fn widget_hide(app: AppHandle) {
    if let Some(w) = widget(&app) {
        let _ = w.hide();
    }
    let _ = app.emit("widget-visibility", false);
}

#[tauri::command]
fn widget_toggle(app: AppHandle, shared: tauri::State<Shared>) -> Res<()> {
    if widget_visible(app.clone()) {
        widget_hide(app);
        Ok(())
    } else {
        widget_show(app, shared)
    }
}

#[tauri::command]
fn widget_reset(app: AppHandle, shared: tauri::State<Shared>) -> Res<()> {
    let state = shared.store.update(|s| {
        s.settings.x = -9999.0;
        s.settings.y = -9999.0;
    })?;
    widget_show(app.clone(), shared)?;
    broadcast(&app, &state);
    Ok(())
}

#[tauri::command]
fn widget_resize(app: AppHandle, shared: tauri::State<Shared>, width: f64, height: f64) -> Res<()> {
    let Some(w) = widget(&app) else { return Ok(()) };
    let _ = w.set_size(LogicalSize::new(width.clamp(200.0, 700.0), height.clamp(120.0, 1400.0)));
    if w.is_visible().unwrap_or(false) {
        let state = shared.store.read()?;
        place_widget(&app, &state);
    }
    Ok(())
}

#[tauri::command]
fn widget_status(shared: tauri::State<Shared>) -> String {
    let pinned = shared.applied.lock().unwrap().0;
    desktop::status(pinned).to_string()
}

#[tauri::command]
fn toggle_top(app: AppHandle, shared: tauri::State<Shared>) -> Res<()> {
    let state = shared.store.update(|s| s.settings.always_on_top = !s.settings.always_on_top)?;
    broadcast(&app, &state);
    place_widget(&app, &state);
    Ok(())
}

#[tauri::command]
fn set_tray_tip(app: AppHandle, shared: tauri::State<Shared>, text: String) {
    let mut cur = shared.tray_tip.lock().unwrap();
    if *cur == text {
        return;
    }
    *cur = text.clone();
    if let Some(t) = app.tray_by_id("kora") {
        let _ = t.set_tooltip(Some(text));
    }
}

/// Keep the machine (and display) awake while a focus block runs.
#[tauri::command]
fn keep_awake(on: bool) {
    #[cfg(windows)]
    unsafe {
        use windows::Win32::System::Power::{SetThreadExecutionState, ES_CONTINUOUS, ES_DISPLAY_REQUIRED, ES_SYSTEM_REQUIRED};
        // The state belongs to the calling thread, so route it through one long-lived thread.
        static TX: std::sync::OnceLock<std::sync::mpsc::Sender<bool>> = std::sync::OnceLock::new();
        let tx = TX.get_or_init(|| {
            let (tx, rx) = std::sync::mpsc::channel::<bool>();
            std::thread::spawn(move || {
                while let Ok(on) = rx.recv() {
                    let flags = if on { ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED } else { ES_CONTINUOUS };
                    SetThreadExecutionState(flags);
                }
            });
            tx
        });
        let _ = tx.send(on);
    }
    #[cfg(not(windows))]
    let _ = on;
}

#[tauri::command]
fn quit(app: AppHandle) {
    app.exit(0);
}

fn save_position_later(app: &AppHandle) {
    let shared = app.state::<Shared>();
    let gen = shared.move_gen.fetch_add(1, Ordering::SeqCst) + 1;
    let app = app.clone();
    std::thread::spawn(move || {
        std::thread::sleep(Duration::from_millis(450));
        let shared = app.state::<Shared>();
        if shared.move_gen.load(Ordering::SeqCst) != gen {
            return;
        }
        let Some(w) = widget(&app) else { return };
        let h = hwnd_of(&w);
        if h == 0 || !w.is_visible().unwrap_or(false) {
            return;
        }
        let (x, y, _, _) = desktop::rect(h);
        {
            let mut applied = shared.applied.lock().unwrap();
            if applied.1 == x && applied.2 == y {
                return;
            }
            applied.1 = x;
            applied.2 = y;
        }
        let _ = shared.store.update(|s| {
            s.settings.x = x as f64;
            s.settings.y = y as f64;
        });
    });
}

fn build_tray(app: &AppHandle) -> tauri::Result<()> {
    let open = MenuItemBuilder::with_id("open", "打开计划").build(app)?;
    let show = MenuItemBuilder::with_id("toggle_widget", "显示 / 隐藏组件").build(app)?;
    let top = MenuItemBuilder::with_id("toggle_top", "切换桌面 / 置顶").build(app)?;
    let focus = MenuItemBuilder::with_id("focus", "开始 / 暂停番茄钟").build(app)?;
    let quit = MenuItemBuilder::with_id("quit", "退出").build(app)?;
    let menu = MenuBuilder::new(app)
        .items(&[&open, &focus, &show, &top, &PredefinedMenuItem::separator(app)?, &quit])
        .build()?;
    let mut builder = TrayIconBuilder::with_id("kora")
        .tooltip("Kora Desk · 刻序")
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id().as_ref() {
            "open" => show_main(app, None),
            "toggle_widget" => {
                let _ = widget_toggle(app.clone(), app.state::<Shared>());
            }
            "toggle_top" => {
                let _ = toggle_top(app.clone(), app.state::<Shared>());
            }
            "focus" => {
                let _ = app.emit("focus-toggle", ());
            }
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click { button: MouseButton::Left, button_state: MouseButtonState::Up, .. } = event {
                show_main(tray.app_handle(), None);
            }
        });
    if let Some(icon) = app.default_window_icon() {
        builder = builder.icon(icon.clone());
    }
    builder.build(app)?;
    Ok(())
}

pub fn run() {
    let dir = Store::default_dir();
    let store = match Store::new(&dir) {
        Ok(s) => s,
        Err(e) => {
            eprintln!("cannot open data folder {}: {e}", dir.display());
            std::process::exit(1);
        }
    };
    let shared = Shared { store, applied: Mutex::new((false, i32::MIN, i32::MIN)), move_gen: AtomicU64::new(0), tray_tip: Mutex::new(String::new()) };

    tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| show_main(app, None)))
        .plugin(tauri_plugin_autostart::init(tauri_plugin_autostart::MacosLauncher::LaunchAgent, Some(vec!["--minimized"])))
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_dialog::init())
        .manage(shared)
        .invoke_handler(tauri::generate_handler![
            read_state,
            write_state,
            import_state,
            export_state,
            write_text_file,
            write_binary_file,
            open_folder,
            open_main,
            widget_visible,
            widget_show,
            widget_hide,
            widget_toggle,
            widget_reset,
            widget_resize,
            widget_status,
            toggle_top,
            set_tray_tip,
            keep_awake,
            quit
        ])
        .on_window_event(|window, event| match event {
            WindowEvent::CloseRequested { api, .. } => {
                // Closing the planner (or widget) hides it; the process stays in the tray.
                api.prevent_close();
                let _ = window.hide();
                if window.label() == "widget" {
                    let _ = window.app_handle().emit("widget-visibility", false);
                }
            }
            WindowEvent::Moved(_) if window.label() == "widget" => save_position_later(window.app_handle()),
            _ => {}
        })
        .setup(|app| {
            let handle = app.handle().clone();
            build_tray(&handle)?;
            let minimized = std::env::args().any(|a| a == "--minimized");
            if let Some(w) = widget(&handle) {
                desktop::install(hwnd_of(&w));
                if let Ok(state) = handle.state::<Shared>().store.read() {
                    desktop::show_no_activate(hwnd_of(&w));
                    place_widget(&handle, &state);
                }
            }
            if !minimized {
                show_main(&handle, None);
            }
            // Keep the widget glued to the desktop layer when Explorer reorders windows.
            let watcher = handle.clone();
            std::thread::spawn(move || loop {
                std::thread::sleep(Duration::from_millis(500));
                let inner = watcher.clone();
                let _ = watcher.run_on_main_thread(move || {
                    if let Some(w) = widget(&inner) {
                        if w.is_visible().unwrap_or(false) {
                            let pinned = inner.state::<Shared>().applied.lock().unwrap().0;
                            desktop::watch(hwnd_of(&w), pinned);
                        }
                    }
                });
            });
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running Kora Desk");
}
