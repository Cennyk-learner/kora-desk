// All sounds are synthesised with WebAudio; no audio files ship with the app.
let ctx: AudioContext | null = null;
let master: GainNode | null = null;
let mute = false;
let volume = 0.6;
let lastPing = 0;
let step = 0;
let noiseNodes: { stop: () => void } | null = null;

// C major pentatonic, two octaves: each quick check-off climbs one step (a combo).
const SCALE = [523.25, 587.33, 659.25, 783.99, 880.0, 1046.5, 1174.66, 1318.51, 1567.98];

export function configure(m: boolean, v: number): void {
  mute = m;
  volume = Math.max(0, Math.min(1, v));
  if (master) master.gain.value = mute ? 0 : volume;
}

function ensure(): AudioContext | null {
  try {
    if (!ctx) {
      ctx = new AudioContext();
      master = ctx.createGain();
      master.connect(ctx.destination);
    }
    if (ctx.state === 'suspended') void ctx.resume();
    master!.gain.value = mute ? 0 : volume;
    return ctx;
  } catch {
    return null;
  }
}

/** Browsers only start audio after a gesture; call this from the first pointer/key event. */
export function unlock(): void {
  ensure();
}

function tone(freq: number, at: number, dur: number, type: OscillatorType = 'sine', gain = 0.2, glideTo?: number): void {
  const c = ensure();
  if (!c || !master) return;
  const o = c.createOscillator();
  const g = c.createGain();
  o.type = type;
  o.frequency.setValueAtTime(freq, c.currentTime + at);
  if (glideTo) o.frequency.exponentialRampToValueAtTime(glideTo, c.currentTime + at + dur);
  g.gain.setValueAtTime(0.0001, c.currentTime + at);
  g.gain.exponentialRampToValueAtTime(gain, c.currentTime + at + 0.01);
  g.gain.exponentialRampToValueAtTime(0.0001, c.currentTime + at + dur);
  o.connect(g).connect(master);
  o.start(c.currentTime + at);
  o.stop(c.currentTime + at + dur + 0.05);
}

/** Check-off ping. Quick successive calls climb the scale. Returns the combo length. */
export function ping(): number {
  const now = performance.now();
  step = now - lastPing < 4000 ? Math.min(step + 1, SCALE.length - 1) : 0;
  lastPing = now;
  const f = SCALE[step];
  tone(f, 0, 0.32, 'sine', 0.22);
  tone(f * 2, 0, 0.16, 'sine', 0.05);
  return step + 1;
}

export function untick(): void {
  tone(392, 0, 0.12, 'sine', 0.1);
}

/** Whole day complete: rising arpeggio. */
export function fanfare(): void {
  [523.25, 659.25, 783.99, 1046.5, 1318.51].forEach((f, i) => tone(f, i * 0.09, 0.5, 'triangle', 0.16));
  tone(1046.5, 0.5, 0.9, 'sine', 0.1);
}

/** Pomodoro finished: a soft bell built from inharmonic partials. */
export function bell(): void {
  const base = 660;
  [1, 2.76, 5.4].forEach((m, i) => tone(base * m, 0, 2.2 - i * 0.5, 'sine', 0.16 / (i + 1)));
  [1, 2.76, 5.4].forEach((m, i) => tone(base * m, 0.7, 1.8 - i * 0.4, 'sine', 0.09 / (i + 1)));
}

/** Break over / gentle reminder. */
export function chime(): void {
  tone(880, 0, 0.35, 'sine', 0.14);
  tone(1174.66, 0.16, 0.5, 'sine', 0.12);
}

/** Give-up: a low, falling tone. Not punishing, just honest. */
export function thud(): void {
  tone(220, 0, 0.7, 'sine', 0.3, 90);
  tone(110, 0, 0.9, 'triangle', 0.15, 55);
}

/** Warm up-tone for "keep going". */
export function lift(): void {
  tone(392, 0, 0.25, 'sine', 0.16);
  tone(587.33, 0.12, 0.4, 'sine', 0.16);
}

export function breathCue(up: boolean): void {
  tone(up ? 330 : 262, 0, 0.6, 'sine', 0.06);
}

export function stopNoise(): void {
  noiseNodes?.stop();
  noiseNodes = null;
}

export type NoiseKind = 'white' | 'brown' | 'rain';

/** Looping generated noise for focus. */
export function startNoise(kind: NoiseKind): void {
  stopNoise();
  const c = ensure();
  if (!c || !master) return;
  const len = c.sampleRate * 4;
  const buf = c.createBuffer(1, len, c.sampleRate);
  const d = buf.getChannelData(0);
  let last = 0;
  for (let i = 0; i < len; i++) {
    const w = Math.random() * 2 - 1;
    if (kind === 'brown') {
      last = (last + 0.02 * w) / 1.02;
      d[i] = last * 3.5;
    } else d[i] = w;
  }
  const src = c.createBufferSource();
  src.buffer = buf;
  src.loop = true;
  const filter = c.createBiquadFilter();
  const gain = c.createGain();
  if (kind === 'rain') {
    filter.type = 'bandpass';
    filter.frequency.value = 2800;
    filter.Q.value = 0.4;
  } else if (kind === 'white') {
    filter.type = 'lowpass';
    filter.frequency.value = 9000;
  } else {
    filter.type = 'lowpass';
    filter.frequency.value = 1200;
  }
  gain.gain.setValueAtTime(0.0001, c.currentTime);
  gain.gain.exponentialRampToValueAtTime(kind === 'brown' ? 0.5 : 0.14, c.currentTime + 1.2);
  src.connect(filter).connect(gain).connect(master);
  src.start();
  noiseNodes = {
    stop: () => {
      try {
        gain.gain.cancelScheduledValues(c.currentTime);
        gain.gain.setValueAtTime(gain.gain.value, c.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, c.currentTime + 0.5);
        src.stop(c.currentTime + 0.6);
      } catch {
        /* already stopped */
      }
    }
  };
}
