using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kora {
 // Dot-matrix instrument: dot-font numerals, one row of dots for progress, a single red signal colour, dotted rules.
 public sealed class DotSkin : Skin {
  const string Red = "#E4212B";
  readonly FontFamily dotFont;
  readonly string dots;

  public DotSkin(bool dark) {
   Id = "dot"; Name = "点阵仪表"; Dark = dark;
   Body = Ui.Font("Microsoft YaHei UI, Segoe UI");
   Display = Body;
   Mono = Ui.Font("Space Mono, Cascadia Mono, Consolas, Microsoft YaHei UI");
   dotFont = Ui.Bundled("KoraDot.ttf", "Kora Dot", "Consolas");
   P = dark ? new Palette {
    Bg = "#0B0B0B", Panel = "#0B0B0B", Card = "transparent", CardLine = "#303030", Text = "#F2F2F0", Sub = "#8A8A8F", Faint = "#66666A", Accent = "#F2F2F0", OnAccent = "#0B0B0B",
    Line = "#303030", Hover = "#1B1B1B", Track = "#262626", Danger = "#FF5560", Thumb = "#66F2F2F0", Input = "#141414", InputLine = "#333333"
   } : new Palette {
    Bg = "#F2F2F0", Panel = "#F2F2F0", Card = "transparent", CardLine = "#C9C9C6", Text = "#101010", Sub = "#76767B", Faint = "#9A9A9E", Accent = "#101010", OnAccent = "#F2F2F0",
    Line = "#C9C9C6", Hover = "#E4E4E1", Track = "#DCDCD9", Danger = Red, Thumb = "#66101010", Input = "#FBFBFA", InputLine = "#C9C9C6"
   };
   dots = dark ? "#1B1B1B" : "#DDDCD9";
   CardRadius = 20; InputRadius = 14; ButtonRadius = 99; WindowRadius = 30;
   TitleSize = 28; TitleWeight = FontWeights.SemiBold; MonoKicker = true;
   FrameFill = P.Bg; FrameBorder = P.Line; ShadowBlur = 20; ShadowOpacity = dark ? 0.6 : 0.3;
   WidgetRadius = 34; WidgetFill = "#0B0B0B"; WidgetBorder = "#2A2A2A"; WidgetShadow = 0.5;
  }

  public override string CheckXaml() { return Xaml.CheckStyle(22, 11, 1.5, P.Text, "transparent", P.Text, P.Text, Red, P.Text); }
  public override string WidgetCheckXaml() { return Xaml.CheckStyle(20, 10, 1.5, "#F2F2F0", "transparent", "#F2F2F0", "#F2F2F0", Red, "#F2F2F0"); }
  public override double WidgetHeight(int tasks, int groups) { return Math.Max(300, 270 + groups * 26 + tasks * 37); }

  TextBlock DotText(string text, double size, string color) {
   return new TextBlock { Text = text, FontFamily = dotFont, FontSize = size, Foreground = Ui.Br(color), FontWeight = FontWeights.Normal, TextWrapping = TextWrapping.NoWrap };
  }

  public override FrameworkElement BuildPlanner(PlannerContext c) {
   var root = Ui.Rows("36", "*");
   root.Margin = new Thickness(34, 22, 34, 26);
   root.Background = Ui.DotGrid(dots, 16);

   var bar = Ui.Cols("*", "auto", "*");
   bar.Background = Brushes.Transparent;
   c.Drag(bar);
   var logo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
   var red = Ui.Dot(8, Red);
   red.VerticalAlignment = VerticalAlignment.Center;
   logo.Children.Add(red);
   var name = Ui.T("KORA / DESK", 11, P.Text, FontWeights.Normal, Mono);
   name.Margin = new Thickness(9, 0, 0, 0);
   logo.Children.Add(name);
   bar.Children.Add(logo);
   var tabs = new StackPanel { Orientation = Orientation.Horizontal };
   tabs.Children.Add(Tab(c, 0, "每日"));
   tabs.Children.Add(Tab(c, 1, "每周"));
   tabs.Children.Add(Tab(c, 2, "设置"));
   bar.Children.Add(Ui.At(new Border { BorderBrush = Ui.Br(P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(99), Padding = new Thickness(3), Background = Ui.Br(P.Bg), Child = tabs, VerticalAlignment = VerticalAlignment.Center }, 1));
   var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
   var addRow = new StackPanel { Orientation = Orientation.Horizontal };
   addRow.Children.Add(Ui.Icon("plus", 12, P.OnAccent, 2.6));
   var addText = Ui.T("添加任务", 12, P.OnAccent, FontWeights.Medium);
   addText.Margin = new Thickness(6, 0, 0, 0);
   addRow.Children.Add(addText);
   var add = Ui.Btn(addRow, c.Add, P.Accent, P.OnAccent, 99, new Thickness(14, 0, 14, 0), null, "添加任务");
   add.Height = 30;
   add.Margin = new Thickness(0, 0, 8, 0);
   right.Children.Add(add);
   right.Children.Add(Round("min", c.Minimize, "最小化"));
   var close = Round("close", c.Close, "关闭");
   close.Margin = new Thickness(6, 0, 0, 0);
   right.Children.Add(close);
   bar.Children.Add(Ui.At(right, 2));
   root.Children.Add(bar);

   var body = c.Page == 0 ? Ui.Cols("292", "*") : Ui.Cols("*");
   Grid.SetRow(body, 1);
   body.Margin = new Thickness(0, 14, 0, 0);
   if (c.Page == 0) {
    var left = Left(c);
    left.Margin = new Thickness(0, 0, 36, 0);
    body.Children.Add(left);
    var scroll = Ui.Scroll(Daily(c));
    Grid.SetColumn(scroll, 1);
    body.Children.Add(scroll);
   } else {
    body.Children.Add(Ui.Scroll(new Border { Padding = new Thickness(0, 0, 4, 12), Child = c.Page == 1 ? Pages.Plan(c) : Pages.Settings(c) }));
   }
   root.Children.Add(body);
   return root;
  }

  Button Round(string icon, Action action, string tip) {
   var b = Ui.Btn(Ui.Icon(icon, 12, P.Text, 1.8), action, P.Bg, P.Text, 15, new Thickness(0), P.Hover, tip);
   b.Width = 30; b.Height = 30;
   return b;
  }

  UIElement Tab(PlannerContext c, int page, string text) {
   bool on = c.Page == page;
   var b = Ui.Btn(Ui.T(text, 12, on ? P.OnAccent : P.Sub, FontWeights.Medium), () => c.Go(page), on ? P.Accent : "transparent", P.Text, 99, new Thickness(16, 5, 16, 5), on ? P.Accent : P.Hover, text);
   return b;
  }

  FrameworkElement Left(PlannerContext c) {
   var g = Ui.Rows("auto", "*", "auto");
   var top = new StackPanel();
   var day = DotText(Dates.Number(c.Selected), 190, P.Text);
   day.LineHeight = 172;
   day.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
   day.Margin = new Thickness(-4, 0, 0, 0);
   top.Children.Add(day);
   var cap = Ui.T(Dates.Short[(int)c.Selected.DayOfWeek] + " · " + c.Selected.Month.ToString() + "月" + (c.IsToday ? " · 今天" : ""), 12, P.Sub, FontWeights.Normal, Mono);
   cap.Margin = new Thickness(0, 14, 0, 0);
   top.Children.Add(cap);
   var tasks = c.Tasks;
   int done = c.DoneCount;
   var dotsRow = new WrapPanel { Margin = new Thickness(0, 20, 0, 0) };
   for (int i = 0; i < tasks.Count; i++) dotsRow.Children.Add(Pip(22, i < done, P.Text, new Thickness(0, 0, 10, 8)));
   top.Children.Add(dotsRow);
   var count = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
   count.Children.Add(DotText(done + "/" + tasks.Count, 52, P.Text));
   var label = Ui.T("已完成", 12, P.Sub, FontWeights.Normal, Mono);
   label.Margin = new Thickness(10, 0, 0, 8);
   label.VerticalAlignment = VerticalAlignment.Bottom;
   count.Children.Add(label);
   top.Children.Add(count);
   g.Children.Add(top);

   var bottom = new StackPanel();
   var nav = new StackPanel { Orientation = Orientation.Horizontal };
   nav.Children.Add(Pill("‹ 上周", () => c.ShiftWeeks(-1), "上一周"));
   nav.Children.Add(Pill("今天", c.Today, "回到今天"));
   nav.Children.Add(Pill("下周 ›", () => c.ShiftWeeks(1), "下一周"));
   bottom.Children.Add(nav);
   var quote = Ui.T("一点一点，也是向前。", 12, P.Sub);
   quote.Margin = new Thickness(0, 14, 0, 0);
   bottom.Children.Add(quote);
   Grid.SetRow(bottom, 2);
   g.Children.Add(bottom);
   return g;
  }

  FrameworkElement Pip(double size, bool filled, string ink, Thickness margin) {
   var e = new System.Windows.Shapes.Ellipse { Width = size, Height = size, Margin = margin, StrokeThickness = 1.5, Stroke = Ui.Br(filled ? Red : ink), Fill = Ui.Br(filled ? Red : "transparent") };
   return e;
  }

  UIElement Pill(string text, Action action, string tip) {
   var b = Ui.Btn(Ui.T(text, 11, P.Text, FontWeights.Normal, Mono), action, P.Bg, P.Text, 99, new Thickness(12, 4, 12, 4), P.Hover, tip);
   return new Border { BorderBrush = Ui.Br(P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(99), Margin = new Thickness(0, 0, 8, 0), Child = b };
  }

  UIElement Daily(PlannerContext c) {
   var page = new StackPanel { Margin = new Thickness(0, 0, 6, 12) };
   var week = Ui.Cols("*", "*", "*", "*", "*", "*", "*");
   var days = c.Week;
   for (int i = 0; i < days.Length; i++) {
    var d = days[i];
    bool on = d.Date == c.Selected.Date;
    var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    var wd = Ui.T(Dates.Short[(int)d.DayOfWeek].Substring(1), 11, on ? P.OnAccent : P.Sub, FontWeights.Normal, Mono);
    wd.HorizontalAlignment = HorizontalAlignment.Center;
    stack.Children.Add(wd);
    var num = DotText(Dates.Number(d), 24, on ? P.OnAccent : P.Text);
    num.HorizontalAlignment = HorizontalAlignment.Center;
    stack.Children.Add(num);
    var b = Ui.Btn(stack, () => c.Select(d), on ? P.Accent : "transparent", P.Text, 16, new Thickness(0), on ? P.Accent : P.Hover, Dates.MonthDay(d) + " " + Dates.Long[(int)d.DayOfWeek]);
    b.Height = 62;
    b.Margin = new Thickness(1, 0, 1, 0);
    week.Children.Add(Ui.At(b, i));
   }
   page.Children.Add(week);

   foreach (var g in c.Groups) {
    var head = Ui.Cols("*", "auto");
    head.Margin = new Thickness(0, 20, 0, 0);
    head.Children.Add(Ui.T(g.Key + " / " + PlannerContext.English(g.Key), 11, P.Sub, FontWeights.Normal, Mono));
    head.Children.Add(Ui.At(Ui.T(g.Count(c.IsDone) + "/" + g.Count(), 11, P.Sub, FontWeights.Normal, Mono), 1));
    page.Children.Add(head);
    page.Children.Add(Ui.Rule(P.Text, 1, new Thickness(0, 6, 0, 0)));
    foreach (var t in g) page.Children.Add(Row(c, t));
   }
   if (c.Tasks.Count == 0) {
    var empty = Ui.T(c.EmptyText, 16, P.Sub);
    empty.TextAlignment = TextAlignment.Center;
    empty.Margin = new Thickness(0, 60, 0, 30);
    page.Children.Add(empty);
   }
   return page;
  }

  UIElement Row(PlannerContext c, TaskItem t) {
   bool done = c.IsDone(t);
   var grid = Ui.Cols("auto", "*", "auto", "auto");
   grid.Margin = new Thickness(0, 10, 0, 10);
   var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
   box.Margin = new Thickness(0, 0, 14, 0);
   box.Tag = "task:" + t.Id;
   grid.Children.Add(box);
   var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   var title = Ui.T(t.Title, 15, done ? P.Sub : P.Text, FontWeights.Medium);
   if (done) title.TextDecorations = TextDecorations.Strikethrough;
   copy.Children.Add(title);
   if (!string.IsNullOrWhiteSpace(t.Note)) copy.Children.Add(Ui.T(t.Note, 12, P.Sub));
   grid.Children.Add(Ui.At(copy, 1));
   var freq = Ui.T(t.Frequency, 11, P.Sub, FontWeights.Normal, Mono);
   freq.VerticalAlignment = VerticalAlignment.Center;
   freq.Margin = new Thickness(10, 0, 4, 0);
   grid.Children.Add(Ui.At(freq, 2));
   var edit = Ui.Btn(Ui.Icon("dots", 16, P.Sub, 2.6), () => c.Edit(t), "transparent", P.Sub, 99, new Thickness(6, 4, 6, 4), P.Hover, "编辑任务");
   edit.VerticalAlignment = VerticalAlignment.Center;
   grid.Children.Add(Ui.At(edit, 3));
   var cell = new Grid { Background = Ui.Br(P.Bg) };
   cell.Children.Add(grid);
   cell.Children.Add(Ui.Dashed(P.Sub));
   Ui.RowClick(cell, box);
   return cell;
  }

  public override FrameworkElement BuildWidget(WidgetContext c) {
   const string ink = "#F2F2F0", mid = "#808085";
   var root = Ui.Rows("auto", "*", "auto");
   root.Margin = new Thickness(22, 22, 22, 16);
   root.Background = Ui.DotGrid("#1C1C1C", 14);
   var tasks = c.Tasks;
   int done = c.DoneCount;

   var head = new StackPanel();
   var grip = new StackPanel { Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.SizeAll };
   var top = Ui.Cols("*", "auto");
   var brand = new StackPanel { Orientation = Orientation.Horizontal };
   var red = Ui.Dot(7, Red);
   red.VerticalAlignment = VerticalAlignment.Center;
   brand.Children.Add(red);
   var bn = Ui.T("KORA", 11, ink, FontWeights.Normal, Mono);
   bn.Margin = new Thickness(7, 0, 0, 0);
   brand.Children.Add(bn);
   top.Children.Add(brand);
   top.Children.Add(Ui.At(Ui.T(Dates.Short[(int)c.Day.DayOfWeek] + " " + Dates.Number(c.Day), 11, mid, FontWeights.Normal, Mono), 1));
   grip.Children.Add(top);
   var big = DotText(done + "/" + tasks.Count, 78, ink);
   big.Margin = new Thickness(-2, 12, 0, 0);
   grip.Children.Add(big);
   head.Children.Add(grip);
   c.Grip(grip);
   var pips = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
   for (int i = 0; i < tasks.Count; i++) pips.Children.Add(Pip(18, i < done, ink, new Thickness(0, 0, 8, 6)));
   head.Children.Add(pips);
   root.Children.Add(head);

   var list = new StackPanel();
   foreach (var g in c.Groups) {
    var label = Ui.T(g.Key + " / " + PlannerContext.English(g.Key), 10.5, mid, FontWeights.Normal, Mono);
    label.Margin = new Thickness(0, 14, 0, 0);
    list.Children.Add(label);
    foreach (var t in g) {
     bool isDone = c.IsDone(t);
     var row = Ui.Cols("auto", "*");
     row.Margin = new Thickness(0, 8, 0, 8);
     var box = Ui.Check(isDone, t.Title, v => c.Toggle(t, v), true);
     box.Margin = new Thickness(0, 0, 12, 0);
     box.Tag = "task:" + t.Id;
     row.Children.Add(box);
     var title = Ui.Line(t.Title, 14, isDone ? mid : ink);
     title.VerticalAlignment = VerticalAlignment.Center;
     title.ToolTip = string.IsNullOrWhiteSpace(t.Note) ? null : t.Note;
     if (isDone) title.TextDecorations = TextDecorations.Strikethrough;
     row.Children.Add(Ui.At(title, 1));
     var cell = new Grid();
     cell.Children.Add(row);
     cell.Children.Add(Ui.Dashed("#3A3A3A"));
     Ui.RowClick(cell, box);
     list.Children.Add(cell);
    }
   }
   if (tasks.Count == 0) { var empty = Ui.T(c.EmptyText, 18, mid); empty.Margin = new Thickness(0, 30, 0, 0); list.Children.Add(empty); }
   var scroll = Ui.Scroll(list);
   scroll.Margin = new Thickness(0, 2, -6, 0);
   Grid.SetRow(scroll, 1);
   root.Children.Add(scroll);

   var foot = Ui.Cols("*", "auto", "auto");
   foot.Margin = new Thickness(0, 12, 0, 0);
   c.Status = Ui.T("桌面组件", 10, mid, FontWeights.Normal, Mono);
   c.Status.VerticalAlignment = VerticalAlignment.Center;
   foot.Children.Add(c.Status);
   foot.Children.Add(Ui.At(Ui.Btn(Ui.T("OPEN PLAN ↗", 11, mid, FontWeights.Normal, Mono), c.OpenPlan, "transparent", mid, 99, new Thickness(8, 3, 8, 3), "#1B1B1B", "打开计划"), 1));
   var dots2 = Ui.Btn(Ui.Icon("dots", 14, mid, 2.6), null, "transparent", mid, 99, new Thickness(6, 3, 6, 3), "#1B1B1B", "组件选项");
   dots2.Click += delegate { c.OpenMenu(dots2); };
   foot.Children.Add(Ui.At(dots2, 2));
   Grid.SetRow(foot, 2);
   root.Children.Add(foot);
   return root;
  }
 }
}
