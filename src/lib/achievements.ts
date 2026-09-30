import { addKey, monday, parse, key } from './dates';
import { doneIndex } from './model';
import { activeDates } from './giveup';
import { computeStreak, dayInfo } from './streak';
import { levelOf, xpTotal } from './xp';
import type { State } from './types';

export interface Achievement {
  id: string;
  group: string;
  icon: string;
  name: string;
  desc: string;
}

export const ACHIEVEMENTS: Achievement[] = [
  { id: 'first', group: '起步', icon: '🌱', name: '第一步', desc: '完成第一个任务' },
  { id: 'focus1', group: '起步', icon: '🍅', name: '第一个番茄', desc: '完成一个专注番茄' },
  { id: 'reflect', group: '起步', icon: '📝', name: '认真复盘', desc: '完成第一次晚间复盘' },

  { id: 'streak3', group: '坚持', icon: '✨', name: '三天热度', desc: '连续 3 天完成当日清单' },
  { id: 'streak7', group: '坚持', icon: '🔥', name: '一周不断', desc: '连续 7 天完成当日清单' },
  { id: 'streak14', group: '坚持', icon: '🌟', name: '两周习惯', desc: '连续 14 天完成当日清单' },
  { id: 'streak30', group: '坚持', icon: '🏔️', name: '一月成峰', desc: '连续 30 天完成当日清单' },
  { id: 'streak60', group: '坚持', icon: '💎', name: '六十日不辍', desc: '连续 60 天完成当日清单' },
  { id: 'streak100', group: '坚持', icon: '👑', name: '百日筑基', desc: '连续 100 天完成当日清单' },

  { id: 'done100', group: '完成', icon: '✅', name: '百项达成', desc: '累计完成 100 个任务' },
  { id: 'done500', group: '完成', icon: '🏅', name: '五百次打卡', desc: '累计完成 500 个任务' },
  { id: 'done1000', group: '完成', icon: '🏆', name: '千次积累', desc: '累计完成 1000 个任务' },
  { id: 'perfect30', group: '完成', icon: '🎯', name: '三十个完美日', desc: '累计 30 天完成了当天的全部任务' },
  { id: 'fullweek', group: '完成', icon: '🗓️', name: '全勤周', desc: '一周内每个有任务的日子都全部完成（至少 5 天）' },

  { id: 'focus10', group: '专注', icon: '⏱️', name: '热身完毕', desc: '累计完成 10 个番茄' },
  { id: 'focus100', group: '专注', icon: '🍅', name: '百个番茄', desc: '累计完成 100 个番茄' },
  { id: 'focus300', group: '专注', icon: '🚀', name: '三百番茄', desc: '累计完成 300 个番茄' },
  { id: 'hours25', group: '专注', icon: '⌛', name: '二十五小时', desc: '累计专注满 25 小时' },
  { id: 'deepday', group: '专注', icon: '🧠', name: '深度一天', desc: '单日完成 5 个番茄' },

  { id: 'zerogiveup', group: '韧性', icon: '🕊️', name: '零放弃周', desc: '一个至少有 4 天记录的自然周，没有放弃' },
  { id: 'restart', group: '韧性', icon: '🔄', name: '重新出发', desc: '有过放弃的一天之后，第二天完成了全部任务' },
  { id: 'rest', group: '韧性', icon: '🌿', name: '懂得休息', desc: '第一次带着理由请假' },

  { id: 'mood14', group: '生活', icon: '😊', name: '心情观察员', desc: '累计记录 14 天的心情' },
  { id: 'grateful10', group: '生活', icon: '💛', name: '小确幸收集者', desc: '累计记录 10 天的小确幸' },
  { id: 'breath10', group: '生活', icon: '🌬️', name: '深呼吸', desc: '累计完成 10 次呼吸练习' },
  { id: 'water', group: '生活', icon: '💧', name: '喝够水', desc: '一天内喝够目标杯数' },
  { id: 'water7', group: '生活', icon: '🚰', name: '水分充足', desc: '累计 7 天喝水达标' },

  { id: 'review5', group: '学习', icon: '🔁', name: '温故知新', desc: '完成 5 次间隔复习' },
  { id: 'exam', group: '学习', icon: '📅', name: '有目标的人', desc: '设定一个考试倒计时' },
  { id: 'favs10', group: '学习', icon: '🔖', name: '书签收藏家', desc: '收藏 10 条名言或单词' },

  { id: 'lv5', group: '成长', icon: '⭐', name: '五级学徒', desc: '达到 Lv.5' },
  { id: 'lv10', group: '成长', icon: '🌠', name: '十级行者', desc: '达到 Lv.10' }
];

