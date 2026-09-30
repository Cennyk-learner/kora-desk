import { QUOTES_1 } from './quotes1';
import { QUOTES_2 } from './quotes2';
import { QUOTES_3 } from './quotes3';
import type { Quote } from './quotes1';

// Order is part of the shuffle bag's identity: only ever append new entries.
export const QUOTES: Quote[] = [...QUOTES_1, ...QUOTES_2, ...QUOTES_3];
