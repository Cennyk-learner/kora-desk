import { addKey, daysBetween } from './dates';
import { doneIndex, dueOnKey } from './model';
import { leaveOn } from './leave';
import type { State } from './types';

const MAX_BACKFILL = 60;

/**
 * Runs when the app starts (and at midnight): each day since the last settlement, up to yesterday, that ended
 * with any due task not done becomes one give-up record (at most one per day). Leave days are skipped,
 * and a task is never charged for days before it was created. The first ever run only sets the
 * marker so old history is not judged retroactively. Returns the number of days recorded.
 */
export function settleDays(s: State, today: string): number {
  const yesterday = addKey(today, -1);
  if (!s.Settled) {
    s.Settled = yesterday;
    return 0;
  }
  if (s.Settled >= yesterday) return 0;
  let from = s.Settled;
  if (daysBetween(from, yesterday) > MAX_BACKFILL) from = addKey(yesterday, -MAX_BACKFILL);
  let added = 0;
  const idx = doneIndex(s);
  for (let d = addKey(from, 1); d <= yesterday; d = addKey(d, 1)) {
    if (leaveOn(s, d)) continue;
    const done = idx.get(d);
    const missed = s.Tasks.filter((t) => dueOnKey(t, d) && !done?.has(t.Id) && !(t.Created && t.Created > d));
    if (!missed.length || s.Giveups.some((g) => g.date === d)) continue;
    // At most one give-up per day, however many tasks were left.
    s.Giveups.push({ ts: new Date(`${d}T23:59:00`).getTime(), date: d, titles: missed.map((t) => t.Title) });
    added++;
  }
  s.Settled = yesterday;
  return added;
}
