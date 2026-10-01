import { describe, expect, it } from 'vitest';
import { pickToday, shuffle } from './bag';
import { addKey } from './dates';
import { giveupStats, projectionText } from './giveup';
import { cancelLeave, leaveWarning, leavesInWeek, setLeave, warnedWeeks } from './leave';
import { settleDays } from './settle';
import { withDefaults, setDone } from './model';
import { computeStreak } from './streak';
import { idle, interrupt, pause, resume, settle, startFocus, leftMs } from './pomodoro';
import { scheduleReviews, cancelReviews } from './review';
import { levelOf, xpTotal } from './xp';
import { ACHIEVEMENTS, eligible } from './achievements';
import type { State, Task } from './types';
import { QUOTES } from '../data/quotes';
import { WORDS } from '../data/words';

function mk(tasks: Partial<Task>[] = []): State {
  return withDefaults({
    Version: 2,
    Tasks: tasks.map((t, i) => ({ Id: `t${i}`, Title: `任务${i}`, Note: '', Group: '学习', Days: [0, 1, 2, 3, 4, 5, 6], OnceDate: '', ...t })),
    Done: [],
    Settings: { AlwaysOnTop: false, X: -9999, Y: -9999 }
  } as unknown as State);
}

function seeded(seed: number) {
  let a = seed;
  return () => {
    a = (a * 1664525 + 1013904223) % 4294967296;
    return a / 4294967296;
  };
}

describe('shuffle bag', () => {
  it('never repeats within a pass and across the pass boundary, for 3 passes', () => {
    const total = 50;
    let bag = withDefaults(mk()).Quotes;
    const rand = seeded(7);
    let date = '2026-01-01';
    const seen: number[] = [];
    for (let d = 0; d < total * 3; d++) {
      bag = pickToday(bag, total, date, rand);
      seen.push(bag.Today!.idx);
      date = addKey(date, 1);
    }
    for (let p = 0; p < 3; p++) expect(new Set(seen.slice(p * total, (p + 1) * total)).size).toBe(total);
    for (let i = 1; i < seen.length; i++) expect(seen[i]).not.toBe(seen[i - 1]);
  });

  it('is stable for the same date', () => {
    const rand = seeded(1);
    const a = pickToday(mk().Quotes, 10, '2026-02-02', rand);
    const b = pickToday(a, 10, '2026-02-02', rand);
    expect(b).toBe(a);
    expect(b.Today!.idx).toBe(a.Today!.idx);
  });

  it('shows new entries after the pool grows', () => {
    const rand = seeded(3);
    let bag = mk().Quotes;
    bag = pickToday(bag, 5, 'd0', rand);
    bag = pickToday(bag, 8, 'd1', rand);
    const rest = [bag.Today!.idx, ...bag.Bag];
    for (const n of [5, 6, 7]) expect(rest).toContain(n);
  });

  it('shuffle is a permutation', () => {
    expect([...shuffle(20)].sort((a, b) => a - b)).toEqual(Array.from({ length: 20 }, (_, i) => i));
  });
});

describe('content', () => {
  it('has at least 400 unique quotes with a source', () => {
    expect(QUOTES.length).toBeGreaterThanOrEqual(400);
    expect(new Set(QUOTES.map((q) => q[0])).size).toBe(QUOTES.length);
    for (const q of QUOTES) {
      expect(q[0].length).toBeGreaterThan(3);
      expect(q[1].length).toBeGreaterThan(0);
    }
  });
  it('has daily words', () => {
    expect(WORDS.length).toBeGreaterThanOrEqual(150);
    expect(new Set(WORDS.map((w) => w[0])).size).toBe(WORDS.length);
  });
  it('a year of daily quotes never repeats', () => {
    const rand = seeded(11);
    let bag = mk().Quotes;
    let date = '2026-01-01';
    const seen = new Set<number>();
    for (let i = 0; i < 365; i++) {
      bag = pickToday(bag, QUOTES.length, date, rand);
      expect(seen.has(bag.Today!.idx)).toBe(false);
      seen.add(bag.Today!.idx);
      date = addKey(date, 1);
    }
  });
});

