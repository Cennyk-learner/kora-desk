import type { FocusRun, PomoCfg, Session } from './types';

const MIN = 60000;

export const idle = (cycle = 0): FocusRun => ({ Mode: 'idle', EndsAt: 0, StartedAt: 0, Planned: 0, TaskId: '', Cycle: cycle, PausedLeft: 0 });

export function startFocus(cfg: PomoCfg, taskId: string, now: number, cycle: number, minutes = cfg.Focus): FocusRun {
  return { Mode: 'focus', StartedAt: now, EndsAt: now + minutes * MIN, Planned: minutes, TaskId: taskId, Cycle: cycle, PausedLeft: 0 };
}

export function startBreak(cfg: PomoCfg, cycle: number, now: number, taskId = ''): FocusRun {
  const long = cycle > 0 && cycle % Math.max(1, cfg.Every) === 0;
  const minutes = long ? cfg.Long : cfg.Short;
  return { Mode: long ? 'long' : 'short', StartedAt: now, EndsAt: now + minutes * MIN, Planned: minutes, TaskId: taskId, Cycle: cycle, PausedLeft: 0 };
}

export const isRunning = (f: FocusRun) => f.Mode !== 'idle' && f.PausedLeft === 0;
export const isPaused = (f: FocusRun) => f.Mode !== 'idle' && f.PausedLeft > 0;

export function leftMs(f: FocusRun, now: number): number {
  if (f.Mode === 'idle') return 0;
  return f.PausedLeft > 0 ? f.PausedLeft : Math.max(0, f.EndsAt - now);
}

export function pause(f: FocusRun, now: number): FocusRun {
  if (!isRunning(f)) return f;
  return { ...f, PausedLeft: Math.max(1, f.EndsAt - now), EndsAt: 0 };
}

export function resume(f: FocusRun, now: number): FocusRun {
  if (!isPaused(f)) return f;
  return { ...f, EndsAt: now + f.PausedLeft, PausedLeft: 0 };
}

export const isDue = (f: FocusRun, now: number) => isRunning(f) && now >= f.EndsAt;

export interface Settled {
  next: FocusRun;
  sessions: Session[];
  /** What just finished, for sounds and toasts. */
  finished: 'focus' | 'break' | null;
  recovered: boolean;
}

/**
 * Settle everything that finished by `now`. Because the run stores `EndsAt`, this gives the same
 * answer after a restart: a focus block that ended while the app was closed is still credited,
 * to the day it ended on.
 */
export function settle(f: FocusRun, cfg: PomoCfg, now: number, dateOf: (ts: number) => string): Settled {
  const sessions: Session[] = [];
  let finished: Settled['finished'] = null;
  let recovered = false;
  let cur = f;
  while (isDue(cur, now)) {
    if (now - cur.EndsAt > 20000) recovered = true;
    if (cur.Mode === 'focus') {
      const cycle = cur.Cycle + 1;
      sessions.push({ ts: cur.EndsAt, date: dateOf(cur.EndsAt), kind: 'focus', minutes: cur.Planned, planned: cur.Planned, taskId: cur.TaskId || undefined, recovered: now - cur.EndsAt > 20000 || undefined });
      cur = startBreak(cfg, cycle, cur.EndsAt, cur.TaskId);
      finished = 'focus';
    } else {
      cur = idle(cur.Cycle);
      finished = 'break';
    }
  }
  return { next: cur, sessions, finished, recovered };
}

/** Stop a running/paused block. A focus block that ran at least a minute is logged as interrupted. */
export function interrupt(f: FocusRun, now: number, dateOf: (ts: number) => string): { next: FocusRun; session?: Session } {
  if (f.Mode === 'idle') return { next: f };
  const next = idle(f.Cycle);
  if (f.Mode !== 'focus') return { next };
  const elapsed = f.Planned * MIN - leftMs(f, now);
  if (elapsed < MIN) return { next };
  return { next, session: { ts: now, date: dateOf(now), kind: 'interrupted', minutes: Math.floor(elapsed / MIN), planned: f.Planned, taskId: f.TaskId || undefined } };
}

export function focusCountFor(sessions: Session[], taskId: string, date: string): number {
  return sessions.filter((s) => s.kind === 'focus' && s.taskId === taskId && s.date === date).length;
}

export function clock(ms: number): string {
  const s = Math.ceil(ms / 1000);
  return `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`;
}
