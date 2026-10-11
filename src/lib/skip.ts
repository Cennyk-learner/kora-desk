import type { State, Task } from './types';

/** A task deliberately set aside for one day. Planned, so it is kept apart from the give-up bank. */
export const skipOf = (s: State, taskId: string, date: string) => s.Skips.find((x) => x.taskId === taskId && x.date === date);

export function skippedIds(s: State, date: string): Set<string> {
  const set = new Set<string>();
  for (const x of s.Skips) if (x.date === date) set.add(x.taskId);
  return set;
}

export function setSkip(s: State, task: Task, date: string, reason: string, now = Date.now()): void {
  s.Skips = s.Skips.filter((x) => !(x.taskId === task.Id && x.date === date));
  s.Skips.push({ date, taskId: task.Id, title: task.Title, reason: reason.trim().slice(0, 60), ts: now });
  s.Skips.sort((a, b) => a.date.localeCompare(b.date));
}

export function cancelSkip(s: State, taskId: string, date: string): void {
  s.Skips = s.Skips.filter((x) => !(x.taskId === taskId && x.date === date));
}
