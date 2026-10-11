<script lang="ts">
  import { app, go, isDoneOn, journalOf, patchJournal, skippedFor, tasksFor, unskipTask } from '../lib/app.svelte';
  import { checkTask } from '../lib/feedback';
  import { startPomo } from '../lib/engine.svelte';
  import { addDays, key, LONG, monthDay, parse, monday, SHORT } from '../lib/dates';
  import { computeStreak } from '../lib/streak';
  import { examInfo, examText } from '../lib/exam';
  import { leaveOn, leavesInWeek, LEAVE_WARN_DAYS } from '../lib/leave';
  import { frequency, groupBy } from '../lib/model';
  import { actualMinutes } from '../lib/model';
  import type { Task } from '../lib/types';
  import Check from '../components/Check.svelte';
  import Progress from '../components/Progress.svelte';
  import QuoteCard from '../components/QuoteCard.svelte';
  import Roll from '../components/Roll.svelte';
  import Icon from '../components/Icon.svelte';
  import SkipModal from '../components/SkipModal.svelte';

  let { onedit, onleave, onexam }: { onedit: (t: Task) => void; onleave: () => void; onexam: () => void } = $props();

  let skipping = $state<Task | null>(null);
  const day = $derived(app.selected);
  const isToday = $derived(day === app.today);
  const tasks = $derived(tasksFor(day));
  const done = $derived(tasks.filter((t) => isDoneOn(t, day)).length);
  const groups = $derived(groupBy(tasks, (t) => t.Group));
  const streak = $derived(computeStreak(app.state, app.today));
  const exam = $derived(examInfo(app.state.Exam, app.today));
  const leave = $derived(leaveOn(app.state, day));
  const weekLeaves = $derived(leavesInWeek(app.state, day));
  const week = $derived.by(() => {
    const m = monday(parse(day));
    return Array.from({ length: 7 }, (_, i) => key(addDays(m, i)));
  });
  const goal = $derived(journalOf(day).goal ?? '');
  const review = $derived(journalOf(day));
  const hour = $derived(new Date(app.now).getHours());

  function ratio(k: string): number {
    const ts = tasksFor(k);
    return ts.length ? ts.filter((t) => isDoneOn(t, k)).length / ts.length : 0;
  }
  const title = $derived(isToday ? '今天，慢慢做好。' : `${monthDay(parse(day))}的安排`);
  const sub = $derived(tasks.length > 0 && done === tasks.length ? '今天的清单已经完成。把剩下的时间留给自己。' : '不必一下完成所有事。从眼前这一件开始。');
  const dateLine = $derived(`${monthDay(parse(day))} ${LONG[parse(day).getDay()]}`);
  const remaining = $derived(tasks.length - done);
  const skipped = $derived(skippedFor(day));
  const tolerance = $derived((app.state.Settings.Tolerance ?? 80) / 100);
  const met = $derived(tasks.length > 0 && done / tasks.length >= tolerance);
</script>

