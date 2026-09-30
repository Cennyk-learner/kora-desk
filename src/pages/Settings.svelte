<script lang="ts">
  import { onMount } from 'svelte';
  import { app, toast, update } from '../lib/app.svelte';
  import * as backend from '../lib/backend';
  import { syncShortcuts } from '../lib/engine.svelte';
  import { toCsv } from '../lib/report';
  import { withDefaults } from '../lib/model';
  import type { Prefs, State } from '../lib/types';
  import Seg from '../components/Seg.svelte';
  import Toggle from '../components/Toggle.svelte';

  const st = $derived(app.state.Settings);
  const pomo = $derived(st.Pomo!);
  const care = $derived(st.Care!);
  const exam = $derived(app.state.Exam);
  let autostart = $state(false);

  const SKINS = [
    { id: 'mac', name: 'macOS', blurb: '半透明侧栏、圆形复选框、分组圆角列表', sw: ['#007AFF', '#F6F6F7', '#FFFFFF', '#1D1D1F'] },
    { id: 'fluent', name: 'Fluent · Mica', blurb: 'Windows 11 的云母底与导航栏', sw: ['#0F7B6C', '#EAEFF0', '#FBFBFB', '#1A1A1A'] },
    { id: 'material', name: 'Material 3 Expressive', blurb: '粗标题、波浪进度、分段清单', sw: ['#2C6A4D', '#B4F1D0', '#FFDCC0', '#EBF1E8'] },
    { id: 'carbon', name: 'IBM Carbon', blurb: '数据块与表格，方角，信息密度高', sw: ['#0F62FE', '#161616', '#262626', '#F4F4F4'] },
    { id: 'dot', name: '点阵仪表', blurb: '点阵数字、圆点进度、单一红色信号', sw: ['#E4212B', '#F2F2F0', '#101010', '#C9C9C6'] }
  ];

  onMount(async () => {
    if (backend.isTauri) {
      try {
        const a = await import('@tauri-apps/plugin-autostart');
        autostart = await a.isEnabled();
      } catch {
        /* optional */
      }
    }
  });

  const pref = (fn: (p: Prefs) => void) => update((s) => fn(s.Settings));
  const num = (e: Event, min: number, max: number, fallback: number) => {
    const v = Math.round(Number((e.currentTarget as HTMLInputElement).value));
    return Number.isFinite(v) ? Math.max(min, Math.min(max, v)) : fallback;
  };

  async function setAutostart(v: boolean) {
    try {
      const a = await import('@tauri-apps/plugin-autostart');
      if (v) await a.enable();
      else await a.disable();
      autostart = v;
    } catch (e) {
      toast(`无法设置开机自启：${e}`, '⚠️');
    }
  }

  function capture(which: 'KeyFocus' | 'KeyOpen', e: KeyboardEvent) {
    e.preventDefault();
    if (e.key === 'Backspace' || e.key === 'Delete') {
      void pref((p) => (p[which] = ''));
      void syncShortcuts();
      return;
    }
    if (['Control', 'Alt', 'Shift', 'Meta'].includes(e.key)) return;
    const mods = [e.ctrlKey && 'Ctrl', e.altKey && 'Alt', e.shiftKey && 'Shift', e.metaKey && 'Super'].filter(Boolean) as string[];
    if (!mods.length) return toast('快捷键需要至少一个修饰键（Ctrl / Alt / Shift）。', '⌨️');
    const k = e.key.length === 1 ? e.key.toUpperCase() : e.key.replace('Arrow', '');
    void pref((p) => (p[which] = [...mods, k].join('+')));
    void syncShortcuts();
  }

  async function exportBackup() {
    const path = await backend.saveDialog(`kora-backup-${app.today}.json`, 'json', 'Kora 备份');
    if (!path) return;
    try {
      await backend.call('export_state', { path });
      toast('备份已导出。', '💾');
    } catch (e) {
      toast(`导出失败：${e}`, '⚠️');
    }
  }
  async function importBackup() {
    const path = await backend.openDialog('json', 'Kora 备份');
    if (!path) return;
    if (!(await backend.confirmDialog('用这个备份替换现有任务和完成记录？当前数据将保留为自动备份。', '导入备份', '替换', '取消'))) return;
    try {
      const s = await backend.call<State>('import_state', { path });
      if (s) app.state = withDefaults(s);
      toast('备份已导入。', '📥');
    } catch (e) {
      toast(`导入失败：${e}`, '⚠️');
    }
  }
  async function exportCsv() {
    const path = await backend.saveDialog(`kora-records-${app.today}.csv`, 'csv', 'CSV 表格');
    if (!path) return;
    await backend.call('write_text_file', { path, text: toCsv(app.state) });
    toast('CSV 已导出。', '📊');
  }
  function setExam(patch: Partial<NonNullable<typeof exam>>) {
    void update((s) => {
      const cur = s.Exam ?? { Name: '雅思考试', Date: '' };
      s.Exam = { ...cur, ...patch };
      if (!s.Exam.Name && !s.Exam.Date) s.Exam = null;
    });
  }
