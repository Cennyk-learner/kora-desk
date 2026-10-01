import type { State } from './types';
import { withDefaults } from './model';
import { mockState } from './mock';

declare global {
  interface Window {
    __TAURI_INTERNALS__?: unknown;
  }
}

export const isTauri = typeof window !== 'undefined' && '__TAURI_INTERNALS__' in window;

export interface Loaded {
  state: State;
  notice: string | null;
  dir: string;
}

type Listener = (s: State) => void;
const browserListeners = new Set<Listener>();
let browserState: State | null = null;

async function core() {
  return import('@tauri-apps/api/core');
}

export async function readState(): Promise<Loaded> {
  if (isTauri) {
    const { invoke } = await core();
    const r = await invoke<{ state: State; notice: string | null; dir: string }>('read_state');
    return { state: withDefaults(r.state), notice: r.notice, dir: r.dir };
  }
  if (!browserState) {
    const saved = safeGet('kora-mock-state');
    const q = new URLSearchParams(location.search);
    browserState = withDefaults(saved ? JSON.parse(saved) : mockState(q.get('mock') === 'rich'));
    if (q.get('skin')) browserState.Settings.Theme = q.get('skin');
    if (q.get('appearance')) browserState.Settings.Appearance = q.get('appearance');
  }
  return { state: structuredClone(browserState), notice: null, dir: '(browser preview)' };
}

function safeGet(k: string): string | null {
  try {
    return localStorage.getItem(k);
  } catch {
    return null;
  }
}

export async function writeState(state: State): Promise<State> {
  if (isTauri) {
    const { invoke } = await core();
    return withDefaults(await invoke<State>('write_state', { state }));
  }
  browserState = structuredClone(state);
  try {
    if (new URLSearchParams(location.search).get('mock') !== 'rich') localStorage.setItem('kora-mock-state', JSON.stringify(state));
  } catch {
    /* preview only */
  }
  browserListeners.forEach((l) => l(structuredClone(state)));
  return structuredClone(state);
}

export async function onStateChanged(cb: Listener): Promise<() => void> {
  if (isTauri) {
    const { listen } = await import('@tauri-apps/api/event');
    return listen<State>('state-changed', (e) => cb(withDefaults(e.payload)));
  }
  browserListeners.add(cb);
  return () => browserListeners.delete(cb);
}

export async function onEvent<T>(name: string, cb: (payload: T) => void): Promise<() => void> {
  if (!isTauri) return () => {};
  const { listen } = await import('@tauri-apps/api/event');
  return listen<T>(name, (e) => cb(e.payload));
}

export async function call<T = void>(cmd: string, args?: Record<string, unknown>): Promise<T | undefined> {
  if (!isTauri) return undefined;
  const { invoke } = await core();
  return invoke<T>(cmd, args);
}

export async function windowLabel(): Promise<string> {
  if (!isTauri) return new URLSearchParams(location.search).get('w') ?? 'main';
  const { getCurrentWindow } = await import('@tauri-apps/api/window');
  return getCurrentWindow().label;
}

export async function winAction(kind: 'minimize' | 'close' | 'drag'): Promise<void> {
  if (!isTauri) return;
  const { getCurrentWindow } = await import('@tauri-apps/api/window');
  const w = getCurrentWindow();
  if (kind === 'minimize') await w.minimize();
  else if (kind === 'close') await w.hide();
  else await w.startDragging();
}

export async function saveDialog(name: string, ext: string, label: string): Promise<string | null> {
  if (!isTauri) return null;
  const { save } = await import('@tauri-apps/plugin-dialog');
  return save({ defaultPath: name, filters: [{ name: label, extensions: [ext] }] });
}

export async function openDialog(ext: string, label: string): Promise<string | null> {
  if (!isTauri) return null;
  const { open } = await import('@tauri-apps/plugin-dialog');
  const r = await open({ multiple: false, filters: [{ name: label, extensions: [ext] }] });
  return typeof r === 'string' ? r : null;
}

export async function confirmDialog(message: string, title: string, ok: string, cancel: string): Promise<boolean> {
  if (!isTauri) return window.confirm(message);
  const { ask } = await import('@tauri-apps/plugin-dialog');
  return ask(message, { title, kind: 'warning', okLabel: ok, cancelLabel: cancel });
}

/** Broadcast an event to every window (the planner window handles focus toggles). */
export async function emitEvent(name: string, payload?: unknown): Promise<void> {
  if (!isTauri) return;
  const { emit } = await import('@tauri-apps/api/event');
  await emit(name, payload);
}

export async function setFullscreen(on: boolean): Promise<void> {
  if (!isTauri) return;
  const { getCurrentWindow } = await import('@tauri-apps/api/window');
  try {
    await getCurrentWindow().setFullscreen(on);
  } catch (e) {
    console.warn('fullscreen failed', e);
  }
}
