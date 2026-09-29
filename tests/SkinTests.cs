#if WPF
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Kora;

// Builds every skin in light and dark, on every page, and lays it out at the real window size.
// This catches XAML parse errors, elements added twice and empty layouts. It cannot judge how a skin looks;
// use `Kora.exe --preview --theme=<id> --appearance=<light|dark>` for that.
static class SkinTests {
 public static void Run(Action<bool, string> check) {
  Exception failure = null;
  var thread = new Thread(delegate() { try { RunSta(check); } catch (Exception e) { failure = e; } });
  thread.SetApartmentState(ApartmentState.STA);
  thread.Start();
  thread.Join();
  if (failure != null) throw new Exception("FAIL: skins did not build: " + failure);
 }

 static void Layout(FrameworkElement root, double width, double height) {
  root.Measure(new Size(width, height));
  root.Arrange(new Rect(0, 0, width, height));
  root.UpdateLayout();
 }

 static ScrollViewer FindScroll(DependencyObject node) {
  var sv = node as ScrollViewer;
  if (sv != null && sv.Name == "PageScroll") return sv;
  int count = VisualTreeHelper.GetChildrenCount(node);
  for (int i = 0; i < count; i++) {
   var found = FindScroll(VisualTreeHelper.GetChild(node, i));
   if (found != null) return found;
  }
  return null;
 }

 static void RunSta(Action<bool, string> check) {
  var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
  string dir = Path.Combine(Path.GetTempPath(), "KoraSkinTests-" + Guid.NewGuid().ToString("N"));
  var store = new Store(dir);
  var full = Preset.Create();
  var empty = new State();
  var monday = new DateTime(2026, 9, 28);
  full.SetDone(full.Tasks[0].Id, monday, true);

  foreach (var info in Skins.Catalog) {
   foreach (bool dark in new[] { false, true }) {
    var skin = Skins.Create(info.Id, dark);
    Ui.Apply(skin);
    string label = info.Id + (dark ? " dark" : " light");

    foreach (var state in new[] { full, empty }) {
     for (int page = 0; page < 3; page++) {
      var ctx = new PlannerContext {
       Skin = skin, State = state, Selected = monday, Page = page, DataFolder = dir,
       Go = i => { }, Select = d => { }, Today = () => { }, ShiftWeeks = n => { }, Add = () => { }, Edit = t => { }, Toggle = (t, v) => { },
       Minimize = () => { }, Close = () => { }, Drag = e => { }, SetTheme = id => { }, SetAppearance = a => { }, SetAlwaysOnTop = v => { },
       ShowWidget = () => { }, ResetWidget = () => { }, Export = () => { }, Import = () => { }, OpenFolder = () => { }
      };
      var root = skin.BuildPlanner(ctx);
      Layout(root, skin.WindowWidth, skin.WindowHeight);
      check(root.ActualWidth > 0 && root.ActualHeight > 0, "Planner lays out: " + label + ", page " + page + (state == empty ? ", no tasks" : ""));
      if (page == 0 && state == full) {
       var sv = FindScroll(root);
       if (sv != null && sv.ExtentHeight > sv.ViewportHeight + 1) Console.WriteLine("NOTE: " + label + " daily page scrolls on a five-task day (" + Math.Round(sv.ExtentHeight) + " > " + Math.Round(sv.ViewportHeight) + ")");
      }
     }
     var wctx = new WidgetContext { Skin = skin, State = state, Day = monday, Toggle = (t, v) => { }, OpenPlan = () => { }, OpenMenu = e => { }, Grip = e => { } };
     var widget = skin.BuildWidget(wctx);
     var tasks = wctx.Tasks;
     double wh = skin.WidgetHeight(tasks.Count, tasks.Select(t => t.Group).Distinct().Count());
     Layout(widget, skin.WidgetWidth, wh);
     check(widget.ActualWidth > 0 && widget.ActualHeight > 0, "Widget lays out: " + label + (state == empty ? ", no tasks" : ""));
     var wsv = FindScroll(widget);
     if (wsv != null && state == full && wsv.ExtentHeight > wsv.ViewportHeight + 1) Console.WriteLine("NOTE: " + label + " widget scrolls on a five-task day (" + Math.Round(wsv.ExtentHeight) + " > " + Math.Round(wsv.ViewportHeight) + ")");
    }

    var editor = new Editor(null, store, full.Tasks[0], monday);
    var blank = new Editor(null, store, null, monday);
    var dialog = new Dialog(null, "标题", "内容", "好", "取消");
    check(editor != null && blank != null && dialog != null, "Editor and dialog build: " + label);
    var field = new Controls.DateField(monday);
    check(field != null && Controls.Segmented(new[] { "a", "b" }, 0, null) != null, "Controls build: " + label);
   }
  }
  try { app.Shutdown(); } catch (InvalidOperationException) { }
 }
}
#endif
