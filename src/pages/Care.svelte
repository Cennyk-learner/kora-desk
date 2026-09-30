<script lang="ts">
  import { app, journalOf, patchJournal, toast, update } from '../lib/app.svelte';
  import { breathCue, chime } from '../lib/audio';
  import { newId } from '../lib/model';
  import { weekSummary } from '../lib/report';
  import { addKey } from '../lib/dates';
  import Seg from '../components/Seg.svelte';

  const day = $derived(app.today);
  const j = $derived(journalOf(day));
  const care = $derived(app.state.Settings.Care!);
  const water = $derived(j.water ?? 0);
  const week = $derived(weekSummary(app.state, day));
  const FACES = ['', '😞', '😕', '😐', '🙂', '😄'];
  const grateful = $derived([j.grateful?.[0] ?? '', j.grateful?.[1] ?? '', j.grateful?.[2] ?? '']);
  const last14 = $derived(Array.from({ length: 14 }, (_, i) => {
    const d = addKey(day, i - 13);
    return { d, mood: app.state.Journal[d]?.mood ?? 0 };
  }));
  let note = $state('');
  $effect(() => {
    note = j.note ?? '';
  });
  let newReward = $state('');

  function setGrateful(i: number, v: string) {
    const arr = [...grateful];
    arr[i] = v.trim();
    void patchJournal(day, { grateful: arr });
  }
  function setWater(n: number) {
    void patchJournal(day, { water: water === n ? n - 1 : n });
  }
  function addReward() {
    const text = newReward.trim();
    if (!text) return;
    void update((s) => s.Rewards.push({ id: newId(), text: text.slice(0, 40) }));
    newReward = '';
  }
  function claim(id: string) {
    void update((s) => {
      const r = s.Rewards.find((x) => x.id === id);
      if (r) r.claimed = r.claimed ? undefined : day;
    });
    toast('享受它，这是你应得的。', '🎁');
  }

  /* ---- breathing ---- */
  const PATTERNS: Record<string, { name: string; steps: [string, number][] }> = {
    box: { name: '方块呼吸 4-4-4-4', steps: [['吸气', 4], ['屏息', 4], ['呼气', 4], ['屏息', 4]] },
    '478': { name: '4-7-8 放松', steps: [['吸气', 4], ['屏息', 7], ['呼气', 8]] }
  };
  let pattern = $state('box');
  let breathing = $state(false);
  let phase = $state('');
  let count = $state(0);
  let scale = $state(1);
  let rounds = $state(0);
  let timer: ReturnType<typeof setTimeout> | undefined;
  const ROUNDS = 4;

  function runStep(i: number, round: number) {
    const steps = PATTERNS[pattern].steps;
    if (i >= steps.length) {
      round++;
      i = 0;
      rounds = round;
      if (round >= ROUNDS) return finishBreath();
    }
    const [name, secs] = steps[i];
    phase = name;
    count = secs;
    scale = name === '吸气' ? 1.35 : name === '呼气' ? 0.85 : scale;
    if (name === '吸气' || name === '呼气') breathCue(name === '吸气');
    let left = secs;
    const tickDown = () => {
      left--;
      count = Math.max(left, 0);
      if (left <= 0) runStep(i + 1, round);
      else timer = setTimeout(tickDown, 1000);
    };
    timer = setTimeout(tickDown, 1000);
  }
  function startBreath() {
    breathing = true;
    rounds = 0;
    scale = 0.85;
    runStep(0, 0);
  }
  function stopBreath() {
    clearTimeout(timer);
    breathing = false;
    phase = '';
    scale = 1;
  }
  function finishBreath() {
    stopBreath();
    chime();
    void patchJournal(day, { breaths: (journalOf(day).breaths ?? 0) + 1 });
    toast('做得很好。肩膀放松了吗？', '🌬️');
  }
  $effect(() => () => clearTimeout(timer));
</script>

