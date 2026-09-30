import { daysBetween } from './dates';
import type { Exam } from './types';

export interface ExamInfo {
  days: number;
  over: boolean;
  /** Suggested daily amount to reach `Total` by the exam date, when a target is set. */
  perDay: number | null;
  remaining: number | null;
}

export function examInfo(exam: Exam | null, today: string): ExamInfo | null {
  if (!exam || !exam.Date) return null;
  const days = daysBetween(today, exam.Date);
  const hasGoal = typeof exam.Total === 'number' && exam.Total > 0;
  const remaining = hasGoal ? Math.max(0, exam.Total! - (exam.Done ?? 0)) : null;
  return {
    days,
    over: days < 0,
    remaining,
    // Today counts as a study day, so a exam 10 days out leaves 10 sessions (today .. exam day - 1).
    perDay: remaining === null || days <= 0 ? null : Math.ceil(remaining / days)
  };
}

export function examText(exam: Exam, info: ExamInfo): string {
  if (info.over) return `${exam.Name}已结束`;
  if (info.days === 0) return `${exam.Name}就在今天，加油`;
  return `距${exam.Name}还有 ${info.days} 天`;
}
