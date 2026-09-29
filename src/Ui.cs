using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Shapes = System.Windows.Shapes;

namespace Kora {
 // Polling must not recursively open modal errors or repeat the same error every second.
 public sealed class PollGuard {
  bool busy;string lastError;readonly Action<Exception> report;
  public PollGuard() : this(e=>Ui.Tell(null,"Kora Desk · 操作未完成",e.Message)) {}
  public PollGuard(Action<Exception> reporter){report=reporter;}
  public void Run(Action action){
   if(busy)return;busy=true;
   try{action();lastError=null;}
   catch(Exception e){string key=e.GetType().FullName+":"+e.Message;if(key!=lastError){lastError=key;report(e);}}
   finally{busy=false;}
  }
 }
 public static class Ui {
  static readonly Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();
  static readonly Dictionary<string, Style> styles = new Dictionary<string, Style>();
  static readonly Dictionary<string, Geometry> geometries = new Dictionary<string, Geometry>();

  // Icons are drawn on a 24 x 24 grid with round strokes, so they scale like the SVG originals.
  static readonly Dictionary<string, string> icons = new Dictionary<string, string> {
   {"today", "M3.5,12 A8.5,8.5 0 1 1 20.5,12 A8.5,8.5 0 1 1 3.5,12 M12,7.5 L12,12 L15,14"},
   {"week", "M6.5,5.5 L17.5,5.5 A2.5,2.5 0 0 1 20,8 L20,17 A2.5,2.5 0 0 1 17.5,19.5 L6.5,19.5 A2.5,2.5 0 0 1 4,17 L4,8 A2.5,2.5 0 0 1 6.5,5.5 Z M4,10 L20,10 M8.5,3.5 L8.5,6.5 M15.5,3.5 L15.5,6.5"},
   {"settings", "M4,7 L13,7 M17,7 L20,7 M4,17 L7,17 M11,17 L20,17 M13,7 A2,2 0 1 1 17,7 A2,2 0 1 1 13,7 M7,17 A2,2 0 1 1 11,17 A2,2 0 1 1 7,17"},
   {"plus", "M12,5 L12,19 M5,12 L19,12"},
   {"dots", "M5,12 L5.01,12 M12,12 L12.01,12 M19,12 L19.01,12"},
   {"lock", "M7,11 L17,11 A2,2 0 0 1 19,13 L19,18 A2,2 0 0 1 17,20 L7,20 A2,2 0 0 1 5,18 L5,13 A2,2 0 0 1 7,11 Z M8,11 L8,8 A4,4 0 0 1 16,8 L16,11"},
   {"left", "M14,6 L8,12 L14,18"},
   {"right", "M10,6 L16,12 L10,18"},
   {"min", "M5,12 L19,12"},
   {"max", "M7.5,6 L16.5,6 A1.5,1.5 0 0 1 18,7.5 L18,16.5 A1.5,1.5 0 0 1 16.5,18 L7.5,18 A1.5,1.5 0 0 1 6,16.5 L6,7.5 A1.5,1.5 0 0 1 7.5,6 Z"},
   {"close", "M6,6 L18,18 M18,6 L6,18"},
   {"search", "M4.5,11 A6.5,6.5 0 1 1 17.5,11 A6.5,6.5 0 1 1 4.5,11 M20,20 L15.5,15.5"},
   {"out", "M7,17 L17,7 M9,7 L17,7 L17,15"},
   {"check", "M5,12.5 L9.5,17 L19,7.5"}
  };

  public static Brush Br(string hex) {
   Brush b;
   if (brushes.TryGetValue(hex, out b)) return b;
   if (hex == "transparent") b = Brushes.Transparent;
   else { b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); }
   brushes[hex] = b;
   return b;
  }

  public static FontFamily Font(string spec) { return new FontFamily(spec); }

