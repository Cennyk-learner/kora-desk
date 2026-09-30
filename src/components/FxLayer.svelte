<script lang="ts">
  import { onMount } from 'svelte';
  import { app } from '../lib/app.svelte';
  import { registerFx, type FxKind } from '../lib/fx';

  interface P { x: number; y: number; vx: number; vy: number; rot: number; vr: number; life: number; color: string; shape: 'rect' | 'circle' | 'square'; size: number }
  let canvas: HTMLCanvasElement;
  let parts: P[] = [];
  let raf = 0;
  let last = 0;

  function palette(): string[] {
    const cs = getComputedStyle(document.documentElement);
    const a = cs.getPropertyValue('--accent').trim() || '#0a84ff';
    return app.skin === 'dot' ? [a, cs.getPropertyValue('--signal').trim() || '#e4212b', a] : [a, '#ff9f0a', '#30d158', '#ff375f', '#bf5af2', '#64d2ff'];
  }

  function burst(kind: FxKind, x = innerWidth / 2, y = innerHeight / 2) {
    const colors = palette();
    const pixel = app.skin === 'dot' || kind === 'pixels';
    const rain = kind === 'rain';
    const n = rain ? 140 : 34;
    for (let i = 0; i < n; i++) {
      const ang = Math.random() * Math.PI * 2;
      const sp = 3 + Math.random() * 6;
      parts.push({
        x: rain ? Math.random() * innerWidth : x,
        y: rain ? -20 - Math.random() * 200 : y,
        vx: rain ? (Math.random() - 0.5) * 2 : Math.cos(ang) * sp,
        vy: rain ? 2 + Math.random() * 3 : Math.sin(ang) * sp - 3,
        rot: Math.random() * 6,
        vr: (Math.random() - 0.5) * 0.4,
        life: 1,
        color: colors[i % colors.length],
        shape: pixel ? 'square' : Math.random() < 0.3 ? 'circle' : 'rect',
        size: pixel ? 5 : 4 + Math.random() * 5
      });
    }
    if (!raf) {
      last = performance.now();
      raf = requestAnimationFrame(loop);
    }
  }

  function loop(t: number) {
    const dt = Math.min(2, (t - last) / 16.7);
    last = t;
    const ctx = canvas.getContext('2d')!;
    ctx.clearRect(0, 0, innerWidth, innerHeight);
    parts = parts.filter((p) => p.life > 0 && p.y < innerHeight + 30);
    for (const p of parts) {
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      p.vy += 0.16 * dt;
      p.vx *= 0.995;
      p.rot += p.vr * dt;
      p.life -= 0.0045 * dt;
      ctx.save();
      ctx.globalAlpha = Math.max(0, Math.min(1, p.life * 1.6));
      ctx.translate(p.x, p.y);
      ctx.rotate(p.shape === 'square' ? 0 : p.rot);
      ctx.fillStyle = p.color;
      if (p.shape === 'circle') {
        ctx.beginPath();
        ctx.arc(0, 0, p.size / 2, 0, 6.3);
        ctx.fill();
      } else if (p.shape === 'square') ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size);
      else ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2);
      ctx.restore();
    }
    raf = parts.length ? requestAnimationFrame(loop) : 0;
  }

  function resize() {
    const dpr = devicePixelRatio || 1;
    canvas.width = innerWidth * dpr;
    canvas.height = innerHeight * dpr;
    canvas.getContext('2d')!.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  onMount(() => {
    resize();
    registerFx((k, x, y) => burst(k, x, y));
    return () => {
      registerFx(null);
      cancelAnimationFrame(raf);
    };
  });
</script>

<svelte:window onresize={resize} />
<canvas class="fx" bind:this={canvas} style="width:100%;height:100%"></canvas>
