<script lang="ts">
  import { app, update } from '../lib/app.svelte';
  import { examInfo, examText } from '../lib/exam';
  import Modal from './Modal.svelte';

  let { onclose }: { onclose: () => void } = $props();
  const exam = $derived(app.state.Exam);
  const info = $derived(examInfo(exam, app.today));
  let add = $state<number | ''>('');

  function log() {
    const n = Number(add);
    if (!Number.isFinite(n) || n === 0) return;
    void update((s) => {
      if (s.Exam) s.Exam.Done = Math.max(0, (s.Exam.Done ?? 0) + Math.round(n));
    });
    add = '';
  }
</script>

<Modal {onclose}>
  {#if exam && info}
    <h3>{examText(exam, info)}</h3>
    {#if exam.Total && info.remaining !== null}
      <p class="sub" style="line-height:1.8;margin:8px 0 0">
        目标 {exam.Total}{exam.Label ?? ''}，已完成 <b style="color:var(--text)">{exam.Done ?? 0}</b>，还剩 {info.remaining}。
        {#if info.perDay}按剩余 {info.days} 天算，<b style="color:var(--text)">每天约 {info.perDay}{exam.Label ?? ''}</b>。{/if}
      </p>
      <div class="bar" style="margin-top:12px"><i style="width:{Math.min(100, ((exam.Done ?? 0) / exam.Total) * 100)}%"></i></div>
      <div class="row" style="gap:8px;margin-top:16px"><span class="sub">今天完成了</span><input type="number" placeholder="0" bind:value={add} onkeydown={(e) => e.key === 'Enter' && log()} /><span class="sub">{exam.Label ?? ''}</span><button class="btn primary" onclick={log}>记录</button></div>
    {:else}
      <p class="sub" style="margin:8px 0 0;line-height:1.8">在「偏好设置 → 考试倒计时」里填写总量，就能得到每天的建议量。</p>
    {/if}
  {/if}
  <div class="actions"><button class="btn" onclick={onclose}>关闭</button></div>
</Modal>
