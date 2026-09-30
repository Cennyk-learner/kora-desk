import { addKey, key, monday, parse } from './dates';
import type { State } from './types';

export const LEAVE_WARN_DAYS = 3;

export const leaveOn = (s: State, date: string) => s.Leaves.find((l) => l.date === date);

/** Leave days in the Monday-to-Sunday week containing `date`. */
export function leavesInWeek(s: State, date: string): number {
  const from = key(monday(parse(date)));
  const to = addKey(from, 6);
  return s.Leaves.filter((l) => l.date >= from && l.date <= to).length;
}

export const leaveWarning = (s: State, date: string) => leavesInWeek(s, date) >= LEAVE_WARN_DAYS;

/** Weeks (Monday keys) that reached the warning threshold, newest first. */
export function warnedWeeks(s: State): { week: string; days: number }[] {
  const m = new Map<string, number>();
  for (const l of s.Leaves) {
    const w = key(monday(parse(l.date)));
    m.set(w, (m.get(w) ?? 0) + 1);
  }
  return [...m.entries()].filter(([, n]) => n >= LEAVE_WARN_DAYS).map(([week, days]) => ({ week, days })).sort((a, b) => b.week.localeCompare(a.week));
}

export function setLeave(s: State, date: string, reason: string, now = Date.now()): void {
  s.Leaves = s.Leaves.filter((l) => l.date !== date);
  s.Leaves.push({ date, reason: reason.trim().slice(0, 60), ts: now });
  s.Leaves.sort((a, b) => a.date.localeCompare(b.date));
}

export function cancelLeave(s: State, date: string): void {
  s.Leaves = s.Leaves.filter((l) => l.date !== date);
}
