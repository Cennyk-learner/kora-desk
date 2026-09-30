const pad = (n: number) => String(n).padStart(2, '0');

export const ORDER = [1, 2, 3, 4, 5, 6, 0];
export const SHORT = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'];
export const LONG = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];

export function key(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export function parse(k: string): Date {
  const [y, m, d] = k.split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function addDays(d: Date, n: number): Date {
  const r = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  r.setDate(r.getDate() + n);
  return r;
}

export function addKey(k: string, n: number): string {
  return key(addDays(parse(k), n));
}

export function monday(d: Date): Date {
  return addDays(d, -((d.getDay() + 6) % 7));
}

export function today(now = new Date()): string {
  return key(now);
}

export function monthDay(d: Date): string {
  return `${d.getMonth() + 1}月${d.getDate()}日`;
}

export function daysBetween(a: string, b: string): number {
  const x = parse(a);
  const y = parse(b);
  return Math.round((Date.UTC(y.getFullYear(), y.getMonth(), y.getDate()) - Date.UTC(x.getFullYear(), x.getMonth(), x.getDate())) / 86400000);
}

export function monthOf(k: string): string {
  return k.slice(0, 7);
}