  // Fonts shipped next to the executable (currently the dot-matrix face). Falls back to the given system family.
  public static FontFamily Bundled(string file, string family, string fallback) {
   try {
    string folder = Path.Combine(Launch.BaseDirectory, "fonts");
    if (File.Exists(Path.Combine(folder, file))) return new FontFamily(new Uri("file:///" + folder.Replace('\\', '/') + "/"), "./#" + family);
   } catch (Exception) { }
   return new FontFamily(fallback);
  }

  public static void Apply(Skin skin) {
   Skin.Current = skin;
   styles.Clear();
   var app = Application.Current;
   if (app == null) return;
   var p = skin.P;
   app.Resources = (ResourceDictionary)XamlReader.Parse(Xaml.Resources(skin.Body.Source, p.Text, p.Thumb, p.Input, p.InputLine, p.Accent, skin.InputRadius));
  }

  static Style Cached(string key, Func<string> xaml) {
   Style s;
   if (!styles.TryGetValue(key, out s)) { s = (Style)XamlReader.Parse(xaml()); styles[key] = s; }
   return s;
  }
  public static Style ButtonStyle(double radius) { return Cached("b" + radius + Skin.Current.Id + Skin.Current.Dark, () => Xaml.ButtonStyle(radius, Skin.Current.P.Accent)); }

  public static TextBlock T(string text, double size, string color, FontWeight? weight = null, FontFamily font = null) {
   return new TextBlock { Text = text, FontSize = size, Foreground = Br(color), TextWrapping = TextWrapping.Wrap, FontWeight = weight ?? FontWeights.Normal, FontFamily = font ?? Skin.Current.Body };
  }
  public static TextBlock Line(string text, double size, string color, FontWeight? weight = null, FontFamily font = null) {
   var t = T(text, size, color, weight, font);
   t.TextWrapping = TextWrapping.NoWrap;
   t.TextTrimming = TextTrimming.CharacterEllipsis;
   return t;
  }