<div class="inner">
  <div class="hero" style="margin-top:6px">
    <div>
      <div class="kicker">{dateLine}{isToday ? ' · 今天' : ''}</div>
      <h1 class="title">{title}</h1>
      <div class="sub">{sub}</div>
      <div class="stats">
        {#if streak.current > 0}<span class="pill" title="连续完成天数"><Icon name="flame" size={14} /><b><Roll value={streak.current} /></b> 天连续</span>{/if}
        {#if streak.freezeTotal > 0}<span class="pill" title="本月还可冻结（漏打卡不断连）的次数">❄ 冻结卡 <b>{streak.freezeLeft}</b>/{streak.freezeTotal}</span>{/if}
        {#if app.state.Exam && exam}
          <button class="pill" onclick={onexam} title="点击记录备考进度">🎯 <b>{examText(app.state.Exam, exam)}</b>{#if exam.perDay}{' · 建议每天 '}{exam.perDay}{app.state.Exam.Label ?? ''}{/if}</button>
        {/if}
      </div>
    </div>
    <div class="row" style="flex-direction:column;align-items:center;gap:6px">
      <Progress {done} total={tasks.length} size={104} />
      <div class="sub num"><Roll value={done} /> / {tasks.length}</div>
    </div>
  </div>

  <div class="daystrip">
    {#each week as k}
      <button class:on={k === day} class:today={k === app.today} onclick={() => (app.selected = k)}>
        <span>{SHORT[parse(k).getDay()]}</span>
        <b>{parse(k).getDate()}</b>
        <span class="prog"><i style="width:{ratio(k) * 100}%"></i></span>
      </button>
    {/each}
  </div>

  {#if isToday}
    <div style="margin:14px 0"><QuoteCard /></div>
    <div class="row" style="gap:8px;margin-bottom:2px">
      <span title="每日承诺">🎯</span>
      <input type="text" placeholder="今天最想做成的一件事…" maxlength="60" value={goal} onchange={(e) => patchJournal(day, { goal: e.currentTarget.value.trim() })} />
    </div>
  {/if}

  {#if leave}
    <div class="card" style="margin-top:14px;display:flex;gap:10px;align-items:center">🌿 <span><b>这一天已请假</b><span class="sub"> · {leave.reason}。不计入放弃库，也不打断连续天数。</span></span></div>
  {/if}
  {#if weekLeaves >= LEAVE_WARN_DAYS}
    <div class="warn" style="margin-top:12px">⚠️ 这一周已请假 {weekLeaves} 天。休息很重要，也请看看是不是该调整计划了。</div>
  {/if}

  {#if tasks.length === 0}
    <div class="empty">这一天留白。{'\n'}添加一件小事，或安心休息。</div>
  {:else}
    {#each groups as [g, items]}
      <div class="group-h"><span>{g}</span><span class="num">{items.filter((t) => isDoneOn(t, day)).length}/{items.length}</span></div>
      <div class="tasks">
        {#each items as t (t.Id)}
          {@const on = isDoneOn(t, day)}
          <div class="task" class:done={on}>
            <Check {on} label={t.Title} onchange={(v, el) => checkTask(t, day, v, el)} />
            <div style="min-width:0">
              <div class="t">{t.Title}</div>
              {#if t.Note}<div class="n">{t.Note}</div>{/if}
            </div>
            <div class="meta">
              {#if t.Est}<span class="chip" title="预计 / 实际专注">{actualMinutes(app.state, t.Id, day)}/{t.Est} 分</span>{/if}
              {#if t.ReviewSrc}<span class="chip accent">复习</span>{:else if t.Review}<span class="chip">复习</span>{/if}
              {#if isToday}
                <button class="btn ghost icon go" title="为这个任务开始番茄钟" onclick={() => { void startPomo(t.Id); go('focus'); }}>🍅</button>
              {/if}
              <span>{frequency(t)}</span>
              {#if day >= app.today && !on}
                <button class="btn ghost sm go" title="今天有计划地不做这一项（计划性请假，不计入放弃库）" onclick={() => (skipping = t)}>请假</button>
              {/if}
              <button class="btn ghost icon" title="编辑" onclick={() => onedit(t)}><Icon name="more" size={14} /></button>
            </div>
          </div>
        {/each}
      </div>
    {/each}
  {/if}

  {#if skipped.length}
    <div class="group-h"><span>已请假的任务</span><span class="num">{skipped.length}</span></div>
    <div class="tasks">
      {#each skipped as x (x.task.Id)}
        <div class="task" style="grid-template-columns:auto 1fr auto;opacity:.85">
          <span>🌿</span>
          <div style="min-width:0"><div class="t" style="font-size:14px">{x.task.Title}</div><div class="n">计划性请假 · {x.reason}</div></div>
          <div class="meta">
            <button class="btn sm ghost" onclick={() => (skipping = x.task)}>修改</button>
            {#if day >= app.today}<button class="btn sm ghost" onclick={() => unskipTask(x.task.Id, day)}>恢复</button>{/if}
          </div>
        </div>
      {/each}
    </div>
  {/if}

  <div class="row spread wrap" style="margin-top:22px">
    <div class="row" style="gap:8px">
      {#if isToday && review.result}
        <span class="chip accent">今晚复盘：{review.result === 'done' ? '做到了' : review.result === 'partial' ? '做到一部分' : '没做到'}{review.mood ? ' · ' + ['', '😞', '😕', '😐', '🙂', '😄'][review.mood] : ''}</span>
      {:else if isToday && hour >= 18}
        <button class="btn sm" onclick={() => go('care')}>📝 写下今天的复盘</button>
      {/if}
    </div>
    <div class="row" style="gap:8px">
      {#if isToday && remaining > 0 && !leave}
        <span class="faint" style="font-size:12px">{met ? `已完成 ${Math.round(tolerance * 100)}% 以上，剩下的不会记入放弃库` : `完成 ${Math.round(tolerance * 100)}% 以上就不会记入放弃库`}</span>
      {/if}
      {#if day >= app.today}<button class="btn sm" onclick={onleave}>{leave ? '修改请假' : '请假'}</button>{/if}
    </div>
  </div>
</div>

{#if skipping}<SkipModal task={skipping} date={day} onclose={() => (skipping = null)} />{/if}
