<script lang="ts">
  import { app } from '../lib/app.svelte';
  let { done, total, size = 96 }: { done: number; total: number; size?: number } = $props();
  const pct = $derived(total ? done / total : 0);
  const kind = $derived(({ mac: 'ring', fluent: 'bar', material: 'wave', carbon: 'bar', dot: 'dots' } as Record<string, string>)[app.skin]);
  const r = $derived(size / 2 - 7);
  const circ = $derived(2 * Math.PI * r);
  let phase = $state(0);
  $effect(() => {
    if (kind !== 'wave') return;
    let raf = 0;
    const loop = (t: number) => {
      phase = t / 400;
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  });
  const wavePath = $derived.by(() => {
    const w = 260;
    const x = Math.max(6, pct * w);
    let d = 'M0 7';
    for (let i = 0; i <= x; i += 3) d += ` L${i} ${7 + Math.sin(i / 7 + phase) * 3.2}`;
    return d;
  });
</script>

{#if kind === 'ring'}
  <div class="ring" style="width:{size}px;height:{size}px">
    <svg width={size} height={size}>
      <circle class="bg" cx={size / 2} cy={size / 2} {r} stroke-width="8" />
      <circle class="fg" cx={size / 2} cy={size / 2} {r} stroke-width="8" stroke-dasharray={circ} stroke-dashoffset={circ * (1 - pct)} />
    </svg>
    <div class="c"><span class="big-num" style="font-size:{size * 0.27}px">{Math.round(pct * 100)}<small style="font-size:.5em">%</small></span></div>
  </div>
{:else if kind === 'wave'}
  <svg class="wave" style="width:{Math.min(260, size * 2.6)}px" viewBox="0 0 260 14" preserveAspectRatio="none">
    <path d="M0 7 H260" stroke="var(--track)" stroke-width="5" stroke-linecap="round" fill="none" />
    <path d={wavePath} stroke="var(--accent)" stroke-width="5" stroke-linecap="round" fill="none" />
  </svg>
{:else if kind === 'dots'}
  <div class="dots red" style="max-width:{Math.min(total, 12) * 28}px">
    {#each Array.from({ length: total }) as _, i}<i class:on={i < done}></i>{/each}
  </div>
{:else}
  <div class="bar" style="width:{Math.min(260, size * 2.6)}px"><i style="width:{pct * 100}%"></i></div>
{/if}
