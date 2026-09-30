import { addKey, daysBetween } from './dates';
import { doneIndex, dueOnKey } from './model';
import { leaveOn } from './leave';
import type { State } from './types';

const MAX_BACKFILL = 60;

/**
 * Runs when the app starts (and at midnight): every task that was due but not done on the days
 * since the last settlement, up to yesterday, becomes one give-up record. Leave days are skipped,
 * and a task is never charged for days before it was created. The first ever run only sets the
 * marker so old history is not judged retroactively. Returns the number of records added.
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
    for (const t of s.Tasks) {
      if (!dueOnKey(t, d) || done?.has(t.Id)) continue;
      if (t.Created && t.Created > d) continue;
      s.Giveups.push({ ts: new Date(`${d}T23:59:00`).getTime(), date: d, taskId: t.Id, title: t.Title });
      added++;
    }
  }
  s.Settled = yesterday;
  return added;
}
