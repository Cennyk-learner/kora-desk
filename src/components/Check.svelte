<script lang="ts">
  let { on, onchange, label = '' }: { on: boolean; onchange: (v: boolean, el: HTMLElement) => void; label?: string } = $props();
  let popping = $state(false);
  let el: HTMLButtonElement;
  function click() {
    const v = !on;
    if (v) {
      popping = true;
      setTimeout(() => (popping = false), 400);
    }
    onchange(v, el);
  }
</script>

<button bind:this={el} class="check" class:on class:pop={popping} role="checkbox" aria-checked={on} aria-label={label} onclick={click}>
  <svg viewBox="0 0 24 24"><path d="M5 12.5l4.5 4.5L19 7.5" /></svg>
</button>
