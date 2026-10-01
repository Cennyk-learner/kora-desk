<script lang="ts">
  import { onMount } from 'svelte';
  import { app } from '../lib/app.svelte';
  import { pausePomo, resumePomo, startPomo, stopPomo } from '../lib/engine.svelte';
  import { clock, isPaused, leftMs } from '../lib/pomodoro';
  import { shuffle } from '../lib/bag';
  import { SCENES } from '../data/scenes';
  import Icon from './Icon.svelte';

  // A shuffle bag that survives restarts, so every opening starts on a different photo and
  // nothing repeats until all of them have been seen.
  const BAG_KEY = 'kora-scene-bag';
  const LAST_KEY = 'kora-scene-last';
  const TOTAL_KEY = 'kora-scene-total';
  function load(): number[] {
    try {
      const raw = JSON.parse(localStorage.getItem(BAG_KEY) ?? '[]');
      const bag: number[] = Array.isArray(raw) ? raw.filter((i: number) => Number.isInteger(i) && i >= 0 && i < SCENES.length) : [];
      const was = Number(localStorage.getItem(TOTAL_KEY) ?? 0);
      // Photos added by an update join the current round at random places.
      if (was > 0 && was < SCENES.length) for (let i = was; i < SCENES.length; i++) bag.splice(Math.floor(Math.random() * (bag.length + 1)), 0, i);
      return bag;
    } catch {
      return [];
    }
  }
  function save(bag: number[], last: number) {
    try {
      localStorage.setItem(BAG_KEY, JSON.stringify(bag));
      localStorage.setItem(LAST_KEY, String(last));
      localStorage.setItem(TOTAL_KEY, String(SCENES.length));
    } catch {
      /* optional */
    }
  }
  let bag = load();
  const history: number[] = [];
  function draw(): number {
    if (bag.length === 0) {
      const last = Number(localStorage.getItem(LAST_KEY) ?? -1);
      bag = shuffle(SCENES.length);
      if (SCENES.length > 1 && bag[0] === last) [bag[0], bag[1]] = [bag[1], bag[0]];
    }
    const i = bag.shift()!;
    save(bag, i);
    return i;
  }

  let front = $state(0); // which of the two layers is visible
  const first = draw();
  let layers = $state<[number, number]>([first, first]);
  history.push(first);
  let hpos = 0;
  let fade = $state(6); // seconds; quick for a manual change, slow for the timed one
  let active = $state(true); // controls visible while the pointer moves
  let idleTimer: ReturnType<typeof setTimeout> | undefined;
  let rotateTimer: ReturnType<typeof setInterval> | undefined;

  const f = $derived(app.state.Focus);
  const cfg = $derived(app.state.Settings.Pomo!);
  const hours = $derived(app.state.Settings.SceneHours ?? 5);
  const running = $derived(f.Mode !== 'idle');
  const left = $derived(leftMs(f, app.now));
  const label = $derived(f.Mode === 'focus' ? '专注中' : f.Mode === 'short' ? '短休息' : f.Mode === 'long' ? '长休息' : '准备好了');
  const task = $derived(app.state.Tasks.find((t) => t.Id === f.TaskId));

  function preload(i: number) {
    const img = new Image();
    img.src = `/scenes/${SCENES[i].file}`;
  }

  function show(i: number, seconds: number) {
    fade = seconds;
    const back = 1 - front;
    layers[back] = i;
    requestAnimationFrame(() => (front = back));
  }

  /** Manual or timed change. Going back replays what was shown before this opening. */
  function advance(step: 1 | -1, seconds = 6) {
    if (step === -1) {
      if (hpos === 0) return;
      hpos--;
    } else if (hpos < history.length - 1) hpos++;
    else {
      history.push(draw());
      hpos = history.length - 1;
    }
    show(history[hpos], seconds);
    if (bag.length) preload(bag[0]);
  }

  function restartTimer() {
    clearInterval(rotateTimer);
    if (hours > 0) rotateTimer = setInterval(() => advance(1), hours * 3600 * 1000);
  }

  function wake() {
    active = true;
    clearTimeout(idleTimer);
    idleTimer = setTimeout(() => (active = false), 3500);
  }

  function exit() {
    app.fullscreen = false;
  }

  onMount(() => {
    if (bag.length) preload(bag[0]);
    wake();
    return () => {
      clearInterval(rotateTimer);
      clearTimeout(idleTimer);
    };
  });
  $effect(() => {
    void hours;
    restartTimer();
  });
</script>

