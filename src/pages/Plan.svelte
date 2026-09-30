<script lang="ts">
  import { app } from '../lib/app.svelte';
  import { frequency, groupBy } from '../lib/model';
  import type { Task } from '../lib/types';

  let { onedit, onadd }: { onedit: (t: Task) => void; onadd: () => void } = $props();
  // Generated review tasks are listed separately so they do not bury the real plan.
  const planTasks = $derived(app.state.Tasks.filter((t) => !t.ReviewSrc));
  const reviews = $derived(app.state.Tasks.filter((t) => t.ReviewSrc && t.OnceDate >= app.today));
  const groups = $derived(groupBy(planTasks, (t) => t.Group));
</script>

<div class="inner">
  <div class="kicker">A WEEK WITH INTENTION</div>
  <h1 class="title">让每周，自有节奏。</h1>
  <div class="sub" style="margin-bottom:14px">每天重复，或每周轮换。所有安排都可以随时调整。</div>
  <button class="btn primary" onclick={onadd}>＋ 添加任务</button>
  {#each groups as [g, items]}
    <div class="label">{g} · {items.length} 项安排</div>
    <div class="tasks">
      {#each items as t (t.Id)}
        <div class="task" style="grid-template-columns:1fr auto">
          <div>
            <div class="t">{t.Title}{#if t.Review}<span class="chip accent" style="margin-left:8px">间隔复习</span>{/if}{#if t.Est}<span class="chip" style="margin-left:6px">{t.Est} 分钟</span>{/if}</div>
            <div class="n">{frequency(t)}</div>
          </div>
          <button class="btn sm" onclick={() => onedit(t)}>编辑</button>
        </div>
      {/each}
    </div>
  {/each}
  {#if reviews.length}
    <div class="label">即将到来的复习 · {reviews.length}</div>
    <div class="tasks">
      {#each reviews.slice(0, 12) as t (t.Id)}
        <div class="task" style="grid-template-columns:1fr auto">
          <div><div class="t">{t.Title}</div><div class="n">{t.Note}</div></div>
          <span class="chip">{t.OnceDate}</span>
        </div>
      {/each}
    </div>
  {/if}
</div>
