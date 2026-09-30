// Background duties of the planner window: pomodoro settling, daily picks, reminders,
// achievements, tray text and global shortcuts. The planner window is hidden, never destroyed,
// so this keeps running while only the tray icon and the widget are visible.
import { app, toast, update, tasksFor, isDoneOn } from './app.svelte';
import * as backend from './backend';
import { bell, chime, fanfare, startNoise, stopNoise } from './audio';
import { pickToday } from './bag';
import { addKey, key } from './dates';
import { fx } from './fx';
import { notify } from './notify';
import { ACHIEVEMENTS, newlyUnlocked } from './achievements';
import { setDone } from './model';
import { clock, focusCountFor, interrupt, isDue, isPaused, isRunning, leftMs, pause, resume, settle, startFocus } from './pomodoro';
import { pruneReviews, scheduleReviews } from './review';
import { settleDays } from './settle';
import { QUOTES } from '../data/quotes';
import { WORDS } from '../data/words';

const dateOf = (ts: number) => key(new Date(ts));
let noiseOn = '';
let lastAchState: unknown = null;
let lastEye = Date.now();
let lastSit = Date.now();
let bedDone = '';
let pruned = false;
let settling = false;

/* ---------- pomodoro controls ---------- */

export function startPomo(taskId = '', minutes?: number): Promise<void> {
  return update((s) => {
    const cfg = s.Settings.Pomo!;
    s.Focus = startFocus(cfg, taskId, Date.now(), s.Focus.Cycle, minutes);
  });
}

export function pausePomo(): Promise<void> {
  if (app.state.Settings.Pomo?.Strict && app.state.Focus.Mode === 'focus') {
    toast('严格模式下专注中不能暂停，可以选择放弃本次番茄。', '🔒');
    return Promise.resolve();
  }
  return update((s) => {
    s.Focus = pause(s.Focus, Date.now());
  });
}

export function resumePomo(): Promise<void> {
  return update((s) => {
    s.Focus = resume(s.Focus, Date.now());
  });
}

/** Stop the current block. A focus block of a minute or more is logged as interrupted. */
export function stopPomo(): Promise<void> {
  const r = interrupt(app.state.Focus, Date.now(), dateOf);
  return update((s) => {
    s.Focus = r.next;
    if (r.session) s.Sessions.push(r.session);
  });
}

export function togglePomo(): Promise<void> {
  const f = app.state.Focus;
  if (f.Mode === 'idle') return startPomo(f.TaskId);
  return isPaused(f) ? resumePomo() : pausePomo();
}

/* ---------- ticking ---------- */

async function tick(): Promise<void> {
  if (!app.ready) return;
  const now = Date.now();
  await settleFocus(now);
  await dailyPicks();
  await settlement();
  syncAwake();
  syncNoise();
  reminders(new Date(now));
  await achievements();
  await tray(now);
}

async function settleFocus(now: number): Promise<void> {
  const s = app.state;
  const cfg = s.Settings.Pomo!;
  if (settling || !isDue(s.Focus, now)) return;
  const r = settle(s.Focus, cfg, now, dateOf);
  settling = true;
  try {
    const checkedTitles: string[] = [];
    await update((st) => {
      st.Focus = r.next;
      st.Sessions.push(...r.sessions);
      for (const ses of r.sessions) {
        if (!ses.taskId || !cfg.AutoCheck) continue;
        const task = st.Tasks.find((t) => t.Id === ses.taskId);
        if (!task) continue;
        const has = st.Done.includes(`${ses.date}/${task.Id}`);
        if (!has && focusCountFor(st.Sessions, task.Id, ses.date) >= cfg.AutoCheck) {
          setDone(st, task.Id, ses.date, true);
          scheduleReviews(st, task, ses.date);
          checkedTitles.push(task.Title);
        }
      }
    });
    if (r.finished === 'focus') {
      const n = r.sessions.length;
      if (r.recovered) toast(`你不在时完成了 ${n} 个番茄，已记入。`, '🍅');
      else {
        bell();
        fx('rain');
        toast('一个番茄完成了，休息一下。', '🍅');
      }
      void notify('番茄完成', r.next.Mode === 'idle' ? '辛苦了！' : `休息 ${r.next.Planned} 分钟，喝口水。`);
      if (checkedTitles.length) toast(`已自动勾选：${checkedTitles.join('、')}`, '✅');
    } else if (r.finished === 'break') {
      chime();
      void notify('休息结束', '准备好了就开始下一个番茄。');
    }
  } finally {
    settling = false;
  }
}

async function dailyPicks(): Promise<void> {
  const s = app.state;
  const t = app.today;
  const q = pickToday(s.Quotes, QUOTES.length, t);
  const w = pickToday(s.Words, WORDS.length, t);
  if (q !== s.Quotes || w !== s.Words) {
    await update((st) => {
      st.Quotes = q;
      st.Words = w;
      if (!pruned) {
        pruned = true;
        pruneReviews(st, t);
      }
    });
  }
}

let awakeOn = false;
function syncAwake(): void {
  const f = app.state.Focus;
  const want = f.Mode === 'focus' && isRunning(f) && app.state.Settings.KeepAwake !== false;
  if (want !== awakeOn) {
    awakeOn = want;
    void backend.call('keep_awake', { on: want });
  }
  if (f.Mode === 'idle' && app.fullscreen) app.fullscreen = false;
}

