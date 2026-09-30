export type FxKind = 'burst' | 'rain' | 'pixels' | 'ripple';

interface Handler {
  (kind: FxKind, x?: number, y?: number): void;
}

let handler: Handler | null = null;
let off = false;

export function registerFx(h: Handler | null): void {
  handler = h;
}

/** Motion preference: 'off' always disables; otherwise follow the OS setting. */
export function setMotion(pref: 'auto' | 'off' | undefined): void {
  off = pref === 'off' || (typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches);
}

export const motionOff = () => off;

export function fx(kind: FxKind, x?: number, y?: number): void {
  if (off) return;
  handler?.(kind, x, y);
}

/** Centre of an element, for anchoring effects. */
export function centerOf(el: Element | null): { x: number; y: number } | undefined {
  if (!el) return undefined;
  const r = el.getBoundingClientRect();
  return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
}
