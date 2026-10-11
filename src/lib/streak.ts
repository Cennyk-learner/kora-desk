import { addKey, daysBetween, key, monthOf, parse } from './dates';
import { doneIndex, dueOnKey } from './model';
import { leaveOn } from './leave';
import { skippedIds } from './skip';
import type { State } from './types';

export interface DayInfo {
  due: number;
  done: number;
}

export function dayInfo(s: State, k: string): DayInfo {
  if (leaveOn(s, k)) return { due: 0, done: 0 };
  const ids = doneIndex(s).get(k);
  const skipped = skippedIds(s, k);
  let due = 0;
  let done = 0;
  for (const t of s.Tasks) {
    if (!dueOnKey(t, k) || skipped.has(t.Id)) continue;
    due++;
    if (ids?.has(t.Id)) done++;
  }
  return { due, done };
}

export interface StreakInfo {
  current: number;
  best: number;
  /** Days that were missed but covered by a freeze card. */
  frozen: Set<string>;
  freezeLeft: number;
  freezeTotal: number;
}

/**
 * A day counts when every task due that day is done. Days with nothing due are neutral (they
 * neither extend nor break the streak). A missed day is covered by a freeze card while the
 * month still has some; otherwise the streak restarts. Today never breaks the streak.
 */
export function computeStreak(s: State, today: string): StreakInfo {
  const total = s.Settings.FreezePerMonth ?? 2;
  const idx = doneIndex(s);
  let first = '';
  for (const [d, ids] of idx) if (ids.size && (!first || d < first)) first = d;
  const frozen = new Set<string>();
  const used = new Map<string, number>();
  let current = 0;
  let best = 0;
  if (first && first <= today) {
    const span = daysBetween(first, today);
    for (let i = 0; i < span; i++) {
      const k = addKey(first, i);
      const { due, done } = dayInfo(s, k);
      if (due === 0) continue;
      if (done === due) {
        current++;
        best = Math.max(best, current);
        continue;
      }
      const m = monthOf(k);
      if ((used.get(m) ?? 0) < total) {
        used.set(m, (used.get(m) ?? 0) + 1);
        frozen.add(k);
      } else current = 0;
    }
    const t = dayInfo(s, today);
    if (t.due > 0 && t.done === t.due) {
      current++;
      best = Math.max(best, current);
    }
  }
  return { current, best, frozen, freezeLeft: Math.max(0, total - (used.get(monthOf(today)) ?? 0)), freezeTotal: total };
}

export interface Cell {
  date: string;
  /** 0..1 completion, or -1 when nothing was due. */
  ratio: number;
  future: boolean;
}

/** Columns of 7 days (Mon..Sun), oldest first, ending with the current week. */
export function heatmap(s: State, today: string, weeks = 53): Cell[][] {
  const t = parse(today);
  const end = parse(addKey(today, 6 - ((t.getDay() + 6) % 7)));
  const cols: Cell[][] = [];
  for (let w = weeks - 1; w >= 0; w--) {
    const col: Cell[] = [];
    for (let d = 0; d < 7; d++) {
      const k = key(new Date(end.getFullYear(), end.getMonth(), end.getDate() - w * 7 - (6 - d)));
      const future = k > today;
      const { due, done } = future ? { due: 0, done: 0 } : dayInfo(s, k);
      col.push({ date: k, ratio: due ? done / due : -1, future });
    }
    cols.push(col);
  }
  return cols;
}

export function rateBetween(s: State, from: string, to: string): { done: number; due: number } {
  let done = 0;
  let due = 0;
  const n = daysBetween(from, to);
  for (let i = 0; i <= n; i++) {
    const r = dayInfo(s, addKey(from, i));
    done += r.done;
    due += r.due;
  }
  return { done, due };
}
