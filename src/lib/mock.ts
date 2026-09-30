import type { State } from './types';
import { addKey, today } from './dates';

const ALL = [1, 2, 3, 4, 5, 6, 0];

/** Preset plan for browser preview; mirrors the Rust preset. `rich` adds ~4 months of history for screenshots. */
export function mockState(rich: boolean): State {
  const mk = (i: number, Title: string, Note: string, Group: string, Days: number[]) => ({ Id: `p${i}`, Title, Note, Group, Days, OnceDate: '' });
  const tasks = [
    mk(0, '词汇真经', '积累词义、搭配与语境。', '学习', ALL),
    mk(1, '顾家北写作', '跟着 31 天计划，每天留下一点输出。', '学习', ALL),
    mk(2, '雅思王语料库', '听写、核对，再听一遍易错词。', '学习', ALL),
    mk(3, '百词斩', '100 个新词 + 200 个复习词；按实际进度调整。', '学习', ALL),
    mk(4, '听力 + 阅读整套', '完成一套听阅，记录需要复盘的问题。', '学习', [2, 4, 6]),
    mk(5, '听阅复盘', '改错、精读、精听，整理同义替换。', '学习', [3, 5, 0]),
    mk(6, '胸 + 肩 + 三头', '', '健身', [1]),
    mk(7, '健身休息', '给身体留一点恢复空间。', '健身', [2, 4]),
    mk(8, '背 + 二头', '', '健身', [3]),
    mk(9, '腿 + 核心', '', '健身', [5]),
    mk(10, '倒三角专项', '', '健身', [6]),
    mk(11, '纯有氧', '', '健身', [0])
  ];
  const s = { Version: 2, Tasks: tasks, Done: [] as string[], Settings: { AlwaysOnTop: false, X: -9999, Y: -9999 } } as State;
  if (!rich) return s;
  let seed = 42;
  const rnd = () => ((seed = (seed * 1664525 + 1013904223) % 4294967296) / 4294967296);
  const t = today();
  s.Giveups = [];
  s.Sessions = [];
  s.Journal = {};
  s.Leaves = [
    { date: addKey(t, -9), reason: '感冒发烧', ts: 1 },
    { date: addKey(t, -2), reason: '考试周', ts: 2 },
    { date: addKey(t, -1), reason: '考试周', ts: 3 },
    { date: t, reason: '家里有事', ts: 4 }
  ];
  s.Settled = addKey(t, -1);
  for (let i = 140; i >= 1; i--) {
    const k = addKey(t, -i);
    const dow = new Date(k.replace(/-/g, '/')).getDay();
    const mood = rnd();
    for (const task of tasks) {
      if (!task.Days.includes(dow)) continue;
      if (rnd() < 0.35 + mood * 0.6) s.Done.push(`${k}/${task.Id}`);
    }
    for (const task of tasks) if (task.Days.includes(dow) && !s.Done.includes(`${k}/${task.Id}`) && rnd() < 0.3) s.Giveups.push({ ts: Date.now() - i * 86400000, date: k, taskId: task.Id, title: task.Title, ...(rnd() < 0.5 ? { reason: ['累', '没状态', '太忙', '拖延'][Math.floor(rnd() * 4)] } : {}) });
    const n = Math.floor(rnd() * 5);
    for (let j = 0; j < n; j++) s.Sessions.push({ ts: Date.now() - i * 86400000 + j, date: k, kind: 'focus', minutes: 25, planned: 25, taskId: tasks[Math.floor(rnd() * 4)].Id });
    if (rnd() < 0.5) s.Journal[k] = { mood: 2 + Math.floor(rnd() * 4), result: rnd() < 0.5 ? 'done' : 'partial', water: Math.floor(rnd() * 9) };
  }
  s.Exam = { Name: '雅思考试', Date: addKey(t, 47), Label: '单词', Total: 3000, Done: 1350 };
  s.Rewards = [
    { id: 'r1', text: '周末看一场电影' },
    { id: 'r2', text: '一杯喜欢的奶茶' }
  ];
  return s;
}
