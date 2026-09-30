<script lang="ts">
  import { app, toggleFav } from '../lib/app.svelte';
  import { QUOTES } from '../data/quotes';
  import { WORDS } from '../data/words';

  let { kind = 'Quotes', compact = false }: { kind?: 'Quotes' | 'Words'; compact?: boolean } = $props();
  const bag = $derived(app.state[kind]);
  const idx = $derived(bag.Today?.date === app.today ? bag.Today.idx : -1);
  const q = $derived(kind === 'Quotes' && idx >= 0 ? QUOTES[idx] : null);
  const w = $derived(kind === 'Words' && idx >= 0 ? WORDS[idx] : null);
  const fav = $derived(idx >= 0 && bag.Favs.includes(idx));
</script>

{#if q}
  <div class="quote" class:compact>
    <p>{q[0]}</p>
    <div class="src"><span>—— {q[1]}</span><button class="star" class:on={fav} title={fav ? '取消收藏' : '收藏'} onclick={() => toggleFav('Quotes', idx)}>{fav ? '★' : '☆'}</button></div>
  </div>
{:else if w}
  <div class="quote">
    <p style="font-size:18px;font-weight:600">{w[0]} <span class="sub" style="font-size:13px;font-weight:400">{w[1]}</span></p>
    <p class="sub" style="font-size:13px;margin-top:4px;font-family:var(--font-body)">{w[2]}</p>
    <div class="src"><span>每日一词</span><button class="star" class:on={fav} title={fav ? '取消收藏' : '收藏'} onclick={() => toggleFav('Words', idx)}>{fav ? '★' : '☆'}</button></div>
  </div>
{/if}

<style>
  .compact p { font-size: 13px; line-height: 1.6; }
</style>
