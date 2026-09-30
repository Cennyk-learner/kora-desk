import { addKey, monday, parse, key, monthDay } from './dates';
import { giveupStats } from './giveup';
import { rateBetween, computeStreak } from './streak';
import { QUOTES } from '../data/quotes';
import type { State } from './types';

export interface WeekSummary {
  from: string;
  to: string;
  done: number;
  due: number;
  rate: number;
  focusMin: number;
  pomos: number;
  giveups: number;
  streak: number;
}

export function weekSummary(s: State, today: string): WeekSummary {
  const from = key(monday(parse(today)));
  const to = today;
  const { done, due } = rateBetween(s, from, to);
  const ses = s.Sessions.filter((x) => x.kind === 'focus' && x.date >= from && x.date <= to);
  return {
    from,
    to,
    done,
    due,
    rate: due ? done / due : 0,
    focusMin: ses.reduce((a, x) => a + x.minutes, 0),
    pomos: ses.length,
    giveups: s.Giveups.filter((g) => g.date >= from && g.date <= to).length,
    streak: computeStreak(s, today).current
  };
}

function css(name: string, fallback: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
}

/** Renders the weekly summary card as a PNG using the current skin's colours. */
export async function renderWeeklyCard(s: State, today: string): Promise<Blob> {
  const w = weekSummary(s, today);
  const gs = giveupStats(s);
  const W = 900;
  const H = 1100;
  const c = document.createElement('canvas');
  c.width = W;
  c.height = H;
  const g = c.getContext('2d')!;
  const bg = css('--bg', '#fff');
  const text = css('--text', '#111');
  const sub = css('--sub', '#666');
  const accent = css('--accent', '#0a84ff');
  const track = css('--track', '#ddd');
  const font = css('--font-body', 'sans-serif');
  g.fillStyle = bg;
  g.fillRect(0, 0, W, H);
  g.fillStyle = accent;
  g.fillRect(0, 0, W, 10);

  g.fillStyle = accent;
  g.font = `600 22px ${font}`;
  g.fillText('KORA DESK · 每周小结', 60, 90);
  g.fillStyle = text;
  g.font = `700 54px ${font}`;
  g.fillText(`${monthDay(parse(w.from))} – ${monthDay(parse(w.to))}`, 60, 170);

  // completion ring
  const cx = 230;
  const cy = 420;
  const r = 130;
  g.lineWidth = 26;
  g.lineCap = 'round';
  g.strokeStyle = track;
  g.beginPath();
  g.arc(cx, cy, r, 0, Math.PI * 2);
  g.stroke();
  g.strokeStyle = accent;
  g.beginPath();
  g.arc(cx, cy, r, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * w.rate);
  g.stroke();
  g.fillStyle = text;
  g.textAlign = 'center';
  g.font = `700 64px ${font}`;
  g.fillText(`${Math.round(w.rate * 100)}%`, cx, cy + 14);
  g.fillStyle = sub;
  g.font = `400 22px ${font}`;
  g.fillText(`完成 ${w.done} / ${w.due}`, cx, cy + 52);
  g.textAlign = 'left';

  const stat = (label: string, value: string, x: number, y: number) => {
    g.fillStyle = sub;
    g.font = `400 22px ${font}`;
    g.fillText(label, x, y);
    g.fillStyle = text;
    g.font = `700 44px ${font}`;
    g.fillText(value, x, y + 54);
  };
  stat('专注时长', `${Math.floor(w.focusMin / 60)} 时 ${w.focusMin % 60} 分`, 460, 320);
  stat('番茄', `${w.pomos} 个`, 460, 440);
  stat('连续打卡', `${w.streak} 天`, 460, 560);

  g.fillStyle = track;
  g.fillRect(60, 640, W - 120, 2);
  g.fillStyle = sub;
  g.font = `400 24px ${font}`;
  g.fillText('放弃库', 60, 700);
  g.fillStyle = text;
  g.font = `700 44px ${font}`;
  g.fillText(`本周 ${w.giveups} 项  ·  累计 ${gs.totalDays.toFixed(1)} 天`, 60, 760);
  g.fillStyle = sub;
  g.font = `400 22px ${font}`;
  g.fillText(w.giveups === 0 ? '这一周没有未完成的任务。' : '没完成的已被诚实地记下，下周继续。', 60, 800);

  const idx = s.Quotes.Today?.idx ?? 0;
  const q = QUOTES[idx] ?? QUOTES[0];
  g.fillStyle = text;
  g.font = `400 30px ${font}`;
  wrap(g, q[0], 60, 900, W - 120, 46);
  g.fillStyle = sub;
  g.font = `400 22px ${font}`;
  g.fillText(`—— ${q[1]}`, 60, 1030);
  g.fillText(addKey(today, 0), W - 200, 1060);

  return new Promise((resolve, reject) => c.toBlob((b) => (b ? resolve(b) : reject(new Error('无法生成图片'))), 'image/png'));
}

function wrap(g: CanvasRenderingContext2D, t: string, x: number, y: number, max: number, lh: number) {
  let line = '';
  let yy = y;
  for (const ch of t) {
    if (g.measureText(line + ch).width > max) {
      g.fillText(line, x, yy);
      line = ch;
      yy += lh;
    } else line += ch;
  }
  g.fillText(line, x, yy);
}

export function toCsv(s: State): string {
  const esc = (v: string | number) => `"${String(v).replace(/"/g, '""')}"`;
  const rows: (string | number)[][] = [['类型', '日期', '内容', '数值', '备注']];
  const title = new Map(s.Tasks.map((t) => [t.Id, t.Title]));
  for (const d of s.Done) rows.push(['完成', d.slice(0, 10), title.get(d.slice(11)) ?? d.slice(11), 1, '']);
  for (const x of s.Sessions) rows.push([x.kind === 'focus' ? '番茄' : '被打断', x.date, title.get(x.taskId ?? '') ?? '自由专注', x.minutes, `计划 ${x.planned} 分钟`]);
  for (const x of s.Giveups) rows.push(['放弃', x.date, x.title ?? '未完成任务', 0.1, x.reason ?? '']);
  for (const l of s.Leaves) rows.push(['请假', l.date, l.reason, 1, '']);
  for (const [d, j] of Object.entries(s.Journal)) rows.push(['心情/复盘', d, j.note ?? j.goal ?? '', j.mood ?? '', j.result ?? '']);
  return '﻿' + rows.map((r) => r.map(esc).join(',')).join('\r\n');
}
