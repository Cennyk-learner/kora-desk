<script lang="ts">
  import { app, toast } from '../lib/app.svelte';
  import * as backend from '../lib/backend';
  import { ACHIEVEMENTS } from '../lib/achievements';
  import { addKey, key, monday, parse } from '../lib/dates';
  import { actualMinutes, doneIndex } from '../lib/model';
  import { renderWeeklyCard, weekSummary } from '../lib/report';
  import { computeStreak, dayInfo, heatmap, rateBetween } from '../lib/streak';
  import { levelOf, xpTotal } from '../lib/xp';
  import Icon from '../components/Icon.svelte';

  const t = $derived(app.today);
  const streak = $derived(computeStreak(app.state, t));
  const xp = $derived(xpTotal(app.state));
  const lvl = $derived(levelOf(xp));
  const week = $derived(weekSummary(app.state, t));
  const heat = $derived(heatmap(app.state, t));
  const weeks = $derived.by(() => {
    const m = key(monday(parse(t)));
    return Array.from({ length: 8 }, (_, i) => {
      const from = addKey(m, -7 * (7 - i));
      const to = i === 7 ? t : addKey(from, 6);
      const r = rateBetween(app.state, from, to);
      return { from, rate: r.due ? r.done / r.due : 0, due: r.due };
    });
  });
  const months = $derived.by(() => {
    const out: { m: string; rate: number }[] = [];
    const d = parse(t);
    for (let i = 5; i >= 0; i--) {
      const first = key(new Date(d.getFullYear(), d.getMonth() - i, 1));
      const last = i === 0 ? t : key(new Date(d.getFullYear(), d.getMonth() - i + 1, 0));
      const r = rateBetween(app.state, first, last);
      out.push({ m: first.slice(5, 7) + '月', rate: r.due ? r.done / r.due : 0 });
    }
    return out;
  });
  const groups = $derived.by(() => {
    const from = addKey(t, -29);
    const byId = new Map(app.state.Tasks.map((x) => [x.Id, x.Group]));
    const m = new Map<string, number>();
    for (const [date, ids] of doneIndex(app.state)) {
      if (date < from || date > t) continue;
      for (const id of ids) m.set(byId.get(id) ?? '其他', (m.get(byId.get(id) ?? '其他') ?? 0) + 1);
    }
    const total = [...m.values()].reduce((a, b) => a + b, 0) || 1;
    return [...m.entries()].sort((a, b) => b[1] - a[1]).map(([g, n]) => ({ g, n, pct: n / total }));
  });
  const focus14 = $derived(Array.from({ length: 14 }, (_, i) => {
    const d = addKey(t, i - 13);
    const ses = app.state.Sessions.filter((s) => s.date === d && s.kind === 'focus');
    return { d, min: ses.reduce((a, s) => a + s.minutes, 0), n: ses.length };
  }));
  const maxFocus = $derived(Math.max(30, ...focus14.map((x) => x.min)));
  const totalPomos = $derived(app.state.Sessions.filter((s) => s.kind === 'focus').length);
  const estRows = $derived(app.state.Tasks.filter((x) => x.Est && !x.ReviewSrc).map((x) => {
    const days = new Set(app.state.Sessions.filter((s) => s.taskId === x.Id).map((s) => s.date));
    const avg = days.size ? [...days].reduce((a, d) => a + actualMinutes(app.state, x.Id, d), 0) / days.size : 0;
    return { title: x.Title, est: x.Est!, avg: Math.round(avg), days: days.size };
  }));
  const groups2 = $derived([...ACHIEVEMENTS.reduce((m, a) => m.set(a.group, [...(m.get(a.group) ?? []), a]), new Map<string, typeof ACHIEVEMENTS>())]);
  const weekDone = $derived(dayInfo(app.state, t));

  const shade = (r: number) => (r < 0 ? 'var(--track)' : `color-mix(in srgb, var(--accent) ${Math.round(18 + r * 82)}%, var(--track))`);

  async function saveCard() {
    try {
      const blob = await renderWeeklyCard(app.state, t);
      const name = `kora-week-${t}.png`;
      if (backend.isTauri) {
        const path = await backend.saveDialog(name, 'png', 'PNG 图片');
        if (!path) return;
        await backend.call('write_binary_file', { path, bytes: Array.from(new Uint8Array(await blob.arrayBuffer())) });
        toast('周报图片已保存。', '🖼️');
      } else {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = name;
        a.click();
      }
    } catch (e) {
      toast(`生成失败：${e}`, '⚠️');
    }
  }
</script>

