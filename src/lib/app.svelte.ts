import * as backend from './backend';
import { configure as configureAudio } from './audio';
import { setMotion } from './fx';
import { addKey, key, today as todayOf } from './dates';
import { doneIndex, forDay, newId, setDone, withDefaults } from './model';
import { cancelLeave, setLeave } from './leave';
import { cancelReviews, scheduleReviews } from './review';
import type { Journal, Page, State, Task } from './types';

const EMPTY = withDefaults({ Version: 2, Tasks: [], Done: [], Settings: { AlwaysOnTop: false, X: -9999, Y: -9999 } } as unknown as State);

export interface Toast {
  id: number;
  icon: string;
  text: string;
}

class App {
  state = $state.raw<State>(EMPTY);
  ready = $state(false);
  now = $state(Date.now());
  dir = $state('');
  label = $state<'main' | 'widget'>('main');
  page = $state<Page>('today');
  /** Date the daily page is showing (key). */
  selected = $state(todayOf());
  toasts = $state<Toast[]>([]);
  error = $state('');
  leaveOpen = $state(false);
  fullscreen = $state(false);
  minimalFocus = $state(false);
  widgetVisible = $state(true);
  systemDark = $state(false);

  today = $derived(todayOf(new Date(this.now)));
  skin = $derived.by(() => {
    const t = this.state.Settings.Theme;
    return t && ['mac', 'fluent', 'material', 'carbon', 'dot'].includes(t) ? t : 'mac';
  });
  mode = $derived.by((): 'light' | 'dark' => {
    const a = this.state.Settings.Appearance;
    return a === 'dark' ? 'dark' : a === 'light' ? 'light' : this.systemDark ? 'dark' : 'light';
  });
  /** Greys the window briefly after a give-up. */
  gray = $state(false);
}

export const app = new App();

let pending = 0;
let chain: Promise<unknown> = Promise.resolve();
let toastId = 1;

export function toast(text: string, icon = '✨'): void {
  const id = toastId++;
  app.toasts = [...app.toasts, { id, icon, text }].slice(-4);
  setTimeout(() => (app.toasts = app.toasts.filter((t) => t.id !== id)), 4200);
}

function applySettings(s: State): void {
  configureAudio(!!s.Settings.Mute, s.Settings.Volume ?? 0.6);
  setMotion(s.Settings.Motion);
}

/** Apply an edit optimistically, then persist. Writes are serialised so they land in order. */
export function update(edit: (s: State) => void): Promise<void> {
  const next = structuredClone($state.snapshot(app.state)) as State;
  edit(next);
  withDefaults(next);
  app.state = next;
  applySettings(next);
  pending++;
  const run = chain.then(async () => {
    try {
      await backend.writeState(next);
    } catch (e) {
      app.error = String(e);
      toast(`保存失败：${e}`, '⚠️');
      const r = await backend.readState();
      app.state = r.state;
    } finally {
      pending--;
    }
  });
  chain = run.catch(() => {});
  return run;
}

export async function init(label: 'main' | 'widget'): Promise<void> {
  app.label = label;
  const r = await backend.readState();
  app.state = r.state;
  app.dir = r.dir;
  applySettings(r.state);
  if (r.notice) toast(r.notice, '🛟');
  await backend.onStateChanged((s) => {
    // Our own writes come back as events; only adopt state when nothing of ours is in flight.
    if (pending === 0) {
      app.state = s;
      applySettings(s);
    }
  });
  const mq = typeof matchMedia === 'function' ? matchMedia('(prefers-color-scheme: dark)') : null;
  if (mq) {
    app.systemDark = mq.matches;
    mq.addEventListener('change', (e) => (app.systemDark = e.matches));
  }
  setInterval(() => (app.now = Date.now()), 1000);
  app.ready = true;
}

/* ---------- tasks ---------- */

export function tasksFor(k: string): Task[] {
  const [y, m, d] = k.split('-').map(Number);
  return forDay(app.state, new Date(y, m - 1, d));
}

export function isDoneOn(t: Task, k: string): boolean {
  return doneIndex(app.state).get(k)?.has(t.Id) ?? false;
}

export function setTaskDone(task: Task, k: string, value: boolean): Promise<void> {
  return update((s) => {
    setDone(s, task.Id, k, value);
    if (value) scheduleReviews(s, s.Tasks.find((t) => t.Id === task.Id) ?? task, k);
    else cancelReviews(s, task.Id, k);
  });
}

export function saveTask(task: Task): Promise<void> {
  return update((s) => {
    const i = s.Tasks.findIndex((t) => t.Id === task.Id);
    if (i >= 0) s.Tasks[i] = task;
    else s.Tasks.push({ ...task, Id: task.Id || newId(), Created: app.today });
  });
}

export function deleteTask(id: string): Promise<void> {
  return update((s) => {
    s.Tasks = s.Tasks.filter((t) => t.Id !== id);
    s.Done = s.Done.filter((d) => d.slice(11) !== id);
  });
}

/* ---------- give-up bank and leave ---------- */

/** Tag a settled miss with a reason (the record is created automatically at settlement). */
export function setGiveupReason(ts: number, taskId: string | undefined, reason: string): Promise<void> {
  return update((s) => {
    const g = s.Giveups.find((x) => x.ts === ts && x.taskId === taskId);
    if (g) {
      if (reason) g.reason = reason;
      else delete g.reason;
    }
  });
}

export function takeLeave(date: string, reason: string): Promise<void> {
  return update((s) => setLeave(s, date, reason));
}

export function dropLeave(date: string): Promise<void> {
  return update((s) => cancelLeave(s, date));
}

/* ---------- journal ---------- */

export function journalOf(k: string): Journal {
  return app.state.Journal[k] ?? {};
}

export function patchJournal(k: string, patch: Partial<Journal>): Promise<void> {
  return update((s) => {
    s.Journal[k] = { ...(s.Journal[k] ?? {}), ...patch };
  });
}

/* ---------- misc ---------- */

export function toggleFav(kind: 'Quotes' | 'Words', idx: number): Promise<void> {
  return update((s) => {
    const f = s[kind].Favs;
    s[kind].Favs = f.includes(idx) ? f.filter((i) => i !== idx) : [...f, idx];
  });
}

export function go(page: Page): void {
  app.page = page;
}

export function weekStart(k: string): string {
  const d = new Date(k.replace(/-/g, '/'));
  return key(new Date(d.getFullYear(), d.getMonth(), d.getDate() - ((d.getDay() + 6) % 7)));
}

export { addKey };
