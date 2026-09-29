using System;
using System.Globalization;
using System.Text;

namespace Kora {
 // XAML text for the templated controls. Kept free of WPF types so the strings can be linted anywhere.
 public static class Xaml {
  const string Ns = "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'";
  static string N(double v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }
  static string C(string color) { return color == "transparent" ? "#00000000" : color; }

  public static string Resources(string font, string text, string thumb, string inputBg, string inputBorder, string accent, double inputRadius) {
   return ("<ResourceDictionary " + Ns + ">" +
    "<Style TargetType='Window'><Setter Property='FontFamily' Value='@FONT@'/><Setter Property='FontSize' Value='14'/><Setter Property='Foreground' Value='@TEXT@'/><Setter Property='UseLayoutRounding' Value='True'/></Style>" +
    "<Style TargetType='TextBox'><Setter Property='Padding' Value='12,9'/><Setter Property='BorderBrush' Value='@BORDER@'/><Setter Property='BorderThickness' Value='1'/><Setter Property='Background' Value='@INPUT@'/><Setter Property='Foreground' Value='@TEXT@'/><Setter Property='CaretBrush' Value='@TEXT@'/>" +
    "<Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TextBox'><Border x:Name='bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='@RADIUS@'><ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}'/></Border>" +
    "<ControlTemplate.Triggers><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='bd' Property='BorderBrush' Value='@ACCENT@'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>" +
    "<Style TargetType='ScrollViewer'><Setter Property='VerticalScrollBarVisibility' Value='Auto'/><Setter Property='HorizontalScrollBarVisibility' Value='Disabled'/><Setter Property='PanningMode' Value='VerticalOnly'/></Style>" +
    "<Style TargetType='ScrollBar'><Setter Property='Width' Value='10'/><Setter Property='MinWidth' Value='10'/><Setter Property='Background' Value='Transparent'/>" +
    "<Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid Background='Transparent'><Track x:Name='PART_Track' IsDirectionReversed='True'>" +
    "<Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageUpCommand' Focusable='False' IsTabStop='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>" +
    "<Track.Thumb><Thumb Focusable='False'><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Margin='2,0' CornerRadius='3' Background='@THUMB@'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>" +
    "<Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' Focusable='False' IsTabStop='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>" +
    "</Track></Grid></ControlTemplate></Setter.Value></Setter></Style>" +
    "</ResourceDictionary>")
    .Replace("@FONT@", font).Replace("@TEXT@", C(text)).Replace("@THUMB@", C(thumb)).Replace("@INPUT@", C(inputBg)).Replace("@BORDER@", C(inputBorder)).Replace("@ACCENT@", C(accent)).Replace("@RADIUS@", N(inputRadius));
  }

  public static string ButtonStyle(double radius, string focus) {
   return ("<Style " + Ns + " TargetType='Button'><Setter Property='Cursor' Value='Hand'/><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='BorderThickness' Value='0'/>" +
    "<Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='@RADIUS@' Padding='{TemplateBinding Padding}' BorderThickness='0' BorderBrush='@FOCUS@' SnapsToDevicePixels='True'>" +
    "<ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='{TemplateBinding VerticalContentAlignment}'/></Border>" +
    "<ControlTemplate.Triggers><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='bd' Property='BorderThickness' Value='1.5'/></Trigger>" +
    "<Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.4'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>")
    .Replace("@RADIUS@", N(radius)).Replace("@FOCUS@", C(focus));
  }

  // A check box that draws only the box. The label lives beside it in the row, so the whole row can be the click target.
  public static string CheckStyle(double size, double radius, double thickness, string off, string onFill, string onBorder, string tick, string dot, string focus) {
   off = C(off); onFill = C(onFill); onBorder = C(onBorder); tick = C(tick); dot = dot == null ? null : C(dot); focus = C(focus);
   var sb = new StringBuilder();
   sb.Append("<Style " + Ns + " TargetType='CheckBox'><Setter Property='Cursor' Value='Hand'/><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'>");
   sb.Append("<Grid Width='" + N(size) + "' Height='" + N(size) + "' Background='Transparent' SnapsToDevicePixels='True'>");
   sb.Append("<Border x:Name='box' CornerRadius='" + N(radius) + "' BorderThickness='" + N(thickness) + "' BorderBrush='" + off + "' Background='Transparent'/>");
   if (dot == null) sb.Append("<Path x:Name='mark' Data='M 5,12.5 L 9.5,17 L 19,7.5' Stroke='" + tick + "' StrokeThickness='2.8' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Stretch='Uniform' Margin='" + N(size * 0.24) + "' Visibility='Collapsed'/>");
   else sb.Append("<Ellipse x:Name='mark' Fill='" + dot + "' Width='" + N(Math.Round(size * 0.42)) + "' Height='" + N(Math.Round(size * 0.42)) + "' Visibility='Collapsed'/>");
   sb.Append("<Border x:Name='ring' CornerRadius='" + N(radius + 3) + "' BorderThickness='2' BorderBrush='" + focus + "' Margin='-3' Visibility='Collapsed'/></Grid>");
   sb.Append("<ControlTemplate.Triggers>");
   sb.Append("<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='" + onBorder + "'/></Trigger>");
   sb.Append("<Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='ring' Property='Visibility' Value='Visible'/></Trigger>");
   sb.Append("<Trigger Property='IsChecked' Value='True'><Setter TargetName='box' Property='Background' Value='" + onFill + "'/><Setter TargetName='box' Property='BorderBrush' Value='" + onBorder + "'/><Setter TargetName='mark' Property='Visibility' Value='Visible'/></Trigger>");
   sb.Append("</ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");
   return sb.ToString();
  }
 }
}