describe('give-up bank', () => {
  it('counts 0.1 day per record and projects a year', () => {
    const s = mk([{ Title: 'a' }]);
    // 20 active days with 32 records => 1.6 per day => 58.4 days/year
    for (let i = 0; i < 20; i++) setDone(s, 't0', addKey('2026-03-01', i), true);
    for (let i = 0; i < 32; i++) s.Giveups.push({ ts: i, date: addKey('2026-03-01', i % 20), titles: ['a'] });
    const st = giveupStats(s);
    expect(st.totalDays).toBe(3.2);
    expect(st.activeDays).toBe(20);
    expect(st.yearDays).toBe(58.4);
    expect(st.smallSample).toBe(false);
    expect(projectionText(st)).toBe('已放弃 3.2 天；照这个速度，一年将放弃 58.4 天');
  });
  it('flags small samples and handles empty history', () => {
    const s = mk();
    expect(giveupStats(s).yearDays).toBe(0);
    s.Giveups.push({ ts: 1, date: '2026-01-01' });
    const st = giveupStats(s);
    expect(st.smallSample).toBe(true);
    expect(st.yearDays).toBe(36.5);
    expect(projectionText(st)).toContain('样本较少');
  });
  it('summarises reasons and the most-missed tasks', () => {
    const s = mk([{}, {}]);
    s.Giveups.push({ ts: 1, date: '2026-01-01', titles: ['背单词'], reason: '拖延' });
    s.Giveups.push({ ts: 2, date: '2026-01-02', titles: ['背单词', '写作'], reason: '拖延' });
    const st = giveupStats(s);
    expect(st.byReason[0]).toEqual(['拖延', 2]);
    expect(st.byTask[0]).toEqual(['背单词', 2]);
  });
});

describe('daily settlement', () => {
  it('records at most one give-up per day, the next day', () => {
    const s = mk([{ Title: 'a' }, { Title: 'b' }, { Title: 'c' }]);
    s.Settled = '2026-03-01';
    setDone(s, 't0', '2026-03-02', true);
    expect(settleDays(s, '2026-03-03')).toBe(1); // b and c missed, still one record
    expect(s.Giveups).toHaveLength(1);
    expect(s.Giveups[0]).toMatchObject({ date: '2026-03-02', titles: ['b', 'c'] });
    expect(s.Settled).toBe('2026-03-02');
    expect(settleDays(s, '2026-03-03')).toBe(0);
    expect(giveupStats(s).totalDays).toBe(0.1);
  });
  it('a fully done day records nothing', () => {
    const s = mk([{}]);
    s.Settled = '2026-03-01';
    setDone(s, 't0', '2026-03-02', true);
    expect(settleDays(s, '2026-03-03')).toBe(0);
  });
  it('settles each missed day after time away, skipping leave days and not-yet-created tasks', () => {
    const s = mk([{ Title: 'a', Created: '2026-03-04' }, { Title: 'b' }]);
    s.Settled = '2026-03-01';
    setLeave(s, '2026-03-03', '生病');
    expect(settleDays(s, '2026-03-05')).toBe(2); // 02 and 04
    expect(s.Giveups.map((g) => g.date)).toEqual(['2026-03-02', '2026-03-04']);
    expect(s.Giveups[0].titles).toEqual(['b']);
    expect(s.Giveups[1].titles).toEqual(['a', 'b']);
  });
  it('the first run only sets the marker (no retroactive judgement)', () => {
    const s = mk([{}]);
    expect(settleDays(s, '2026-03-10')).toBe(0);
    expect(s.Settled).toBe('2026-03-09');
  });
});

describe('leave', () => {
  it('records a reason per day and warns at 3 days in a week', () => {
    const s = mk([{}]);
    setLeave(s, '2026-03-02', '生病'); // Monday
    setLeave(s, '2026-03-03', '生病');
    expect(leaveWarning(s, '2026-03-04')).toBe(false);
    setLeave(s, '2026-03-05', '考试');
    expect(leavesInWeek(s, '2026-03-08')).toBe(3);
    expect(leaveWarning(s, '2026-03-04')).toBe(true);
    expect(leavesInWeek(s, '2026-03-09')).toBe(0);
    expect(warnedWeeks(s)).toEqual([{ week: '2026-03-02', days: 3 }]);
    setLeave(s, '2026-03-05', '改理由');
    expect(s.Leaves.filter((l) => l.date === '2026-03-05')).toHaveLength(1);
    cancelLeave(s, '2026-03-05');
    expect(leaveWarning(s, '2026-03-04')).toBe(false);
  });
  it('leave days do not break the streak or count as due', () => {
    const s = mk([{}]);
    setDone(s, 't0', '2026-03-01', true);
    setLeave(s, '2026-03-02', '生病');
    setDone(s, 't0', '2026-03-03', true);
    s.Settings.FreezePerMonth = 0;
    expect(computeStreak(s, '2026-03-03').current).toBe(2);
  });
});

