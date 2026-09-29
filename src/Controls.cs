using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Kora {
 // Small themed controls. Native combo boxes, date pickers, menus and message boxes ignore the skin
 // (and turn unreadable in dark mode), so the app draws its own.
 public static class Controls {
  public static FrameworkElement Segmented(string[] items, int selected, Action<int> changed) {
   var s = Skin.Current;
   var row = new WrapPanel { Orientation = Orientation.Horizontal };
   var buttons = new List<Button>();
   int current = selected;
   Action paint = delegate {
    for (int i = 0; i < buttons.Count; i++) {
     bool on = i == current;
     buttons[i].Background = Ui.Br(on ? s.P.Accent : "transparent");
     ((TextBlock)buttons[i].Content).Foreground = Ui.Br(on ? s.P.OnAccent : s.P.Sub);
    }
   };
   for (int i = 0; i < items.Length; i++) {
    int index = i;
    var b = Ui.Btn(Ui.T(items[i], 13, s.P.Sub, FontWeights.Medium), () => { current = index; paint(); if (changed != null) changed(index); }, "transparent", s.P.Sub, Math.Max(0, s.ButtonRadius - 2), new Thickness(16, 7, 16, 7));
    buttons.Add(b);
    row.Children.Add(b);
   }
   paint();
   return new Border { Background = Ui.Br(s.P.Hover), CornerRadius = new CornerRadius(s.ButtonRadius), Padding = new Thickness(3), Child = row, HorizontalAlignment = HorizontalAlignment.Left };
  }

  public sealed class Chip : Button {
   public bool On { get; private set; }
   public Chip(string text, bool on) {
    var s = Skin.Current;
    On = on;
    Style = Ui.ButtonStyle(Math.Max(4, s.ButtonRadius - 2));
    Padding = new Thickness(0);
    Width = 44; Height = 34;
    Margin = new Thickness(0, 0, 8, 0);
    Content = Ui.T(text, 13, on ? s.P.OnAccent : s.P.Sub, FontWeights.Medium);
    ((TextBlock)Content).HorizontalAlignment = HorizontalAlignment.Center;
    Paint();
    Click += delegate { On = !On; Paint(); };
   }
   void Paint() {
    var s = Skin.Current;
    Background = Ui.Br(On ? s.P.Accent : s.P.Hover);
    ((TextBlock)Content).Foreground = Ui.Br(On ? s.P.OnAccent : s.P.Sub);
    System.Windows.Automation.AutomationProperties.SetName(this, ((TextBlock)Content).Text + (On ? "，已选" : "，未选"));
   }
  }

  public static void Menu(FrameworkElement anchor, string[] labels, Action[] actions) {
   var s = Skin.Current;
   var stack = new StackPanel { Width = 210 };
   var popup = new Popup { PlacementTarget = anchor, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true, PopupAnimation = PopupAnimation.None };
   for (int i = 0; i < labels.Length; i++) {
    var action = actions[i];
    var item = Ui.Btn(Ui.Line(labels[i], 13, s.P.Text), () => { popup.IsOpen = false; action(); }, "transparent", s.P.Text, Math.Max(0, Math.Min(s.ButtonRadius, 8) - 1), new Thickness(12, 8, 12, 8), s.P.Hover);
    item.HorizontalContentAlignment = HorizontalAlignment.Left;
    stack.Children.Add(item);
   }
   var box = new Border { Background = Ui.Br(s.P.Card), BorderBrush = Ui.Br(s.P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Math.Min(s.CardRadius, 12)), Padding = new Thickness(4), Child = stack, Margin = new Thickness(10), Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 3, Opacity = 0.28 } };
   popup.Child = box;
   popup.HorizontalOffset = Math.Min(0, anchor.ActualWidth - 210 - 20 + 10);
   popup.IsOpen = true;
  }

  public sealed class DateField : Border {
   public DateTime Value { get; private set; }
   readonly TextBlock label;
   readonly Popup popup;
   readonly StackPanel host = new StackPanel { Width = 244 };
   DateTime month;
   public DateField(DateTime initial) {
    var s = Skin.Current;
    Value = initial.Date;
    month = new DateTime(Value.Year, Value.Month, 1);
    label = Ui.T(Format(Value), 14, s.P.Text);
    var row = new Grid();
    row.ColumnDefinitions.Add(new ColumnDefinition());
    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
    row.Children.Add(label);
    var icon = Ui.Icon("week", 16, s.P.Sub);
    Grid.SetColumn(icon, 1);
    row.Children.Add(icon);
    var button = Ui.Btn(row, Open, s.P.Input, s.P.Text, s.InputRadius, new Thickness(12, 9, 12, 9), null, "选择日期");
    button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
    BorderBrush = Ui.Br(s.P.InputLine);
    BorderThickness = new Thickness(1);
    CornerRadius = new CornerRadius(s.InputRadius);
    Child = button;
    popup = new Popup { PlacementTarget = this, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true, PopupAnimation = PopupAnimation.None };
    popup.Child = new Border { Background = Ui.Br(s.P.Card), BorderBrush = Ui.Br(s.P.Line), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Math.Min(s.CardRadius, 12)), Padding = new Thickness(12), Child = host, Margin = new Thickness(10), Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 3, Opacity = 0.28 } };
   }
   static string Format(DateTime d) { return d.Year.ToString(CultureInfo.InvariantCulture) + "年" + Dates.MonthDay(d) + "  " + Dates.Short[(int)d.DayOfWeek]; }
   void Open() { month = new DateTime(Value.Year, Value.Month, 1); Build(); popup.IsOpen = true; }
   void Build() {
    var s = Skin.Current;
    host.Children.Clear();
    var head = new Grid { Margin = new Thickness(0, 0, 0, 8) };
    head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
    head.ColumnDefinitions.Add(new ColumnDefinition());
    head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
    head.Children.Add(Ui.Btn(Ui.Icon("left", 14, s.P.Text), () => { month = month.AddMonths(-1); Build(); }, "transparent", s.P.Text, 6, new Thickness(8), s.P.Hover, "上个月"));
    var title = Ui.T(month.Year.ToString(CultureInfo.InvariantCulture) + "年" + month.Month.ToString(CultureInfo.InvariantCulture) + "月", 14, s.P.Text, FontWeights.SemiBold);
    title.HorizontalAlignment = HorizontalAlignment.Center;
    title.VerticalAlignment = VerticalAlignment.Center;
    Grid.SetColumn(title, 1);
    head.Children.Add(title);
    var next = Ui.Btn(Ui.Icon("right", 14, s.P.Text), () => { month = month.AddMonths(1); Build(); }, "transparent", s.P.Text, 6, new Thickness(8), s.P.Hover, "下个月");
    Grid.SetColumn(next, 2);
    head.Children.Add(next);
    host.Children.Add(head);
    var grid = new UniformGrid { Columns = 7 };
    foreach (var name in new[] { "一", "二", "三", "四", "五", "六", "日" }) {
     var t = Ui.T(name, 11, s.P.Faint);
     t.HorizontalAlignment = HorizontalAlignment.Center;
     t.Margin = new Thickness(0, 4, 0, 4);
     grid.Children.Add(t);
    }
    int lead = ((int)month.DayOfWeek + 6) % 7;
    for (int i = 0; i < lead; i++) grid.Children.Add(new Border());
    for (int d = 1; d <= DateTime.DaysInMonth(month.Year, month.Month); d++) {
     var day = new DateTime(month.Year, month.Month, d);
     bool on = day == Value, today = day == DateTime.Today;
     var cell = Ui.Btn(Ui.T(d.ToString(CultureInfo.InvariantCulture), 13, on ? s.P.OnAccent : today ? s.P.Accent : s.P.Text, today ? FontWeights.Bold : FontWeights.Normal), () => { Value = day; label.Text = Format(day); popup.IsOpen = false; }, on ? s.P.Accent : "transparent", s.P.Text, Math.Max(4, s.ButtonRadius), new Thickness(0, 6, 0, 6), on ? s.P.Accent : s.P.Hover);
     ((TextBlock)cell.Content).HorizontalAlignment = HorizontalAlignment.Center;
     cell.Margin = new Thickness(1);
     grid.Children.Add(cell);
    }
    host.Children.Add(grid);
   }
  }
 }

 public class Dialog : Window {
  public Dialog(Window owner, string title, string message, string yes, string no) {
   var s = Skin.Current;
   Ui.Chromeless(this);
   Title = title;
   ShowInTaskbar = false;
   ResizeMode = ResizeMode.NoResize;
   SizeToContent = SizeToContent.WidthAndHeight;
   if (owner != null && owner.IsVisible) { Owner = owner; WindowStartupLocation = WindowStartupLocation.CenterOwner; } else WindowStartupLocation = WindowStartupLocation.CenterScreen;
   var body = new StackPanel { Width = 372, Margin = new Thickness(26, 24, 26, 22) };
   var head = Ui.T(title, 17, s.P.Text, FontWeights.SemiBold, s.Display);
   body.Children.Add(head);
   var text = Ui.T(message, 13.5, s.P.Sub);
   text.Margin = new Thickness(0, 10, 0, 22);
   body.Children.Add(text);
   var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
   if (no != null) {
    var cancel = s.Secondary(no, () => { DialogResult = false; });
    cancel.Margin = new Thickness(0, 0, 8, 0);
    buttons.Children.Add(cancel);
   }
   buttons.Children.Add(s.Primary(yes, () => { DialogResult = true; }));
   body.Children.Add(buttons);
   Content = Ui.Frame(body, Math.Min(s.WindowRadius, 20), s.FrameFill, s.FrameBorder, 20, s.ShadowOpacity);
   Ui.Drag(this, head);
   KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) DialogResult = false; };
  }
 }

 public static class Dialogs {
  public static bool Show(Window owner, string title, string message, string yes, string no) {
   var dialog = new Dialog(owner, title, message, yes, no);
   return dialog.ShowDialog() == true;
  }
 }

 public class Editor : Window {
  readonly TextBox title, note;
  readonly Controls.Chip[] weekdays = new Controls.Chip[7];
  readonly Controls.DateField once;
  readonly FrameworkElement groupPick, repeatPick;
  readonly StackPanel days;
  readonly List<string> groups = new List<string> { "学习", "健身", "生活" };
  readonly TaskItem original;
  readonly Store store;
  int group, repeat;

  public Editor(Window owner, Store storage, TaskItem task, DateTime selected) {
   var s = Skin.Current;
   Ui.Chromeless(this);
   Owner = owner;
   store = storage;
   original = task;
   Title = task == null ? "新建任务" : "编辑任务";
   Width = 524;
   Height = Math.Min(676, SystemParameters.WorkArea.Height - 20);
   ResizeMode = ResizeMode.NoResize;
   ShowInTaskbar = false;
   WindowStartupLocation = WindowStartupLocation.CenterOwner;
   foreach (var g in store.Read().Tasks.Select(t => t.Group).Distinct()) if (!groups.Contains(g)) groups.Add(g);
   if (task != null && !groups.Contains(task.Group)) groups.Add(task.Group);
   group = task == null ? 0 : groups.IndexOf(task.Group);
   bool isOnce = task != null && !string.IsNullOrEmpty(task.OnceDate);
   repeat = isOnce ? 1 : 0;

   var root = new Grid { Margin = new Thickness(28, 24, 28, 22) };
   root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
   root.RowDefinitions.Add(new RowDefinition());
   root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

   var header = new DockPanel();
   var close = Ui.Btn(Ui.Icon("close", 16, s.P.Sub), () => Close(), "transparent", s.P.Sub, s.ButtonRadius, new Thickness(8), s.P.Hover, "关闭");
   DockPanel.SetDock(close, Dock.Right);
   header.Children.Add(close);
   var heading = Ui.T(Title, 22, s.P.Text, s.TitleWeight, s.Display);
   header.Children.Add(heading);
   root.Children.Add(header);

   var body = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
   body.Children.Add(s.Label("任务名称"));
   title = new TextBox { Text = task == null ? "" : task.Title, MaxLength = 80 };
   body.Children.Add(title);
   body.Children.Add(s.Label("一点备注（可选）"));
   note = new TextBox { Text = task == null ? "" : task.Note, MaxLength = 300, Height = 74, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
   body.Children.Add(note);
   body.Children.Add(s.Label("分组"));
   groupPick = Controls.Segmented(groups.Select(g => string.IsNullOrEmpty(g) ? "未分组" : g).ToArray(), group, i => group = i);
   body.Children.Add(groupPick);
   body.Children.Add(s.Label("重复方式"));
   repeatPick = Controls.Segmented(new[] { "每周重复", "仅指定日期" }, repeat, i => { repeat = i; UpdateMode(); });
   body.Children.Add(repeatPick);

   days = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
   foreach (int i in Dates.Order) {
    var chip = new Controls.Chip(Dates.Short[i].Substring(1), task == null || isOnce ? task == null : task.Days.Contains(i));
    weekdays[i] = chip;
    days.Children.Add(chip);
   }
   body.Children.Add(days);
   once = new Controls.DateField(isOnce ? Dates.Parse(task.OnceDate) : selected) { Margin = new Thickness(0, 16, 0, 0), Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
   body.Children.Add(once);
   var hint = Ui.T("修改重复规则会同步到每周计划。已有的每日勾选记录会保留。", 12, s.P.Sub);
   hint.Margin = new Thickness(0, 16, 0, 4);
   body.Children.Add(hint);
   UpdateMode();
   var scroll = Ui.Scroll(body);
   Grid.SetRow(scroll, 1);
   scroll.Margin = new Thickness(0, 4, -10, 12);
   root.Children.Add(scroll);

   var actions = new DockPanel();
   if (task != null) {
    var delete = Ui.Btn(Ui.T("删除任务", 13, s.P.Danger, FontWeights.Medium), Delete, "transparent", s.P.Danger, s.ButtonRadius, new Thickness(12, 9, 12, 9), s.P.Hover);
    DockPanel.SetDock(delete, Dock.Left);
    actions.Children.Add(delete);
   }
   var save = s.Primary("保存任务", Save);
   save.HorizontalAlignment = HorizontalAlignment.Right;
   actions.Children.Add(save);
   Grid.SetRow(actions, 2);
   root.Children.Add(actions);

   Content = Ui.Frame(root, Math.Min(s.WindowRadius, 24), s.FrameFill, s.FrameBorder, 20, s.ShadowOpacity);
   Ui.Drag(this, heading);
   Loaded += delegate { title.Focus(); title.SelectAll(); };
   KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); };
  }

  void UpdateMode() {
   if (once == null || days == null) return;
   days.Visibility = repeat == 0 ? Visibility.Visible : Visibility.Collapsed;
   once.Visibility = repeat == 1 ? Visibility.Visible : Visibility.Collapsed;
  }

  void Save() {
   string name = title.Text.Trim();
   if (name.Length == 0) throw new InvalidDataException("请输入任务名称。");
   var days = Dates.Order.Where(i => weekdays[i].On).ToList();
   if (repeat == 0 && days.Count == 0) throw new InvalidDataException("请至少选择一天。");
   var t = new TaskItem {
    Id = original == null ? Guid.NewGuid().ToString("N") : original.Id,
    Title = name,
    Note = note.Text.Trim(),
    Group = groups[group],
    Days = repeat == 0 ? days : new List<int>(),
    OnceDate = repeat == 1 ? Dates.Key(once.Value) : ""
   };
   store.Update(s => {
    int i = s.Tasks.FindIndex(x => x.Id == t.Id);
    if (i < 0) s.Tasks.Add(t); else s.Tasks[i] = t;
   });
   DialogResult = true;
  }

  void Delete() {
   if (!Ui.Ask(this, "删除任务", "删除“" + original.Title + "”？历史完成记录会保留。", "删除", "取消")) return;
   store.Update(s => s.Tasks.RemoveAll(t => t.Id == original.Id));
   DialogResult = true;
  }
 }
}
