using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kora {
 // IBM Carbon: shell header, side navigation, data tiles and a data table. Square corners, dense.
 public sealed class CarbonSkin : Skin {
  readonly string layer, layerHover, header, tag1Fill, tag1Text, tag2Fill, tag2Text;

  public CarbonSkin(bool dark) {
   Id = "carbon"; Name = "IBM Carbon"; Dark = dark;
   Body = Ui.Font("IBM Plex Sans, Segoe UI, Microsoft YaHei UI");
   Display = Body;
   Mono = Ui.Font("IBM Plex Mono, Consolas, Cascadia Mono, Microsoft YaHei UI");
   if (dark) {
    P = new Palette {
     Bg = "#161616", Panel = "#262626", Card = "#262626", CardLine = "#393939", Text = "#F4F4F4", Sub = "#C6C6C6", Faint = "#A8A8A8", Accent = "#0F62FE", OnAccent = "#FFFFFF",
     Line = "#393939", Hover = "#393939", Track = "#393939", Danger = "#FF8389", Thumb = "#8D8D8D", Input = "#262626", InputLine = "#6F6F6F"
    };
    layer = "#262626"; layerHover = "#333333"; tag1Fill = "#005D5D"; tag1Text = "#9EF0F0"; tag2Fill = "#6929C4"; tag2Text = "#E8DAFF";
   } else {
    P = new Palette {
     Bg = "#FFFFFF", Panel = "#F4F4F4", Card = "#F4F4F4", CardLine = "#E0E0E0", Text = "#161616", Sub = "#525252", Faint = "#6F6F6F", Accent = "#0F62FE", OnAccent = "#FFFFFF",
     Line = "#E0E0E0", Hover = "#E8E8E8", Track = "#E0E0E0", Danger = "#DA1E28", Thumb = "#8D8D8D", Input = "#F4F4F4", InputLine = "#8D8D8D"
    };
    layer = "#F4F4F4"; layerHover = "#E8E8E8"; tag1Fill = "#9EF0F0"; tag1Text = "#004144"; tag2Fill = "#E8DAFF"; tag2Text = "#491D8B";
   }
   header = "#161616";
   CardRadius = 0; InputRadius = 0; ButtonRadius = 0; WindowRadius = 0;
   TitleSize = 32; TitleWeight = FontWeights.Light; MonoKicker = true;
   FrameFill = P.Bg; FrameBorder = dark ? "#000000" : "#8D8D8D"; ShadowBlur = 20; ShadowOpacity = dark ? 0.6 : 0.4;
   WidgetRadius = 0; WidgetFill = layer; WidgetBorder = P.Line; WidgetShadow = dark ? 0.55 : 0.35;
  }

  public override string SecondaryFill { get { return P.Hover; } }
  public override string CheckXaml() { return Xaml.CheckStyle(16, 0, 1, P.Text, P.Text, P.Text, P.Bg, null, "#0F62FE"); }
  public override double WidgetHeight(int tasks, int groups) { return Math.Max(280, 214 + groups * 30 + tasks * 35); }

  public override FrameworkElement BuildPlanner(PlannerContext c) {
   var root = Ui.Rows("48", "*");
   var bar = Ui.Cols("*", "auto");
   bar.Background = Ui.Br(header);
   var brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent };
   brand.Children.Add(Ui.T("Kora", 14, "#F4F4F4", FontWeights.SemiBold));
   var desk = Ui.T(" Desk", 14, "#F4F4F4");
   brand.Children.Add(desk);
   bar.Children.Add(brand);
   c.Drag(brand);
   var caption = new StackPanel { Orientation = Orientation.Horizontal };
   caption.Children.Add(Caption("min", c.Minimize, "最小化", "#353535"));
   caption.Children.Add(Caption("close", c.Close, "关闭", "#DA1E28"));
   bar.Children.Add(Ui.At(caption, 1));
   var dragFill = new Border { Background = Brushes.Transparent };
   c.Drag(dragFill);
   bar.Children.Insert(0, dragFill);
   root.Children.Add(bar);

   var body = Ui.Cols("220", "*");
   Grid.SetRow(body, 1);
   body.Children.Add(SideNav(c));
   UIElement page = c.Page == 0 ? Daily(c) : c.Page == 1 ? Pages.Plan(c) : Pages.Settings(c);
   var holder = new Border { Padding = new Thickness(32, 22, 32, 24), Child = page };
   var scroll = Ui.Scroll(holder);
   Grid.SetColumn(scroll, 1);
   body.Children.Add(scroll);
   root.Children.Add(body);
   return root;
  }

  Button Caption(string icon, Action action, string tip, string hover) {
   var b = Ui.Btn(Ui.Icon(icon, 14, "#F4F4F4", 1.5), action, "transparent", "#F4F4F4", 0, new Thickness(0), hover, tip);
   b.Width = 48; b.Height = 48;
   return b;
  }

  UIElement SideNav(PlannerContext c) {
   var side = Ui.Rows("*", "auto");
   var list = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
   var label = Ui.T("计划", 12, P.Faint);
   label.Margin = new Thickness(16, 8, 16, 8);
   list.Children.Add(label);
   list.Children.Add(NavItem(c, 0, "today", "每日清单"));
   list.Children.Add(NavItem(c, 1, "week", "每周计划"));
   list.Children.Add(NavItem(c, 2, "settings", "偏好设置"));
   side.Children.Add(list);
   var foot = new Border { BorderBrush = Ui.Br(P.Line), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16), Child = Ui.T("LOCAL ONLY · 无网络请求", 11, P.Faint, FontWeights.Normal, Mono) };
   side.Children.Add(Ui.At(foot, 0, 1));
   return new Border { Background = Ui.Br(P.Panel), BorderBrush = Ui.Br(P.Line), BorderThickness = new Thickness(0, 0, 1, 0), Child = side };
  }

  UIElement NavItem(PlannerContext c, int page, string icon, string text) {
   bool on = c.Page == page;
   var row = Ui.Cols("16", "*");
   row.Children.Add(Ui.Icon(icon, 16, P.Text, 1.5));
   var t = Ui.T(text, 14, P.Text, on ? FontWeights.SemiBold : FontWeights.Normal);
   t.Margin = new Thickness(12, 0, 0, 0);
   t.VerticalAlignment = VerticalAlignment.Center;
   row.Children.Add(Ui.At(t, 1));
   var b = Ui.Btn(row, () => c.Go(page), on ? P.Hover : "transparent", P.Text, 0, new Thickness(16, 0, 16, 0), P.Hover, text);
   b.Height = 40;
   b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
   var wrap = new Grid();
   wrap.Children.Add(b);
   if (on) wrap.Children.Add(new Border { Width = 3, Background = Ui.Br("#0F62FE"), HorizontalAlignment = HorizontalAlignment.Left, IsHitTestVisible = false });
   return wrap;
  }

  UIElement Daily(PlannerContext c) {
   var page = new StackPanel();
   var tasks = c.Tasks;
   int done = c.DoneCount;

   var bc = new StackPanel { Orientation = Orientation.Horizontal };
   bc.Children.Add(Ui.T("计划", 12, Dark ? "#78A9FF" : "#0F62FE"));
   bc.Children.Add(Ui.T(" / 每日清单", 12, P.Faint));
   page.Children.Add(bc);
   page.Children.Add(Ui.T("每日清单", 32, P.Text, FontWeights.Light));

   var days = c.Week;
   var switcher = Ui.Cols("*", "*", "*", "*", "*", "*", "*");
   switcher.Margin = new Thickness(0, 10, 0, 0);
   for (int i = 0; i < days.Length; i++) {
    var d = days[i];
    bool on = d.Date == c.Selected.Date;
    var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    row.Children.Add(Ui.T(Dates.Short[(int)d.DayOfWeek], 13, on ? P.Bg : P.Sub));
    var num = Ui.T(Dates.Number(d), 13, on ? P.Bg : P.Text, FontWeights.SemiBold);
    num.Margin = new Thickness(6, 0, 0, 0);
    row.Children.Add(num);
    var b = Ui.Btn(row, () => c.Select(d), on ? P.Text : "transparent", P.Text, 0, new Thickness(0), on ? P.Text : P.Hover, Dates.MonthDay(d) + " " + Dates.Long[(int)d.DayOfWeek]);
    b.Height = 40;
    var cell = new Border { BorderBrush = Ui.Br(on ? P.Text : P.Line), BorderThickness = new Thickness(1), Margin = new Thickness(i == 0 ? 0 : -1, 0, 0, 0), Child = b };
    switcher.Children.Add(Ui.At(cell, i));
   }
   page.Children.Add(switcher);

   var tiles = Ui.Cols("*", "*", "*");
   tiles.Margin = new Thickness(0, 14, 0, 0);
   tiles.Children.Add(Ui.At(Tile("今日进度", done, tasks.Count, 0), 0));
   var study = tasks.Where(t => t.Group == "学习").ToList();
   var fit = tasks.Where(t => t.Group == "健身").ToList();
   tiles.Children.Add(Ui.At(Tile("学习", study.Count(c.IsDone), study.Count, 2), 1));
   tiles.Children.Add(Ui.At(Tile("健身", fit.Count(c.IsDone), fit.Count, 2), 2));
   page.Children.Add(tiles);

   var toolbar = Ui.Cols("*", "auto");
   toolbar.Background = Ui.Br(layer);
   toolbar.Margin = new Thickness(0, 14, 0, 0);
   var search = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
   search.Children.Add(Ui.Icon("search", 16, P.Faint, 1.6));
   var hint = Ui.T("共 " + tasks.Count + " 项任务", 14, P.Faint);
   hint.Margin = new Thickness(10, 0, 0, 0);
   search.Children.Add(hint);
   toolbar.Children.Add(search);
   var addRow = Ui.Cols("auto", "auto");
   var addText = Ui.T("添加任务", 14, "#FFFFFF");
   addText.VerticalAlignment = VerticalAlignment.Center;
   addRow.Children.Add(addText);
   var plus = Ui.Icon("plus", 16, "#FFFFFF", 1.8);
   plus.Margin = new Thickness(28, 0, 0, 0);
   addRow.Children.Add(Ui.At(plus, 1));
   var add = Ui.Btn(addRow, c.Add, "#0F62FE", "#FFFFFF", 0, new Thickness(16, 0, 16, 0), "#0353E9", "添加任务");
   add.Height = 48;
   toolbar.Children.Add(Ui.At(add, 1));
   page.Children.Add(toolbar);

   page.Children.Add(TableHead());
   foreach (var t in tasks) page.Children.Add(Row(c, t));
   if (tasks.Count == 0) {
    var empty = Ui.T(c.EmptyText, 16, P.Faint);
    empty.TextAlignment = TextAlignment.Center;
    empty.Margin = new Thickness(0, 40, 0, 30);
    page.Children.Add(empty);
   }
   return page;
  }

  UIElement Tile(string label, int value, int total, int marginLeft) {
   var stack = new StackPanel { Margin = new Thickness(16, 12, 16, 0) };
   stack.Children.Add(Ui.T(label, 12, P.Faint));
   var num = new StackPanel { Orientation = Orientation.Horizontal };
   num.Children.Add(Ui.T(value.ToString(), 28, P.Text, FontWeights.Light));
   var of = Ui.T("/ " + total, 14, P.Sub);
   of.Margin = new Thickness(6, 0, 0, 5);
   of.VerticalAlignment = VerticalAlignment.Bottom;
   num.Children.Add(of);
   stack.Children.Add(num);
   var grid = new Grid { Height = 82, Background = Ui.Br(layer), Margin = new Thickness(marginLeft, 0, 0, 0) };
   grid.Children.Add(stack);
   var bar = Ui.Bar(total == 0 ? 0 : (double)value / total, P.Line, "#0F62FE", 4, 0);
   bar.VerticalAlignment = VerticalAlignment.Bottom;
   grid.Children.Add(bar);
   return grid;
  }

  Grid Columns() { return Ui.Cols("44", "150", "*", "72", "60", "40"); }

  UIElement TableHead() {
   var g = Columns();
   g.Height = 40;
   g.Background = Ui.Br(P.Hover);
   string[] names = { "", "任务", "备注", "分组", "重复", "" };
   for (int i = 0; i < names.Length; i++) if (names[i].Length > 0) g.Children.Add(Ui.At(Cell(Ui.T(names[i], 14, P.Text, FontWeights.SemiBold)), i));
   return g;
  }

  static FrameworkElement Cell(FrameworkElement e) { e.VerticalAlignment = VerticalAlignment.Center; return e; }

  UIElement Row(PlannerContext c, TaskItem t) {
   bool done = c.IsDone(t);
   var g = Columns();
   g.Height = 40;
   var box = Ui.Check(done, t.Title, v => c.Toggle(t, v));
   box.Margin = new Thickness(16, 0, 0, 0);
   box.Tag = "task:" + t.Id;
   g.Children.Add(Ui.At(box, 0));
   var name = Ui.Line(t.Title, 14, done ? P.Faint : P.Text);
   name.Margin = new Thickness(0, 0, 12, 0);
   if (done) name.TextDecorations = TextDecorations.Strikethrough;
   g.Children.Add(Ui.At(Cell(name), 1));
   var note = Ui.Line(string.IsNullOrWhiteSpace(t.Note) ? "—" : t.Note, 13, done ? P.Faint : P.Sub);
   note.Margin = new Thickness(0, 0, 12, 0);
   note.ToolTip = string.IsNullOrWhiteSpace(t.Note) ? null : t.Note;
   g.Children.Add(Ui.At(Cell(note), 2));
   bool study = t.Group == "学习";
   var tagText = Ui.T(t.Group, 12, study ? tag1Text : tag2Text);
   tagText.TextWrapping = TextWrapping.NoWrap;
   var tag = new Border { Background = Ui.Br(study ? tag1Fill : tag2Fill), CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 1, 8, 2), Child = tagText, HorizontalAlignment = HorizontalAlignment.Left };
   g.Children.Add(Ui.At(Cell(tag), 3));
   var freq = Ui.Line(t.Frequency, 13, P.Sub);
   freq.Margin = new Thickness(0, 0, 6, 0);
   g.Children.Add(Ui.At(Cell(freq), 4));
   var edit = Ui.Btn(Ui.Icon("dots", 16, P.Sub, 2.6), () => c.Edit(t), "transparent", P.Sub, 0, new Thickness(0), P.Hover, "编辑任务");
   edit.Width = 32; edit.Height = 32;
   g.Children.Add(Ui.At(Cell(edit), 5));
   var wrap = new Border { Background = Ui.Br(layer), BorderBrush = Ui.Br(P.Line), BorderThickness = new Thickness(0, 0, 0, 1), Child = g };
   Ui.RowClick(wrap, box);
   wrap.MouseEnter += delegate { wrap.Background = Ui.Br(layerHover); };
   wrap.MouseLeave += delegate { wrap.Background = Ui.Br(layer); };
   return wrap;
  }

  public override FrameworkElement BuildWidget(WidgetContext c) {
   var root = Ui.Rows("auto", "*", "auto");
   root.Margin = new Thickness(16, 16, 16, 12);
   var tasks = c.Tasks;
   int done = c.DoneCount;

   var head = new StackPanel();
   var grip = new StackPanel { Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.SizeAll };
   var top = Ui.Cols("*", "auto");
   top.Children.Add(Ui.T("TODAY", 11, P.Faint, FontWeights.Normal, Mono));
   top.Children.Add(Ui.At(Ui.T(c.Day.ToString("MM.dd", System.Globalization.CultureInfo.InvariantCulture) + " " + c.Day.DayOfWeek.ToString().Substring(0, 3).ToUpperInvariant(), 11, P.Faint, FontWeights.Normal, Mono), 1));
   grip.Children.Add(top);
   var big = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
   big.Children.Add(Ui.T(done.ToString(), 60, P.Text, FontWeights.Light));
   var of = Ui.T("/ " + tasks.Count, 22, P.Sub);
   of.Margin = new Thickness(6, 0, 0, 10);
   of.VerticalAlignment = VerticalAlignment.Bottom;
   big.Children.Add(of);
   grip.Children.Add(big);
   head.Children.Add(grip);
   c.Grip(grip);
   var bar = Ui.Bar(tasks.Count == 0 ? 0 : (double)done / tasks.Count, P.Line, "#0F62FE", 4, 0);
   bar.Margin = new Thickness(-16, 12, -16, 0);
   head.Children.Add(bar);
   root.Children.Add(head);

   var list = new StackPanel();
   foreach (var g in c.Groups) {
    var label = Ui.T(g.Key, 12, P.Faint);
    label.Margin = new Thickness(0, 12, 0, 2);
    list.Children.Add(label);
    foreach (var t in g) {
     bool isDone = c.IsDone(t);
     var row = Ui.Cols("auto", "*");
     var box = Ui.Check(isDone, t.Title, v => c.Toggle(t, v));
     box.Margin = new Thickness(0, 0, 12, 0);
     box.Tag = "task:" + t.Id;
     row.Children.Add(box);
     var title = Ui.Line(t.Title, 14, isDone ? P.Faint : P.Text);
     title.VerticalAlignment = VerticalAlignment.Center;
     title.ToolTip = string.IsNullOrWhiteSpace(t.Note) ? null : t.Note;
     if (isDone) title.TextDecorations = TextDecorations.Strikethrough;
     row.Children.Add(Ui.At(title, 1));
     var wrap = new Border { Height = 34, BorderBrush = Ui.Br(P.Line), BorderThickness = new Thickness(0, 0, 0, 1), Child = row };
     row.VerticalAlignment = VerticalAlignment.Center;
     Ui.RowClick(wrap, box);
     list.Children.Add(wrap);
    }
   }
   if (tasks.Count == 0) { var empty = Ui.T(c.EmptyText, 18, P.Faint); empty.Margin = new Thickness(0, 30, 0, 0); list.Children.Add(empty); }
   var scroll = Ui.Scroll(list);
   scroll.Margin = new Thickness(0, 4, -6, 0);
   Grid.SetRow(scroll, 1);
   root.Children.Add(scroll);

   var foot = Ui.Cols("*", "auto", "auto");
   foot.Margin = new Thickness(0, 10, 0, 0);
   c.Status = Ui.T("桌面组件", 10, P.Faint, FontWeights.Normal, Mono);
   c.Status.VerticalAlignment = VerticalAlignment.Center;
   foot.Children.Add(c.Status);
   var linkColor = Dark ? "#78A9FF" : "#0F62FE";
   var open = Ui.Btn(Ui.T("打开计划  ↗", 13, linkColor), c.OpenPlan, "transparent", linkColor, 0, new Thickness(6, 3, 6, 3), P.Hover, "打开计划");
   foot.Children.Add(Ui.At(open, 1));
   var dots = Ui.Btn(Ui.Icon("dots", 14, P.Sub, 2.6), null, "transparent", P.Sub, 0, new Thickness(6, 3, 6, 3), P.Hover, "组件选项");
   dots.Click += delegate { c.OpenMenu(dots); };
   foot.Children.Add(Ui.At(dots, 2));
   Grid.SetRow(foot, 2);
   root.Children.Add(foot);
   return root;
  }
 }
}
