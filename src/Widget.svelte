<script lang="ts">
  import { onMount } from 'svelte';
  import { app, isDoneOn, skippedFor, tasksFor } from './lib/app.svelte';
  import * as backend from './lib/backend';
  import { checkTask } from './lib/feedback';
  import { examInfo, examText } from './lib/exam';
  import { groupBy } from './lib/model';
  import { isPaused } from './lib/pomodoro';
  import { computeStreak } from './lib/streak';
  import { monthDay, parse } from './lib/dates';
  import Check from './components/Check.svelte';
  import FxLayer from './components/FxLayer.svelte';
  import Icon from './components/Icon.svelte';
  import PomoRing from './components/PomoRing.svelte';
  import Progress from './components/Progress.svelte';
  import QuoteCard from './components/QuoteCard.svelte';
  import Toasts from './components/Toasts.svelte';

  const day = $derived(app.today);
  const tasks = $derived(tasksFor(day));
  const done = $derived(tasks.filter((t) => isDoneOn(t, day)).length);
  const groups = $derived(groupBy(tasks, (t) => t.Group));
  const skipCount = $derived(skippedFor(day).length);
  const f = $derived(app.state.Focus);
  const exam = $derived(examInfo(app.state.Exam, day));
  const streak = $derived(computeStreak(app.state, day));
  const pinned = $derived(app.state.Settings.AlwaysOnTop);
  const bound = $derived(app.state.Tasks.find((t) => t.Id === f.TaskId));
  let menu = $state(false);
  let status = $state('');
  let frame: HTMLDivElement;

  const summary = $derived(tasks.length && done === tasks.length ? '全部完成 · 今天辛苦了' : `已完成 ${done} / ${tasks.length}`);

  onMount(() => {
    const ro = new ResizeObserver(() => {
      if (!frame) return;
      void backend.call('widget_resize', { width: frame.offsetWidth + 24, height: frame.offsetHeight + 24 });
    });
    ro.observe(frame);
    const poll = setInterval(async () => (status = (await backend.call<string>('widget_status')) ?? ''), 2000);
    void backend.call<string>('widget_status').then((s) => (status = s ?? ''));
    return () => {
      ro.disconnect();
      clearInterval(poll);
    };
  });

  const drag = (e: MouseEvent) => {
    if (e.button === 0) void backend.winAction('drag');
  };
  const run = (fn: () => unknown) => {
    menu = false;
    void fn();
  };
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="w-frame" bind:this={frame} onmousedown={(e) => menu && !(e.target as HTMLElement).closest('.menu, .more') && (menu = false)}>
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <div class="w-head" onmousedown={drag} title="拖动移动位置">
    <div>
      <div class="kicker">{monthDay(parse(day))} · KORA</div>
      <div class="w-title">今天的事</div>
    </div>
    <span class="pill count num">{done}/{tasks.length}</span>
  </div>

  <div class="w-prog"><Progress {done} total={tasks.length} size={64} /></div>
  <div class="sub" style="font-size:12px;margin:2px 2px 6px">{summary}{#if streak.current > 0} · 🔥{streak.current}{/if}</div>

  {#if app.state.Exam && exam && !exam.over}
    <div class="chip accent" style="margin-bottom:6px">🎯 {examText(app.state.Exam, exam)}</div>
  {/if}

  {#if f.Mode !== 'idle'}
    <div class="w-pomo">
      <PomoRing size={54} compact />
      <div style="min-width:0;flex:1">
        <div style="font-size:12px;font-weight:600">{f.Mode === 'focus' ? '专注中' : '休息中'}{isPaused(f) ? ' · 暂停' : ''}</div>
        <div class="sub" style="font-size:11px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">{bound?.Title ?? '自由专注'}</div>
      </div>
      <button class="btn ghost icon" aria-label={isPaused(f) ? '继续' : '暂停'} onclick={() => backend.emitEvent('focus-toggle')}><Icon name={isPaused(f) ? 'play' : 'pause'} size={14} /></button>
    </div>
  {/if}

  <div class="w-list">
    {#if tasks.length === 0}
      <div class="empty" style="padding:22px 6px">今天留白，{'\n'}也很好。</div>
    {/if}
    {#each groups as [g, items]}
      <div class="group-h" style="margin:10px 2px 4px"><span>{g}</span></div>
      {#each items as t (t.Id)}
        {@const on = isDoneOn(t, day)}
        <div class="w-task" class:done={on}>
          <Check {on} label={t.Title} onchange={(v, el) => checkTask(t, day, v, el)} />
          <span class="t">{t.Title}</span>
        </div>
      {/each}
    {/each}
  </div>

  {#if skipCount}<div class="faint" style="font-size:11px;margin:6px 2px 0">🌿 {skipCount} 项已请假</div>{/if}
  <div class="w-quote"><QuoteCard compact /></div>

  <div class="w-foot">
    <span class="faint" style="font-size:11px">{status}</span>
    <div class="row" style="gap:4px">
      <button class="btn sm" onclick={() => backend.call('open_main', { page: 'today' })}>打开计划 ↗</button>
      <button class="btn ghost icon more" aria-label="更多" onclick={() => (menu = !menu)}><Icon name="more" size={14} /></button>
    </div>
  </div>

  {#if menu}
    <div class="menu" style="right:10px;bottom:46px">
      <button onclick={() => run(() => backend.call('toggle_top'))}>{pinned ? '✓ 始终置顶（点击关闭）' : '切换为始终置顶'}</button>
      <button onclick={() => run(() => backend.emitEvent('focus-toggle'))}>{f.Mode === 'idle' ? '开始番茄钟' : isPaused(f) ? '继续番茄钟' : '暂停番茄钟'}</button>
      <button onclick={() => run(() => backend.call('open_main', { page: 'plan' }))}>编辑每周计划</button>
      <hr />
      <button onclick={() => run(() => backend.call('widget_hide'))}>隐藏组件（可从托盘恢复）</button>
    </div>
  {/if}
  <Toasts />
</div>
<FxLayer />

<style>
  .w-frame {
    position: absolute; left: 12px; top: 12px; width: 300px; min-height: 240px; padding: 14px 14px 10px;
    background: var(--widget-fill); border: 1px solid var(--widget-border); border-radius: var(--r-widget, 22px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, calc(var(--shadow-op) * 0.9)); display: flex; flex-direction: column; overflow: hidden;
  }
  .w-head { display: flex; justify-content: space-between; align-items: center; cursor: grab; padding-bottom: 6px; }
  .w-title { font-family: var(--font-display); font-weight: var(--title-w); font-size: 20px; margin-top: 2px; }
  .count { font-weight: 600; color: var(--text); }
  .w-prog { margin: 4px 0 2px; display: flex; }
  .w-pomo { display: flex; align-items: center; gap: 8px; padding: 6px 8px; margin: 2px 0 4px; border-radius: var(--r-card); background: var(--hover); }
  .w-list { max-height: 330px; overflow-y: auto; margin: 0 -6px; padding: 0 6px; }
  .w-task { display: flex; align-items: center; gap: 10px; padding: 6px 4px; border-radius: var(--r-input); transition: background .15s; }
  .w-task:hover { background: var(--hover); }
  .w-task .t { font-size: 13.5px; transition: color .3s; }
  .w-task.done .t { color: var(--faint); text-decoration: line-through; }
  .w-quote { margin-top: 8px; }
  .w-quote :global(.quote) { padding: 8px 12px 6px 14px; background: transparent; border: 0; }
  .w-quote :global(.quote p) { font-size: 12px; color: var(--sub); }
  .w-quote :global(.src) { font-size: 11px; }
  .w-foot { display: flex; justify-content: space-between; align-items: center; margin-top: auto; padding-top: 6px; }
  :global(:root[data-skin='mac']) .w-frame { --r-widget: 22px; }
  :global(:root[data-skin='fluent']) .w-frame { --r-widget: 8px; }
  :global(:root[data-skin='material']) .w-frame { --r-widget: 32px; }
  :global(:root[data-skin='carbon']) .w-frame { --r-widget: 0px; }
  :global(:root[data-skin='dot']) .w-frame { --r-widget: 34px; --text: #f2f2f0; --sub: #8a8a8f; --faint: #66666a; --accent: #f2f2f0; --on-accent: #0b0b0b; --line: #303030; --hover: #1b1b1b; --track: #262626; --check-line: #f2f2f0; --signal: #ff3b45; --btn2: transparent; --bg: #0b0b0b; --frame-border: #303030; --card: transparent; --card-line: #303030; color: var(--text); }
</style>
