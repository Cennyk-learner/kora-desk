import { key, parse, ORDER, SHORT } from './dates';
import type { State, Task, BagState, PomoCfg, CareCfg, FocusRun } from './types';

export const DEFAULT_POMO: PomoCfg = { Focus: 25, Short: 5, Long: 15, Every: 4, AutoCheck: 0, Strict: false, Noise: 'off', Fullscreen: false };
export const DEFAULT_CARE: CareCfg = { Eye: false, EyeMin: 20, Sit: false, SitMin: 45, Bed: false, BedTime: '23:00', WaterGoal: 8 };
const emptyBag = (): BagState => ({ Bag: [], Total: 0, Today: null, Last: -1, Favs: [] });

/** Fill every v2 field so the UI never checks for undefined. Idempotent. */
export function withDefaults(s: State): State {
  s.Tasks ??= [];
  s.Done ??= [];
  s.Settings ??= { AlwaysOnTop: false, X: -9999, Y: -9999 };
  s.Settings.Pomo = { ...DEFAULT_POMO, ...(s.Settings.Pomo ?? {}) };
  s.Settings.Care = { ...DEFAULT_CARE, ...(s.Settings.Care ?? {}) };
  s.Settings.Volume ??= 0.6;
  s.Settings.FreezePerMonth ??= 2;
  s.Settings.KeyFocus ??= 'Ctrl+Alt+P';
  s.Settings.KeyOpen ??= 'Ctrl+Alt+K';
  s.Quotes = { ...emptyBag(), ...(s.Quotes ?? {}) };
  s.Words = { ...emptyBag(), ...(s.Words ?? {}) };
  s.Giveups ??= [];
  s.Sessions ??= [];
  s.Focus = { Mode: 'idle', EndsAt: 0, StartedAt: 0, Planned: 0, TaskId: '', Cycle: 0, PausedLeft: 0, ...((s.Focus as Partial<FocusRun> | undefined) ?? {}) };
  s.Journal ??= {};
  s.Exam ??= null;
  s.Ach ??= {};
  s.Rewards ??= [];
  s.Leaves ??= [];
  s.Settled ??= '';
  s.Settings.KeepAwake ??= true;
  return s;
}

export function due(t: Task, d: Date): boolean {
  return t.OnceDate ? t.OnceDate === key(d) : t.Days.includes(d.getDay());
}

export function dueOnKey(t: Task, k: string): boolean {
  return t.OnceDate ? t.OnceDate === k : t.Days.includes(parse(k).getDay());
}

export function forDay(s: State, d: Date): Task[] {
  return s.Tasks.filter((t) => due(t, d));
}

/** date -> set of done task ids. Cached per Done array instance and length. */
const cache = new WeakMap<string[], { n: number; map: Map<string, Set<string>> }>();
export function doneIndex(s: State): Map<string, Set<string>> {
  const hit = cache.get(s.Done);
  if (hit && hit.n === s.Done.length) return hit.map;
  const map = new Map<string, Set<string>>();
  for (const k of s.Done) {
    const date = k.slice(0, 10);
    let set = map.get(date);
    if (!set) map.set(date, (set = new Set()));
    set.add(k.slice(11));
  }
  cache.set(s.Done, { n: s.Done.length, map });
  return map;
}

export function isDone(s: State, t: Task, d: Date | string): boolean {
  const k = typeof d === 'string' ? d : key(d);
  return doneIndex(s).get(k)?.has(t.Id) ?? false;
}

export function setDone(s: State, id: string, k: string, value: boolean): void {
  const full = `${k}/${id}`;
  s.Done = s.Done.filter((x) => x !== full);
  if (value) s.Done.push(full);
}

export function frequency(t: Task): string {
  if (t.OnceDate) return `${t.OnceDate} · 仅一次`;
  if (t.Days.length === 7) return '每天';
  return ORDER.filter((i) => t.Days.includes(i)).map((i) => SHORT[i]).join(' / ');
}

export function newId(): string {
  const a = new Uint8Array(16);
  crypto.getRandomValues(a);
  return Array.from(a, (b) => b.toString(16).padStart(2, '0')).join('');
}

export function groupBy<T>(items: T[], f: (t: T) => string): [string, T[]][] {
  const m = new Map<string, T[]>();
  for (const it of items) {
    const g = f(it);
    const list = m.get(g);
    if (list) list.push(it);
    else m.set(g, [it]);
  }
  return [...m.entries()];
}

export function actualMinutes(s: State, taskId: string, date?: string): number {
  return s.Sessions.filter((x) => x.taskId === taskId && (!date || x.date === date)).reduce((a, x) => a + x.minutes, 0);
}
