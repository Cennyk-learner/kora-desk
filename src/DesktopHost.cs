using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Input;

namespace Kora {
 public sealed class DesktopHost {
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string title);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
  delegate bool EnumProc(IntPtr hwnd,IntPtr data);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr data);
  [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetParent(IntPtr child,IntPtr parent);
  [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr child);
  [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
  [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd,int index);
  [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd,int index,int value);
  [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
  [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT point);
  [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hwnd,ref POINT point);
  [DllImport("user32.dll")] static extern IntPtr MonitorFromRect(ref RECT rect,uint flags);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MONITORINFO info);
  [StructLayout(LayoutKind.Sequential)] struct POINT {public int X,Y;}
  [StructLayout(LayoutKind.Sequential)] struct RECT {public int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct MONITORINFO {public int Size;public RECT Monitor,Work;public uint Flags;}
  const int GWL_STYLE=-16, GWL_EXSTYLE=-20, WS_CHILD=0x40000000, WS_POPUP=unchecked((int)0x80000000);
  readonly Window window;readonly Store store;IntPtr handle,parent;bool pinned,dragging;POINT cursorStart;RECT rectStart;DispatcherTimer watcher;UIElement grip;double appliedX=double.NaN,appliedY=double.NaN;DateTime lastAttempt=DateTime.MinValue;
  public bool Attached {get;private set;}
  public string Status {get;private set;}
  public DesktopHost(Window widget,Store storage){
   window=widget;store=storage;
   widget.SourceInitialized+=delegate{handle=new WindowInteropHelper(widget).Handle;};
   widget.Loaded+=delegate{
    Apply(store.Read().Settings);
    watcher=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};
    watcher.Tick+=delegate{Watch();};
    watcher.Start();
   };
   widget.Closed+=delegate{if(watcher!=null)watcher.Stop();};
  }
  // The header element is rebuilt whenever the widget redraws, so the drag handlers follow it.
  public void Attach(UIElement element){
   if(grip!=null){grip.MouseLeftButtonDown-=OnDown;grip.MouseMove-=OnMove;grip.MouseLeftButtonUp-=OnUp;grip.LostMouseCapture-=OnLost;}
   grip=element;dragging=false;
   if(grip==null)return;
   grip.MouseLeftButtonDown+=OnDown;grip.MouseMove+=OnMove;grip.MouseLeftButtonUp+=OnUp;grip.LostMouseCapture+=OnLost;
  }
  void OnDown(object s,MouseButtonEventArgs e){if(e.LeftButton!=MouseButtonState.Pressed)return;GetCursorPos(out cursorStart);GetWindowRect(handle,out rectStart);dragging=true;grip.CaptureMouse();e.Handled=true;}
  void OnMove(object s,MouseEventArgs e){if(!dragging)return;POINT p;GetCursorPos(out p);Move(rectStart.Left+p.X-cursorStart.X,rectStart.Top+p.Y-cursorStart.Y);}
  void OnUp(object s,MouseButtonEventArgs e){if(!dragging)return;dragging=false;grip.ReleaseMouseCapture();SavePosition();}
  void OnLost(object s,MouseEventArgs e){dragging=false;}
  // Re-attach only when the desktop window changed or has newly appeared. When the desktop layer is unavailable
  // the widget stays a plain window instead of being re-parented and moved every few seconds.
  void Watch(){
   try{
    if(pinned)return;
    var desktop=FindDesktop();
    bool stale=parent==IntPtr.Zero?desktop!=IntPtr.Zero:(!IsWindow(parent)||desktop!=parent);
    if(!stale)return;
    if(!Attached&&(DateTime.UtcNow-lastAttempt).TotalSeconds<30)return;
    Apply(store.Read().Settings);
   }catch(Exception){}
  }
  public bool IsAt(Preferences settings){return settings.X==appliedX&&settings.Y==appliedY;}
  static IntPtr FindDesktop(){IntPtr host=IntPtr.Zero;EnumWindows(delegate(IntPtr h,IntPtr d){if(FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null)!=IntPtr.Zero){host=h;return false;}return true;},IntPtr.Zero);return host;}
  public void Apply(Preferences settings){if(handle==IntPtr.Zero)return;lastAttempt=DateTime.UtcNow;appliedX=settings.X;appliedY=settings.Y;RECT rect;GetWindowRect(handle,out rect);pinned=settings.AlwaysOnTop;parent=pinned?IntPtr.Zero:FindDesktop();
   SetParent(handle,IntPtr.Zero);int style=GetWindowLong(handle,GWL_STYLE);SetWindowLong(handle,GWL_STYLE,(style&~WS_CHILD)|WS_POPUP);SetWindowLong(handle,GWL_EXSTYLE,GetWindowLong(handle,GWL_EXSTYLE)|0x80);
   Attached=false;
   if(!pinned&&parent!=IntPtr.Zero){SetWindowLong(handle,GWL_STYLE,(style&~WS_POPUP)|WS_CHILD);SetParent(handle,parent);Attached=GetParent(handle)==parent;if(!Attached){SetWindowLong(handle,GWL_STYLE,(style&~WS_CHILD)|WS_POPUP);parent=IntPtr.Zero;}}
   window.Topmost=pinned;if(Environment.GetEnvironmentVariable("KORA_DIAGNOSTICS")=="1" && pinned) {window.ShowInTaskbar=true;SetWindowLong(handle,GWL_EXSTYLE,GetWindowLong(handle,GWL_EXSTYLE)&~0x80);} Status=pinned?"始终置顶":Attached?"桌面组件":"桌面层暂不可用 · 普通窗口";
   int x=settings.X==-9999?(int)(SystemParameters.WorkArea.Right*System.Windows.Media.VisualTreeHelper.GetDpi(window).DpiScaleX)-rect.Right+rect.Left-24:(int)settings.X;
   int y=settings.Y==-9999?60:(int)settings.Y;
   RECT target=new RECT{Left=x,Top=y,Right=x+rect.Right-rect.Left,Bottom=y+rect.Bottom-rect.Top};var mi=new MONITORINFO{Size=Marshal.SizeOf(typeof(MONITORINFO))};GetMonitorInfo(MonitorFromRect(ref target,2),ref mi);x=Math.Max(mi.Work.Left,Math.Min(x,mi.Work.Right-(rect.Right-rect.Left)));y=Math.Max(mi.Work.Top,Math.Min(y,mi.Work.Bottom-(rect.Bottom-rect.Top)));Move(x,y); if(Environment.GetEnvironmentVariable("KORA_DIAGNOSTICS")=="1") { RECT actual;GetWindowRect(handle,out actual);System.IO.File.WriteAllText(System.IO.Path.Combine(store.Folder,"desktop-diagnostics.txt"),"handle="+handle+" parent="+GetParent(handle)+" expectedParent="+parent+" style="+GetWindowLong(handle,GWL_STYLE).ToString("X")+" ex="+GetWindowLong(handle,GWL_EXSTYLE).ToString("X")+" rect="+actual.Left+","+actual.Top+","+actual.Right+","+actual.Bottom+" status="+Status+" visible="+window.IsVisible); }
  }
  void Move(int x,int y){var p=new POINT{X=x,Y=y};if(parent!=IntPtr.Zero)ScreenToClient(parent,ref p);SetWindowPos(handle,pinned?new IntPtr(-1):IntPtr.Zero,p.X,p.Y,0,0,0x1|0x10|0x20);}
  public void SavePosition(){RECT r;GetWindowRect(handle,out r);appliedX=r.Left;appliedY=r.Top;Ui.Guard(delegate{store.Update(s=>{s.Settings.X=r.Left;s.Settings.Y=r.Top;});});}
 }
}
