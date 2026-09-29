using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kora {
 // Sonoma-style: tinted sidebar, Reminders-like large title, round check boxes, grouped rounded lists.
 public sealed class MacSkin : Skin {
  public MacSkin(bool dark) {
   Id = "mac"; Name = "macOS"; Dark = dark;
   Body = Ui.Font("SF Pro Text, Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI");
   Display = Body; Mono = Body;
   P = dark ? new Palette {
    Bg = "#1E1E20", Panel = "#2C2C30", Card = "#2A2A2C", CardLine = "#12FFFFFF", Text = "#F5F5F7", Sub = "#99EBEBF5", Faint = "#4DEBEBF5", Accent = "#0A84FF", OnAccent = "#FFFFFF",
    Line = "#17FFFFFF", Hover = "#1AFFFFFF", Track = "#1FFFFFFF", Danger = "#FF453A", Thumb = "#4DFFFFFF", Input = "#323234", InputLine = "#26FFFFFF"
   } : new Palette {
    Bg = "#F6F6F7", Panel = "#E8E8EC", Card = "#FFFFFF", CardLine = "#12000000", Text = "#1D1D1F", Sub = "#9E3C3C43", Faint = "#573C3C43", Accent = "#007AFF", OnAccent = "#FFFFFF",
    Line = "#14000000", Hover = "#12000000", Track = "#14000000", Danger = "#FF3B30", Thumb = "#4D000000", Input = "#FFFFFF", InputLine = "#1F000000"
   };
   CardRadius = 10; InputRadius = 7; ButtonRadius = 7; WindowRadius = 12;
   TitleSize = 28; TitleWeight = FontWeights.Bold;
   FrameFill = P.Bg; FrameBorder = dark ? "#48484C" : "#BDBDC2"; ShadowBlur = 20; ShadowOpacity = dark ? 0.55 : 0.32;
   WidgetRadius = 22; WidgetFill = dark ? "#EB2C2C30" : "#EBFAFAFC"; WidgetBorder = dark ? "#24FFFFFF" : "#99FFFFFF"; WidgetShadow = dark ? 0.5 : 0.28;
  }

  public override string CheckXaml() { return Xaml.CheckStyle(18, 9, 1.5, P.Faint, P.Accent, P.Accent, "#FFFFFF", null, P.Accent); }
  public override double WidgetHeight(int tasks, int groups) { return Math.Max(220, 160 + groups * 27 + tasks * 33); }

  public override FrameworkElement BuildPlanner(PlannerContext c) {
   var root = Ui.Cols("208", "*");
   root.Children.Add(Sidebar(c));
   var main = Ui.Rows("52", "*");
   main.Background = Ui.Br(P.Bg);
   Grid.SetColumn(main, 1);

   var bar = Ui.Cols("*", "auto", "auto");
   bar.Background = Brushes.Transparent;
   bar.Margin = new Thickness(0, 0, 16, 0);
   c.Drag(bar);
   if (c.Page == 0) {
    var seg = new StackPanel { Orientation = Orientation.Horizontal };
    seg.Children.Add(Ui.Btn(Ui.Icon("left", 12, P.Text, 2.4), () => c.ShiftWeeks(-1), "transparent", P.Text, 6, new Thickness(9, 5, 9, 5), P.Hover, "上一周"));
    seg.Children.Add(Ui.Btn(Ui.T("今天", 12, P.Text), c.Today, P.Card, P.Text, 6, new Thickness(12, 4, 12, 4), null, "回到今天"));
    seg.Children.Add(Ui.Btn(Ui.Icon("right", 12, P.Text, 2.4), () => c.ShiftWeeks(1), "transparent", P.Text, 6, new Thickness(9, 5, 9, 5), P.Hover, "下一周"));
    var frame = new Border { Background = Ui.Br(Dark ? "#14FFFFFF" : "#0D000000"), CornerRadius = new CornerRadius(7), Padding = new Thickness(1), Child = seg, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
    bar.Children.Add(Ui.At(frame, 1));
   }
   if (c.Page != 2) bar.Children.Add(Ui.At(Ui.Btn(Ui.Icon("plus", 14, P.Accent, 2.2), c.Add, Dark ? "#14FFFFFF" : "#0D000000", P.Accent, 7, new Thickness(9, 6, 9, 6), P.Hover, "添加任务"), 2));
   main.Children.Add(bar);

   UIElement page = c.Page == 0 ? Daily(c) : c.Page == 1 ? Pages.Plan(c) : Pages.Settings(c);
   var holder = new Border { Padding = new Thickness(32, 0, 32, 28), Child = page };
   var scroll = Ui.Scroll(holder);
   Grid.SetRow(scroll, 1);
   main.Children.Add(scroll);
   root.Children.Add(main);
   return root;
  }

  UIElement Sidebar(PlannerContext c) {
   var side = Ui.Rows("*", "auto");
   var top = new StackPanel { Margin = new Thickness(10, 14, 10, 0), Background = Brushes.Transparent };
   c.Drag(top);
   var lights = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 20), Height = 24 };
   lights.Children.Add(Light("#FF5F57", c.Close, "关闭"));
   lights.Children.Add(Light("#FEBC2E", c.Minimize, "最小化"));
   var zoom = Ui.Dot(12, Dark ? "#4A4A4D" : "#C9C9CC");
   zoom.VerticalAlignment = VerticalAlignment.Center;
   lights.Children.Add(zoom);
   top.Children.Add(lights);
   var head = Ui.T("刻序", 11, P.Sub, FontWeights.SemiBold);
   head.Margin = new Thickness(10, 0, 0, 6);
   top.Children.Add(head);
   top.Children.Add(NavItem(c, 0, "today", "今天", "#007AFF", c.State.ForDay(DateTime.Today).Count(t => !c.State.IsDone(t, DateTime.Today)).ToString()));
   top.Children.Add(NavItem(c, 1, "week", "每周计划", "#FF453A", null));
   top.Children.Add(NavItem(c, 2, "settings", "偏好设置", "#8E8E93", null));
   side.Children.Add(top);

   var foot = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 8, 10, 16) };
   foot.Children.Add(Ui.Icon("lock", 12, P.Sub, 2));
   var note = Ui.T("只在本机，安心记录", 11, P.Sub);
   note.Margin = new Thickness(6, 0, 0, 0);
   foot.Children.Add(note);
   Ui.At(foot, 0, 1);
   side.Children.Add(foot);
   return new Border { Background = Ui.Br(P.Panel), BorderBrush = Ui.Br(Dark ? "#14FFFFFF" : "#17000000"), BorderThickness = new Thickness(0, 0, 1, 0), Child = side };
  }

  static Button Light(string color, Action action, string tip) {
   var b = Ui.Btn(null, action, color, "#000000", 6, new Thickness(0), null, tip);
   b.Width = 12; b.Height = 12;
   b.Margin = new Thickness(0, 0, 8, 0);
   return b;
  }

  UIElement NavItem(PlannerContext c, int page, string icon, string text, string tint, string badge) {
   var row = Ui.Cols("20", "*", "auto");
   var tile = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(5), Background = Ui.Br(tint), Child = Ui.Icon(icon, 12, "#FFFFFF", 2.2) };
   ((FrameworkElement)tile.Child).HorizontalAlignment = HorizontalAlignment.Center;
   ((FrameworkElement)tile.Child).VerticalAlignment = VerticalAlignment.Center;
   row.Children.Add(tile);
   var label = Ui.T(text, 13, P.Text);
   label.Margin = new Thickness(9, 0, 0, 0);
   label.VerticalAlignment = VerticalAlignment.Center;
   row.Children.Add(Ui.At(label, 1));
   if (badge != null && badge != "0") row.Children.Add(Ui.At(new TextBlock { Text = badge, FontSize = 12, Foreground = Ui.Br(P.Sub), VerticalAlignment = VerticalAlignment.Center, FontFamily = Body }, 2));
   var b = Ui.Btn(row, () => c.Go(page), c.Page == page ? P.Hover : "transparent", P.Text, 7, new Thickness(8, 4, 8, 4), P.Hover, text);
   b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
   b.Margin = new Thickness(0, 1, 0, 1);
   return b;
  }

  UIElement Daily(PlannerContext c) {
   var page = new StackPanel();
   var tasks = c.Tasks;
   int done = c.DoneCount;
   var titleRow = Ui.Cols("*", "auto");
   var left = new StackPanel();
   left.Children.Add(Ui.T(c.IsToday ? "今天" : Dates.MonthDay(c.Selected), 28, P.Accent, FontWeights.Bold));
   var sub = Ui.T(c.DateLine + "  ·  已完成 " + done + " 项", 13, P.Sub);
   sub.Margin = new Thickness(0, 4, 0, 0);
   left.Children.Add(sub);
   titleRow.Children.Add(left);
   var big = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
   var count = Ui.T((tasks.Count - done).ToString(), 40, P.Text, FontWeights.Bold);
   count.HorizontalAlignment = HorizontalAlignment.Right;
   big.Children.Add(count);
   var cap = Ui.T("项待办", 11, P.Sub);
   cap.HorizontalAlignment = HorizontalAlignment.Right;
   big.Children.Add(cap);
   titleRow.Children.Add(Ui.At(big, 1));
   page.Children.Add(titleRow);

   var week = Ui.Cols("*", "*", "*", "*", "*", "*", "*");
   var days = c.Week;
   for (int i = 0; i < days.Length; i++) {
    var d = days[i];
    bool on = d.Date == c.Selected.Date;
    var cell = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
    var name = Ui.T(Dates.Short[(int)d.DayOfWeek].Substring(1), 11, P.Sub);
    name.HorizontalAlignment = HorizontalAlignment.Center;
    cell.Children.Add(name);
    var circle = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(14), Margin = new Thickness(0, 3, 0, 0), Background = Ui.Br(on ? P.Accent : "transparent") };
    var num = Ui.T(Dates.Number(d), 15, on ? P.OnAccent : d.Date == DateTime.Today ? P.Accent : P.Text, FontWeights.Medium);
    num.HorizontalAlignment = HorizontalAlignment.Center;
    num.VerticalAlignment = VerticalAlignment.Center;
    circle.Child = num;
    cell.Children.Add(circle);
    var b = Ui.Btn(cell, () => c.Select(d), "transparent", P.Text, 8, new Thickness(0, 4, 0, 4), P.Hover, Dates.MonthDay(d) + " " + Dates.Long[(int)d.DayOfWeek]);
    week.Children.Add(Ui.At(b, i));
   }
   var weekCard = Ui.Card(week, new Thickness(6, 6, 6, 6));
   weekCard.Margin = new Thickness(0, 16, 0, 4);
   page.Children.Add(weekCard);

   foreach (var g in c.Groups) {
    var head = Ui.Cols("*", "auto");
    head.Margin = new Thickness(4, 16, 4, 6);
    head.Children.Add(Ui.T(g.Key, 13, P.Text, FontWeights.SemiBold));
    head.Children.Add(Ui.At(Ui.T(g.Count(c.IsDone) + " / " + g.Count(), 12, P.Sub), 1));
    page.Children.Add(head);
    var rows = new StackPanel();
    bool first = true;
    foreach (var t in g) {
     if (!first) rows.Children.Add(Ui.Rule(P.Line, 1, new Thickness(40, 0, 0, 0)));
     first = false;
     rows.Children.Add(Row(c, t));
    }
    page.Children.Add(Ui.Card(rows, new Thickness(0)));
   }
   if (tasks.Count == 0) {
    var empty = Ui.T(c.EmptyText, 16, P.Sub);
    empty.TextAlignment = TextAlignment.Center;
    empty.Margin = new Thickness(0, 44, 0, 30);
    page.Children.Add(empty);
   }
   return page;
  }

  UIElement Row(PlannerContext c, TaskItem t) {
   bool done = c.IsDone(t);
   var grid = Ui.Cols("auto", "*", "auto");
   var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
   box.VerticalAlignment = VerticalAlignment.Top;
   box.Margin = new Thickness(0, 1, 10, 0);
   box.Tag = "task:" + t.Id;
   grid.Children.Add(box);
   var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   copy.Children.Add(Ui.T(t.Title, 13, done ? P.Sub : P.Text));
   if (!string.IsNullOrWhiteSpace(t.Note)) { var n = Ui.T(t.Note, 11.5, P.Sub); n.Margin = new Thickness(0, 1, 0, 0); copy.Children.Add(n); }
   grid.Children.Add(Ui.At(copy, 1));
   var edit = Ui.Btn(Ui.Icon("dots", 16, P.Faint, 2.6), () => c.Edit(t), "transparent", P.Faint, 6, new Thickness(7, 4, 7, 4), P.Hover, "编辑任务");
   edit.VerticalAlignment = VerticalAlignment.Center;
   grid.Children.Add(Ui.At(edit, 2));
   var wrap = new Border { Padding = new Thickness(12, 9, 6, 9), Child = grid };
   Ui.RowClick(wrap, box);
   return wrap;
  }

  public override FrameworkElement BuildWidget(WidgetContext c) {
   var root = Ui.Rows("auto", "*", "auto");
   root.Margin = new Thickness(16, 16, 16, 10);
   var tasks = c.Tasks;
   var head = Ui.Cols("*", "auto");
   var grip = new StackPanel { Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.SizeAll };
   var titleLine = new StackPanel { Orientation = Orientation.Horizontal };
   var badge = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), Background = Ui.Br(P.Accent), Child = Ui.Icon("today", 12, "#FFFFFF", 2.4) };
   titleLine.Children.Add(badge);
   var t1 = Ui.T("今天", 15, P.Accent, FontWeights.Bold);
   t1.Margin = new Thickness(6, 0, 0, 0);
   titleLine.Children.Add(t1);
   grip.Children.Add(titleLine);
   var date = Ui.T(Dates.MonthDay(c.Day) + " " + Dates.Short[(int)c.Day.DayOfWeek], 11, P.Sub);
   date.Margin = new Thickness(0, 4, 0, 0);
   grip.Children.Add(date);
   head.Children.Add(grip);
   c.Grip(grip);
   head.Children.Add(Ui.At(Ui.T((tasks.Count - c.DoneCount).ToString(), 36, P.Text, FontWeights.Bold), 1));
   root.Children.Add(head);

   var list = new StackPanel();
   foreach (var g in c.Groups) {
    var label = Ui.T(g.Key, 11, P.Sub, FontWeights.SemiBold);
    label.Margin = new Thickness(0, 10, 0, 3);
    list.Children.Add(label);
    bool first = true;
    foreach (var t in g) {
     if (!first) list.Children.Add(Ui.Rule(P.Line, 1, new Thickness(27, 0, 0, 0)));
     first = false;
     bool done = c.IsDone(t);
     var row = Ui.Cols("auto", "*");
     row.Margin = new Thickness(0, 6, 0, 6);
     var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
     box.Margin = new Thickness(0, 0, 9, 0);
     box.Tag = "task:" + t.Id;
     row.Children.Add(box);
     var title = Ui.Line(t.Title, 13, done ? P.Sub : P.Text);
     title.VerticalAlignment = VerticalAlignment.Center;
     title.ToolTip = string.IsNullOrWhiteSpace(t.Note) ? null : t.Note;
     row.Children.Add(Ui.At(title, 1));
     Ui.RowClick(row, box);
     list.Children.Add(row);
    }
   }
   if (tasks.Count == 0) { var empty = Ui.T(c.EmptyText, 18, P.Sub); empty.Margin = new Thickness(0, 30, 0, 0); list.Children.Add(empty); }
   var scroll = Ui.Scroll(list);
   scroll.Margin = new Thickness(0, 4, -6, 0);
   Grid.SetRow(scroll, 1);
   root.Children.Add(scroll);

   var foot = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
   foot.Children.Add(Ui.Rule(P.Line, 1, new Thickness(0, 0, 0, 8)));
   var line = Ui.Cols("*", "auto");
   line.Children.Add(Ui.T(c.Summary, 11, P.Accent, FontWeights.Medium));
   line.Children.Add(Ui.At(Ui.Btn(Ui.T("打开计划", 11, P.Accent, FontWeights.Medium), c.OpenPlan, "transparent", P.Accent, 5, new Thickness(6, 2, 6, 2), P.Hover, "打开计划"), 1));
   foot.Children.Add(line);
   var low = Ui.Cols("*", "auto");
   c.Status = Ui.T("桌面组件", 10, P.Faint);
   c.Status.VerticalAlignment = VerticalAlignment.Center;
   low.Children.Add(c.Status);
   var dots = Ui.Btn(Ui.Icon("dots", 14, P.Sub, 2.6), null, "transparent", P.Sub, 5, new Thickness(6, 2, 6, 2), P.Hover, "组件选项");
   dots.Click += delegate { c.OpenMenu(dots); };
   low.Children.Add(Ui.At(dots, 1));
   foot.Children.Add(low);
   Grid.SetRow(foot, 2);
   root.Children.Add(foot);
   return root;
  }
 }
}