describe('streak', () => {
  it('counts consecutive complete days and skips days with nothing due', () => {
    const s = mk([{ Days: [1, 2, 3, 4, 5] }]);
    // 2026-03-02 is Monday
    for (const d of ['2026-03-02', '2026-03-03', '2026-03-04', '2026-03-05', '2026-03-06', '2026-03-09']) setDone(s, 't0', d, true);
    const r = computeStreak(s, '2026-03-09');
    expect(r.current).toBe(6);
    expect(r.best).toBe(6);
  });
  it('uses freeze cards then breaks', () => {
    const s = mk([{}]);
    s.Settings.FreezePerMonth = 1;
    setDone(s, 't0', '2026-03-01', true);
    setDone(s, 't0', '2026-03-02', true);
    // 03-03 missed (frozen), 03-04 done, 03-05 missed (no freeze left), 03-06 done
    setDone(s, 't0', '2026-03-04', true);
    setDone(s, 't0', '2026-03-06', true);
    const r = computeStreak(s, '2026-03-06');
    expect(r.frozen.has('2026-03-03')).toBe(true);
    expect(r.current).toBe(1);
    expect(r.best).toBe(3);
    expect(r.freezeLeft).toBe(0);
  });
  it('does not break on an unfinished today', () => {
    const s = mk([{}]);
    setDone(s, 't0', '2026-03-01', true);
    setDone(s, 't0', '2026-03-02', true);
    expect(computeStreak(s, '2026-03-03').current).toBe(2);
  });
});

describe('pomodoro', () => {
  const cfg = withDefaults(mk()).Settings.Pomo!;
  const dateOf = (ts: number) => new Date(ts).toISOString().slice(0, 10);
  it('settles a finished focus block into a session and a break', () => {
    const f = startFocus(cfg, 't0', 1_000_000, 0);
    const r = settle(f, cfg, f.EndsAt + 500, dateOf);
    expect(r.sessions).toHaveLength(1);
    expect(r.sessions[0]).toMatchObject({ kind: 'focus', minutes: 25, taskId: 't0' });
    expect(r.next.Mode).toBe('short');
    expect(r.finished).toBe('focus');
    expect(r.recovered).toBe(false);
  });
  it('recovers a block that ended while the app was closed, once', () => {
    const f = startFocus(cfg, 't0', 1_000_000, 3);
    const r = settle(f, cfg, f.EndsAt + 3 * 3600_000, dateOf);
    expect(r.sessions).toHaveLength(1);
    expect(r.sessions[0].recovered).toBe(true);
    expect(r.next.Mode).toBe('idle');
    expect(r.next.Cycle).toBe(4);
    expect(settle(r.next, cfg, f.EndsAt + 4 * 3600_000, dateOf).sessions).toHaveLength(0);
  });
  it('takes a long break every N blocks', () => {
    const f = startFocus(cfg, '', 0, 3);
    expect(settle(f, cfg, f.EndsAt, dateOf).next.Mode).toBe('long');
  });
  it('pauses and resumes without losing time', () => {
    let f = startFocus(cfg, '', 0, 0);
    f = pause(f, 10 * 60000);
    expect(leftMs(f, 99 * 60000)).toBe(15 * 60000);
    f = resume(f, 100 * 60000);
    expect(f.EndsAt).toBe(115 * 60000);
  });
  it('logs interruptions of at least a minute only', () => {
    const f = startFocus(cfg, 't1', 0, 0);
    expect(interrupt(f, 30_000, dateOf).session).toBeUndefined();
    const r = interrupt(f, 7 * 60000 + 5000, dateOf);
    expect(r.session).toMatchObject({ kind: 'interrupted', minutes: 7 });
    expect(r.next).toEqual(idle(0));
  });
});

