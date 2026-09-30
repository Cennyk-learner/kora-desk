//! Desktop-layer behaviour for the widget window (ported from legacy DesktopHost.cs).
//!
//! The widget stays a normal top-level window. Unless "always on top" is on, we keep it
//! just above the desktop and below application windows, and stop it from being activated
//! or re-ordered by clicks. Position is native (physical) pixels, same as v1.

#[cfg(windows)]
mod imp {
    use std::sync::atomic::{AtomicBool, AtomicIsize, Ordering};
    use windows::Win32::Foundation::{HWND, LPARAM, LRESULT, POINT, RECT, WPARAM};
    use windows::Win32::Graphics::Gdi::{GetMonitorInfoW, MonitorFromPoint, MonitorFromRect, MONITORINFO, MONITOR_DEFAULTTONEAREST, MONITOR_DEFAULTTOPRIMARY};
    use windows::Win32::UI::Shell::{DefSubclassProc, SetWindowSubclass};
    use windows::Win32::UI::WindowsAndMessaging::*;

    static PINNED: AtomicBool = AtomicBool::new(false);
    static POSITIONING: AtomicBool = AtomicBool::new(false);
    static DESKTOP: AtomicIsize = AtomicIsize::new(0);
    static DESKTOP_ACTIVE: AtomicBool = AtomicBool::new(false);

    fn hwnd(h: isize) -> HWND {
        HWND(h as *mut _)
    }
    fn raw(h: HWND) -> isize {
        h.0 as isize
    }

    unsafe extern "system" fn subclass(h: HWND, msg: u32, w: WPARAM, l: LPARAM, _id: usize, _data: usize) -> LRESULT {
        if !PINNED.load(Ordering::Relaxed) {
            if msg == WM_MOUSEACTIVATE {
                // Clicks still reach the page, but the widget never steals focus.
                return LRESULT(MA_NOACTIVATE as isize);
            }
            if msg == WM_WINDOWPOSCHANGING && !POSITIONING.load(Ordering::Relaxed) {
                let pos = &mut *(l.0 as *mut WINDOWPOS);
                pos.flags |= SWP_NOZORDER;
            }
        }
        DefSubclassProc(h, msg, w, l)
    }

    pub fn install(handle: isize) {
        unsafe {
            let _ = SetWindowSubclass(hwnd(handle), Some(subclass), 0x4B4F, 0);
        }
    }

    unsafe extern "system" fn find_host(h: HWND, data: LPARAM) -> windows::core::BOOL {
        let found = FindWindowExW(Some(h), None, windows::core::w!("SHELLDLL_DefView"), None);
        if found.is_ok() {
            *(data.0 as *mut isize) = raw(h);
            return false.into();
        }
        true.into()
    }

    fn find_desktop() -> isize {
        let mut host: isize = 0;
        unsafe {
            let _ = EnumWindows(Some(find_host), LPARAM(&mut host as *mut isize as isize));
            if host == 0 {
                host = FindWindowW(windows::core::w!("Progman"), None).map(raw).unwrap_or(0);
            }
        }
        host
    }

    fn desktop_active(shell: isize) -> bool {
        unsafe {
            let active = raw(GetForegroundWindow());
            let progman = FindWindowW(windows::core::w!("Progman"), None).map(raw).unwrap_or(0);
            shell != 0 && (active == shell || active == progman)
        }
    }

    fn insert_after(me: isize) -> HWND {
        if PINNED.load(Ordering::Relaxed) {
            return HWND_TOPMOST;
        }
        let desktop = DESKTOP.load(Ordering::Relaxed);
        if DESKTOP_ACTIVE.load(Ordering::Relaxed) || desktop == 0 {
            return HWND_TOP;
        }
        unsafe {
            let mut above = GetWindow(hwnd(desktop), GW_HWNDPREV).map(raw).unwrap_or(0);
            if above == me {
                above = GetWindow(hwnd(me), GW_HWNDPREV).map(raw).unwrap_or(0);
            }
            if above == 0 || (GetWindowLongW(hwnd(above), GWL_EXSTYLE) as u32 & WS_EX_TOPMOST.0) != 0 {
                return HWND_TOP;
            }
            hwnd(above)
        }
    }

