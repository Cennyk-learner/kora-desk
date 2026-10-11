<script lang="ts">
  import { app, skipTask, toast, unskipTask } from '../lib/app.svelte';
  import { skipOf } from '../lib/skip';
  import { monthDay, parse } from '../lib/dates';
  import type { Task } from '../lib/types';
  import Modal from './Modal.svelte';

  let { task, date, onclose }: { task: Task; date: string; onclose: () => void } = $props();
  const existing = $derived(skipOf(app.state, task.Id, date));
  let reason = $state('');
  const QUICK = ['今天时间被占满', '身体不适', '考试 / 作业优先', '计划调整', '需要休息'];

  $effect(() => {
    reason = existing?.reason ?? '';
  });

  function save() {
    if (!reason.trim()) return toast('请写一个理由，之后可以回看。', '📝');
    void skipTask(task, date, reason);
    onclose();
  }
</script>

<Modal {onclose}>
  <h3>「{task.Title}」请假</h3>
  <p class="sub" style="line-height:1.8;margin:8px 0 0">{monthDay(parse(date))}这一项有计划地不做。它不计入当天的完成率，也不记入放弃库，但会单独记录在「放弃与请假」页。</p>
  <div class="row wrap" style="gap:6px;margin-top:14px">
    {#each QUICK as q}<button class="tag" class:on={reason === q} onclick={() => (reason = q)}>{q}</button>{/each}
  </div>
  <input type="text" style="margin-top:10px" placeholder="理由" maxlength="60" bind:value={reason} onkeydown={(e) => e.key === 'Enter' && save()} />
  <div class="actions">
    {#if existing}<button class="btn danger" style="margin-right:auto" onclick={() => { void unskipTask(task.Id, date); onclose(); }}>取消请假</button>{/if}
    <button class="btn" onclick={onclose}>关闭</button>
    <button class="btn primary" onclick={save}>{existing ? '更新理由' : '确认请假'}</button>
  </div>
</Modal>
