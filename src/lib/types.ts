export interface Task {
  Id: string;
  Title: string;
  Note: string;
  Group: string;
  Days: number[];
  OnceDate: string;
  /** Estimated minutes (v2). */
  Est?: number;
  /** Spaced review: completing this task schedules follow-up reviews (v2). */
  Review?: boolean;
  /** Set on generated review tasks: "date/taskId" of the source (v2). */
  ReviewSrc?: string;
  /** Date the task was created (v2). Settlement never charges a task for days before it existed. */
  Created?: string;
}

export interface PomoCfg {
  Focus: number;
  Short: number;
  Long: number;
  Every: number;
  /** Auto-check the bound task after this many focus sessions today (0 = off). */
  AutoCheck: number;
  Strict: boolean;
  Noise: 'off' | 'white' | 'brown' | 'rain';
  Fullscreen: boolean;
}

export interface CareCfg {
  Eye: boolean;
  EyeMin: number;
  Sit: boolean;
  SitMin: number;
  Bed: boolean;
  BedTime: string;
  WaterGoal: number;
}

export interface Prefs {
  AlwaysOnTop: boolean;
  X: number;
  Y: number;
  Theme?: string | null;
  Appearance?: string | null;
  Mute?: boolean;
  Volume?: number;
  Motion?: 'auto' | 'off';
  Pomo?: PomoCfg;
  Care?: CareCfg;
  FreezePerMonth?: number;
  KeyFocus?: string;
  KeyOpen?: string;
  KeepAwake?: boolean;
}

export interface BagState {
  Bag: number[];
  Total: number;
  Today: { date: string; idx: number } | null;
  Last: number;
  Favs: number[];
}

/** One record per day that ended with unfinished tasks, made automatically at settlement. Worth 0.1 day. */
export interface Giveup {
  ts: number;
  date: string;
  /** Titles of the tasks left undone that day. */
  titles?: string[];
  reason?: string;
}

export interface Leave {
  date: string;
  reason: string;
  ts: number;
}

export interface Session {
  ts: number;
  date: string;
  kind: 'focus' | 'interrupted';
  minutes: number;
  planned: number;
  taskId?: string;
  recovered?: boolean;
}

export interface FocusRun {
  Mode: 'idle' | 'focus' | 'short' | 'long';
  EndsAt: number;
  StartedAt: number;
  Planned: number;
  TaskId: string;
  Cycle: number;
  PausedLeft: number;
}

export interface Journal {
  goal?: string;
  result?: 'done' | 'partial' | 'none';
  note?: string;
  mood?: number;
  grateful?: string[];
  water?: number;
  breaths?: number;
}

export interface Exam {
  Name: string;
  Date: string;
  Label?: string;
  Total?: number;
  Done?: number;
}

export interface Reward {
  id: string;
  text: string;
  claimed?: string;
}

export interface State {
  Version: number;
  Tasks: Task[];
  Done: string[];
  Settings: Prefs;
  Quotes: BagState;
  Words: BagState;
  Giveups: Giveup[];
  Sessions: Session[];
  Focus: FocusRun;
  Journal: Record<string, Journal>;
  Exam: Exam | null;
  Ach: Record<string, string>;
  Rewards: Reward[];
  Leaves: Leave[];
  /** Last date (inclusive) whose unfinished tasks were settled into the give-up bank. */
  Settled: string;
}

export type Page = 'today' | 'plan' | 'focus' | 'stats' | 'giveup' | 'care' | 'favs' | 'settings';
