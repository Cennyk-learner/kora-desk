import { addKey, daysBetween } from './dates';
import { newId } from './model';
import type { State, Task } from './types';

/** Ebbinghaus-style gaps, in days after the task was first completed. */
export const REVIEW_GAPS = [1, 2, 4, 7, 15];

/** Add one-off review tasks after `task` is completed on `doneDate`. Returns how many were added. */
export function scheduleReviews(s: State, task: Task, doneDate: string): number {
  if (!task.Review || task.ReviewSrc) return 0;
  const src = `${doneDate}/${task.Id}`;
  let added = 0;
  for (const gap of REVIEW_GAPS) {
    const date = addKey(doneDate, gap);
    if (s.Tasks.length >= 1000) break;
    if (s.Tasks.some((t) => t.ReviewSrc === src && t.OnceDate === date)) continue;
    s.Tasks.push({
      Id: newId(),
      Title: `复习：${task.Title}`.slice(0, 80),
      Note: `第 ${gap} 天复习 · 来自 ${doneDate}`,
      Group: task.Group,
      Days: [],
      OnceDate: date,
      Est: task.Est,
      ReviewSrc: src,
      Created: doneDate
    });
    added++;
  }
  return added;
}

/** Un-checking the source removes its reviews that are still untouched. */
export function cancelReviews(s: State, taskId: string, doneDate: string): void {
  const src = `${doneDate}/${taskId}`;
  const finished = new Set(s.Done.map((d) => d.slice(11)));
  s.Tasks = s.Tasks.filter((t) => t.ReviewSrc !== src || finished.has(t.Id));
}

/** Drop generated review tasks that are long past, so they do not pile up. */
export function pruneReviews(s: State, today: string, keepDays = 120): boolean {
  const before = s.Tasks.length;
  const doneIds = new Set(s.Done.map((d) => d.slice(11)));
  s.Tasks = s.Tasks.filter((t) => !(t.ReviewSrc && t.OnceDate && daysBetween(t.OnceDate, today) > keepDays && !doneIds.has(t.Id)));
  return s.Tasks.length !== before;
}