<svelte:window onkeydown={(e) => e.key === 'Escape' && exit()} />
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="fs" class:active onmousemove={wake} onmousedown={wake}>
  {#each [0, 1] as i}
    <div class="layer" class:show={front === i} style="--fade:{fade}s">
      <div class="bg" style="background-image:url('/scenes/{SCENES[layers[i]].file}')"></div>
      <div class="caption">
        <div class="place">{SCENES[layers[i]].place}</div>
        <div class="where">{SCENES[layers[i]].sub}</div>
        <div class="credit">摄影：{SCENES[layers[i]].author} · {SCENES[layers[i]].license} · Wikimedia Commons</div>
      </div>
    </div>
  {/each}
  <div class="shade"></div>

  <div class="top ctl"><button class="x" onclick={exit} title="退出全屏 (Esc)"><Icon name="close" size={16} /> 退出全屏</button></div>

  <div class="center">
    <div class="state">{label}{f.PausedLeft > 0 ? ' · 已暂停' : ''}</div>
    <div class="clock">{running ? clock(left) : clock(cfg.Focus * 60000)}</div>
    <div class="what">{task ? task.Title : running ? '自由专注' : '一杯茶的时间，只做一件事'}</div>
    <div class="row ctl" style="gap:12px;margin-top:26px;justify-content:center">
      {#if !running}
        <button class="big primary" onclick={() => startPomo(f.TaskId)}><Icon name="play" size={16} /> 开始专注</button>
      {:else}
        {#if isPaused(f)}
          <button class="big primary" onclick={resumePomo}><Icon name="play" size={16} /> 继续</button>
        {:else if !(cfg.Strict && f.Mode === 'focus')}
          <button class="big primary" onclick={pausePomo}><Icon name="pause" size={16} /> 暂停</button>
        {/if}
        <button class="big" onclick={stopPomo}><Icon name="stop" size={16} /> {f.Mode === 'focus' ? '放弃本次' : '结束休息'}</button>
      {/if}
    </div>
  </div>

  <div class="next ctl">
    <button class="x" onclick={() => { advance(-1, 1.2); restartTimer(); }} aria-label="上一张"><Icon name="left" size={16} /></button>
    <button class="x" onclick={() => { advance(1, 1.2); restartTimer(); }} aria-label="下一张"><Icon name="right" size={16} /></button>
  </div>
</div>

<style>
  .fs { position: fixed; inset: 0; z-index: 60; color: #fff; background: #0b0f14; overflow: hidden; cursor: none; user-select: none; }
  .fs.active { cursor: default; }
  .layer { position: absolute; inset: 0; opacity: 0; transition: opacity var(--fade, 6s) ease-in-out; }
  .layer.show { opacity: 1; }
  .bg { position: absolute; inset: 0; background-size: cover; background-position: center; transform: scale(1.04); }
  .layer.show .bg { animation: drift 60s ease-out forwards; }
  @keyframes drift { from { transform: scale(1.04); } to { transform: scale(1.12); } }
  /* Keep the photograph bright: only a soft pool behind the clock and a light foot for the caption. */
  .shade { position: absolute; inset: 0; pointer-events: none; background: radial-gradient(ellipse 46% 34% at 50% 48%, rgba(0, 0, 0, 0.34), rgba(0, 0, 0, 0) 100%), linear-gradient(to top, rgba(0, 0, 0, 0.32), transparent 22%); }
  .center { pointer-events: none; position: absolute; inset: 0; display: grid; place-content: center; text-align: center; padding: 0 24px; }
  .state { font-size: 15px; letter-spacing: 0.3em; opacity: 0.95; text-shadow: 0 1px 3px rgba(0, 0, 0, 0.5), 0 2px 14px rgba(0, 0, 0, 0.7); }
  .clock { font-size: clamp(88px, 17vw, 240px); font-weight: 300; font-variant-numeric: tabular-nums; line-height: 1.05; letter-spacing: -0.02em; text-shadow: 0 2px 4px rgba(0, 0, 0, 0.35), 0 6px 40px rgba(0, 0, 0, 0.6); }
  .what { font-size: 20px; text-shadow: 0 1px 3px rgba(0, 0, 0, 0.5), 0 2px 16px rgba(0, 0, 0, 0.75); }
  .caption { position: absolute; left: 28px; bottom: 24px; max-width: 60%; text-shadow: 0 1px 3px rgba(0, 0, 0, 0.6), 0 1px 12px rgba(0, 0, 0, 0.8); }
  .place { font-size: 17px; font-weight: 600; }
  .where { font-size: 13px; opacity: 0.85; margin-top: 2px; }
  .credit { font-size: 11px; opacity: 0.65; margin-top: 6px; }
  .center :global(.ctl) { pointer-events: auto; }
  .top { z-index: 2; position: absolute; right: 22px; top: 18px; }
  .next { z-index: 2; position: absolute; right: 22px; bottom: 22px; display: flex; gap: 8px; }
  .ctl { transition: opacity 0.5s; opacity: 0; }
  .fs.active .ctl { opacity: 1; }
  .x, .big { display: inline-flex; align-items: center; gap: 6px; color: #fff; background: rgba(255, 255, 255, 0.16); backdrop-filter: blur(10px); border: 1px solid rgba(255, 255, 255, 0.28); border-radius: 99px; padding: 8px 14px; font-size: 13px; cursor: pointer; }
  .x:hover, .big:hover { background: rgba(255, 255, 255, 0.28); }
  .big { padding: 12px 26px; font-size: 15px; }
  .big.primary { background: rgba(255, 255, 255, 0.92); color: #10151c; border-color: transparent; font-weight: 600; }
  .big.primary:hover { background: #fff; }
</style>
