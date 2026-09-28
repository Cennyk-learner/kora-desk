using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Kora {
 public static class Ui {
  public static Brush Ink=B("#263831"), Muted=B("#7C8780"), Accent=B("#557665"), Line=B("#E7EBE5"), Paper=B("#FAFBF8"), Soft=B("#EEF2EC");
  public static Brush B(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
  public static void Init(Application app) {
   app.Resources=(ResourceDictionary)XamlReader.Parse(@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='Window'><Setter Property='FontFamily' Value='Segoe UI, Microsoft YaHei UI'/><Setter Property='FontSize' Value='14'/><Setter Property='Foreground' Value='#263831'/><Setter Property='UseLayoutRounding' Value='True'/></Style>
<Style TargetType='Button'><Setter Property='Background' Value='Transparent'/><Setter Property='Foreground' Value='#56675D'/><Setter Property='Padding' Value='14,9'/><Setter Property='BorderThickness' Value='0'/><Setter Property='Cursor' Value='Hand'/><Setter Property='HorizontalContentAlignment' Value='Center'/><Setter Property='VerticalContentAlignment' Value='Center'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='10' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bd' Property='Opacity' Value='0.72'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='bd' Property='BorderBrush' Value='#557665'/><Setter TargetName='bd' Property='BorderThickness' Value='1'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.4'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='CheckBox'><Setter Property='Cursor' Value='Hand'/><Setter Property='VerticalContentAlignment' Value='Center'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><Grid Background='Transparent'><Grid.ColumnDefinitions><ColumnDefinition Width='28'/><ColumnDefinition Width='*'/></Grid.ColumnDefinitions><Border x:Name='box' Width='20' Height='20' CornerRadius='7' BorderThickness='1.4' BorderBrush='#BDC9BF' VerticalAlignment='Center'><Path x:Name='tick' Data='M 4,9 L 8,13 L 15,5' Stroke='White' StrokeThickness='1.7' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Visibility='Collapsed'/></Border><ContentPresenter Grid.Column='1' Margin='8,0,0,0' VerticalAlignment='Center'/></Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='box' Property='Background' Value='#557665'/><Setter TargetName='box' Property='BorderBrush' Value='#557665'/><Setter TargetName='tick' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='#557665'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='box' Property='BorderThickness' Value='2.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='TextBox'><Setter Property='Padding' Value='12,10'/><Setter Property='BorderBrush' Value='#DDE4DC'/><Setter Property='BorderThickness' Value='1'/><Setter Property='Background' Value='#FFFFFF'/><Setter Property='Foreground' Value='#263831'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TextBox'><Border x:Name='bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='8'><ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}'/></Border><ControlTemplate.Triggers><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='bd' Property='BorderBrush' Value='#557665'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ScrollViewer'><Setter Property='VerticalScrollBarVisibility' Value='Auto'/><Setter Property='HorizontalScrollBarVisibility' Value='Disabled'/><Setter Property='PanningMode' Value='VerticalOnly'/></Style>
</ResourceDictionary>");
  }
  public static TextBlock Text(string text,double size,Brush brush=null) { return new TextBlock{Text=text,FontSize=size,Foreground=brush??Ink,TextWrapping=TextWrapping.Wrap}; }
  public static Button Button(string text,Action action,bool primary=false) { var b=new Button{Content=text}; if(primary){b.Background=Accent;b.Foreground=Brushes.White;} b.Click+=delegate { Guard(action); }; return b; }
  public static void Guard(Action action) { try { action(); } catch(Exception e) { MessageBox.Show(e.Message,"Kora Desk · 操作未完成",MessageBoxButton.OK,MessageBoxImage.Warning); } }
  public static void Label(Panel panel,string text) { var t=Text(text,12,Muted); t.Margin=new Thickness(0,18,0,8); panel.Children.Add(t); }
  public static Border Card(UIElement child,Thickness padding) { return new Border{Background=Brushes.White,BorderBrush=Line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Padding=padding,Child=child}; }
  public static Border WindowFrame(Window w,UIElement child,double radius=22) { w.WindowStyle=WindowStyle.None;w.AllowsTransparency=true;w.Background=Brushes.Transparent; var b=new Border{Background=Paper,CornerRadius=new CornerRadius(radius),BorderBrush=B("#DCE3DA"),BorderThickness=new Thickness(1),Margin=new Thickness(12),Child=child,Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=20,ShadowDepth=3,Opacity=0.12}}; b.Child=null;var clipHost=new Grid();clipHost.Children.Add(child);b.Child=clipHost;clipHost.SizeChanged+=delegate{clipHost.Clip=new RectangleGeometry(new Rect(0,0,clipHost.ActualWidth,clipHost.ActualHeight),radius-1,radius-1);};w.Content=b;return b; }
  public static void Drag(Window w,UIElement grip) { grip.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){ if(e.ClickCount==1 && e.LeftButton==MouseButtonState.Pressed) w.DragMove(); }; }
  public static void Snapshot(FrameworkElement element,string path) { element.UpdateLayout();var dpi=VisualTreeHelper.GetDpi(element);var bmp=new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth*dpi.DpiScaleX),(int)Math.Ceiling(element.ActualHeight*dpi.DpiScaleY),dpi.PixelsPerInchX,dpi.PixelsPerInchY,PixelFormats.Pbgra32);bmp.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var stream=File.Create(path))encoder.Save(stream); }
  public static UIElement TaskRow(TaskItem task,bool done,Action<bool> toggle,Action edit,bool compact) {
   var row=new Grid{Margin=new Thickness(0,3,0,3)}; row.ColumnDefinitions.Add(new ColumnDefinition()); if(edit!=null)row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
   var copy=new StackPanel();var title=Text(task.Title,compact?14:15,done?Muted:Ink);title.FontWeight=FontWeights.Medium;if(done)title.TextDecorations=TextDecorations.Strikethrough;copy.Children.Add(title);
   if(!compact&&!string.IsNullOrWhiteSpace(task.Note)){var note=Text(task.Note,12,Muted);note.Margin=new Thickness(0,5,0,0);copy.Children.Add(note);}
   var check=new CheckBox{Content=copy,IsChecked=done,Padding=new Thickness(0),VerticalContentAlignment=VerticalAlignment.Center,MinHeight=compact?39:43,ToolTip=task.Note};
   System.Windows.Automation.AutomationProperties.SetName(check,task.Title);
   check.Click+=delegate { Guard(delegate { toggle(check.IsChecked==true); }); };row.Children.Add(check);
   if(edit!=null){var b=Button("···",edit);b.FontSize=20;b.Padding=new Thickness(12,4,12,4);b.VerticalAlignment=VerticalAlignment.Center;b.ToolTip="编辑任务";Grid.SetColumn(b,1);row.Children.Add(b);}return row;
  }
 }
 public class Editor : Window {
  TextBox title,note;ComboBox group,repeat;DatePicker once;CheckBox[] weekdays=new CheckBox[7];TaskItem original;Store store;StackPanel days;Button delete;
  public Editor(Window owner,Store storage,TaskItem task,DateTime selected) {
   Owner=owner;store=storage;original=task;Title=task==null?"新建任务":"编辑任务";Width=500;Height=630;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
   var body=new StackPanel{Margin=new Thickness(28)};var header=new DockPanel();var close=Ui.Button("×",Close);close.FontSize=20;DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);var heading=Ui.Text(Title,23);heading.FontWeight=FontWeights.SemiBold;header.Children.Add(heading);body.Children.Add(header);
   Ui.Label(body,"任务名称");title=new TextBox{Text=task==null?"":task.Title,MaxLength=80};body.Children.Add(title);
   Ui.Label(body,"一点备注（可选）");note=new TextBox{Text=task==null?"":task.Note,MaxLength=300,Height=70,TextWrapping=TextWrapping.Wrap,AcceptsReturn=true};body.Children.Add(note);
   Ui.Label(body,"分组");group=new ComboBox{ItemsSource=new[]{"学习","健身","生活"},SelectedItem=task==null?"学习":task.Group,Padding=new Thickness(9)};if(group.SelectedIndex<0)group.SelectedIndex=2;body.Children.Add(group);
   Ui.Label(body,"重复方式");repeat=new ComboBox{ItemsSource=new[]{"每周重复","仅指定日期"},SelectedIndex=task!=null&&!string.IsNullOrEmpty(task.OnceDate)?1:0,Padding=new Thickness(9)};body.Children.Add(repeat);
   days=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,16,0,12)};
   foreach(int i in Dates.Order){var c=new CheckBox{Content=Dates.Short[i].Substring(1),IsChecked=task==null||task.Days.Contains(i),Margin=new Thickness(0,0,11,0)}; weekdays[i]=c;days.Children.Add(c);}body.Children.Add(days);
   once=new DatePicker{SelectedDate=task!=null&&!string.IsNullOrEmpty(task.OnceDate)?DateTime.ParseExact(task.OnceDate,"yyyy-MM-dd",null):selected,Margin=new Thickness(0,12,0,12)};body.Children.Add(once);repeat.SelectionChanged+=delegate{UpdateMode();};UpdateMode();
   var hint=Ui.Text("修改重复规则会同步到每周计划。已有的每日勾选记录会保留。",12,Ui.Muted);hint.Margin=new Thickness(0,8,0,18);body.Children.Add(hint);
   var actions=new DockPanel();delete=Ui.Button("删除任务",Delete);delete.Foreground=Ui.B("#A15F55");delete.Visibility=task==null?Visibility.Collapsed:Visibility.Visible;DockPanel.SetDock(delete,Dock.Left);actions.Children.Add(delete);var save=Ui.Button("保存任务",Save,true);save.HorizontalAlignment=HorizontalAlignment.Right;actions.Children.Add(save);body.Children.Add(actions);Ui.WindowFrame(this,body);Ui.Drag(this,heading);Loaded+=delegate{title.Focus();};
  }
  void UpdateMode(){if(once==null)return;days.Visibility=repeat.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;once.Visibility=repeat.SelectedIndex==1?Visibility.Visible:Visibility.Collapsed;}
  void Save(){var t=new TaskItem{Id=original==null?Guid.NewGuid().ToString("N"):original.Id,Title=title.Text.Trim(),Note=note.Text.Trim(),Group=(string)group.SelectedItem,Days=Dates.Order.Where(i=>weekdays[i].IsChecked==true).ToList(),OnceDate=repeat.SelectedIndex==1&&once.SelectedDate.HasValue?Dates.Key(once.SelectedDate.Value):""};if(repeat.SelectedIndex==1&&!once.SelectedDate.HasValue)throw new InvalidDataException("请选择日期。");store.Update(s=>{int i=s.Tasks.FindIndex(x=>x.Id==t.Id);if(i<0)s.Tasks.Add(t);else s.Tasks[i]=t;});DialogResult=true;}
  void Delete(){if(MessageBox.Show(this,"删除“"+original.Title+"”？历史完成记录会保留。","删除任务",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;store.Update(s=>s.Tasks.RemoveAll(t=>t.Id==original.Id));DialogResult=true;}
 }
}