/** Achievements the current data qualifies for (ids). Unlock bookkeeping lives in state.Ach. */
export function eligible(s: State, today: string): string[] {
  const out: string[] = [];
  const add = (cond: boolean, id: string) => {
    if (cond) out.push(id);
  };
  const idx = doneIndex(s);
  const streak = computeStreak(s, today);
  const focus = s.Sessions.filter((x) => x.kind === 'focus');
  const focusMin = focus.reduce((a, x) => a + x.minutes, 0);
  const perDay = new Map<string, number>();
  for (const x of focus) perDay.set(x.date, (perDay.get(x.date) ?? 0) + 1);
  const perfectDays = new Set<string>();
  for (const d of idx.keys()) {
    const { due, done } = dayInfo(s, d);
    if (due > 0 && done === due) perfectDays.add(d);
  }
  const reviewIds = new Set(s.Tasks.filter((t) => t.ReviewSrc).map((t) => t.Id));
  const reviewsDone = s.Done.filter((k) => reviewIds.has(k.slice(11))).length;
  const journal = Object.values(s.Journal);
  const goal = s.Settings.Care?.WaterGoal ?? 8;
  const weeks = weekAchievements(s, today);
  const level = levelOf(xpTotal(s)).level;

  add(s.Done.length > 0, 'first');
  add(focus.length >= 1, 'focus1');
  add(journal.some((j) => !!j.result), 'reflect');
  const marks: [number, string][] = [[3, 'streak3'], [7, 'streak7'], [14, 'streak14'], [30, 'streak30'], [60, 'streak60'], [100, 'streak100']];
  for (const [n, id] of marks) add(streak.best >= n, id);
  add(s.Done.length >= 100, 'done100');
  add(s.Done.length >= 500, 'done500');
  add(s.Done.length >= 1000, 'done1000');
  add(perfectDays.size >= 30, 'perfect30');
  add(weeks.full, 'fullweek');
  add(focus.length >= 10, 'focus10');
  add(focus.length >= 100, 'focus100');
  add(focus.length >= 300, 'focus300');
  add(focusMin >= 1500, 'hours25');
  add([...perDay.values()].some((n) => n >= 5), 'deepday');
  add(weeks.zero, 'zerogiveup');
  add(s.Giveups.some((g) => perfectDays.has(addKey(g.date, 1))), 'restart');
  add(s.Leaves.some((l) => l.reason.trim().length > 0), 'rest');
  add(journal.filter((j) => !!j.mood).length >= 14, 'mood14');
  add(journal.filter((j) => (j.grateful?.filter(Boolean).length ?? 0) > 0).length >= 10, 'grateful10');
  add(journal.reduce((a, j) => a + (j.breaths ?? 0), 0) >= 10, 'breath10');
  add(journal.some((j) => (j.water ?? 0) >= goal), 'water');
  add(journal.filter((j) => (j.water ?? 0) >= goal).length >= 7, 'water7');
  add(reviewsDone >= 5, 'review5');
  add(!!s.Exam?.Date, 'exam');
  add(s.Quotes.Favs.length + s.Words.Favs.length >= 10, 'favs10');
  add(level >= 5, 'lv5');
  add(level >= 10, 'lv10');
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
