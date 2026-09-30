<script lang="ts">
  import type { Snippet } from 'svelte';
  let { onclose, children }: { onclose: () => void; children: Snippet } = $props();
  function key(e: KeyboardEvent) {
    if (e.key === 'Escape') onclose();
  }
</script>

<svelte:window onkeydown={key} />
<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
<div class="scrim" onmousedown={(e) => e.target === e.currentTarget && onclose()}>
  <div class="modal" role="dialog" aria-modal="true">{@render children()}</div>
</div>
