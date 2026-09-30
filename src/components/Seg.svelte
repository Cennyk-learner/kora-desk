<script lang="ts">
  let { items, value, onpick }: { items: [string, string][]; value: string; onpick: (v: string) => void } = $props();
</script>

<div class="seg" role="tablist">
  {#each items as [v, label]}
    <button class:on={value === v} role="tab" aria-selected={value === v} onclick={() => onpick(v)}>{label}</button>
  {/each}
</div>