describe('spaced review', () => {
  it('schedules 5 reviews once and cancels untouched ones', () => {
    const s = mk([{ Review: true, Title: '词汇' }]);
    expect(scheduleReviews(s, s.Tasks[0], '2026-05-01')).toBe(5);
    expect(scheduleReviews(s, s.Tasks[0], '2026-05-01')).toBe(0);
    expect(s.Tasks.filter((t) => t.ReviewSrc).map((t) => t.OnceDate)).toEqual(['2026-05-02', '2026-05-03', '2026-05-05', '2026-05-08', '2026-05-16']);
    setDone(s, s.Tasks[1].Id, '2026-05-02', true);
    cancelReviews(s, 't0', '2026-05-01');
    expect(s.Tasks.filter((t) => t.ReviewSrc)).toHaveLength(1);
  });
  it('review tasks do not chain', () => {
    const s = mk([{ Review: true }]);
    scheduleReviews(s, s.Tasks[0], '2026-05-01');
    s.Tasks[1].Review = true;
    expect(scheduleReviews(s, s.Tasks[1], '2026-05-02')).toBe(0);
  });
});

describe('xp and achievements', () => {
  it('derives xp from history', () => {
    const s = mk([{}]);
    setDone(s, 't0', '2026-03-01', true);
    s.Sessions.push({ ts: 1, date: '2026-03-01', kind: 'focus', minutes: 25, planned: 25 });
    expect(xpTotal(s)).toBe(10 + 15 + 20);
    expect(levelOf(0).level).toBe(1);
    expect(levelOf(100).level).toBe(2);
    expect(levelOf(299).level).toBe(2);
    expect(levelOf(300).level).toBe(3);
  });
  it('unlocks the first-step badge', () => {
    const s = mk([{}]);
    expect(eligible(s, '2026-03-01')).not.toContain('first');
    setDone(s, 't0', '2026-03-01', true);
    expect(eligible(s, '2026-03-01')).toContain('first');
  });
});

describe('achievements', () => {
  it('has unique ids and every group is named', () => {
    expect(new Set(ACHIEVEMENTS.map((a) => a.id)).size).toBe(ACHIEVEMENTS.length);
    expect(ACHIEVEMENTS.length).toBeGreaterThanOrEqual(30);
  });
  it('unlocks streak, focus, restart and rest badges from data', () => {
    const s = mk([{}]);
    for (const d of ['2026-03-02', '2026-03-03', '2026-03-04']) setDone(s, 't0', d, true);
    for (let i = 0; i < 10; i++) s.Sessions.push({ ts: i, date: '2026-03-04', kind: 'focus', minutes: 25, planned: 25 });
    s.Giveups.push({ ts: 1, date: '2026-03-01', titles: ['x'] });
    setLeave(s, '2026-03-10', '生病');
    const got = eligible(s, '2026-03-05');
    for (const id of ['first', 'streak3', 'focus1', 'focus10', 'deepday', 'restart', 'rest']) expect(got).toContain(id);
    expect(got).not.toContain('streak7');
    expect(got).not.toContain('focus100');
  });
});

describe('focus scenes', () => {
  it('has 50 captioned photos whose files exist', async () => {
    const { SCENES } = await import('../data/scenes');
    // @ts-ignore node types are not part of the front-end tsconfig
    const fs = await import('node:fs');
    expect(SCENES.length).toBe(50);
    expect(new Set(SCENES.map((s) => s.file)).size).toBe(50);
    for (const s of SCENES) {
      expect(s.place && s.author && s.license && s.source.startsWith('https://commons.wikimedia.org/')).toBeTruthy();
      expect(fs.existsSync(`public/scenes/${s.file}`)).toBe(true);
    }
  });
});

describe('defaults', () => {
  it('withDefaults is idempotent and keeps user data', () => {
    const s = mk([{}]);
    s.Giveups.push({ ts: 1, date: '2026-01-01' });
    const again = withDefaults(JSON.parse(JSON.stringify(s)));
    expect(again).toEqual(s);
  });
});

describe('exam countdown', () => {
  it('counts days and suggests a daily amount', async () => {
    const { examInfo, examText } = await import('./exam');
    const exam = { Name: '雅思', Date: '2026-11-16', Label: '词', Total: 3000, Done: 1350 };
    const info = examInfo(exam, '2026-09-30')!;
    expect(info.days).toBe(47);
    expect(info.remaining).toBe(1650);
    expect(info.perDay).toBe(36);
    expect(examText(exam, info)).toBe('距雅思还有 47 天');
    expect(examInfo(null, '2026-09-30')).toBeNull();
    expect(examInfo({ Name: 'x', Date: '2026-09-29' }, '2026-09-30')!.over).toBe(true);
    expect(examInfo({ Name: 'x', Date: '2026-09-30' }, '2026-09-30')!.perDay).toBeNull();
  });
});
