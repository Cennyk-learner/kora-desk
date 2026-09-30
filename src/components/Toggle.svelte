<script lang="ts">
  let { on, onchange, label = '' }: { on: boolean; onchange: (v: boolean) => void; label?: string } = $props();
</script>

<button class="switch" class:on role="switch" aria-checked={on} aria-label={label} onclick={() => onchange(!on)}></button>
