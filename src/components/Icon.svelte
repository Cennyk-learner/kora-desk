<script lang="ts">
  let { name, size = 16, stroke = 2 }: { name: string; size?: number; stroke?: number } = $props();
  const P: Record<string, string> = {
    today: 'M4 12l5 5L20 6',
    week: 'M4 7h16M4 12h16M4 17h10',
    focus: 'M12 8v5l3 2M12 3a9 9 0 100 18 9 9 0 000-18M10 1.5h4',
    stats: 'M5 20V11M12 20V4M19 20v-6',
    giveup: 'M5 21V4M5 5h11l-2 4 2 4H5',
    care: 'M12 20s-7-4.4-7-10a4 4 0 017-2.6A4 4 0 0119 10c0 5.6-7 10-7 10z',
    settings: 'M4 7h9M17 7h3M4 17h3M11 17h9M15 5v4M9 15v4',
    favs: 'M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z',
    plus: 'M12 5v14M5 12h14',
    left: 'M15 5l-7 7 7 7',
    right: 'M9 5l7 7-7 7',
    close: 'M6 6l12 12M18 6L6 18',
    min: 'M5 12h14',
    more: 'M5 12h.01M12 12h.01M19 12h.01',
    play: 'M8 5v14l11-7z',
    pause: 'M9 5v14M15 5v14',
    stop: 'M6 6h12v12H6z',
    lock: 'M7 11V8a5 5 0 0110 0v3M6 11h12v9H6z',
    flame: 'M12 3c1 4 5 5 5 10a5 5 0 01-10 0c0-2 1-3 2-4 0 2 1 3 2 3 0-3-1-5 1-9z',
    edit: 'M4 20h4L19 9l-4-4L4 16z',
    trash: 'M5 7h14M9 7V4h6v3M7 7l1 13h8l1-13',
    check: 'M5 12l5 5 9-10',
    image: 'M4 5h16v14H4zM4 16l5-5 4 4 3-3 4 4M9 9h.01'
  };
</script>

<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width={stroke} stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d={P[name] ?? ''} /></svg>
