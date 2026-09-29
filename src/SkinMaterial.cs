using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kora {
 // Material 3 Expressive: heavy headline, wavy progress, cookie badge, segmented list, shapes that change when selected.
 public sealed class MaterialSkin : Skin {
  readonly string pc, onpc, secc, onsc, tc, ontc, sch, schh, onv;

  public MaterialSkin(bool dark) {
   Id = "material"; Name = "Material 3 Expressive"; Dark = dark;
   Body = Ui.Font("Roboto Flex, Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI");
   Display = Ui.Font("Roboto Flex, Segoe UI Variable Display, Segoe UI, Microsoft YaHei UI");
   Mono = Body;
   string sf, sc, on, outline, pri, onpri;
   if (dark) {
    sf = "#0F1511"; sc = "#1A201C"; sch = "#242A26"; schh = "#2F3531"; on = "#DEE4DD"; onv = "#BFC9C1"; outline = "#8A938C";
    pri = "#98D5B6"; onpri = "#003921"; pc = "#0C5136"; onpc = "#B4F1D0"; secc = "#384B3F"; onsc = "#D3E8D9"; tc = "#6B3A14"; ontc = "#FFDCC0";
   } else {
    sf = "#F5FBF3"; sc = "#EBF1E8"; sch = "#E4EBE2"; schh = "#DBE3D9"; on = "#171D19"; onv = "#404943"; outline = "#707972";
    pri = "#2C6A4D"; onpri = "#FFFFFF"; pc = "#B4F1D0"; onpc = "#00210F"; secc = "#D3E8D9"; onsc = "#0E1F15"; tc = "#FFDCC0"; ontc = "#2E1500";
   }
   P = new Palette {
    Bg = sf, Panel = sf, Card = sc, CardLine = "transparent", Text = on, Sub = onv, Faint = outline, Accent = pri, OnAccent = onpri,
    Line = sch, Hover = sch, Track = schh, Danger = dark ? "#FFB4AB" : "#BA1A1A", Thumb = "#66" + outline.Substring(1), Input = sch, InputLine = "#59" + outline.Substring(1)
   };
   CardRadius = 24; InputRadius = 14; ButtonRadius = 20; WindowRadius = 28;
   TitleSize = 34; TitleWeight = FontWeights.ExtraBold;
   FrameFill = sf; FrameBorder = dark ? "#26FFFFFF" : "#1F000000"; ShadowBlur = 20; ShadowOpacity = dark ? 0.5 : 0.3;
   WidgetRadius = 32; WidgetFill = sc; WidgetBorder = dark ? "#1FFFFFFF" : "#14000000"; WidgetShadow = dark ? 0.5 : 0.28;
  }

  public override string SecondaryFill { get { return secc; } }
  public override string CheckXaml() { return Xaml.CheckStyle(24, 8, 2, P.Faint, P.Accent, P.Accent, P.OnAccent, null, P.Accent); }
  public override double WidgetHeight(int tasks, int groups) { return Math.Max(280, 210 + groups * 26 + tasks * 44); }

  public override FrameworkElement BuildPlanner(PlannerContext c) {
   var root = Ui.Cols("96", "*");

   var rail = Ui.Rows("auto", "*", "auto");
   rail.Margin = new Thickness(0, 22, 0, 20);
   var top = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Background = Brushes.Transparent };
   c.Drag(top);
   var fab = Ui.Btn(Ui.Icon("plus", 24, ontc, 2.2), c.Add, tc, ontc, 18, new Thickness(0), null, "添加任务");
   fab.Width = 56; fab.Height = 56;
   fab.Margin = new Thickness(0, 10, 0, 22);
   top.Children.Add(fab);
   top.Children.Add(RailItem(c, 0, "today", "清单"));
   top.Children.Add(RailItem(c, 1, "week", "计划"));
   rail.Children.Add(top);
   rail.Children.Add(Ui.At(RailItem(c, 2, "settings", "设置"), 0, 2));
   root.Children.Add(rail);

   UIElement page = c.Page == 0 ? Daily(c) : c.Page == 1 ? Pages.Plan(c) : Pages.Settings(c);
   var main = new Grid();
   Grid.SetColumn(main, 1);
   var holder = new Border { Padding = new Thickness(6, 24, 36, 24), Child = page };
   main.Children.Add(Ui.Scroll(holder));
   var wc = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 10, 14, 0) };
   wc.Children.Add(RoundButton("min", c.Minimize, "最小化"));
   wc.Children.Add(RoundButton("close", c.Close, "关闭"));
   main.Children.Add(wc);
   var dragBar = new Border { Height = 24, VerticalAlignment = VerticalAlignment.Top, Background = Brushes.Transparent, Margin = new Thickness(0, 0, 100, 0) };
   c.Drag(dragBar);
   main.Children.Add(dragBar);
   root.Children.Add(main);
   return root;
  }

  Button RoundButton(string icon, Action action, string tip) {
   var b = Ui.Btn(Ui.Icon(icon, 18, onv, 1.8), action, "transparent", onv, 20, new Thickness(0), sch, tip);
   b.Width = 40; b.Height = 40;
   return b;
  }

  UIElement RailItem(PlannerContext c, int page, string icon, string text) {
   bool on = c.Page == page;
   var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
   var pill = new Border { Width = 56, Height = 32, CornerRadius = new CornerRadius(16), Background = Ui.Br(on ? secc : "transparent"), HorizontalAlignment = HorizontalAlignment.Center };
   var glyph = Ui.Icon(icon, 22, on ? onsc : onv, 1.9);
   ((FrameworkElement)glyph).HorizontalAlignment = HorizontalAlignment.Center;
   ((FrameworkElement)glyph).VerticalAlignment = VerticalAlignment.Center;
   pill.Child = glyph;
   stack.Children.Add(pill);
   var label = Ui.T(text, 12, on ? P.Text : onv, on ? FontWeights.Bold : FontWeights.Medium);
   label.HorizontalAlignment = HorizontalAlignment.Center;
   label.Margin = new Thickness(0, 4, 0, 0);
   stack.Children.Add(label);
   var b = Ui.Btn(stack, () => c.Go(page), "transparent", P.Text, 16, new Thickness(8, 4, 8, 6), sch, text);
   b.HorizontalAlignment = HorizontalAlignment.Center;
   b.Margin = new Thickness(0, 4, 0, 0);
   return b;
  }

  UIElement Daily(PlannerContext c) {
   var page = new StackPanel();
   var tasks = c.Tasks;
   int done = c.DoneCount, left = tasks.Count - done;
   double p = tasks.Count == 0 ? 0 : (double)done / tasks.Count;

   page.Children.Add(Ui.T(c.DateLine, 14, onv, FontWeights.Medium));
   var headline = Ui.T(c.Headline, 42, P.Text, FontWeights.ExtraBold, Display);
   headline.Margin = new Thickness(0, 2, 0, 0);
   page.Children.Add(headline);

   var hero = Ui.Cols("auto", "*");
   var badge = new Grid { Width = 76, Height = 76, Margin = new Thickness(0, 0, 20, 0) };
   badge.Children.Add(Ui.Cookie(76, P.Accent));
   var ratio = Ui.T(done + "/" + tasks.Count, 27, P.OnAccent, FontWeights.ExtraBold, Display);
   ratio.HorizontalAlignment = HorizontalAlignment.Center;
   ratio.VerticalAlignment = VerticalAlignment.Center;
   ratio.TextWrapping = TextWrapping.NoWrap;
   badge.Children.Add(ratio);
   hero.Children.Add(badge);
   var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   info.Children.Add(Ui.T("今日进度", 13, onpc, FontWeights.SemiBold));
   info.Children.Add(Ui.T(tasks.Count == 0 ? "今天留白" : left > 0 ? "还剩 " + left + " 件" : "全部完成", 26, onpc, FontWeights.ExtraBold, Display));
   var wave = Ui.Wave(560, p, P.Accent, "#47" + onpc.Substring(1));
   wave.HorizontalAlignment = HorizontalAlignment.Left;
   info.Children.Add(wave);
   hero.Children.Add(Ui.At(info, 1));
   page.Children.Add(new Border { Background = Ui.Br(pc), CornerRadius = new CornerRadius(32), Padding = new Thickness(16, 14, 26, 14), Margin = new Thickness(0, 14, 0, 12), Child = hero });

   var week = Ui.Cols("*", "*", "*", "*", "*", "*", "*");
   var days = c.Week;
   for (int i = 0; i < days.Length; i++) {
    var d = days[i];
    bool on = d.Date == c.Selected.Date;
    var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    var wd = Ui.T(Dates.Short[(int)d.DayOfWeek].Substring(1), 12, on ? P.OnAccent : onv, FontWeights.Medium);
    wd.HorizontalAlignment = HorizontalAlignment.Center;
    stack.Children.Add(wd);
    var num = Ui.T(Dates.Number(d), 19, on ? P.OnAccent : P.Text, FontWeights.Bold);
    num.HorizontalAlignment = HorizontalAlignment.Center;
    stack.Children.Add(num);
    var b = Ui.Btn(stack, () => c.Select(d), on ? P.Accent : sch, P.Text, on ? 26 : 16, new Thickness(0, 6, 0, 6), on ? P.Accent : schh, Dates.MonthDay(d) + " " + Dates.Long[(int)d.DayOfWeek]);
    b.Height = 52;
    b.Margin = new Thickness(3, 0, 3, 0);
    week.Children.Add(Ui.At(b, i));
   }
   week.Margin = new Thickness(-3, 0, -3, 0);
   page.Children.Add(week);

   foreach (var g in c.Groups) {
    var label = Ui.T(g.Key, 14, P.Accent, FontWeights.Bold);
    label.Margin = new Thickness(10, 14, 0, 6);
    page.Children.Add(label);
    var items = g.ToList();
    for (int i = 0; i < items.Count; i++) page.Children.Add(Item(c, items[i], items.Count == 1 ? "o" : i == 0 ? "f" : i == items.Count - 1 ? "l" : "m"));
   }
   if (tasks.Count == 0) {
    var empty = Ui.T(c.EmptyText, 16, onv);
    empty.TextAlignment = TextAlignment.Center;
    empty.Margin = new Thickness(0, 40, 0, 30);
    page.Children.Add(empty);
   }
   return page;
  }

  UIElement Item(PlannerContext c, TaskItem t, string pos) {
   bool done = c.IsDone(t);
   double big = 24, small = 8;
   var radius = pos == "o" ? new CornerRadius(big) : pos == "f" ? new CornerRadius(big, big, small, small) : pos == "l" ? new CornerRadius(small, small, big, big) : new CornerRadius(small);
   var grid = Ui.Cols("auto", "*", "auto", "auto");
   var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
   box.Margin = new Thickness(0, 0, 16, 0);
   box.Tag = "task:" + t.Id;
   grid.Children.Add(box);
   var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   var title = Ui.T(t.Title, 16, done ? onv : P.Text, FontWeights.Medium);
   if (done) title.TextDecorations = TextDecorations.Strikethrough;
   copy.Children.Add(title);
   if (!string.IsNullOrWhiteSpace(t.Note)) copy.Children.Add(Ui.T(t.Note, 13, onv));
   grid.Children.Add(Ui.At(copy, 1));
   var chipText = Ui.T(t.Frequency, 12, onv, FontWeights.SemiBold);
   chipText.VerticalAlignment = VerticalAlignment.Center;
   var chip = new Border { Background = Ui.Br(schh), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 3, 10, 3), Child = chipText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 4, 0) };
   grid.Children.Add(Ui.At(chip, 2));
   var edit = Ui.Btn(Ui.Icon("dots", 16, onv, 2.6), () => c.Edit(t), "transparent", onv, 18, new Thickness(0), schh, "编辑任务");
   edit.Width = 36; edit.Height = 36;
   grid.Children.Add(Ui.At(edit, 3));
   var wrap = new Border { Background = Ui.Br(P.Card), CornerRadius = radius, Padding = new Thickness(16, 6, 8, 6), MinHeight = 48, Margin = new Thickness(0, 0, 0, 2), Child = grid };
   Ui.RowClick(wrap, box);
   return wrap;
  }

  public override FrameworkElement BuildWidget(WidgetContext c) {
   var root = Ui.Rows("auto", "*", "auto");
   root.Margin = new Thickness(16, 18, 16, 14);
   var tasks = c.Tasks;
   double p = tasks.Count == 0 ? 0 : (double)c.DoneCount / tasks.Count;

   var head = new StackPanel();
   var top = Ui.Cols("*", "auto");
   var grip = new StackPanel { Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.SizeAll, Margin = new Thickness(4, 0, 0, 0) };
   grip.Children.Add(Ui.T("今天的事", 22, P.Text, FontWeights.ExtraBold, Display));
   top.Children.Add(grip);
   c.Grip(grip);
   var chipText = Ui.T(c.DoneCount + " / " + tasks.Count, 13, onpc, FontWeights.Bold);
   chipText.TextWrapping = TextWrapping.NoWrap;
   top.Children.Add(Ui.At(new Border { Background = Ui.Br(pc), CornerRadius = new CornerRadius(14), Padding = new Thickness(12, 4, 12, 4), Child = chipText, VerticalAlignment = VerticalAlignment.Center }, 1));
   head.Children.Add(top);
   var wave = Ui.Wave(250, p, P.Accent, "#47" + onv.Substring(1));
   wave.HorizontalAlignment = HorizontalAlignment.Left;
   wave.Margin = new Thickness(2, 8, 0, 0);
   head.Children.Add(wave);
   root.Children.Add(head);

   var list = new StackPanel();
   foreach (var g in c.Groups) {
    var label = Ui.T(g.Key, 12, P.Accent, FontWeights.Bold);
    label.Margin = new Thickness(6, 8, 0, 4);
    list.Children.Add(label);
    foreach (var t in g) {
     bool done = c.IsDone(t);
     var row = Ui.Cols("auto", "*");
     var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
     box.Margin = new Thickness(0, 0, 12, 0);
     box.Tag = "task:" + t.Id;
     row.Children.Add(box);
     var title = Ui.Line(t.Title, 14, done ? onv : P.Text, FontWeights.Medium);
     title.VerticalAlignment = VerticalAlignment.Center;
     title.ToolTip = string.IsNullOrWhiteSpace(t.Note) ? null : t.Note;
     if (done) title.TextDecorations = TextDecorations.Strikethrough;
     row.Children.Add(Ui.At(title, 1));
     var wrap = new Border { Background = Ui.Br(schh), CornerRadius = new CornerRadius(16), Padding = new Thickness(12, 6, 12, 6), MinHeight = 40, Margin = new Thickness(0, 0, 0, 4), Child = row };
     Ui.RowClick(wrap, box);
     list.Children.Add(wrap);
    }
   }
   if (tasks.Count == 0) { var empty = Ui.T(c.EmptyText, 18, onv); empty.Margin = new Thickness(6, 30, 0, 0); list.Children.Add(empty); }
   var scroll = Ui.Scroll(list);
   scroll.Margin = new Thickness(0, 4, -6, 0);
   Grid.SetRow(scroll, 1);
   root.Children.Add(scroll);

   var foot = Ui.Cols("*", "auto", "auto");
   foot.Margin = new Thickness(6, 8, 0, 0);
   c.Status = Ui.T("桌面组件", 10, P.Faint);
   c.Status.VerticalAlignment = VerticalAlignment.Center;
   foot.Children.Add(c.Status);
   var open = Ui.Btn(Ui.T("打开计划  ↗", 13, ontc, FontWeights.Bold), c.OpenPlan, tc, ontc, 14, new Thickness(14, 10, 14, 10), null, "打开计划");
   open.Margin = new Thickness(0, 0, 4, 0);
   foot.Children.Add(Ui.At(open, 1));
   var dots = Ui.Btn(Ui.Icon("dots", 16, onv, 2.6), null, "transparent", onv, 14, new Thickness(0), sch, "组件选项");
   dots.Width = 40; dots.Height = 40;
   dots.Click += delegate { c.OpenMenu(dots); };
   foot.Children.Add(Ui.At(dots, 2));
   Grid.SetRow(foot, 2);
   root.Children.Add(foot);
   return root;
  }
 }
}
