<script lang="ts">
  import { app, dropLeave, takeLeave, toast } from '../lib/app.svelte';
  import { leaveOn, leavesInWeek, LEAVE_WARN_DAYS } from '../lib/leave';
  import { monthDay, parse } from '../lib/dates';
  import Modal from './Modal.svelte';

  let { onclose }: { onclose: () => void } = $props();
  const date = $derived(app.selected < app.today ? app.today : app.selected);
  const existing = $derived(leaveOn(app.state, date));
  let reason = $state('');
  const QUICK = ['生病', '考试周', '出行', '家里有事', '需要休息'];
  const inWeek = $derived(leavesInWeek(app.state, date) + (existing ? 0 : 1));

  $effect(() => {
    reason = existing?.reason ?? '';
  });

  function save() {
    if (!reason.trim()) return toast('请写一个请假理由，之后可以回看。', '📝');
    void takeLeave(date, reason);
    if (inWeek >= LEAVE_WARN_DAYS) toast(`这一周已请假 ${inWeek} 天，注意节奏。`, '⚠️');
    onclose();
  }
</script>

<Modal {onclose}>
  <h3>{monthDay(parse(date))}请假</h3>
  <p class="sub" style="line-height:1.8;margin:8px 0 0">请假的日子不计入放弃库，也不会打断连续天数。理由会被记录下来，方便之后回看。</p>
  <div class="row wrap" style="gap:6px;margin-top:14px">
    {#each QUICK as q}<button class="tag" class:on={reason === q} onclick={() => (reason = q)}>{q}</button>{/each}
  </div>
  <input type="text" style="margin-top:10px" placeholder="请假理由" maxlength="60" bind:value={reason} onkeydown={(e) => e.key === 'Enter' && save()} />
  {#if inWeek >= LEAVE_WARN_DAYS}
    <div class="warn" style="margin-top:12px">⚠️ 这一周请假将达到 {inWeek} 天（≥ {LEAVE_WARN_DAYS} 天会预警）。</div>
  {/if}
  <div class="actions">
    {#if existing}<button class="btn danger" style="margin-right:auto" onclick={() => { void dropLeave(date); onclose(); }}>取消请假</button>{/if}
    <button class="btn" onclick={onclose}>关闭</button>
    <button class="btn primary" onclick={save}>{existing ? '更新理由' : '确认请假'}</button>
  </div>
</Modal>