  public static FrameworkElement Icon(string name, double size, string color, double stroke = 1.8) {
   Geometry g;
   if (!geometries.TryGetValue(name, out g)) { g = Geometry.Parse(icons[name]); g.Freeze(); geometries[name] = g; }
   var path = new Shapes.Path { Data = g, Stroke = Br(color), StrokeThickness = stroke, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
   var canvas = new Canvas { Width = 24, Height = 24 };
   canvas.Children.Add(path);
   return new Viewbox { Width = size, Height = size, Child = canvas, IsHitTestVisible = false };
  }

  public static Button Btn(object content, Action action, string bg, string fg, double radius, Thickness padding, string hover = null, string tip = null) {
   var b = new Button { Content = content, Padding = padding, Foreground = Br(fg), Background = Br(bg), Style = ButtonStyle(radius), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
   if (tip != null) { b.ToolTip = tip; System.Windows.Automation.AutomationProperties.SetName(b, tip); }
   if (hover != null) {
    b.MouseEnter += delegate { b.Background = Br(hover); };
    b.MouseLeave += delegate { b.Background = Br(bg); };
   }
   if (action != null) b.Click += delegate { Guard(action); };
   return b;
  }

  public static void Hover(Border b, string normal, string hover) {
   b.Background = Br(normal);
   b.MouseEnter += delegate { b.Background = Br(hover); };
   b.MouseLeave += delegate { b.Background = Br(normal); };
  }

  public static void Guard(Action action) {
   try { action(); } catch (Exception e) { Tell(null, "Kora Desk · 操作未完成", e.Message); }
  }

  public static ScrollViewer Scroll(UIElement child) {
   return new ScrollViewer { Name = "PageScroll", Content = child, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.VerticalOnly };
  }

  public static Border Card(UIElement child, Thickness padding) {
   var s = Skin.Current;
   return new Border { Background = Br(s.P.Card), BorderBrush = Br(s.P.CardLine), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(s.CardRadius), Padding = padding, Child = child };
  }

  public static Shapes.Rectangle Rule(string color, double thickness, Thickness margin) {
   return new Shapes.Rectangle { Height = thickness, Fill = Br(color), Margin = margin, IsHitTestVisible = false };
  }

  // ----- windows -----
  public static void Chromeless(Window w) {
   w.WindowStyle = WindowStyle.None;
   w.AllowsTransparency = true;
   w.Background = Brushes.Transparent;
   w.UseLayoutRounding = true;
  }
  public static Border Frame(UIElement child, double radius, string fill, string border, double blur, double opacity) {
   var host = new Grid();
   host.Children.Add(child);
   host.SizeChanged += delegate { host.Clip = new RectangleGeometry(new Rect(0, 0, host.ActualWidth, host.ActualHeight), Math.Max(0, radius - 1), Math.Max(0, radius - 1)); };
   return new Border { Background = Br(fill), CornerRadius = new CornerRadius(radius), BorderBrush = Br(border), BorderThickness = new Thickness(1), Margin = new Thickness(12), Child = host, Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = blur, ShadowDepth = 3, Opacity = opacity } };
  }
  public static void Drag(Window w, UIElement grip) {
   grip.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed) w.DragMove(); };
  }
  public static void Snapshot(FrameworkElement element, string path) {
   element.UpdateLayout();
   var dpi = VisualTreeHelper.GetDpi(element);
   var bmp = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
   bmp.Render(element);
   var encoder = new PngBitmapEncoder();
   encoder.Frames.Add(BitmapFrame.Create(bmp));
   using (var stream = File.Create(path)) encoder.Save(stream);
  }

  // ----- checkbox rows -----
  public static CheckBox Check(bool done, string name, Action<bool> toggle, bool inWidget = false) {
   var s = Skin.Current;
   var box = new CheckBox { IsChecked = done, Style = Cached("c" + s.Id + s.Dark + inWidget, inWidget ? (Func<string>)s.WidgetCheckXaml : s.CheckXaml), VerticalAlignment = VerticalAlignment.Center };
   System.Windows.Automation.AutomationProperties.SetName(box, name);
   box.Click += delegate {
    bool value = box.IsChecked == true;
    try { toggle(value); } catch (Exception e) { box.IsChecked = !value; Tell(null, "Kora Desk · 操作未完成", e.Message); }
   };
   return box;
  }
  // Makes the whole row toggle its check box, not just the box itself.
  public static void RowClick(FrameworkElement row, CheckBox box) {
   var panel = row as Panel;
   var border = row as Border;
   if (panel != null && panel.Background == null) panel.Background = Brushes.Transparent;
   if (border != null && border.Background == null) border.Background = Brushes.Transparent;
   row.Cursor = Cursors.Hand;
   row.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
    var source = e.OriginalSource as DependencyObject;
    if (source != null && IsInside(source, box)) return;
    if (source != null && FindButton(source, row) != null) return;
    box.IsChecked = !(box.IsChecked == true);
    box.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
   };
  }
  // A click on another button inside the row (edit, menu) must not also toggle the task.
  static Button FindButton(DependencyObject from, DependencyObject stop) {
   while (from != null && !ReferenceEquals(from, stop)) {
    var b = from as Button;
    if (b != null) return b;
    from = from is Visual ? VisualTreeHelper.GetParent(from) : LogicalTreeHelper.GetParent(from);
   }
   return null;
  }
  static bool IsInside(DependencyObject child, DependencyObject ancestor) {
   while (child != null) {
    if (ReferenceEquals(child, ancestor)) return true;
    child = child is Visual ? VisualTreeHelper.GetParent(child) : LogicalTreeHelper.GetParent(child);
   }
   return false;
  }

  // ----- drawn elements -----
  public static FrameworkElement Ring(double size, double thick, double p, string track, string bar) {
   var c = new Canvas { Width = size, Height = size };
   double r = (size - thick) / 2, mid = size / 2;
   var back = new Shapes.Ellipse { Width = size - thick, Height = size - thick, Stroke = Br(track), StrokeThickness = thick };
   Canvas.SetLeft(back, thick / 2); Canvas.SetTop(back, thick / 2);
   c.Children.Add(back);
   if (p >= 0.999) {
    var full = new Shapes.Ellipse { Width = size - thick, Height = size - thick, Stroke = Br(bar), StrokeThickness = thick };
    Canvas.SetLeft(full, thick / 2); Canvas.SetTop(full, thick / 2);
    c.Children.Add(full);
   } else if (p > 0) {
    double a = 2 * Math.PI * p;
    var figure = new PathFigure { StartPoint = new Point(mid, mid - r), IsClosed = false };
    figure.Segments.Add(new ArcSegment(new Point(mid + r * Math.Sin(a), mid - r * Math.Cos(a)), new Size(r, r), 0, a > Math.PI, SweepDirection.Clockwise, true));
    var geo = new PathGeometry();
    geo.Figures.Add(figure);
    c.Children.Add(new Shapes.Path { Data = geo, Stroke = Br(bar), StrokeThickness = thick, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
   }
   return c;
  }

  // Expressive progress: a wave over the finished part, a straight track with an end dot for the rest.
  public static FrameworkElement Wave(double width, double p, string bar, string track, double height = 16) {
   var c = new Canvas { Width = width, Height = height };
   double mid = height / 2, end = width * p - 6;
   if (end > 6) {
    var figure = new PathFigure { StartPoint = new Point(4, mid), IsClosed = false };
    var points = new PointCollection();
    for (double x = 4; x <= end; x += 2) points.Add(new Point(x, mid + 4 * Math.Sin((x - 4) / 22 * 2 * Math.PI)));
    figure.Segments.Add(new PolyLineSegment(points, true));
    var geo = new PathGeometry();
    geo.Figures.Add(figure);
    c.Children.Add(new Shapes.Path { Data = geo, Stroke = Br(bar), StrokeThickness = 4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round });
   }
   double start = Math.Max(4, end + 12);
   c.Children.Add(new Shapes.Line { X1 = start, Y1 = mid, X2 = width - 6, Y2 = mid, Stroke = Br(track), StrokeThickness = 4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
   var dot = new Shapes.Ellipse { Width = 5, Height = 5, Fill = Br(bar) };
   Canvas.SetLeft(dot, width - 5.5); Canvas.SetTop(dot, mid - 2.5);
   c.Children.Add(dot);
   return c;
  }

  // Nine-lobed scalloped badge.
  public static Shapes.Path Cookie(double size, string fill) {
   var points = new PointCollection();
   double half = size / 2, r = half * 0.9;
   for (int i = 0; i <= 120; i++) {
    double t = i / 120.0 * 2 * Math.PI, rr = r * (1 + 0.05 * Math.Cos(9 * t));
    points.Add(new Point(half + rr * Math.Cos(t), half + rr * Math.Sin(t)));
   }
   var figure = new PathFigure { StartPoint = points[0], IsClosed = true, IsFilled = true };
   figure.Segments.Add(new PolyLineSegment(points, true));
   var geo = new PathGeometry();
   geo.Figures.Add(figure);
   return new Shapes.Path { Data = geo, Fill = Br(fill), Width = size, Height = size };
  }

  public static Brush DotGrid(string color, double pitch) {
   var group = new DrawingGroup();
   group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, pitch, pitch))));
   group.Children.Add(new GeometryDrawing(Br(color), null, new EllipseGeometry(new Point(pitch / 2, pitch / 2), 0.9, 0.9)));
   var brush = new DrawingBrush(group) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, pitch, pitch), ViewportUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, pitch, pitch), ViewboxUnits = BrushMappingMode.Absolute };
   brush.Freeze();
   return brush;
  }

  public static Shapes.Line Dashed(string color, double dash = 1, double gap = 3) {
   return new Shapes.Line { X1 = 0, Y1 = 0, X2 = 1, Y2 = 0, Stretch = Stretch.Fill, Height = 1, Stroke = Br(color), StrokeThickness = 1, StrokeDashArray = new DoubleCollection(new double[] { dash, gap }), StrokeDashCap = PenLineCap.Round, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
  }

  // ----- layout helpers -----
  static GridLength Len(string w) {
   if (w == "auto") return GridLength.Auto;
   if (w.EndsWith("*")) return new GridLength(w.Length == 1 ? 1 : double.Parse(w.TrimEnd('*'), System.Globalization.CultureInfo.InvariantCulture), GridUnitType.Star);
   return new GridLength(double.Parse(w, System.Globalization.CultureInfo.InvariantCulture));
  }
  public static Grid Cols(params string[] widths) {
   var g = new Grid();
   foreach (var w in widths) g.ColumnDefinitions.Add(new ColumnDefinition { Width = Len(w) });
   return g;
  }
  public static Grid Rows(params string[] heights) {
   var g = new Grid();
   foreach (var h in heights) g.RowDefinitions.Add(new RowDefinition { Height = Len(h) });
   return g;
  }
  public static E At<E>(E element, int column, int row = 0) where E : UIElement {
   Grid.SetColumn(element, column);
   Grid.SetRow(element, row);
   return element;
  }
  public static FrameworkElement Bar(double p, string track, string fill, double height, double radius) {
   p = Math.Max(0, Math.Min(1, p));
   var g = new Grid { Height = height };
   g.Children.Add(new Border { Background = Br(track), CornerRadius = new CornerRadius(radius) });
   var split = Cols(p.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "*", (1 - p).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "*");
   split.Children.Add(new Border { Background = Br(fill), CornerRadius = new CornerRadius(radius) });
   g.Children.Add(split);
   return g;
  }
  public static Shapes.Ellipse Dot(double size, string color) { return new Shapes.Ellipse { Width = size, Height = size, Fill = Br(color) }; }

  // Rebuilding a page throws away the scroll position and keyboard focus; this puts them back.
  public sealed class Keep {
   readonly double offset;
   readonly string tag;
   Keep(double offset, string tag) { this.offset = offset; this.tag = tag; }
   public static Keep Take(DependencyObject root) {
   double o = 0;
   if (root != null) { var sv = Find<ScrollViewer>(root, x => x.Name == "PageScroll"); if (sv != null) o = sv.VerticalOffset; }
   var focused = Keyboard.FocusedElement as FrameworkElement;
   string t = focused == null ? null : focused.Tag as string;
   bool byKeyboard = InputManager.Current.MostRecentInputDevice is KeyboardDevice;
   return new Keep(o, byKeyboard && t != null && t.StartsWith("task:") ? t : null);
   }
   public void Restore(DependencyObject root, System.Windows.Threading.Dispatcher dispatcher, bool scroll) {
    dispatcher.BeginInvoke(new Action(delegate {
     if (scroll && offset > 0) { var sv = Find<ScrollViewer>(root, x => x.Name == "PageScroll"); if (sv != null) sv.ScrollToVerticalOffset(offset); }
     if (tag != null) { var el = Find<FrameworkElement>(root, x => x.Tag as string == tag); if (el != null) el.Focus(); }
    }), System.Windows.Threading.DispatcherPriority.Loaded);
   }
  }
  static E Find<E>(DependencyObject root, Func<E, bool> match) where E : DependencyObject {
   if (root is E && match((E)root)) return (E)root;
   foreach (object child in LogicalTreeHelper.GetChildren(root)) {
    var d = child as DependencyObject;
    if (d == null) continue;
    var found = Find(d, match);
    if (found != null) return found;
   }
   return null;
  }

  public static void Tell(Window owner, string title, string message) { Dialogs.Show(owner, title, message, "好", null); }
  public static bool Ask(Window owner, string title, string message, string yes, string no) { return Dialogs.Show(owner, title, message, yes, no); }
 }
}
