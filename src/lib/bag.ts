import type { BagState } from './types';

export function shuffle(n: number, rand: () => number = Math.random): number[] {
  const a = Array.from({ length: n }, (_, i) => i);
  for (let i = n - 1; i > 0; i--) {
    const j = Math.floor(rand() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

/**
 * Shuffle-bag daily pick. Same date gives the same item. Within one pass nothing repeats, and the
 * first item of a new pass never equals the last item of the previous one. When the pool grows
 * (an app update ships more entries) the new indices are dropped into the bag at random spots.
 */
export function pickToday(b: BagState, total: number, date: string, rand: () => number = Math.random): BagState {
  if (total <= 0) return b;
  if (b.Today && b.Today.date === date && b.Today.idx >= 0 && b.Today.idx < total) return b;
  const bag = b.Bag.filter((i) => i >= 0 && i < total);
  if (b.Total && total > b.Total) {
    for (let i = b.Total; i < total; i++) bag.splice(Math.floor(rand() * (bag.length + 1)), 0, i);
  }
  let next = bag;
  if (next.length === 0) {
    next = shuffle(total, rand);
    if (total > 1 && next[0] === b.Last) {
      const j = 1 + Math.floor(rand() * (total - 1));
      [next[0], next[j]] = [next[j], next[0]];
    }
  }
  const idx = next.shift()!;
  return { ...b, Bag: next, Total: total, Today: { date, idx }, Last: idx };
}
