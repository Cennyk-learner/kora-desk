<script lang="ts">
  import { app, toggleFav } from '../lib/app.svelte';
  import { QUOTES } from '../data/quotes';
  import { WORDS } from '../data/words';

  const quotes = $derived(app.state.Quotes.Favs.filter((i) => QUOTES[i]));
  const words = $derived(app.state.Words.Favs.filter((i) => WORDS[i]));
</script>

<div class="inner">
  <div class="kicker">SAVED</div>
  <h1 class="title">收藏夹</h1>
  <div class="sub">在每日名言或每日一词旁点 ☆，就会收在这里。</div>

  <h2 class="sec">名言 <small>{quotes.length}</small></h2>
  {#if quotes.length === 0}<div class="sub">还没有收藏。</div>{/if}
  <div class="stack">
    {#each quotes as i (i)}
      <div class="quote"><p>{QUOTES[i][0]}</p><div class="src"><span>—— {QUOTES[i][1]}</span><button class="star on" title="取消收藏" onclick={() => toggleFav('Quotes', i)}>★</button></div></div>
    {/each}
  </div>

  <h2 class="sec">单词 <small>{words.length}</small></h2>
  {#if words.length === 0}<div class="sub">还没有收藏。</div>{/if}
  <div class="stack">
    {#each words as i (i)}
      <div class="quote">
        <p style="font-size:17px;font-weight:600">{WORDS[i][0]} <span class="sub" style="font-size:13px;font-weight:400">{WORDS[i][1]}</span></p>
        <p class="sub" style="font-size:13px;margin-top:4px">{WORDS[i][2]}</p>
        <div class="src"><span></span><button class="star on" title="取消收藏" onclick={() => toggleFav('Words', i)}>★</button></div>
      </div>
    {/each}
  </div>
</div>
