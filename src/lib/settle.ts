import { addKey, daysBetween } from './dates';
import { doneIndex, dueOnKey } from './model';
import { leaveOn } from './leave';
import { skippedIds } from './skip';
import type { State } from './types';

const MAX_BACKFILL = 60;

/**
 * Runs when the app starts (and at midnight): each day since the last settlement, up to yesterday, that ended
 * with fewer than `Tolerance` percent of its tasks done becomes one give-up record (at most one per day). Leave days are skipped,
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
  const tolerance = Math.min(1, Math.max(0.5, (s.Settings.Tolerance ?? 80) / 100));
  const idx = doneIndex(s);
  for (let d = addKey(from, 1); d <= yesterday; d = addKey(d, 1)) {
    if (leaveOn(s, d)) continue;
    const done = idx.get(d);
    const skipped = skippedIds(s, d);
    // Tasks set aside on purpose (per-task leave) are neither done nor missed.
    const due = s.Tasks.filter((t) => dueOnKey(t, d) && !skipped.has(t.Id) && !(t.Created && t.Created > d));
    const missed = due.filter((t) => !done?.has(t.Id));
    if (!missed.length || s.Giveups.some((g) => g.date === d)) continue;
    // Close enough counts as a met day: with 80% done, the last 20% is not recorded.
    if ((due.length - missed.length) / due.length >= tolerance) continue;
    // At most one give-up per day, however many tasks were left.
    s.Giveups.push({ ts: new Date(`${d}T23:59:00`).getTime(), date: d, titles: missed.map((t) => t.Title) });
    added++;
  }
  s.Settled = yesterday;
  return added;
}
