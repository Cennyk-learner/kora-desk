using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Kora {
 // Colours are hex strings so a skin's palette can be written down as plain data.
 public sealed class Palette {
  public string Bg, Panel, Card, CardLine, Text, Sub, Faint, Accent, OnAccent, Line, Hover, Track, Danger, Thumb, Input, InputLine;
 }

 public sealed class SkinInfo {
  public string Id, Name, Blurb;
  public string[] Swatch;
 }

 // A skin owns the whole look: window chrome, the daily page and the desktop widget.
 // The weekly plan and preferences pages are shared and take their tokens from the skin.
 public abstract class Skin {
  static Skin current;
  public static Skin Current { get { return current ?? (current = Skins.Create(Skins.DefaultId, false)); } set { current = value; } }

  public string Id, Name;
  public bool Dark;
  public Palette P;
  public FontFamily Body, Display, Mono;
  public double CardRadius = 12, InputRadius = 8, ButtonRadius = 8;
  public double WindowRadius = 12, WindowWidth = 900, WindowHeight = 640, ShadowBlur = 20, ShadowOpacity = 0.3;
  public string FrameFill, FrameBorder;
  public double WidgetWidth = 300, WidgetRadius = 20, WidgetShadow = 0.3;
  public string WidgetFill, WidgetBorder;
  public double TitleSize = 28;
  public FontWeight TitleWeight = FontWeights.SemiBold;
  public bool MonoKicker;

  public abstract string CheckXaml();
  // Skins whose widget keeps a fixed dark face can give it its own check box.
  public virtual string WidgetCheckXaml() { return CheckXaml(); }
  public abstract FrameworkElement BuildPlanner(PlannerContext c);
  public abstract FrameworkElement BuildWidget(WidgetContext c);
  public abstract double WidgetHeight(int tasks, int groups);

  public virtual string SecondaryFill { get { return P.Hover; } }

  public Button Primary(string text, Action action) {
   return Ui.Btn(Ui.T(text, 13, P.OnAccent, FontWeights.SemiBold), action, P.Accent, P.OnAccent, ButtonRadius, new Thickness(16, 9, 16, 9));
  }
  public Button Secondary(string text, Action action) {
   return Ui.Btn(Ui.T(text, 13, P.Text), action, SecondaryFill, P.Text, ButtonRadius, new Thickness(14, 9, 14, 9), P.Line);
  }
  public UIElement Heading(string kicker, string title, string subtitle) {
   var stack = new StackPanel();
   var k = Ui.T(kicker, 11, P.Accent, FontWeights.SemiBold, MonoKicker ? Mono : Body);
   stack.Children.Add(k);
   var h = Ui.T(title, TitleSize, P.Text, TitleWeight, Display);
   h.Margin = new Thickness(0, 8, 0, 8);
   stack.Children.Add(h);
   var s = Ui.T(subtitle, 13, P.Sub);
   s.Margin = new Thickness(0, 0, 0, 16);
   stack.Children.Add(s);
   return stack;
  }
  public TextBlock Label(string text) {
   var t = Ui.T(text, 12, P.Sub, FontWeights.SemiBold, MonoKicker ? Mono : Body);
   t.Margin = new Thickness(2, 18, 0, 8);
   return t;
  }
 }

 public sealed class PlannerContext {
  public Skin Skin;
  public State State;
  public DateTime Selected;
  public int Page;
  public Window Window;
  public string DataFolder;
  public Action<int> Go;
  public Action<DateTime> Select;
  public Action Today;
  public Action<int> ShiftWeeks;
  public Action Add;
  public Action<TaskItem> Edit;
  public Action<TaskItem, bool> Toggle;
  public Action Minimize, Close;
  public Action<UIElement> Drag;
  public Action<string> SetTheme, SetAppearance;
  public Action<bool> SetAlwaysOnTop;
  public Action ShowWidget, ResetWidget, Export, Import, OpenFolder;

  public bool IsToday { get { return Selected.Date == DateTime.Today; } }
  public List<TaskItem> Tasks { get { return State.ForDay(Selected); } }
  public bool IsDone(TaskItem t) { return State.IsDone(t, Selected); }
  public int DoneCount { get { return Tasks.Count(IsDone); } }
  public string Headline { get { return IsToday ? "今天，慢慢做好。" : Dates.MonthDay(Selected) + "的安排"; } }
  public string Subline {
   get {
    var tasks = Tasks;
    return tasks.Count > 0 && DoneCount == tasks.Count ? "今天的清单已经完成。把剩下的时间留给自己。" : "不必一下完成所有事。从眼前这一件开始。";
   }
  }
  public string DateLine { get { return Dates.MonthDay(Selected) + " " + Dates.Long[(int)Selected.DayOfWeek]; } }
  public IEnumerable<IGrouping<string, TaskItem>> Groups { get { return Tasks.GroupBy(t => t.Group); } }
  public DateTime[] Week { get { var monday = Dates.Monday(Selected); return Enumerable.Range(0, 7).Select(i => monday.AddDays(i)).ToArray(); } }
  public string EmptyText { get { return "这一天留白。\n添加一件小事，或安心休息。"; } }
  public static string English(string group) { return group == "学习" ? "STUDY" : group == "健身" ? "MOVEMENT" : "LIFE"; }
  public static string Weekday(DateTime d) { return Dates.Short[(int)d.DayOfWeek]; }
 }

 public sealed class WidgetContext {
  public Skin Skin;
  public State State;
  public DateTime Day;
  public Action<TaskItem, bool> Toggle;
  public Action OpenPlan;
  public Action<FrameworkElement> OpenMenu;
  public Action<UIElement> Grip;
  public TextBlock Status;

  public List<TaskItem> Tasks { get { return State.ForDay(Day); } }
  public bool IsDone(TaskItem t) { return State.IsDone(t, Day); }
  public int DoneCount { get { return Tasks.Count(IsDone); } }
  public IEnumerable<IGrouping<string, TaskItem>> Groups { get { return Tasks.GroupBy(t => t.Group); } }
  public string Summary {
   get {
    int n = DoneCount, total = Tasks.Count;
    return n == total && n > 0 ? "全部完成  ·  今天辛苦了" : "已完成 " + n + " / " + total;
   }
  }
  public string EmptyText { get { return "今天留白，\n也很好。"; } }
 }

 public static class Skins {
  public const string DefaultId = "mac";
  public static string ForcedTheme, ForcedAppearance;
  static bool systemDark;
  static DateTime systemChecked;

  public static readonly SkinInfo[] Catalog = {
   new SkinInfo { Id = "mac", Name = "macOS", Blurb = "半透明侧栏、圆形复选框、分组圆角列表", Swatch = new[] { "#007AFF", "#F6F6F7", "#FFFFFF", "#1D1D1F" } },
   new SkinInfo { Id = "fluent", Name = "Fluent · Mica", Blurb = "Windows 11 的云母底与导航栏", Swatch = new[] { "#0F7B6C", "#EAEFF0", "#FBFBFB", "#1A1A1A" } },
   new SkinInfo { Id = "material", Name = "Material 3 Expressive", Blurb = "粗标题、波浪进度、分段清单", Swatch = new[] { "#2C6A4D", "#B4F1D0", "#FFDCC0", "#EBF1E8" } },
   new SkinInfo { Id = "carbon", Name = "IBM Carbon", Blurb = "数据块与表格，方角，信息密度高", Swatch = new[] { "#0F62FE", "#161616", "#262626", "#F4F4F4" } },
   new SkinInfo { Id = "dot", Name = "点阵仪表", Blurb = "点阵数字、圆点进度、单一红色信号", Swatch = new[] { "#E4212B", "#F2F2F0", "#101010", "#C9C9C6" } }
  };

  public static bool Known(string id) { return Catalog.Any(s => s.Id == id); }

  public static Skin Create(string id, bool dark) {
   switch (id) {
    case "fluent": return new FluentSkin(dark);
    case "material": return new MaterialSkin(dark);
    case "carbon": return new CarbonSkin(dark);
    case "dot": return new DotSkin(dark);
    default: return new MacSkin(dark);
   }
  }

  public static string ThemeOf(Preferences p) {
   string id = ForcedTheme ?? (p == null ? null : p.Theme);
   return Known(id) ? id : DefaultId;
  }
  public static string AppearanceOf(Preferences p) {
   string a = ForcedAppearance ?? (p == null ? null : p.Appearance);
   return a == "light" || a == "dark" ? a : "system";
  }
  public static Skin Resolve(Preferences p) {
   string appearance = AppearanceOf(p);
   bool dark = appearance == "dark" || (appearance == "system" && SystemDark());
   return Create(ThemeOf(p), dark);
  }

  public static bool SystemDark() {
   if ((DateTime.UtcNow - systemChecked).TotalSeconds < 2) return systemDark;
   systemChecked = DateTime.UtcNow;
   try {
    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) {
     object value = key == null ? null : key.GetValue("AppsUseLightTheme");
     systemDark = value is int && (int)value == 0;
    }
   } catch (Exception) { systemDark = false; }
   return systemDark;
  }

  public static bool SameLook(Skin a, Skin b) { return a != null && b != null && a.Id == b.Id && a.Dark == b.Dark; }
 }

 // Weekly plan and preferences. These look the same in every skin apart from tokens.
 public static class Pages {
  public static UIElement Plan(PlannerContext c) {
   var s = c.Skin;
   var root = new StackPanel();
   root.Children.Add(s.Heading("A WEEK WITH INTENTION", "让每周，自有节奏。", "每天重复，或每周轮换。所有安排都可以随时调整。"));
   var add = s.Primary("＋  添加任务", c.Add);
   add.HorizontalAlignment = HorizontalAlignment.Left;
   add.Margin = new Thickness(0, 0, 0, 6);
   root.Children.Add(add);
   foreach (var g in c.State.Tasks.GroupBy(t => t.Group)) {
    root.Children.Add(s.Label(g.Key + "  ·  " + g.Count() + " 项安排"));
    var rows = new StackPanel();
    bool first = true;
    foreach (var t in g) {
     var task = t;
     if (!first) rows.Children.Add(Ui.Rule(s.P.Line, 1, new Thickness(0)));
     first = false;
     var row = new Grid { Margin = new Thickness(0, 8, 0, 8) };
     row.ColumnDefinitions.Add(new ColumnDefinition());
     row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
     var copy = new StackPanel { Margin = new Thickness(0, 2, 12, 2) };
     copy.Children.Add(Ui.T(task.Title, 15, s.P.Text));
     var freq = Ui.T(task.Frequency, 12, s.P.Sub);
     freq.Margin = new Thickness(0, 4, 0, 0);
     copy.Children.Add(freq);
     row.Children.Add(copy);
     var edit = s.Secondary("编辑", () => c.Edit(task));
     edit.VerticalAlignment = VerticalAlignment.Center;
     Grid.SetColumn(edit, 1);
     row.Children.Add(edit);
     rows.Children.Add(row);
    }
    root.Children.Add(Ui.Card(rows, new Thickness(20, 6, 14, 6)));
   }
   return root;
  }

  static UIElement Section(Skin s, string title, string desc, UIElement body) {
   var box = new StackPanel();
   box.Children.Add(Ui.T(title, 17, s.P.Text, FontWeights.SemiBold));
   if (desc != null) {
    var d = Ui.T(desc, 13, s.P.Sub);
    d.Margin = new Thickness(0, 8, 0, 0);
    box.Children.Add(d);
   }
   var b = body as FrameworkElement;
   if (b != null) b.Margin = new Thickness(b.Margin.Left, 16, b.Margin.Right, b.Margin.Bottom);
   box.Children.Add(body);
   var card = Ui.Card(box, new Thickness(24));
   card.Margin = new Thickness(0, 0, 0, 14);
   return card;
  }

  public static UIElement Settings(PlannerContext c) {
   var s = c.Skin;
   var st = c.State.Settings;
   var root = new StackPanel();
   root.Children.Add(s.Heading("MAKE ROOM FOR YOUR DAY", "轻一点，刚刚好。", "桌面组件只保留当天清单。复杂的安排，留在这里。"));

   var tiles = new WrapPanel();
   string current = Skins.ThemeOf(st);
   foreach (var info in Skins.Catalog) {
    var id = info.Id;
    var swatch = new Grid { Height = 18, Margin = new Thickness(0, 0, 0, 10) };
    for (int i = 0; i < info.Swatch.Length; i++) {
     swatch.ColumnDefinitions.Add(new ColumnDefinition());
     var cell = new Border { Background = Ui.Br(info.Swatch[i]) };
     Grid.SetColumn(cell, i);
     swatch.Children.Add(cell);
    }
    var frame = new Border { CornerRadius = new CornerRadius(4), ClipToBounds = true, Child = swatch, BorderBrush = Ui.Br(s.P.Line), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 10) };
    var body = new StackPanel { Width = 150 };
    body.Children.Add(frame);
    body.Children.Add(Ui.T(info.Name, 13, s.P.Text, FontWeights.SemiBold));
    var blurb = Ui.T(info.Blurb, 11, s.P.Sub);
    blurb.Margin = new Thickness(0, 3, 0, 0);
    body.Children.Add(blurb);
    bool on = id == current;
    var tile = Ui.Btn(body, () => c.SetTheme(id), on ? s.P.Hover : "transparent", s.P.Text, Math.Min(s.CardRadius, 12), new Thickness(12, 12, 12, 12), s.P.Hover, info.Name);
    tile.HorizontalContentAlignment = HorizontalAlignment.Left;
    tile.VerticalContentAlignment = VerticalAlignment.Top;
    tile.Margin = new Thickness(0, 0, 8, 8);
    tile.BorderBrush = Ui.Br(on ? s.P.Accent : "transparent");
    tile.BorderThickness = new Thickness(on ? 2 : 0);
    if (on) tile.Padding = new Thickness(10);
    tiles.Children.Add(tile);
   }
   root.Children.Add(Section(s, "界面风格", "五种风格共用同一份数据，随时切换，管理窗口和桌面组件会一起更换。", tiles));

   string appearance = Skins.AppearanceOf(st);
   var modes = new[] { "system", "light", "dark" };
   var seg = Controls.Segmented(new[] { "跟随系统", "浅色", "深色" }, Array.IndexOf(modes, appearance), i => c.SetAppearance(modes[i]));
   seg.HorizontalAlignment = HorizontalAlignment.Left;
   root.Children.Add(Section(s, "外观", "跟随系统时，会在 Windows 切换浅色或深色后自动更新。", seg));

   var widget = new StackPanel();
   var top = new Grid { Margin = new Thickness(0, 0, 0, 16) };
   top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   top.ColumnDefinitions.Add(new ColumnDefinition());
   var box = Ui.Check(st.AlwaysOnTop, "始终置顶", v => c.SetAlwaysOnTop(v));
   top.Children.Add(box);
   var label = Ui.T("始终置顶", 14, s.P.Text);
   label.Margin = new Thickness(10, 0, 0, 0);
   label.VerticalAlignment = VerticalAlignment.Center;
   Grid.SetColumn(label, 1);
   top.Children.Add(label);
   Ui.RowClick(top, box);
   widget.Children.Add(top);
   var actions = new WrapPanel();
   var show = s.Primary("显示桌面组件", c.ShowWidget);
   show.Margin = new Thickness(0, 0, 8, 0);
   actions.Children.Add(show);
   actions.Children.Add(s.Secondary("重置组件位置", c.ResetWidget));
   widget.Children.Add(actions);
   root.Children.Add(Section(s, "桌面组件", "默认留在桌面层，其他应用可以盖住它。\n打开置顶后，组件会显示在其他窗口之上。", widget));

   var data = new WrapPanel();
   var export = s.Secondary("导出备份", c.Export);
   export.Margin = new Thickness(0, 0, 8, 0);
   data.Children.Add(export);
   var import = s.Secondary("导入备份", c.Import);
   import.Margin = new Thickness(0, 0, 8, 0);
   data.Children.Add(import);
   data.Children.Add(s.Secondary("打开数据文件夹", c.OpenFolder));
   root.Children.Add(Section(s, "属于你的记录", "任务与完成记录仅保存在这台电脑。无需账号，没有广告，应用不连接网络。自动保留上一次保存的备份。", data));

   var about = Ui.T("Kora Desk · 刻序\nv1.1.1  /  一点一点，也是向前。\n拖动组件顶部可移动位置；右键托盘图标可隐藏组件或退出。", 12, s.P.Faint);
   about.Margin = new Thickness(3, 10, 0, 0);
   root.Children.Add(about);
   return root;
  }
 }
}
