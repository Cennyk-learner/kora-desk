import { doneIndex } from './model';
import { dayInfo } from './streak';
import type { State } from './types';

export const XP_TASK = 10;
export const XP_FOCUS = 15;
export const XP_FULL_DAY = 20;

/** XP is derived from history, never stored, so it can not drift or be double counted. Give-ups cost nothing. */
export function xpTotal(s: State): number {
  let xp = s.Done.length * XP_TASK;
  xp += s.Sessions.filter((x) => x.kind === 'focus').length * XP_FOCUS;
  for (const d of doneIndex(s).keys()) {
    const { due, done } = dayInfo(s, d);
    if (due > 0 && done === due) xp += XP_FULL_DAY;
  }
  return xp;
}

/** Cumulative XP needed to reach level L (level 1 = 0). */
export const xpForLevel = (l: number) => 50 * l * (l - 1);

export function levelOf(xp: number): { level: number; into: number; span: number; pct: number } {
  let level = 1;
  while (xp >= xpForLevel(level + 1)) level++;
  const into = xp - xpForLevel(level);
  const span = xpForLevel(level + 1) - xpForLevel(level);
  return { level, into, span, pct: span ? into / span : 0 };
}
