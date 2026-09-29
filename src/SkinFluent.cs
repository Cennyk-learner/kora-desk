using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kora {
 // Windows 11: Mica base, a rounded content layer, a navigation pane with settings pinned to the bottom.
 public sealed class FluentSkin : Skin {
  readonly string select, strong;

  public FluentSkin(bool dark) {
   Id = "fluent"; Name = "Fluent · Mica"; Dark = dark;
   Body = Ui.Font("Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI");
   Display = Ui.Font("Segoe UI Variable Display, Segoe UI, Microsoft YaHei UI");
   Mono = Body;
   P = dark ? new Palette {
    Bg = "#282828", Panel = "#202323", Card = "#323232", CardLine = "#17FFFFFF", Text = "#FFFFFF", Sub = "#B8FFFFFF", Faint = "#75FFFFFF", Accent = "#4CC9B0", OnAccent = "#04211C",
    Line = "#17FFFFFF", Hover = "#0FFFFFFF", Track = "#17FFFFFF", Danger = "#FF99A4", Thumb = "#66FFFFFF", Input = "#323232", InputLine = "#29FFFFFF"
   } : new Palette {
    Bg = "#FBFBFB", Panel = "#EAEFF0", Card = "#FFFFFF", CardLine = "#17000000", Text = "#1A1A1A", Sub = "#5F5F5F", Faint = "#8B8B8B", Accent = "#0F7B6C", OnAccent = "#FFFFFF",
    Line = "#17000000", Hover = "#0B000000", Track = "#12000000", Danger = "#C42B1C", Thumb = "#66000000", Input = "#FFFFFF", InputLine = "#29000000"
   };
   select = dark ? "#17FFFFFF" : "#12000000";
   strong = dark ? "#9EFFFFFF" : "#8F000000";
   CardRadius = 8; InputRadius = 4; ButtonRadius = 4; WindowRadius = 8;
   TitleSize = 28; TitleWeight = FontWeights.SemiBold;
   FrameFill = P.Panel; FrameBorder = dark ? "#40FFFFFF" : "#40000000"; ShadowBlur = 20; ShadowOpacity = dark ? 0.55 : 0.34;
   WidgetRadius = 8; WidgetFill = dark ? "#F2282828" : "#F2FCFCFC"; WidgetBorder = dark ? "#1FFFFFFF" : "#80FFFFFF"; WidgetShadow = dark ? 0.5 : 0.3;
  }

  public override string CheckXaml() { return Xaml.CheckStyle(20, 4, 1, strong, P.Accent, P.Accent, P.OnAccent, null, P.Accent); }
  public override double WidgetHeight(int tasks, int groups) { return Math.Max(250, 230 + groups * 26 + tasks * 37); }

  public override FrameworkElement BuildPlanner(PlannerContext c) {
   var root = Ui.Rows("32", "*");
   root.Background = Ui.Br(P.Panel);

   var title = Ui.Cols("auto", "auto", "*", "auto");
   title.Background = Brushes.Transparent;
   c.Drag(title);
   var tile = AppTile();
   tile.Margin = new Thickness(12, 0, 9, 0);
   title.Children.Add(tile);
   var name = Ui.T("刻序", 12, P.Text);
   name.VerticalAlignment = VerticalAlignment.Center;
   title.Children.Add(Ui.At(name, 1));
   var caption = new StackPanel { Orientation = Orientation.Horizontal };
   caption.Children.Add(Caption("min", c.Minimize, "最小化", null));
   caption.Children.Add(Caption("close", c.Close, "关闭", "#C42B1C"));
   title.Children.Add(Ui.At(caption, 3));
   root.Children.Add(title);

   var body = Ui.Cols("236", "*");
   Grid.SetRow(body, 1);
   var nav = Ui.Rows("auto", "*", "auto");
   nav.Margin = new Thickness(8, 6, 8, 10);
   var upper = new StackPanel();
   upper.Children.Add(NavItem(c, 0, "today", "每日清单"));
   var plan = NavItem(c, 1, "week", "每周计划");
   ((FrameworkElement)plan).Margin = new Thickness(0, 2, 0, 0);
   upper.Children.Add(plan);
   nav.Children.Add(upper);
   nav.Children.Add(Ui.At(NavItem(c, 2, "settings", "偏好设置"), 0, 2));
   body.Children.Add(nav);

   UIElement page = c.Page == 0 ? Daily(c) : c.Page == 1 ? Pages.Plan(c) : Pages.Settings(c);
   var holder = new Border { Padding = new Thickness(32, 22, 32, 24), Child = page };
   var layer = new Border { Background = Ui.Br(P.Bg), BorderBrush = Ui.Br(P.CardLine), BorderThickness = new Thickness(1, 1, 0, 0), CornerRadius = new CornerRadius(8, 0, 0, 0), Child = Ui.Scroll(holder) };
   Grid.SetColumn(layer, 1);
   body.Children.Add(layer);
   root.Children.Add(body);
   return root;
  }

  Border AppTile() {
   var t = Ui.T("k", 11, P.OnAccent, FontWeights.Bold);
   t.HorizontalAlignment = HorizontalAlignment.Center;
   t.VerticalAlignment = VerticalAlignment.Center;
   return new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(4), Background = Ui.Br(P.Accent), Child = t, VerticalAlignment = VerticalAlignment.Center };
  }

  Button Caption(string icon, Action action, string tip, string hoverFill) {
   var b = Ui.Btn(Ui.Icon(icon, 12, P.Text, 1.5), action, "transparent", P.Text, 0, new Thickness(0), hoverFill ?? P.Hover, tip);
   b.Width = 46; b.Height = 32;
   return b;
  }

  UIElement NavItem(PlannerContext c, int page, string icon, string text) {
   bool on = c.Page == page;
   var row = Ui.Cols("17", "*");
   row.Children.Add(Ui.Icon(icon, 17, P.Text, 1.6));
   var label = Ui.T(text, 14, P.Text);
   label.Margin = new Thickness(13, 0, 0, 0);
   label.VerticalAlignment = VerticalAlignment.Center;
   row.Children.Add(Ui.At(label, 1));
   var b = Ui.Btn(row, () => c.Go(page), on ? select : "transparent", P.Text, 5, new Thickness(12, 0, 12, 0), on ? select : P.Hover, text);
   b.Height = 38;
   b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
   var wrap = new Grid();
   wrap.Children.Add(b);
   if (on) wrap.Children.Add(new Border { Width = 3, Height = 16, CornerRadius = new CornerRadius(2), Background = Ui.Br(P.Accent), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false });
   return wrap;
  }

  Border Outlined(object content, Action action, string tip) {
   var b = Ui.Btn(content, action, "transparent", P.Text, 4, new Thickness(12, 0, 12, 0), P.Hover, tip);
   b.Height = 32;
   return new Border { BorderBrush = Ui.Br(P.CardLine), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Background = Ui.Br(P.Card), Child = b, Margin = new Thickness(6, 0, 0, 0) };
  }

  UIElement Daily(PlannerContext c) {
   var page = new StackPanel();
   var tasks = c.Tasks;
   int done = c.DoneCount;
   double p = tasks.Count == 0 ? 0 : (double)done / tasks.Count;

   var head = Ui.Cols("*", "auto");
   var left = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
   left.Children.Add(Ui.T("每日清单", 28, P.Text, FontWeights.SemiBold, Display));
   var date = Ui.T(Dates.MonthDay(c.Selected) + " · " + Dates.Short[(int)c.Selected.DayOfWeek], 13, P.Sub);
   date.Margin = new Thickness(0, 3, 0, 0);
   left.Children.Add(date);
   head.Children.Add(left);
   var cmd = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
   cmd.Children.Add(Outlined(IconText("left", "上周"), () => c.ShiftWeeks(-1), "上一周"));
   cmd.Children.Add(Outlined(Ui.T("今天", 13, P.Text), c.Today, "回到今天"));
   cmd.Children.Add(Outlined(IconText("right", "下周"), () => c.ShiftWeeks(1), "下一周"));
   var add = Ui.Btn(Ui.T("＋  添加任务", 13, P.OnAccent, FontWeights.SemiBold), c.Add, P.Accent, P.OnAccent, 4, new Thickness(14, 0, 14, 0), null, "添加任务");
   add.Height = 32;
   add.Margin = new Thickness(6, 0, 0, 0);
   cmd.Children.Add(add);
   head.Children.Add(Ui.At(cmd, 1));
   page.Children.Add(head);

   var summary = Ui.Cols("auto", "*");
   var ring = new Grid { Width = 44, Height = 44, Margin = new Thickness(0, 0, 16, 0) };
   ring.Children.Add(Ui.Ring(44, 5, p, P.Track, P.Accent));
   var ratio = Ui.T(done + "/" + tasks.Count, 12, P.Text, FontWeights.SemiBold);
   ratio.HorizontalAlignment = HorizontalAlignment.Center;
   ratio.VerticalAlignment = VerticalAlignment.Center;
   ring.Children.Add(ratio);
   summary.Children.Add(ring);
   var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   text.Children.Add(Ui.T(c.IsToday ? c.Headline : "已完成 " + done + " / " + tasks.Count, 17, P.Text, FontWeights.SemiBold));
   text.Children.Add(Ui.T(c.Subline, 12.5, P.Sub));
   summary.Children.Add(Ui.At(text, 1));
   var summaryCard = Ui.Card(summary, new Thickness(18, 10, 18, 10));
   summaryCard.Margin = new Thickness(0, 16, 0, 12);
   page.Children.Add(summaryCard);

   var week = Ui.Cols("*", "*", "*", "*", "*", "*", "*");
   var days = c.Week;
   for (int i = 0; i < days.Length; i++) {
    var d = days[i];
    bool on = d.Date == c.Selected.Date;
    var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    var wd = Ui.T(Dates.Short[(int)d.DayOfWeek], 12, P.Sub);
    wd.HorizontalAlignment = HorizontalAlignment.Center;
    stack.Children.Add(wd);
    var num = Ui.T(Dates.Number(d), 17, P.Text, on ? FontWeights.SemiBold : FontWeights.Normal);
    num.HorizontalAlignment = HorizontalAlignment.Center;
    stack.Children.Add(num);
    var b = Ui.Btn(stack, () => c.Select(d), "transparent", P.Text, 6, new Thickness(0), P.Hover, Dates.MonthDay(d) + " " + Dates.Long[(int)d.DayOfWeek]);
    var cell = new Grid();
    cell.Children.Add(new Border { Background = Ui.Br(P.Card), BorderBrush = Ui.Br(on ? P.Accent : P.CardLine), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Height = 50, Child = b });
    if (on) cell.Children.Add(new Border { Width = 16, Height = 3, CornerRadius = new CornerRadius(2), Background = Ui.Br(P.Accent), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 3), IsHitTestVisible = false });
    cell.Margin = new Thickness(i == 0 ? 0 : 3, 0, i == 6 ? 0 : 3, 0);
    week.Children.Add(Ui.At(cell, i));
   }
   page.Children.Add(week);

   foreach (var g in c.Groups) {
    var label = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 8) };
    label.Children.Add(Ui.T(g.Key, 14, P.Text, FontWeights.SemiBold));
    var en = Ui.T(PlannerContext.English(g.Key), 11, P.Faint);
    en.Margin = new Thickness(8, 3, 0, 0);
    label.Children.Add(en);
    page.Children.Add(label);
    var rows = new StackPanel();
    bool first = true;
    foreach (var t in g) {
     if (!first) rows.Children.Add(Ui.Rule(P.Line, 1, new Thickness(0)));
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

  UIElement IconText(string icon, string text) {
   var row = new StackPanel { Orientation = Orientation.Horizontal };
   var i = Ui.Icon(icon, 14, P.Text, 2);
   if (icon == "left") { row.Children.Add(i); var t = Ui.T(text, 13, P.Text); t.Margin = new Thickness(6, 0, 0, 0); t.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(t); }
   else { var t = Ui.T(text, 13, P.Text); t.Margin = new Thickness(0, 0, 6, 0); t.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(t); row.Children.Add(i); }
   return row;
  }

  UIElement Row(PlannerContext c, TaskItem t) {
   bool done = c.IsDone(t);
   var grid = Ui.Cols("auto", "*", "auto", "auto");
   var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
   box.Margin = new Thickness(0, 0, 14, 0);
   box.Tag = "task:" + t.Id;
   grid.Children.Add(box);
   var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   copy.Children.Add(Ui.T(t.Title, 14, done ? P.Sub : P.Text));
   if (!string.IsNullOrWhiteSpace(t.Note)) copy.Children.Add(Ui.T(t.Note, 12, P.Sub));
   grid.Children.Add(Ui.At(copy, 1));
   var freq = Ui.T(t.Frequency, 12, P.Sub);
   freq.VerticalAlignment = VerticalAlignment.Center;
   freq.Margin = new Thickness(12, 0, 4, 0);
   grid.Children.Add(Ui.At(freq, 2));
   var edit = Ui.Btn(Ui.Icon("dots", 16, P.Sub, 2.6), () => c.Edit(t), "transparent", P.Sub, 4, new Thickness(8), P.Hover, "编辑任务");
   grid.Children.Add(Ui.At(edit, 3));
   var wrap = new Border { Padding = new Thickness(16, 6, 6, 6), MinHeight = 46, Child = grid };
   Ui.RowClick(wrap, box);
   return wrap;
  }

  public override FrameworkElement BuildWidget(WidgetContext c) {
   var root = Ui.Rows("auto", "*", "auto");
   root.Margin = new Thickness(16, 14, 16, 12);
   var tasks = c.Tasks;
   double p = tasks.Count == 0 ? 0 : (double)c.DoneCount / tasks.Count;

   var head = new StackPanel();
   var top = Ui.Cols("*", "auto");
   var grip = new StackPanel { Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.SizeAll };
   var brand = new StackPanel { Orientation = Orientation.Horizontal };
   brand.Children.Add(AppTile());
   var bn = Ui.T("刻序", 12, P.Sub);
   bn.Margin = new Thickness(8, 0, 0, 0);
   bn.VerticalAlignment = VerticalAlignment.Center;
   brand.Children.Add(bn);
   grip.Children.Add(brand);
   var tt = Ui.T("今天的事", 20, P.Text, FontWeights.SemiBold, Display);
   tt.Margin = new Thickness(0, 10, 0, 0);
   grip.Children.Add(tt);
   grip.Children.Add(Ui.T(Dates.MonthDay(c.Day) + " · " + Dates.Short[(int)c.Day.DayOfWeek], 12, P.Sub));
   top.Children.Add(grip);
   c.Grip(grip);
   var dots = Ui.Btn(Ui.Icon("dots", 16, P.Sub, 2.6), null, "transparent", P.Sub, 4, new Thickness(6), P.Hover, "组件选项");
   dots.VerticalAlignment = VerticalAlignment.Top;
   dots.Click += delegate { c.OpenMenu(dots); };
   top.Children.Add(Ui.At(dots, 1));
   head.Children.Add(top);
   var bar = Ui.Bar(p, P.Track, P.Accent, 4, 2);
   bar.Margin = new Thickness(0, 14, 0, 6);
   head.Children.Add(bar);
   head.Children.Add(Ui.T(c.Summary, 12, P.Accent, FontWeights.SemiBold));
   root.Children.Add(head);

   var list = new StackPanel();
   foreach (var g in c.Groups) {
    var label = Ui.T(g.Key, 11, P.Faint);
    label.Margin = new Thickness(0, 10, 0, 2);
    list.Children.Add(label);
    foreach (var t in g) {
     bool done = c.IsDone(t);
     var row = Ui.Cols("auto", "*");
     row.Margin = new Thickness(0, 0, 0, 0);
     row.MinHeight = 36;
     var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
     box.Margin = new Thickness(0, 0, 10, 0);
     box.Tag = "task:" + t.Id;
     row.Children.Add(box);
     var title = Ui.Line(t.Title, 14, done ? P.Sub : P.Text);
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

   var foot = Ui.Cols("*", "auto");
   foot.Margin = new Thickness(0, 8, 0, 0);
   c.Status = Ui.T("桌面组件", 10, P.Faint);
   c.Status.VerticalAlignment = VerticalAlignment.Center;
   foot.Children.Add(c.Status);
   foot.Children.Add(Ui.At(Ui.Btn(Ui.T("打开计划  ↗", 12, P.Accent, FontWeights.SemiBold), c.OpenPlan, "transparent", P.Accent, 4, new Thickness(6, 3, 6, 3), P.Hover, "打开计划"), 1));
   Grid.SetRow(foot, 2);
   root.Children.Add(foot);
   return root;
  }
 }
}
