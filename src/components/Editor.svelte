<script lang="ts">
  import { untrack } from 'svelte';
  import { app, deleteTask, saveTask, toast } from '../lib/app.svelte';
  import { newId } from '../lib/model';
  import { ORDER, SHORT } from '../lib/dates';
  import type { Task } from '../lib/types';
  import Modal from './Modal.svelte';
  import Toggle from './Toggle.svelte';
  import Seg from './Seg.svelte';

  let { task, defaultDate, onclose }: { task: Task | null; defaultDate: string; onclose: () => void } = $props();

  const init: Task = untrack(() => task) ?? { Id: '', Title: '', Note: '', Group: '学习', Days: [1, 2, 3, 4, 5, 6, 0], OnceDate: '' };
  let title = $state(init.Title);
  let note = $state(init.Note);
  let group = $state(init.Group);
  let mode = $state(init.OnceDate ? 'once' : 'weekly');
  let days = $state<number[]>([...init.Days]);
  let date = $state(init.OnceDate || untrack(() => defaultDate));
  let est = $state<number | ''>(init.Est ?? '');
  let review = $state(!!init.Review);
  let confirmDelete = $state(false);

  const groups = $derived([...new Set(['学习', '健身', '生活', ...app.state.Tasks.map((t) => t.Group)])].slice(0, 8));

  function toggleDay(d: number) {
    days = days.includes(d) ? days.filter((x) => x !== d) : [...days, d];
  }

  function save() {
    const t = title.trim();
    if (!t) return toast('请填写任务名称。', '⚠️');
    if (t.length > 80) return toast('名称最多 80 个字。', '⚠️');
    if (note.length > 300) return toast('备注最多 300 个字。', '⚠️');
    if (!group.trim() || group.trim().length > 20) return toast('分组需要 1–20 个字。', '⚠️');
    if (mode === 'weekly' && days.length === 0) return toast('请至少选择一天。', '⚠️');
    if (mode === 'once' && !/^\d{4}-\d{2}-\d{2}$/.test(date)) return toast('请选择日期。', '⚠️');
    const out: Task = {
      ...(task ?? {}),
      Id: task?.Id ?? newId(),
      Title: t,
      Note: note.trim(),
      Group: group.trim(),
      Days: mode === 'weekly' ? [...days] : [],
      OnceDate: mode === 'once' ? date : ''
    };
    if (est === '' || !(Number(est) > 0)) delete out.Est;
    else out.Est = Math.min(600, Math.round(Number(est)));
    if (review) out.Review = true;
    else delete out.Review;
    void saveTask(out);
    onclose();
  }

  function remove() {
    if (!task) return;
    if (!confirmDelete) {
      confirmDelete = true;
      return;
    }
    void deleteTask(task.Id);
    onclose();
  }
</script>

<Modal {onclose}>
  <h3>{task ? '编辑任务' : '添加任务'}</h3>
  <div class="stack" style="margin-top:14px">
    <input type="text" placeholder="任务名称" bind:value={title} maxlength="80" />
    <textarea rows="2" placeholder="备注（可选）" bind:value={note} maxlength="300" class="field"></textarea>
    <div>
      <div class="label" style="margin-top:4px">分组</div>
      <div class="row wrap" style="gap:6px">
        {#each groups as g}<button class="tag" class:on={group === g} onclick={() => (group = g)}>{g}</button>{/each}
        <input type="text" style="width:110px" placeholder="自定义" maxlength="20" value={groups.includes(group) ? '' : group} oninput={(e) => (group = e.currentTarget.value)} />
      </div>
    </div>
    <div>
      <div class="row spread"><div class="label" style="margin:4px 2px">重复</div><Seg items={[['weekly', '每周'], ['once', '仅一次']]} value={mode} onpick={(v) => (mode = v)} /></div>
      {#if mode === 'weekly'}
        <div class="row wrap" style="gap:6px;margin-top:8px">
          {#each ORDER as d}<button class="tag" class:on={days.includes(d)} onclick={() => toggleDay(d)}>{SHORT[d]}</button>{/each}
          <button class="btn quiet" onclick={() => (days = [1, 2, 3, 4, 5, 6, 0])}>每天</button>
        </div>
      {:else}
        <div style="margin-top:8px"><input type="date" bind:value={date} /></div>
      {/if}
    </div>
    <div class="row spread">
      <span>预计用时（分钟）<span class="hint">用来对比实际专注时长</span></span>
      <input type="number" min="0" max="600" placeholder="—" bind:value={est} />
    </div>
    <div class="row spread">
      <span>间隔复习<span class="hint">完成后在第 1/2/4/7/15 天自动安排复习</span></span>
      <Toggle on={review} onchange={(v) => (review = v)} label="间隔复习" />
    </div>
  </div>
  <div class="actions">
    {#if task}<button class="btn danger" style="margin-right:auto" onclick={remove}>{confirmDelete ? '再点一次确认删除' : '删除'}</button>{/if}
    <button class="btn" onclick={onclose}>取消</button>
    <button class="btn primary" onclick={save}>保存</button>
  </div>
</Modal>
