<script lang="ts">
  import { onMount } from 'svelte';
  import { app, go, isDoneOn, tasksFor } from './lib/app.svelte';
  import * as backend from './lib/backend';
  import { startEngine, syncShortcuts } from './lib/engine.svelte';
  import { addDays, monthDay, parse, key, today as todayOf } from './lib/dates';
  import { levelOf, xpTotal } from './lib/xp';
  import type { Page, Task } from './lib/types';
  import Editor from './components/Editor.svelte';
  import ExamModal from './components/ExamModal.svelte';
  import FocusScreen from './components/FocusScreen.svelte';
  import FxLayer from './components/FxLayer.svelte';
  import LeaveModal from './components/LeaveModal.svelte';
  import Icon from './components/Icon.svelte';
  import Toasts from './components/Toasts.svelte';
  import Care from './pages/Care.svelte';
  import Favs from './pages/Favs.svelte';
  import Focus from './pages/Focus.svelte';
  import Giveup from './pages/Giveup.svelte';
  import Plan from './pages/Plan.svelte';
  import Settings from './pages/Settings.svelte';
  import Stats from './pages/Stats.svelte';
  import Today from './pages/Today.svelte';

  const NAV: [Page, string, string, string][] = [
    ['today', '今天', 'today', '#007AFF'],
    ['plan', '每周计划', 'week', '#FF453A'],
    ['focus', '专注', 'focus', '#FF9F0A'],
    ['stats', '统计', 'stats', '#30D158'],
    ['giveup', '放弃与请假', 'giveup', '#8E8E93'],
    ['care', '照顾自己', 'care', '#FF375F'],
    ['favs', '收藏', 'favs', '#FFCC00'],
    ['settings', '偏好设置', 'settings', '#8E8E93']
  ];

  let editing = $state<Task | 'new' | null>(null);
  let examOpen = $state(false);
  const left = $derived(tasksFor(app.today).filter((t) => !isDoneOn(t, app.today)).length);
  const lvl = $derived(levelOf(xpTotal(app.state)));
  const minimal = $derived(app.minimalFocus && app.page === 'focus');

  onMount(() => {
    void startEngine();
  });
  $effect(() => {
    void app.state.Settings.KeyFocus;
    void app.state.Settings.KeyOpen;
    void syncShortcuts();
  });

  $effect(() => {
    document.documentElement.dataset.full = app.fullscreen ? '1' : '';
    void backend.setFullscreen(app.fullscreen);
  });
  function esc(e: KeyboardEvent) {
    if (e.key !== 'Escape') return;
    if (app.fullscreen) app.fullscreen = false;
    else if (app.minimalFocus) app.minimalFocus = false;
  }

  function shiftWeek(n: number) {
    app.selected = key(addDays(parse(app.selected), n * 7));
  }
  const drag = (e: MouseEvent) => {
    if (e.button === 0 && e.detail === 1) void backend.winAction('drag');
  };
</script>

<svelte:window onkeydown={esc} />
<div class="frame" class:minimal class:gray={app.gray} class:full={app.fullscreen}>
  <div class="controls" role="toolbar" aria-label="窗口">
    <button class="m" title="最小化" aria-label="最小化" onclick={() => backend.winAction('minimize')}><Icon name="min" size={14} /></button>
    <button class="x" title="关闭（组件继续运行）" aria-label="关闭" onclick={() => backend.winAction('close')}><Icon name="close" size={14} /></button>
  </div>

  <aside class="side">
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div class="drag" onmousedown={drag}></div>
    <div class="brand">刻序 · KORA</div>
    <nav class="nav">
      {#each NAV as [id, label, icon, tile]}
        <button class="nav-item" class:on={app.page === id} style="--tile:{tile}" onclick={() => go(id)}>
          <span class="ico"><Icon name={icon} size={13} stroke={2.2} /></span>
          <span>{label}</span>
          {#if id === 'today' && left > 0}<span class="badge">{left}</span>{/if}
        </button>
      {/each}
    </nav>
    <div class="side-foot">
      <div class="lvl"><div class="row spread"><span>Lv.{lvl.level}</span><span class="num">{lvl.into}/{lvl.span} XP</span></div><div class="bar"><i style="width:{lvl.pct * 100}%"></i></div></div>
      <div class="lock"><Icon name="lock" size={12} /> 只在本机，安心记录</div>
    </div>
  </aside>

  <section class="main">
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <header class="head" role="presentation" onmousedown={drag}>
      {#if app.page === 'today'}
        <div class="row seg" onmousedown={(e) => e.stopPropagation()} role="group">
          <button aria-label="上一周" onclick={() => shiftWeek(-1)}><Icon name="left" size={12} stroke={2.4} /></button>
          <button class:on={app.selected === app.today} onclick={() => (app.selected = todayOf(new Date(app.now)))}>今天</button>
          <button aria-label="下一周" onclick={() => shiftWeek(1)}><Icon name="right" size={12} stroke={2.4} /></button>
        </div>
        <span class="sub" style="font-size:12px">{monthDay(parse(app.selected))}</span>
      {/if}
      <div class="grow"></div>
      {#if app.page === 'today' || app.page === 'plan'}
        <button class="btn primary sm" onmousedown={(e) => e.stopPropagation()} onclick={() => (editing = 'new')}><Icon name="plus" size={13} stroke={2.4} /> 添加任务</button>
      {/if}
    </header>

    <div class="page">
      {#if app.page === 'today'}
        <Today onedit={(t) => (editing = t)} onleave={() => (app.leaveOpen = true)} onexam={() => (examOpen = true)} />
      {:else if app.page === 'plan'}
        <Plan onedit={(t) => (editing = t)} onadd={() => (editing = 'new')} />
      {:else if app.page === 'focus'}
        <Focus />
      {:else if app.page === 'stats'}
        <Stats />
      {:else if app.page === 'giveup'}
        <Giveup />
      {:else if app.page === 'care'}
        <Care />
      {:else if app.page === 'favs'}
        <Favs />
      {:else}
        <Settings />
      {/if}
    </div>
  </section>

  {#if editing}<Editor task={editing === 'new' ? null : editing} defaultDate={app.selected} onclose={() => (editing = null)} />{/if}
  {#if app.leaveOpen}<LeaveModal onclose={() => (app.leaveOpen = false)} />{/if}
  {#if examOpen}<ExamModal onclose={() => (examOpen = false)} />{/if}
  {#if app.fullscreen}<FocusScreen />{/if}
  <Toasts />
</div>
<FxLayer />

<style>
  .frame :global(.head .seg) { padding: 1px; }
  .frame.gray { filter: grayscale(1) brightness(0.92); }
  .frame { transition: filter 0.6s ease; }
</style>