    fn place(me: isize, x: i32, y: i32, flags: SET_WINDOW_POS_FLAGS) {
        POSITIONING.store(true, Ordering::Relaxed);
        unsafe {
            if !PINNED.load(Ordering::Relaxed) {
                // Leaving the topmost band first, otherwise HWND_TOP keeps the topmost style.
                if (GetWindowLongW(hwnd(me), GWL_EXSTYLE) as u32 & WS_EX_TOPMOST.0) != 0 {
                    let _ = SetWindowPos(hwnd(me), Some(HWND_NOTOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
            let _ = SetWindowPos(hwnd(me), Some(insert_after(me)), x, y, 0, 0, flags);
        }
        POSITIONING.store(false, Ordering::Relaxed);
    }

    pub fn show_no_activate(me: isize) {
        unsafe {
            let _ = ShowWindow(hwnd(me), SW_SHOWNOACTIVATE);
        }
    }

    pub fn status(pinned: bool) -> &'static str {
        if pinned {
            "始终置顶"
        } else if DESKTOP.load(Ordering::Relaxed) != 0 {
            "桌面组件"
        } else {
            "桌面暂不可用 · 普通窗口"
        }
    }

    pub fn rect(me: isize) -> (i32, i32, i32, i32) {
        let mut r = RECT::default();
        unsafe {
            let _ = GetWindowRect(hwnd(me), &mut r);
        }
        (r.left, r.top, r.right - r.left, r.bottom - r.top)
    }

    /// Re-apply pin state and position. `x`/`y` of -9999 mean "default corner".
    /// Returns the native position that was applied.
    pub fn apply(me: isize, pinned: bool, x: f64, y: f64) -> (i32, i32) {
        PINNED.store(pinned, Ordering::Relaxed);
        let desktop = find_desktop();
        DESKTOP.store(desktop, Ordering::Relaxed);
        DESKTOP_ACTIVE.store(desktop_active(desktop), Ordering::Relaxed);
        let (_, _, w, h) = rect(me);
        unsafe {
            let mut mi = MONITORINFO { cbSize: std::mem::size_of::<MONITORINFO>() as u32, ..Default::default() };
            let primary = MonitorFromPoint(POINT { x: 0, y: 0 }, MONITOR_DEFAULTTOPRIMARY);
            let _ = GetMonitorInfoW(primary, &mut mi);
            let mut px = if x == -9999.0 { mi.rcWork.right - w - 24 } else { x as i32 };
            let mut py = if y == -9999.0 { mi.rcWork.top + 60 } else { y as i32 };
            let target = RECT { left: px, top: py, right: px + w, bottom: py + h };
            let mut ti = MONITORINFO { cbSize: std::mem::size_of::<MONITORINFO>() as u32, ..Default::default() };
            if GetMonitorInfoW(MonitorFromRect(&target, MONITOR_DEFAULTTONEAREST), &mut ti).as_bool() {
                px = px.min(ti.rcWork.right - w).max(ti.rcWork.left);
                py = py.min(ti.rcWork.bottom - h).max(ti.rcWork.top);
            }
            place(me, px, py, SWP_NOSIZE | SWP_NOACTIVATE);
            (px, py)
        }
    }

    /// Called every ~500 ms: follow Explorer when the desktop comes forward.
    pub fn watch(me: isize, pinned: bool) {
        if pinned {
            return;
        }
        let shell = find_desktop();
        let active = desktop_active(shell);
        if shell == DESKTOP.load(Ordering::Relaxed) && active == DESKTOP_ACTIVE.load(Ordering::Relaxed) {
            return;
        }
        DESKTOP.store(shell, Ordering::Relaxed);
        DESKTOP_ACTIVE.store(active, Ordering::Relaxed);
        place(me, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }
}

#[cfg(not(windows))]
mod imp {
    pub fn install(_h: isize) {}
    pub fn status(pinned: bool) -> &'static str {
        if pinned { "始终置顶" } else { "普通窗口" }
    }
    pub fn rect(_h: isize) -> (i32, i32, i32, i32) {
        (0, 0, 0, 0)
    }
    pub fn show_no_activate(_h: isize) {}
    pub fn apply(_h: isize, _p: bool, x: f64, y: f64) -> (i32, i32) {
        (x as i32, y as i32)
    }
    pub fn watch(_h: isize, _p: bool) {}
}

pub use imp::*;
