<script lang="ts">
  import { app, isDoneOn, tasksFor, update } from '../lib/app.svelte';
  import { pausePomo, resumePomo, startPomo, stopPomo } from '../lib/engine.svelte';
  import { isPaused } from '../lib/pomodoro';
  import PomoRing from '../components/PomoRing.svelte';
  import Icon from '../components/Icon.svelte';
  import Seg from '../components/Seg.svelte';
  import Toggle from '../components/Toggle.svelte';

  const f = $derived(app.state.Focus);
  const cfg = $derived(app.state.Settings.Pomo!);
  const running = $derived(f.Mode !== 'idle');
  const paused = $derived(isPaused(f));
  const open = $derived(tasksFor(app.today).filter((t) => !isDoneOn(t, app.today)));
  const bound = $derived(app.state.Tasks.find((t) => t.Id === f.TaskId));
  const todays = $derived(app.state.Sessions.filter((s) => s.date === app.today));
  const focusMin = $derived(todays.filter((s) => s.kind === 'focus').reduce((a, s) => a + s.minutes, 0));
  const count = $derived(todays.filter((s) => s.kind === 'focus').length);
  const inCycle = $derived(f.Cycle % Math.max(1, cfg.Every));
  let chosen = $state('');
  const pomo = (fn: (p: typeof cfg) => void) => update((s) => fn(s.Settings.Pomo!));
  const num = (e: Event, min: number, max: number, fb: number) => {
    const v = Math.round(Number((e.currentTarget as HTMLInputElement).value));
    return Number.isFinite(v) ? Math.max(min, Math.min(max, v)) : fb;
  };
  function begin() {
    void startPomo(taskId);
    if (cfg.Fullscreen) {
      app.fullscreen = true;
      app.minimalFocus = true;
    }
  }
  function toggleFull() {
    app.fullscreen = !app.fullscreen;
    app.minimalFocus = app.fullscreen;
  }
  function bind(id: string) {
    chosen = id;
    if (running && f.Mode === 'focus') void update((s) => (s.Focus.TaskId = id));
  }
  const taskId = $derived(running ? f.TaskId : chosen);
</script>

