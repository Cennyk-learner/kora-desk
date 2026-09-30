import { app, isDoneOn, setTaskDone, tasksFor } from './app.svelte';
import { fanfare, ping, untick } from './audio';
import { centerOf, fx } from './fx';
import { toast } from './app.svelte';
import type { Task } from './types';

/** Check or un-check a task with the full feedback chain: tick, ping (climbing), burst, day-complete party. */
export function checkTask(task: Task, k: string, value: boolean, el?: Element | null): void {
  void setTaskDone(task, k, value);
  if (!value) {
    untick();
    return;
  }
  const combo = ping();
  const c = centerOf(el ?? null);
  fx(app.skin === 'dot' ? 'pixels' : 'burst', c?.x, c?.y);
  const tasks = tasksFor(k);
  if (tasks.length && tasks.every((t) => isDoneOn(t, k))) {
    setTimeout(() => {
      fanfare();
      fx('rain');
      toast('今天的清单全部完成，辛苦了！', '🎉');
    }, 260);
  } else if (combo >= 3) {
    toast(`连击 ×${combo}`, '⚡');
  }
}