</script>

<div class="inner">
  <div class="kicker">MAKE ROOM FOR YOUR DAY</div>
  <h1 class="title">轻一点，刚刚好。</h1>
  <div class="sub">桌面组件只保留当天清单。复杂的安排，留在这里。</div>

  <h2 class="sec">界面风格 <small>五种风格共用同一份数据，管理窗口和桌面组件一起更换</small></h2>
  <div class="row wrap" style="gap:10px;align-items:stretch">
    {#each SKINS as s}
      <button class="card" style="width:158px;text-align:left;padding:12px;{app.skin === s.id ? 'outline:2px solid var(--accent);outline-offset:-2px' : ''}" onclick={() => pref((p) => (p.Theme = s.id))}>
        <div style="display:grid;grid-template-columns:repeat(4,1fr);height:18px;border-radius:4px;overflow:hidden;margin-bottom:10px;border:1px solid var(--line)">{#each s.sw as c}<i style="background:{c}"></i>{/each}</div>
        <div style="font-weight:600;font-size:13px">{s.name}</div>
        <div class="sub" style="font-size:11px;margin-top:3px;line-height:1.5">{s.blurb}</div>
      </button>
    {/each}
  </div>

  <h2 class="sec">外观</h2>
  <Seg items={[['system', '跟随系统'], ['light', '浅色'], ['dark', '深色']]} value={st.Appearance === 'light' || st.Appearance === 'dark' ? st.Appearance : 'system'} onpick={(v) => pref((p) => (p.Appearance = v))} />

  <h2 class="sec">声音与动效</h2>
  <div class="card stack">
    <div class="row spread"><span>静音</span><Toggle on={!!st.Mute} onchange={(v) => pref((p) => (p.Mute = v))} label="静音" /></div>
    <div class="row spread"><span>音量</span><input type="range" min="0" max="1" step="0.05" value={st.Volume ?? 0.6} style="width:180px;accent-color:var(--accent)" oninput={(e) => pref((p) => (p.Volume = Number(e.currentTarget.value)))} /></div>
    <div class="row spread"><span>动效<span class="hint">彩纸与过渡；「跟随系统」会遵守减少动态效果</span></span><Seg items={[['auto', '跟随系统'], ['off', '关闭']]} value={st.Motion ?? 'auto'} onpick={(v) => pref((p) => (p.Motion = v as 'auto' | 'off'))} /></div>
  </div>

  <h2 class="sec">桌面组件</h2>
  <div class="card stack">
    <div class="row spread"><span>始终置顶<span class="hint">关闭时留在桌面层，其他窗口可以盖住它</span></span><Toggle on={st.AlwaysOnTop} onchange={(v) => pref((p) => (p.AlwaysOnTop = v))} label="始终置顶" /></div>
    <div class="row wrap" style="gap:8px">
      <button class="btn primary" onclick={() => backend.call('widget_show')}>显示桌面组件</button>
      <button class="btn" onclick={() => backend.call('widget_reset')}>重置组件位置</button>
      <button class="btn" onclick={() => backend.call('widget_hide')}>隐藏组件</button>
    </div>
  </div>

  <h2 class="sec">照顾自己 <small>提醒只在应用运行时生效</small></h2>
  <div class="card stack">
    <div class="row spread"><span>20-20-20 护眼<span class="hint">每隔一段时间提醒看远处 20 秒</span></span><div class="row" style="gap:8px"><input type="number" value={care.EyeMin} onchange={(e) => pref((p) => (p.Care!.EyeMin = num(e, 5, 120, 20)))} />分<Toggle on={care.Eye} onchange={(v) => pref((p) => (p.Care!.Eye = v))} label="护眼提醒" /></div></div>
    <div class="row spread"><span>久坐提醒</span><div class="row" style="gap:8px"><input type="number" value={care.SitMin} onchange={(e) => pref((p) => (p.Care!.SitMin = num(e, 10, 180, 45)))} />分<Toggle on={care.Sit} onchange={(v) => pref((p) => (p.Care!.Sit = v))} label="久坐提醒" /></div></div>
    <div class="row spread"><span>就寝提醒</span><div class="row" style="gap:8px"><input type="time" value={care.BedTime} onchange={(e) => pref((p) => (p.Care!.BedTime = e.currentTarget.value || '23:00'))} /><Toggle on={care.Bed} onchange={(v) => pref((p) => (p.Care!.Bed = v))} label="就寝提醒" /></div></div>
    <div class="row spread"><span>每日喝水目标</span><div class="row" style="gap:8px"><input type="number" value={care.WaterGoal} onchange={(e) => pref((p) => (p.Care!.WaterGoal = num(e, 1, 20, 8)))} />杯</div></div>
  </div>

  <h2 class="sec">连续打卡</h2>
  <div class="card row spread"><span>每月冻结卡<span class="hint">漏做一天时自动使用，保住连续天数</span></span><div class="row" style="gap:8px"><input type="number" value={st.FreezePerMonth ?? 2} onchange={(e) => pref((p) => (p.FreezePerMonth = num(e, 0, 10, 2)))} />次</div></div>

  <h2 class="sec">考试倒计时 <small>组件与今天页会显示剩余天数</small></h2>
  <div class="card stack">
    <div class="row wrap" style="gap:12px"><input type="text" style="width:160px" placeholder="考试名称" maxlength="20" value={exam?.Name ?? ''} onchange={(e) => setExam({ Name: e.currentTarget.value.trim() })} /><input type="date" value={exam?.Date ?? ''} onchange={(e) => setExam({ Date: e.currentTarget.value })} />
      {#if exam}<button class="btn quiet" onclick={() => update((s) => (s.Exam = null))}>清除</button>{/if}</div>
    <div class="row wrap" style="gap:10px"><span class="sub">备考目标（可选）</span>
      <input type="text" style="width:90px" placeholder="单位，如：词" maxlength="6" value={exam?.Label ?? ''} onchange={(e) => setExam({ Label: e.currentTarget.value.trim() })} />
      <span class="sub">总量</span><input type="number" value={exam?.Total ?? ''} onchange={(e) => setExam({ Total: num(e, 0, 100000, 0) })} />
      <span class="sub">已完成</span><input type="number" value={exam?.Done ?? 0} onchange={(e) => setExam({ Done: num(e, 0, 100000, 0) })} />
    </div>
  </div>

  <h2 class="sec">快捷键与启动</h2>
  <div class="card stack">
    <div class="row spread"><span>开始 / 暂停番茄钟</span><input type="text" readonly style="width:180px;text-align:center" value={st.KeyFocus ?? ''} placeholder="点击后按下组合键" onkeydown={(e) => capture('KeyFocus', e)} /></div>
    <div class="row spread"><span>打开今日清单</span><input type="text" readonly style="width:180px;text-align:center" value={st.KeyOpen ?? ''} placeholder="点击后按下组合键" onkeydown={(e) => capture('KeyOpen', e)} /></div>
    <div class="faint" style="font-size:12px">点击输入框后按下组合键；按 Backspace 清除。</div>
    <div class="row spread"><span>开机自启动<span class="hint">启动后只在托盘运行，不弹出窗口</span></span><Toggle on={autostart} onchange={setAutostart} label="开机自启" /></div>
  </div>

  <h2 class="sec">属于你的记录</h2>
  <div class="card stack">
    <div class="sub" style="line-height:1.7">任务与完成记录仅保存在这台电脑。无需账号，没有广告，应用不连接网络。自动保留上一次保存的备份。</div>
    <div class="row wrap" style="gap:8px">
      <button class="btn" onclick={exportBackup}>导出备份</button>
      <button class="btn" onclick={importBackup}>导入备份</button>
      <button class="btn" onclick={exportCsv}>导出 CSV</button>
      <button class="btn" onclick={() => backend.call('open_folder')}>打开数据文件夹</button>
    </div>
    <div class="faint" style="font-size:12px">{app.dir}</div>
  </div>

  <div class="faint" style="font-size:12px;margin:14px 4px;line-height:1.8">Kora Desk · 刻序 v2.0.0 / 一点一点，也是向前。<br />拖动组件顶部可移动位置；右键托盘图标可隐藏组件或退出。</div>
</div>