<div class="inner">
  <div class="kicker">YOUR PROGRESS</div>
  <h1 class="title">看见自己的积累。</h1>
  <div class="sub">数据只在这台电脑上。慢一点也没关系，曲线总体向上就好。</div>

  <div class="grid2" style="margin-top:16px">
    <div class="card">
      <div class="row spread"><span class="sub">等级</span><span class="chip accent">{xp} XP</span></div>
      <div class="big-num" style="font-size:34px;margin:4px 0 8px">Lv.{lvl.level}</div>
      <div class="bar"><i style="width:{lvl.pct * 100}%"></i></div>
      <div class="faint" style="font-size:12px;margin-top:6px">距下一级还差 {lvl.span - lvl.into} XP · 放弃不会扣分</div>
    </div>
    <div class="card">
      <div class="row spread"><span class="sub">连续打卡</span><Icon name="flame" size={16} /></div>
      <div class="big-num" style="font-size:34px;margin:4px 0 4px">{streak.current}<span class="sub" style="font-size:14px;font-weight:400"> 天</span></div>
      <div class="faint" style="font-size:12px">最长 {streak.best} 天 · 本月冻结卡 {streak.freezeLeft}/{streak.freezeTotal}</div>
    </div>
    <div class="card">
      <div class="sub">本周完成率</div>
      <div class="big-num" style="font-size:34px;margin:4px 0 4px">{Math.round(week.rate * 100)}<span class="sub" style="font-size:14px;font-weight:400">%</span></div>
      <div class="faint" style="font-size:12px">{week.done}/{week.due} 项 · 今天 {weekDone.done}/{weekDone.due}</div>
    </div>
    <div class="card">
      <div class="sub">本周专注</div>
      <div class="big-num" style="font-size:34px;margin:4px 0 4px">{Math.floor(week.focusMin / 60)}<span class="sub" style="font-size:14px;font-weight:400"> 时 </span>{week.focusMin % 60}<span class="sub" style="font-size:14px;font-weight:400"> 分</span></div>
      <div class="faint" style="font-size:12px">{week.pomos} 个番茄 · 累计 {totalPomos} 个</div>
    </div>
  </div>

  <h2 class="sec">全年热力图 <small>颜色越深，当天完成得越多</small></h2>
  <div class="card">
    <div class="heat">
      {#each heat as col}{#each col as c}<i class:f={c.future} style="background:{c.future ? 'transparent' : shade(c.ratio)}" title="{c.date}{c.ratio >= 0 ? ' · ' + Math.round(c.ratio * 100) + '%' : ' · 无安排'}"></i>{/each}{/each}
    </div>
  </div>

  <div class="grid2">
    <div>
      <h2 class="sec">近 8 周完成率</h2>
      <div class="card">
        <div class="bars">{#each weeks as w}<div title="{w.from} 起：{Math.round(w.rate * 100)}%"><i style="height:{Math.max(2, w.rate * 100)}%"></i></div>{/each}</div>
        <div class="faint" style="font-size:11px;margin-top:6px">每根柱子代表一周（最右为本周）</div>
      </div>
    </div>
    <div>
      <h2 class="sec">近 6 个月</h2>
      <div class="card">
        <div class="bars">{#each months as m}<div title="{m.m}：{Math.round(m.rate * 100)}%"><i style="height:{Math.max(2, m.rate * 100)}%"></i></div>{/each}</div>
        <div class="row" style="gap:4px;margin-top:6px">{#each months as m}<span class="faint" style="flex:1;text-align:center;font-size:11px">{m.m}</span>{/each}</div>
      </div>
    </div>
  </div>

  <div class="grid2">
    <div>
      <h2 class="sec">近 30 天分组占比</h2>
      <div class="card stack">
        {#if groups.length === 0}<div class="sub">还没有完成记录。</div>{/if}
        {#each groups as g}
          <div class="row" style="gap:10px"><span style="width:54px">{g.g}</span><div class="bar" style="flex:1"><i style="width:{g.pct * 100}%"></i></div><span class="num sub" style="width:40px;text-align:right">{Math.round(g.pct * 100)}%</span></div>
        {/each}
      </div>
    </div>
    <div>
      <h2 class="sec">近 14 天专注分钟</h2>
      <div class="card">
        <div class="bars">{#each focus14 as f}<div title="{f.d}：{f.min} 分钟 / {f.n} 个番茄"><i style="height:{Math.max(2, (f.min / maxFocus) * 100)}%"></i></div>{/each}</div>
        <div class="faint" style="font-size:11px;margin-top:6px">最高 {maxFocus} 分钟</div>
      </div>
    </div>
  </div>

  {#if estRows.length}
    <h2 class="sec">预估 vs 实际 <small>用番茄钟记录的专注时长，帮你校准计划</small></h2>
    <div class="tasks">
      {#each estRows as r}
        <div class="task" style="grid-template-columns:1fr auto auto">
          <div class="t" style="font-size:14px">{r.title}</div>
          <span class="chip">预计 {r.est} 分</span>
          <span class="chip {r.days && Math.abs(r.avg - r.est) > r.est * 0.3 ? 'accent' : ''}">{r.days ? `实际均值 ${r.avg} 分` : '暂无记录'}</span>
        </div>
      {/each}
    </div>
  {/if}

  <h2 class="sec">成就徽章 <small>{Object.keys(app.state.Ach).length}/{ACHIEVEMENTS.length}</small></h2>
  {#each groups2 as [g, list]}
    <div class="label">{g} · {list.filter((a) => app.state.Ach[a.id]).length}/{list.length}</div>
    <div class="badge-grid">
      {#each list as a}
        {@const got = app.state.Ach[a.id]}
        <div class="ach" class:locked={!got} title={got ? `解锁于 ${got}` : '未解锁'}>
          <span class="i">{a.icon}</span><b style="font-size:13px">{a.name}</b><small>{a.desc}</small>
        </div>
      {/each}
    </div>
  {/each}

  <h2 class="sec">每周小结</h2>
  <div class="card row spread wrap">
    <span class="sub">生成一张本周总结图片（完成率、专注时长、放弃库），保存后可以分享给自己或朋友。</span>
    <button class="btn primary" onclick={saveCard}><Icon name="image" size={14} /> 保存周报图片</button>
  </div>
</div>
