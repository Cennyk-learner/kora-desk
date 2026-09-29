using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Kora {
 public class Widget : Window {
  readonly Store store;
  readonly bool preview;
  State state;
  Skin skin;
  WidgetContext context;
  DesktopHost host;
  DispatcherTimer timer;
  readonly PollGuard polling = new PollGuard();
  DateTime stamp, day = DateTime.Today;
  EventWaitHandle wake;
  Forms.NotifyIcon tray;

  public Widget(Store storage, bool isPreview) {
   store = storage;
   preview = isPreview;
   state = store.Read();
   Title = "Kora Desk · 桌面组件";
   ResizeMode = ResizeMode.NoResize;
   ShowInTaskbar = Environment.GetEnvironmentVariable("KORA_DIAGNOSTICS") == "1";
   WindowStartupLocation = WindowStartupLocation.Manual;
   Left = 100; Top = 90;
   Ui.Chromeless(this);
   if (!preview) host = new DesktopHost(this, store);
   wake = preview ? new EventWaitHandle(false, EventResetMode.AutoReset) : new EventWaitHandle(false, EventResetMode.AutoReset, Activation.Name("Widget")+".Wake");
   timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
   timer.Tick += delegate { polling.Run(Tick); };
   Loaded += delegate { Ui.Guard(() => Refresh(false)); timer.Start(); };
   Closed += delegate {
    timer.Stop();
    wake.Dispose();
    if (tray != null) { tray.Visible = false; tray.Dispose(); }
   };
   if (!preview) MakeTray();
   Refresh(false);
  }

  void MakeTray() {
   tray = new Forms.NotifyIcon { Text = "Kora Desk · 刻序", Visible = true };
   var icon = Path.Combine(Launch.BaseDirectory, "Kora.ico");
   tray.Icon = File.Exists(icon) ? new System.Drawing.Icon(icon) : System.Drawing.SystemIcons.Application;
   var menu = new Forms.ContextMenuStrip();
   menu.Items.Add("打开计划", null, delegate { Ui.Guard(() => Launch.App("Kora.exe")); });
   menu.Items.Add("显示 / 隐藏组件", null, delegate { if (IsVisible) Hide(); else { Show(); host.Apply(store.Read().Settings); } });
   menu.Items.Add("切换桌面 / 置顶", null, delegate { Ui.Guard(ToggleTop); });
   menu.Items.Add(new Forms.ToolStripSeparator());
   menu.Items.Add("退出组件", null, delegate { Close(); });
   tray.ContextMenuStrip = menu;
   tray.DoubleClick += delegate { Ui.Guard(() => Launch.App("Kora.exe")); };
  }

  void ToggleTop() {
   store.Update(s => s.Settings.AlwaysOnTop = !s.Settings.AlwaysOnTop);
   Refresh(true);
  }

  void OpenMenu(FrameworkElement anchor) {
   bool top = state.Settings.AlwaysOnTop;
   Controls.Menu(anchor,
    new[] { top ? "✓ 始终置顶（点击关闭）" : "切换为始终置顶", "编辑每周计划", "隐藏组件（可从托盘恢复）" },
    new Action[] { () => Ui.Guard(ToggleTop), () => Ui.Guard(() => Launch.App("Kora.exe")), () => Hide() });
  }

  void Tick() {
   if (wake.WaitOne(0)) { Show(); if (host != null) host.Apply(store.Read().Settings); }
   var current = File.GetLastWriteTimeUtc(store.FilePath);
   bool system = Skins.AppearanceOf(state.Settings) == "system" && skin != null && Skins.SystemDark() != skin.Dark;
   if (current != stamp || day != DateTime.Today || system) Refresh(true);
   if (host != null && context != null && context.Status != null) context.Status.Text = host.Status;
  }

  void Refresh(bool reread) {
   var old = state.Settings;
   if (reread) state = store.Read();
   stamp = File.GetLastWriteTimeUtc(store.FilePath);
   day = DateTime.Today;
   var next = Skins.Resolve(state.Settings);
   if (!Skins.SameLook(skin, next)) { skin = next; Ui.Apply(skin); }

   var keep = Ui.Keep.Take(Content as DependencyObject);
   context = new WidgetContext {
    Skin = skin, State = state, Day = day,
    Toggle = (task, value) => { store.Update(s => s.SetDone(task.Id, day, value)); Refresh(true); },
    OpenPlan = () => Launch.App("Kora.exe"),
    OpenMenu = OpenMenu,
    Grip = element => { if (host != null) host.Attach(element); else Ui.Drag(this, element); }
   };
   var root = skin.BuildWidget(context);
   var tasks = context.Tasks;
   Width = skin.WidgetWidth + 24;
   Height = Math.Min(SystemParameters.WorkArea.Height - 24, skin.WidgetHeight(tasks.Count, tasks.Select(t => t.Group).Distinct().Count()) + 24);
   Content = Ui.Frame(root, skin.WidgetRadius, skin.WidgetFill, skin.WidgetBorder, 24, skin.WidgetShadow);
   keep.Restore(root, Dispatcher, true);
   if (host != null && (old.AlwaysOnTop != state.Settings.AlwaysOnTop || !host.IsAt(state.Settings))) host.Apply(state.Settings);
   if (host != null && context.Status != null) context.Status.Text = host.Status;
  }
 }

 public static class WidgetProgram {
  static string Arg(string[] args, string name) {
   string prefix = "--" + name + "=";
   return args.Where(a => a.StartsWith(prefix)).Select(a => a.Substring(prefix.Length)).FirstOrDefault();
  }

  [STAThread] public static int Main(string[] args) {
   bool preview = args.Contains("--preview");
   Mutex mutex = null;
   try {
    bool first;
    mutex = new Mutex(true, Activation.Name("Widget"), out first);
    if (!first && !preview) {
     Activation.WakeStarting("Widget");
     return 0;
    }
    Skins.ForcedTheme = Arg(args, "theme");
    Skins.ForcedAppearance = Arg(args, "appearance");
    var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
    Ui.Apply(Skins.Resolve(null));
    var win = new Widget(new Store(), preview);
    if (preview) win.Loaded += delegate {
     win.Dispatcher.BeginInvoke(new Action(() => {
      string file = Arg(args, "out") ?? Path.Combine(Launch.BaseDirectory, "widget.png");
      Ui.Snapshot(win, file);
      win.Close();
     }), DispatcherPriority.ApplicationIdle);
    };
    app.Run(win);
    return 0;
   } catch (Exception e) {
    if (preview) { File.WriteAllText(Path.Combine(Launch.BaseDirectory, "preview-error.txt"), e.ToString()); return 1; }
    MessageBox.Show(e.Message, "Kora Desk · 组件无法启动", MessageBoxButton.OK, MessageBoxImage.Warning);
    return 1;
   } finally { if (mutex != null) mutex.Dispose(); }
  }
 }
}
