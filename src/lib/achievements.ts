import { addKey, monday, parse, key } from './dates';
import { doneIndex } from './model';
import { activeDates } from './giveup';
import { computeStreak, dayInfo } from './streak';
import type { State } from './types';

export interface Achievement {
  id: string;
  icon: string;
  name: string;
  desc: string;
}

export const ACHIEVEMENTS: Achievement[] = [
  { id: 'first', icon: '🌱', name: '第一步', desc: '完成第一个任务' },
  { id: 'streak7', icon: '🔥', name: '连续 7 天', desc: '连续 7 天完成当日清单' },
  { id: 'streak30', icon: '🏔️', name: '连续 30 天', desc: '连续 30 天完成当日清单' },
  { id: 'streak100', icon: '👑', name: '连续 100 天', desc: '连续 100 天完成当日清单' },
  { id: 'focus1', icon: '🍅', name: '第一个番茄', desc: '完成一个专注番茄' },
  { id: 'focus100', icon: '🍅', name: '百个番茄', desc: '累计完成 100 个番茄' },
  { id: 'fullweek', icon: '🗓️', name: '全勤周', desc: '一周内每个有任务的日子都全部完成（至少 5 天）' },
  { id: 'zerogiveup', icon: '🕊️', name: '零放弃周', desc: '一个至少有 4 天记录的自然周，没有放弃' },
  { id: 'reflect', icon: '📝', name: '认真复盘', desc: '完成第一次晚间复盘' },
  { id: 'grateful10', icon: '💛', name: '小确幸收集者', desc: '累计记录 10 天的小确幸' },
  { id: 'breath10', icon: '🌬️', name: '深呼吸', desc: '累计完成 10 次呼吸练习' },
  { id: 'water', icon: '💧', name: '喝够水', desc: '一天内喝够目标杯数' }
];

/** Achievements the current data qualifies for (ids). Unlock bookkeeping lives in state.Ach. */
export function eligible(s: State, today: string): string[] {
  const out: string[] = [];
  const streak = computeStreak(s, today);
  const focusCount = s.Sessions.filter((x) => x.kind === 'focus').length;
  if (s.Done.length > 0) out.push('first');
  if (streak.best >= 7) out.push('streak7');
  if (streak.best >= 30) out.push('streak30');
  if (streak.best >= 100) out.push('streak100');
  if (focusCount >= 1) out.push('focus1');
  if (focusCount >= 100) out.push('focus100');
  const weeks = weekAchievements(s, today);
  if (weeks.full) out.push('fullweek');
  if (weeks.zero) out.push('zerogiveup');
  const journal = Object.values(s.Journal);
  if (journal.some((j) => j.result)) out.push('reflect');
  if (journal.filter((j) => (j.grateful?.filter(Boolean).length ?? 0) > 0).length >= 10) out.push('grateful10');
  if (journal.reduce((a, j) => a + (j.breaths ?? 0), 0) >= 10) out.push('breath10');
  const goal = s.Settings.Care?.WaterGoal ?? 8;
  if (journal.some((j) => (j.water ?? 0) >= goal)) out.push('water');
  return out;
}

/** Evaluate calendar weeks (Mon..Sun) that had any activity. "Zero give-up" needs the week to be over. */
export function weekAchievements(s: State, today: string): { full: boolean; zero: boolean } {
  const active = activeDates(s);
  if (!active.size) return { full: false, zero: false };
  const gaveOn = new Set(s.Giveups.map((g) => g.date));
  let full = false;
  let zero = false;
  let start = key(monday(parse([...active].sort()[0])));
  const thisWeek = key(monday(parse(today)));
  while (start <= thisWeek) {
    let dueDays = 0;
    let fullDays = 0;
    let activeDays = 0;
    let gave = 0;
    for (let i = 0; i < 7; i++) {
      const k = addKey(start, i);
      if (k > today) break;
      const info = dayInfo(s, k);
      if (info.due > 0) {
        dueDays++;
        if (info.done === info.due) fullDays++;
      }
      if (active.has(k)) activeDays++;
      if (gaveOn.has(k)) gave++;
    }
    if (dueDays >= 5 && fullDays === dueDays) full = true;
    if (start < thisWeek && activeDays >= 4 && gave === 0) zero = true;
    start = addKey(start, 7);
  }
  return { full, zero };
}

/** Ids newly qualified and not yet recorded. */
export function newlyUnlocked(s: State, today: string): string[] {
  return eligible(s, today).filter((id) => !s.Ach[id]);
}
