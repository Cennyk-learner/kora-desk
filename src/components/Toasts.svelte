<script lang="ts">
  import { app } from '../lib/app.svelte';
</script>

<div class="toasts" aria-live="polite">
  {#each app.toasts as t (t.id)}
    <div class="toast"><span>{t.icon}</span><span>{t.text}</span></div>
  {/each}
</div>