<div class="inner">
  <div class="kicker">TAKE CARE</div>
  <h1 class="title">照顾好自己。</h1>
  <div class="sub">学习是长跑：睡好、喝水、喘口气、记下小确幸，比多做一小时更有用。</div>

  <h2 class="sec">今晚复盘 <small>做到了什么？</small></h2>
  <div class="card stack">
    <div class="row wrap" style="gap:8px">
      {#each [['done', '✅ 做到了'], ['partial', '🌗 做到一部分'], ['none', '🌧 没做到']] as [v, l]}
        <button class="tag" class:on={j.result === v} onclick={() => patchJournal(day, { result: j.result === v ? undefined : (v as 'done') })}>{l}</button>
      {/each}
    </div>
    <div class="row" style="gap:10px"><span class="sub">今天的心情</span>
      <div class="moods">{#each [1, 2, 3, 4, 5] as m}<button class:on={j.mood === m} title={`心情 ${m}`} onclick={() => patchJournal(day, { mood: j.mood === m ? undefined : m })}>{FACES[m]}</button>{/each}</div>
    </div>
    <input type="text" placeholder="一句话反思（今天学到了什么？明天想调整什么？）" maxlength="120" bind:value={note} onchange={() => patchJournal(day, { note: note.trim() })} />
    {#if j.goal}<div class="faint" style="font-size:12px">今天的承诺：{j.goal}</div>{/if}
    <div class="row" style="gap:4px;align-items:center" title="近 14 天心情">
      <span class="faint" style="font-size:12px;margin-right:6px">近 14 天</span>
      {#each last14 as m}<span style="font-size:15px;opacity:{m.mood ? 1 : 0.18}" title={m.d}>{m.mood ? FACES[m.mood] : '·'}</span>{/each}
    </div>
  </div>

  <h2 class="sec">今日小确幸 <small>写下三件让你觉得还不错的小事</small></h2>
  <div class="card stack">
    {#each [0, 1, 2] as i}
      <div class="row" style="gap:10px"><span>{['🌼', '☕', '🌙'][i]}</span><input type="text" maxlength="60" placeholder={['今天遇到的一点温柔', '一个小小的舒服时刻', '值得感谢的人或事'][i]} value={grateful[i]} onchange={(e) => setGrateful(i, e.currentTarget.value)} /></div>
    {/each}
  </div>

  <div class="grid2">
    <div>
      <h2 class="sec">喝水 <small>{water}/{care.WaterGoal} 杯</small></h2>
      <div class="card">
        <div class="cups">{#each Array.from({ length: care.WaterGoal }) as _, i}<button class:on={i < water} aria-label={`第 ${i + 1} 杯`} onclick={() => setWater(i + 1)}><i></i></button>{/each}</div>
        <div class="faint" style="font-size:12px;margin-top:10px">{water >= care.WaterGoal ? '今天喝够了，真棒。' : '点一下杯子记录一杯水。'}</div>
      </div>
    </div>
    <div>
      <h2 class="sec">睡个好觉</h2>
      <div class="card sub" style="line-height:1.8">
        睡前 30 分钟放下屏幕，把明天最重要的一件事写下来。{#if care.Bed}<br />已开启就寝提醒：{care.BedTime}。{:else}<br />可以在「偏好设置」里开启就寝提醒。{/if}
      </div>
    </div>
  </div>

  <h2 class="sec">呼吸练习 <small>累计 {Object.values(app.state.Journal).reduce((a, x) => a + (x.breaths ?? 0), 0)} 次</small></h2>
  <div class="card" style="text-align:center">
    <div class="row" style="justify-content:center"><Seg items={Object.entries(PATTERNS).map(([k, v]) => [k, v.name])} value={pattern} onpick={(v) => !breathing && (pattern = v)} /></div>
    <div class="breath" style="transform:scale({scale});--bt:{breathing ? (phase === '屏息' ? 0.4 : count + 1) : 0.4}s">
      <div><div style="font-size:20px;font-weight:600">{breathing ? phase : '准备'}</div><div class="num sub">{breathing ? count : ''}</div></div>
    </div>
    <div class="sub" style="margin-bottom:10px">{breathing ? `第 ${Math.min(rounds + 1, ROUNDS)} / ${ROUNDS} 轮` : `${ROUNDS} 轮约 ${Math.round((PATTERNS[pattern].steps.reduce((a, s) => a + s[1], 0) * ROUNDS) / 60 * 10) / 10} 分钟`}</div>
    {#if breathing}<button class="btn" onclick={stopBreath}>结束</button>{:else}<button class="btn primary" onclick={startBreath}>开始</button>{/if}
  </div>

  <h2 class="sec">奖励清单 <small>本周完成率 {Math.round(week.rate * 100)}%{week.rate >= 0.8 ? ' · 已达成，可以兑换啦' : ''}</small></h2>
  <div class="card stack">
    {#each app.state.Rewards as r (r.id)}
      <div class="row spread">
        <span style="{r.claimed ? 'text-decoration:line-through;opacity:.6' : ''}">🎁 {r.text}</span>
        <div class="row" style="gap:6px">
          {#if r.claimed}<span class="chip">{r.claimed} 已兑换</span>{/if}
          <button class="btn sm" class:primary={week.rate >= 0.8 && !r.claimed} onclick={() => claim(r.id)}>{r.claimed ? '撤销' : '兑换'}</button>
          <button class="btn sm ghost" aria-label="删除" onclick={() => update((s) => (s.Rewards = s.Rewards.filter((x) => x.id !== r.id)))}>✕</button>
        </div>
      </div>
    {/each}
    <div class="row" style="gap:8px"><input type="text" placeholder="给自己一个小奖励，比如：周末看场电影" maxlength="40" bind:value={newReward} onkeydown={(e) => e.key === 'Enter' && addReward()} /><button class="btn" onclick={addReward}>添加</button></div>
  </div>
</div>