/** Yesterday's (and any missed days') unfinished tasks become give-up records. */
async function settlement(): Promise<void> {
  const s = app.state;
  const y = addKey(app.today, -1);
  if (s.Settled && s.Settled >= y) return;
  let added = 0;
  await update((st) => {
    added = settleDays(st, app.today);
  });
  if (added > 0) toast(added === 1 ? '昨天有任务没完成，已记一次放弃（0.1 天）。今天重新开始。' : `有 ${added} 天没完成全部任务，已各记一次放弃。今天重新开始。`, '🍂');
}

function syncNoise(): void {
  const f = app.state.Focus;
  const kind = f.Mode === 'focus' && isRunning(f) ? app.state.Settings.Pomo!.Noise : 'off';
  if (kind === noiseOn) return;
  noiseOn = kind;
  if (kind === 'off') stopNoise();
  else startNoise(kind);
}

function reminders(now: Date): void {
  const care = app.state.Settings.Care!;
  const hour = now.getHours();
  const resting = app.state.Focus.Mode === 'short' || app.state.Focus.Mode === 'long';
  if (care.Eye && hour >= 7 && !resting && now.getTime() - lastEye >= care.EyeMin * 60000) {
    lastEye = now.getTime();
    chime();
    toast('20-20-20：看看 6 米外的东西，放松 20 秒。', '👀');
    void notify('护眼提醒', '看向 6 米外的地方 20 秒，让眼睛歇一歇。');
  }
  if (care.Sit && hour >= 7 && !resting && now.getTime() - lastSit >= care.SitMin * 60000) {
    lastSit = now.getTime();
    chime();
    toast('坐了一阵了，站起来活动一下肩颈吧。', '🧘');
    void notify('久坐提醒', '站起来走一走，伸个懒腰。');
  }
  if (care.Bed) {
    const [h, m] = care.BedTime.split(':').map(Number);
    const mins = now.getHours() * 60 + now.getMinutes();
    const target = h * 60 + m;
    if (mins >= target && mins < target + 5 && bedDone !== app.today) {
      bedDone = app.today;
      chime();
      toast('该放下今天了。剩下的，明天的你会接着做。晚安。', '🌙');
      void notify('该睡觉啦', '睡眠也是学习的一部分。');
    }
  }
}

async function achievements(): Promise<void> {
  if (app.state === lastAchState) return;
  lastAchState = app.state;
  const fresh = newlyUnlocked(app.state, app.today);
  if (!fresh.length) return;
  await update((s) => {
    for (const id of fresh) s.Ach[id] = app.today;
  });
  lastAchState = app.state;
  if (fresh.length > 2) toast(`一次解锁了 ${fresh.length} 枚徽章，去「统计」看看吧。`, '🏅');
  else
    for (const id of fresh) {
      const a = ACHIEVEMENTS.find((x) => x.id === id);
      if (a) toast(`解锁徽章「${a.name}」`, a.icon);
    }
  fanfare();
  fx('rain');
}

async function tray(now: number): Promise<void> {
  const f = app.state.Focus;
  let text: string;
  if (f.Mode !== 'idle') {
    const label = f.Mode === 'focus' ? '专注' : '休息';
    text = `${label} ${clock(leftMs(f, now))}${isPaused(f) ? '（已暂停）' : ''} · Kora`;
  } else {
    const tasks = tasksFor(app.today);
    const done = tasks.filter((t) => isDoneOn(t, app.today)).length;
    text = tasks.length ? `Kora Desk · 今天 ${done}/${tasks.length}` : 'Kora Desk · 刻序';
  }
  await backend.call('set_tray_tip', { text });
}

/* ---------- shortcuts ---------- */

let registered = '';

export async function syncShortcuts(): Promise<void> {
  if (!backend.isTauri) return;
  const focus = app.state.Settings.KeyFocus ?? '';
  const open = app.state.Settings.KeyOpen ?? '';
  const sig = `${focus}|${open}`;
  if (sig === registered) return;
  registered = sig;
  try {
    const gs = await import('@tauri-apps/plugin-global-shortcut');
    await gs.unregisterAll();
    if (focus) await gs.register(focus, (e) => e.state === 'Pressed' && void togglePomo());
    if (open) await gs.register(open, (e) => e.state === 'Pressed' && void backend.call('open_main', { page: 'today' }));
  } catch (e) {
    toast(`快捷键无法注册（可能被其他程序占用）：${e}`, '⌨️');
  }
}

export async function startEngine(): Promise<void> {
  await backend.onEvent('focus-toggle', () => void togglePomo());
  await backend.onEvent('focus-stop', () => void stopPomo());
  await backend.onEvent<string>('navigate', (p) => {
    if (p === 'focus-start') {
      app.page = 'focus';
      void startPomo();
    } else app.page = p as typeof app.page;
  });
  await backend.onEvent<boolean>('widget-visibility', (v) => (app.widgetVisible = v));
  app.widgetVisible = (await backend.call<boolean>('widget_visible')) ?? true;
  void tick();
  setInterval(() => void tick(), 1000);
}
