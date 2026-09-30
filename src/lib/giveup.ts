import { doneIndex } from './model';
import type { State } from './types';

/** Each unfinished task settled into the bank is worth this many days. */
export const UNIT = 0.1;

export interface GiveupStats {
  count: number;
  /** Accumulated, in days (1 record = 0.1 day). */
  totalDays: number;
  activeDays: number;
  perActiveDay: number;
  yearDays: number;
  smallSample: boolean;
  byMonth: [string, number][];
  byReason: [string, number][];
  byTask: [string, number][];
}

export const round1 = (n: number) => Math.round(n * 10) / 10;

/** Days with any recorded activity: a check-off, a focus session or a settled miss. */
export function activeDates(s: State): Set<string> {
  const set = new Set<string>();
  for (const [d, ids] of doneIndex(s)) if (ids.size) set.add(d);
  for (const x of s.Sessions) set.add(x.date);
  for (const g of s.Giveups) set.add(g.date);
  return set;
}

const tally = (items: string[]) => {
  const m = new Map<string, number>();
  for (const i of items) m.set(i, (m.get(i) ?? 0) + 1);
  return [...m.entries()];
};

export function giveupStats(s: State): GiveupStats {
  const count = s.Giveups.length;
  const active = activeDates(s).size;
  const per = active ? count / active : 0;
  return {
    count,
    totalDays: round1(count * UNIT),
    activeDays: active,
    perActiveDay: per,
    yearDays: round1(per * UNIT * 365),
    smallSample: active < 7,
    byMonth: tally(s.Giveups.map((g) => g.date.slice(0, 7))).sort((a, b) => a[0].localeCompare(b[0])),
    byReason: tally(s.Giveups.filter((g) => g.reason).map((g) => g.reason!)).sort((a, b) => b[1] - a[1]),
    byTask: tally(s.Giveups.flatMap((g) => g.titles ?? [])).sort((a, b) => b[1] - a[1]).slice(0, 5)
  };
}

export function projectionText(st: GiveupStats): string {
  const base = `已放弃 ${st.totalDays.toFixed(1)} 天；照这个速度，一年将放弃 ${st.yearDays.toFixed(1)} 天`;
  return st.smallSample ? `${base}（样本较少，仅供参考）` : base;
}