<div class="inner">
  {#if !app.minimalFocus}
    <div class="kicker">FOCUS</div>
    <h1 class="title">专注一会儿。</h1>
    <div class="sub">{cfg.Focus} 分钟专注，{cfg.Short} 分钟休息；每 {cfg.Every} 个番茄一次长休息（{cfg.Long} 分钟）。</div>
  {/if}

  <div class="focus-stage">
    <PomoRing size={app.minimalFocus ? 320 : 260} />
    <div class="sub" style="min-height:20px">
      {#if bound}正在做：<b style="color:var(--text)">{bound.Title}</b>{:else if running}自由专注{:else}选一件要做的事，或直接开始{/if}
    </div>
    <div class="dots" style="justify-content:center">
      {#each Array.from({ length: cfg.Every }) as _, i}<i class:on={i < inCycle} style="width:12px;height:12px"></i>{/each}
    </div>
    <div class="row" style="gap:10px">
      {#if !running}
        <button class="btn primary" style="padding:11px 28px" onclick={begin}><Icon name="play" size={14} /> 开始专注</button>
      {:else}
        {#if paused}
          <button class="btn primary" style="padding:11px 24px" onclick={resumePomo}><Icon name="play" size={14} /> 继续</button>
        {:else if !(cfg.Strict && f.Mode === 'focus')}
          <button class="btn primary" style="padding:11px 24px" onclick={pausePomo}><Icon name="pause" size={14} /> 暂停</button>
        {/if}
        <button class="btn" onclick={stopPomo}><Icon name="stop" size={14} /> {f.Mode === 'focus' ? '放弃本次' : '结束休息'}</button>
      {/if}
      {#if running}<button class="btn ghost" onclick={() => (app.minimalFocus = !app.minimalFocus)}>{app.minimalFocus ? '退出极简视图' : '极简视图'}</button>{/if}
      <button class="btn ghost" onclick={toggleFull}>{app.fullscreen ? '退出全屏 (Esc)' : '全屏专注'}</button>
    </div>
  </div>

  {#if !app.minimalFocus}
    {#if !running}
      <h2 class="sec" style="margin-top:8px">开始前的选择 <small>会记住你上次的选择</small></h2>
      <div class="card stack">
        <div class="row spread wrap"><span>专注时长</span>
          <div class="row" style="gap:6px">
            {#each [15, 25, 45, 60] as m}<button class="tag" class:on={cfg.Focus === m} onclick={() => pomo((p) => (p.Focus = m))}>{m}</button>{/each}
            <input type="number" min="1" max="180" value={cfg.Focus} onchange={(e) => pomo((p) => (p.Focus = num(e, 1, 180, 25)))} /><span class="sub">分</span>
          </div>
        </div>
        <div class="row spread"><span>短休息</span><div class="row" style="gap:6px"><input type="number" min="1" max="60" value={cfg.Short} onchange={(e) => pomo((p) => (p.Short = num(e, 1, 60, 5)))} /><span class="sub">分</span></div></div>
        <div class="row spread"><span>长休息<span class="hint">每 N 个番茄一次</span></span><div class="row" style="gap:6px"><input type="number" min="1" max="90" value={cfg.Long} onchange={(e) => pomo((p) => (p.Long = num(e, 1, 90, 15)))} /><span class="sub">分 · 每</span><input type="number" min="2" max="12" value={cfg.Every} onchange={(e) => pomo((p) => (p.Every = num(e, 2, 12, 4)))} /><span class="sub">个</span></div></div>
        <div>
          <div class="label" style="margin:0 0 8px">绑定任务</div>
          <div class="row wrap" style="gap:6px">
            <button class="tag" class:on={!taskId} onclick={() => bind('')}>不绑定</button>
            {#each open as t}<button class="tag" class:on={taskId === t.Id} onclick={() => bind(t.Id)}>{t.Title}</button>{/each}
          </div>
        </div>
        <div class="row spread"><span>完成 N 个番茄后自动勾选任务<span class="hint">0 = 不自动勾选</span></span><input type="number" min="0" max="20" value={cfg.AutoCheck} onchange={(e) => pomo((p) => (p.AutoCheck = num(e, 0, 20, 0)))} /></div>
        <div class="row spread"><span>背景声</span><Seg items={[['off', '关'], ['brown', '棕噪音'], ['white', '白噪音'], ['rain', '雨声']]} value={cfg.Noise} onpick={(v) => pomo((p) => (p.Noise = v as typeof cfg.Noise))} /></div>
        <div class="row spread"><span>严格模式<span class="hint">专注中不能暂停，只能放弃本次</span></span><Toggle on={cfg.Strict} onchange={(v) => pomo((p) => (p.Strict = v))} label="严格模式" /></div>
        <div class="row spread"><span>专注时保持屏幕常亮<span class="hint">防止电脑休眠或熄屏</span></span><Toggle on={app.state.Settings.KeepAwake !== false} onchange={(v) => update((s) => (s.Settings.KeepAwake = v))} label="防休眠" /></div>
        <div class="row spread"><span>开始时进入全屏</span><Toggle on={cfg.Fullscreen} onchange={(v) => pomo((p) => (p.Fullscreen = v))} label="开始时全屏" /></div>
      </div>
    {:else}
      <div class="card row spread wrap" style="margin-top:6px">
        <span class="sub">专注期间{app.state.Settings.KeepAwake !== false ? '屏幕保持常亮，' : ''}背景声：{({ off: '关', brown: '棕噪音', white: '白噪音', rain: '雨声' } as Record<string, string>)[cfg.Noise]}</span>
        <span class="sub">{cfg.Strict && f.Mode === 'focus' ? '严格模式：不能暂停' : ''}</span>
      </div>
    {/if}

    <h2 class="sec">今天 <small>{count} 个番茄 · {focusMin} 分钟</small></h2>
    {#if todays.length === 0}
      <div class="sub">还没有专注记录。完成第一个番茄就会出现在这里。</div>
    {:else}
      <div class="tasks">
        {#each [...todays].reverse().slice(0, 10) as s}
          <div class="task" style="grid-template-columns:auto 1fr auto">
            <span>{s.kind === 'focus' ? '🍅' : '⏸'}</span>
            <div><div class="t" style="font-size:14px">{app.state.Tasks.find((t) => t.Id === s.taskId)?.Title ?? '自由专注'}</div><div class="n">{s.kind === 'focus' ? '完成' : '被打断'}{s.recovered ? ' · 离开时完成' : ''}</div></div>
            <span class="chip">{s.minutes} 分钟</span>
          </div>
        {/each}
      </div>
    {/if}
  {/if}
</div>
