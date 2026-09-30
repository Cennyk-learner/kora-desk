<script lang="ts">
  // Number that counts up or down to its new value.
  import { motionOff } from '../lib/fx';
  let { value, decimals = 0 }: { value: number; decimals?: number } = $props();
  let shown = $state(0);
  let init = false;
  $effect(() => {
    const target = value;
    if (!init || motionOff()) {
      shown = target;
      init = true;
      return;
    }
    const from = shown;
    const t0 = performance.now();
    let raf = 0;
    const step = (t: number) => {
      const p = Math.min(1, (t - t0) / 500);
      shown = from + (target - from) * (1 - Math.pow(1 - p, 3));
      if (p < 1) raf = requestAnimationFrame(step);
    };
    raf = requestAnimationFrame(step);
    return () => cancelAnimationFrame(raf);
  });
</script>

<span class="num">{shown.toFixed(decimals)}</span>
