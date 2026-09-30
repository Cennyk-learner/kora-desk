<script lang="ts">
  import { onMount } from 'svelte';
  import { app, init } from './lib/app.svelte';
  import { unlock } from './lib/audio';
  import * as backend from './lib/backend';
  import App from './App.svelte';
  import Widget from './Widget.svelte';

  let label = $state<'main' | 'widget' | null>(null);

  $effect(() => {
    const el = document.documentElement;
    el.dataset.skin = app.skin;
    el.dataset.mode = app.mode;
    el.dataset.window = label ?? '';
  });

  onMount(async () => {
    const l = (await backend.windowLabel()) === 'widget' ? 'widget' : 'main';
    await init(l);
    label = l;
    const p = new URLSearchParams(location.search).get('page');
    if (p && !backend.isTauri) app.page = p as typeof app.page;
    const once = () => {
      unlock();
      removeEventListener('pointerdown', once);
      removeEventListener('keydown', once);
    };
    addEventListener('pointerdown', once);
    addEventListener('keydown', once);
    if (l === 'main') {
      addEventListener('contextmenu', (e) => e.preventDefault());
    }
  });
</script>

{#if label === 'main'}
  <App />
{:else if label === 'widget'}
  <Widget />
{/if}
