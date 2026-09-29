using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Kora {
 public class Manager : Window {
  readonly Store store;
  readonly bool preview;
  State state;
  Skin skin;
  DateTime selected = DateTime.Today, lastToday = DateTime.Today, stamp;
  int page;
  DispatcherTimer timer;
  EventWaitHandle wake;

  public Manager(Store storage, bool isPreview, int startPage) {
   store = storage;
   preview = isPreview;
   page = startPage;
   state = store.Read();
   stamp = File.GetLastWriteTimeUtc(store.FilePath);
   Title = "Kora Desk · 刻序";
   ResizeMode = ResizeMode.CanMinimize;
   WindowStartupLocation = WindowStartupLocation.CenterScreen;
   Ui.Chromeless(this);
   Draw();

   wake = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\KoraDesk.Manager.Wake");
   timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
   timer.Tick += delegate { Ui.Guard(Tick); };
   timer.Start();
   Closed += delegate { timer.Stop(); wake.Dispose(); };
   if (!preview) Loaded += delegate {
    Ui.Guard(() => Launch.App("Kora.Widget.exe"));
    if (store.RecoveryNotice != null) Ui.Tell(this, "数据恢复", store.RecoveryNotice);
   };
  }

  void Tick() {
   if (wake.WaitOne(0)) { Show(); WindowState = WindowState.Normal; Activate(); }
   var current = File.GetLastWriteTimeUtc(store.FilePath);
   if (current != stamp) { state = store.Read(); stamp = current; Draw(); return; }
   if (DateTime.Today != lastToday) {
    bool following = selected.Date == lastToday;
    lastToday = DateTime.Today;
    if (following) selected = DateTime.Today;
    Draw();
    return;
   }
   if (Skins.AppearanceOf(state.Settings) == "system" && Skins.SystemDark() != skin.Dark) Draw();
  }

  void Reload() {
   state = store.Read();
   stamp = File.GetLastWriteTimeUtc(store.FilePath);
   Draw();
  }

  void Edit(TaskItem task) {
   new Editor(this, store, task, selected).ShowDialog();
   Reload();
  }

  void Draw() {
   var next = Skins.Resolve(state.Settings);
   if (!Skins.SameLook(skin, next)) { skin = next; Ui.Apply(skin); }
   var keep = Ui.Keep.Take(Content as DependencyObject);
   var root = skin.BuildPlanner(Context());
   Width = skin.WindowWidth + 24;
   Height = Math.Min(skin.WindowHeight + 24, SystemParameters.WorkArea.Height);
   Content = Ui.Frame(root, skin.WindowRadius, skin.FrameFill, skin.FrameBorder, skin.ShadowBlur, skin.ShadowOpacity);
   keep.Restore(root, Dispatcher, true);
  }

  PlannerContext Context() {
   return new PlannerContext {
    Skin = skin, State = state, Selected = selected, Page = page, Window = this, DataFolder = store.Folder,
    Go = i => { page = i; Draw(); },
    Select = d => { selected = d.Date; Draw(); },
    Today = () => { selected = DateTime.Today; Draw(); },
    ShiftWeeks = n => { selected = selected.AddDays(7 * n); Draw(); },
    Add = () => Edit(null),
    Edit = Edit,
    Toggle = (task, value) => { var day = selected; store.Update(s => s.SetDone(task.Id, day, value)); Reload(); },
    Minimize = () => WindowState = WindowState.Minimized,
    Close = Close,
    Drag = element => Ui.Drag(this, element),
    SetTheme = id => { store.Update(s => s.Settings.Theme = id); Reload(); },
    SetAppearance = a => { store.Update(s => s.Settings.Appearance = a); Reload(); },
    SetAlwaysOnTop = v => { store.Update(s => s.Settings.AlwaysOnTop = v); Reload(); },
    ShowWidget = () => Launch.App("Kora.Widget.exe"),
    ResetWidget = () => { store.Update(s => { s.Settings.X = -9999; s.Settings.Y = -9999; }); Launch.App("Kora.Widget.exe"); Reload(); },
    Export = Export,
    Import = Import,
    OpenFolder = () => Launch.Folder(store.Folder)
   };
  }

  void Export() {
   var dialog = new SaveFileDialog { Filter = "Kora 备份 (*.json)|*.json", FileName = "kora-backup-" + Dates.Key(DateTime.Today) + ".json" };
   if (dialog.ShowDialog(this) == true) {
    store.Export(dialog.FileName);
    Ui.Tell(this, "Kora Desk", "备份已导出。");
   }
  }

  void Import() {
   var dialog = new OpenFileDialog { Filter = "Kora 备份 (*.json)|*.json" };
   if (dialog.ShowDialog(this) != true) return;
   Store.Decode(File.ReadAllBytes(dialog.FileName));
   if (!Ui.Ask(this, "导入备份", "用这个备份替换现有任务和完成记录？当前数据将保留为自动备份。", "替换", "取消")) return;
   store.Import(dialog.FileName);
   Reload();
  }
 }

 public static class Program {
  static string Arg(string[] args, string name) {
   string prefix = "--" + name + "=";
   return args.Where(a => a.StartsWith(prefix)).Select(a => a.Substring(prefix.Length)).FirstOrDefault();
  }

  [STAThread] public static int Main(string[] args) {
   bool preview = args.Contains("--preview");
   Mutex mutex = null;
   try {
    bool first;
    mutex = new Mutex(true, "Local\\KoraDesk.Manager", out first);
    if (!first && !preview) {
     using (var e = EventWaitHandle.OpenExisting("Local\\KoraDesk.Manager.Wake")) e.Set();
     return 0;
    }
    Skins.ForcedTheme = Arg(args, "theme");
    Skins.ForcedAppearance = Arg(args, "appearance");
    int startPage;
    int.TryParse(Arg(args, "page"), out startPage);
    var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
    Ui.Apply(Skins.Resolve(null));
    var win = new Manager(new Store(), preview, Math.Max(0, Math.Min(2, startPage)));
    if (preview) win.Loaded += delegate {
     win.Dispatcher.BeginInvoke(new Action(() => {
      string file = Arg(args, "out") ?? Path.Combine(Launch.BaseDirectory, "manager.png");
      Ui.Snapshot(win, file);
      win.Close();
     }), DispatcherPriority.ApplicationIdle);
    };
    app.Run(win);
    return 0;
   } catch (Exception e) {
    if (preview) { File.WriteAllText(Path.Combine(Launch.BaseDirectory, "preview-error.txt"), e.ToString()); return 1; }
    MessageBox.Show(e.Message, "Kora Desk · 无法启动", MessageBoxButton.OK, MessageBoxImage.Warning);
    return 1;
   } finally { if (mutex != null) mutex.Dispose(); }
  }
 }
}
