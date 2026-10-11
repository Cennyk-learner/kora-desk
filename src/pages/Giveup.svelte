<script lang="ts">
  import { app, dropLeave, setGiveupReason, unskipTask } from '../lib/app.svelte';
  import { giveupStats, projectionText } from '../lib/giveup';
  import { LEAVE_WARN_DAYS, leavesInWeek, warnedWeeks } from '../lib/leave';
  import { monthDay, parse } from '../lib/dates';

  const st = $derived(giveupStats(app.state));
  const maxMonth = $derived(Math.max(1, ...st.byMonth.map((m) => m[1])));
  const top = $derived(st.byReason[0]);
  const total = $derived(Math.max(1, st.byReason.reduce((a, r) => a + r[1], 0)));
  const REASONS = ['累', '没状态', '太忙', '拖延'];
  const recent = $derived([...app.state.Giveups].sort((a, b) => b.ts - a.ts).slice(0, 12));
  const leaves = $derived([...app.state.Leaves].sort((a, b) => b.date.localeCompare(a.date)));
  const weekLeaves = $derived(leavesInWeek(app.state, app.today));
  const warned = $derived(warnedWeeks(app.state));
  const leaveMonths = $derived.by(() => {
    const m = new Map<string, number>();
    for (const l of app.state.Leaves) m.set(l.date.slice(0, 7), (m.get(l.date.slice(0, 7)) ?? 0) + 1);
    return [...m.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  });
  const leaveReasons = $derived.by(() => {
    const m = new Map<string, number>();
    for (const l of app.state.Leaves) m.set(l.reason, (m.get(l.reason) ?? 0) + 1);
    return [...m.entries()].sort((a, b) => b[1] - a[1]).slice(0, 4);
  });

  const skips = $derived([...app.state.Skips].sort((a, b) => b.date.localeCompare(a.date)));
  const skipByTask = $derived.by(() => {
    const m = new Map<string, number>();
    for (const x of app.state.Skips) m.set(x.title, (m.get(x.title) ?? 0) + 1);
    return [...m.entries()].sort((a, b) => b[1] - a[1]).slice(0, 5);
  });
  const skipByReason = $derived.by(() => {
    const m = new Map<string, number>();
    for (const x of app.state.Skips) m.set(x.reason, (m.get(x.reason) ?? 0) + 1);
    return [...m.entries()].sort((a, b) => b[1] - a[1]).slice(0, 5);
  });
  const skipWeek = $derived.by(() => {
    const from = app.today;
    const n = app.state.Skips.filter((x) => x.date >= from.slice(0, 8) + '01').length;
    return n;
  });

  function nextReason(cur?: string): string {
    const i = cur ? REASONS.indexOf(cur) : -1;
    return i + 1 >= REASONS.length ? '' : REASONS[i + 1];
  }
</script>

<div class="inner">
  <div class="kicker">THE GIVE-UP BANK</div>
  <h1 class="title">放弃与请假</h1>
  <div class="sub">如果一天结束时还有任务没完成，第二天打开程序时会自动记一次放弃：每天最多一次，每次 0.1 天。请假的日子不计；当天完成度达到「达标线」（默认 80%）也不会记。任务级的计划性请假单独记录在下方。</div>

  {#if weekLeaves >= LEAVE_WARN_DAYS}
    <div class="warn" style="margin-top:14px">⚠️ 本周已请假 {weekLeaves} 天（≥ {LEAVE_WARN_DAYS} 天预警）。休息是应该的，也请留意是不是需要调整计划。</div>
  {/if}

  <h2 class="sec">放弃库</h2>
  <div class="card">
    <div class="row spread wrap">
      <div>
        <div class="big-num" style="font-size:44px">{st.totalDays.toFixed(1)}<span class="sub" style="font-size:16px;font-weight:400"> 天</span></div>
        <div class="sub">累计 {st.count} 天有未完成 × 0.1 天</div>
      </div>
      <div style="max-width:320px;line-height:1.7" class="sub">{st.count ? projectionText(st) : '还没有记录。这很好。'}</div>
    </div>
    <div class="kv" style="margin-top:14px">
      <span>有记录的活跃天数</span><span class="num" style="color:var(--text)">{st.activeDays}</span>
      <span>日均放弃</span><span class="num" style="color:var(--text)">{st.perActiveDay.toFixed(2)} 次</span>
      <span>一年推算（日均 × 0.1 × 365）</span><span class="num" style="color:var(--text)">{st.yearDays.toFixed(1)} 天</span>
    </div>
    {#if st.smallSample && st.count}<div class="faint" style="font-size:12px;margin-top:8px">活跃天数不足 7 天，样本较少，推算仅供参考。</div>{/if}
  </div>

  {#if st.byMonth.length}
    <div class="label">按月</div>
    <div class="card">
      <div class="bars" style="height:110px">
        {#each st.byMonth as [m, n]}<div title="{m}：{n} 项"><i style="height:{(n / maxMonth) * 100}%"></i></div>{/each}
      </div>
      <div class="row" style="gap:4px;margin-top:6px">
        {#each st.byMonth as [m]}<span class="faint num" style="flex:1;text-align:center;font-size:11px">{m.slice(5)}月</span>{/each}
      </div>
    </div>
  {/if}

  <div class="grid2">
    {#if st.byTask.length}
      <div>
        <div class="label">最常没完成的任务</div>
        <div class="card stack">
          {#each st.byTask as [name, n]}<div class="row spread"><span>{name}</span><span class="chip">{n} 次</span></div>{/each}
        </div>
      </div>
    {/if}
    {#if st.byReason.length}
      <div>
        <div class="label">原因{top ? ` · 你最常因「${top[0]}」放弃` : ''}</div>
        <div class="card stack">
          {#each st.byReason as [r, n]}
            <div class="row" style="gap:12px"><span style="width:56px">{r}</span><div class="bar" style="flex:1"><i style="width:{(n / total) * 100}%"></i></div><span class="num sub" style="width:30px;text-align:right">{n}</span></div>
          {/each}
        </div>
      </div>
    {/if}
  </div>

  {#if recent.length}
    <div class="label">最近记录 <span class="faint" style="font-weight:400">点右侧标签可以记下原因</span></div>
    <div class="tasks">
      {#each recent as g (g.date)}
        <div class="task" style="grid-template-columns:1fr auto auto">
          <div style="min-width:0"><div class="t" style="font-size:14px">{g.date}</div><div class="n">{(g.titles ?? []).join('、') || '未完成任务'}</div></div>
          <button class="chip" onclick={() => setGiveupReason(g.date, nextReason(g.reason))}>{g.reason ?? '标注原因'}</button>
        </div>
      {/each}
    </div>
  {/if}

  <h2 class="sec">任务请假 <small>计划性不做 · 不计入放弃库 · 共 {skips.length} 次，本月 {skipWeek} 次</small></h2>
  {#if skips.length === 0}
    <div class="sub">在「今天」页任务右侧点「请假」，就会在这里留下记录。适合“今天有计划地不做这一项”，和因为懒而没完成区分开。</div>
  {:else}
    <div class="grid2">
      <div class="card stack">
        <div class="label" style="margin:0">最常请假的任务</div>
        {#each skipByTask as [name, n]}<div class="row spread"><span>{name}</span><span class="chip">{n} 次</span></div>{/each}
      </div>
      <div class="card stack">
        <div class="label" style="margin:0">常见理由</div>
        {#each skipByReason as [r, n]}<div class="row spread"><span>{r}</span><span class="chip">{n} 次</span></div>{/each}
      </div>
    </div>
    <div class="tasks" style="margin-top:12px">
      {#each skips.slice(0, 20) as x (x.date + x.taskId)}
        <div class="task" style="grid-template-columns:auto 1fr auto">
          <span>🌿</span>
          <div><div class="t" style="font-size:14px">{x.title}</div><div class="n">{x.date} · {x.reason}</div></div>
          {#if x.date >= app.today}<button class="btn sm ghost" onclick={() => unskipTask(x.taskId, x.date)}>恢复</button>{/if}
        </div>
      {/each}
    </div>
  {/if}

  <h2 class="sec">请假记录 <small>共 {leaves.length} 天</small></h2>
  {#if warned.length}
    <div class="warn" style="margin-bottom:10px">
      ⚠️ 预警周：{#each warned as w, i}{i ? '、' : ''}{monthDay(parse(w.week))}那一周（{w.days} 天）{/each}
    </div>
  {/if}
  {#if leaves.length === 0}
    <div class="sub">还没有请假。需要休息时，在「今天」页点「请假」并写下理由。</div>
  {:else}
    <div class="grid2">
      <div class="card stack">
        <div class="label" style="margin:0">按月天数</div>
        {#each leaveMonths as [m, n]}<div class="row spread"><span>{m}</span><span class="chip">{n} 天</span></div>{/each}
      </div>
      <div class="card stack">
        <div class="label" style="margin:0">常见理由</div>
        {#each leaveReasons as [r, n]}<div class="row spread"><span>{r}</span><span class="chip">{n} 天</span></div>{/each}
      </div>
    </div>
    <div class="tasks" style="margin-top:12px">
      {#each leaves.slice(0, 20) as l (l.date)}
        <div class="task" style="grid-template-columns:auto 1fr auto">
          <span>🌿</span>
          <div><div class="t" style="font-size:14px">{l.reason}</div><div class="n">{l.date}</div></div>
          {#if l.date >= app.today}<button class="btn sm ghost" onclick={() => dropLeave(l.date)}>取消</button>{/if}
        </div>
      {/each}
    </div>
  {/if}
</div>
