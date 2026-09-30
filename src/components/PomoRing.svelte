<script lang="ts">
  import { app } from '../lib/app.svelte';
  import { clock, leftMs } from '../lib/pomodoro';

  let { size = 260, compact = false }: { size?: number; compact?: boolean } = $props();
  const f = $derived(app.state.Focus);
  const left = $derived(leftMs(f, app.now));
  const total = $derived(f.Planned * 60000);
  const pct = $derived(f.Mode === 'idle' || !total ? 0 : 1 - left / total);
  const r = $derived(size / 2 - (compact ? 5 : 10));
  const circ = $derived(2 * Math.PI * r);
  const label = $derived(f.Mode === 'focus' ? '专注中' : f.Mode === 'short' ? '短休息' : f.Mode === 'long' ? '长休息' : '准备好了');
  const idleClock = $derived(clock((app.state.Settings.Pomo?.Focus ?? 25) * 60000));
</script>

<div class="ring" style="width:{size}px;height:{size}px">
  <svg width={size} height={size}>
    <circle class="bg" cx={size / 2} cy={size / 2} {r} stroke-width={compact ? 5 : 10} />
    <circle
      class="fg"
      style="transition:stroke-dashoffset 1s linear;stroke:{f.Mode === 'short' || f.Mode === 'long' ? 'var(--signal, #30d158)' : 'var(--accent)'}"
      cx={size / 2}
      cy={size / 2}
      {r}
      stroke-width={compact ? 5 : 10}
      stroke-dasharray={circ}
      stroke-dashoffset={circ * (1 - pct)}
    />
  </svg>
  <div class="c">
    <div>
      <div class="big-num" style="font-size:{compact ? size * 0.27 : size * 0.2}px">{f.Mode === 'idle' ? idleClock : clock(left)}</div>
      {#if !compact}<div class="sub" style="margin-top:2px">{label}{f.PausedLeft > 0 ? ' · 已暂停' : ''}</div>{/if}
    </div>
  </div>
</div>
