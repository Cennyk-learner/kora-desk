using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Input;

namespace Kora {
 // Keep the WPF layered HWND a top-level window for its entire lifetime. Reparenting
 // that HWND into Explorer invalidates its composition surface on some Windows builds.
 public sealed class DesktopHost {
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string title);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
  delegate bool EnumProc(IntPtr hwnd,IntPtr data);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr data);
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd,uint command);
  [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd,int index);
  [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
  [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT point);
  [DllImport("user32.dll")] static extern IntPtr MonitorFromRect(ref RECT rect,uint flags);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MONITORINFO info);
  [StructLayout(LayoutKind.Sequential)] struct POINT {public int X,Y;}
  [StructLayout(LayoutKind.Sequential)] struct RECT {public int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential)] struct WINDOWPOS {public IntPtr Hwnd,After;public int X,Y,Cx,Cy;public uint Flags;}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct MONITORINFO {public int Size;public RECT Monitor,Work;public uint Flags;}
  readonly Window window;readonly Store store;IntPtr handle,desktop;bool pinned,dragging,positioning,desktopActive;POINT cursorStart;RECT rectStart;DispatcherTimer watcher;UIElement grip;HwndSource source;double appliedX=double.NaN,appliedY=double.NaN;
  public bool Attached {get;private set;}
  public string Status {get;private set;}
  public DesktopHost(Window widget,Store storage){
   window=widget;store=storage;window.ShowActivated=false;
   widget.SourceInitialized+=delegate{handle=new WindowInteropHelper(widget).Handle;source=HwndSource.FromHwnd(handle);source.AddHook(Messages);};
   widget.Loaded+=delegate{
    Apply(store.Read().Settings);
    if(watcher!=null)return;
    watcher=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
    watcher.Tick+=delegate{Watch();};watcher.Start();
   };
   widget.Closed+=delegate{if(watcher!=null)watcher.Stop();if(source!=null)source.RemoveHook(Messages);};
  }
  IntPtr Messages(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled){
   if(msg==0x21 && !pinned){handled=true;return new IntPtr(3);} // MA_NOACTIVATE: clicks still reach controls.
   if(msg==0x46 && !pinned && !positioning){
    var pos=(WINDOWPOS)Marshal.PtrToStructure(l,typeof(WINDOWPOS));pos.Flags|=0x4;Marshal.StructureToPtr(pos,l,false);
   }
   return IntPtr.Zero;
  }
  public void Attach(UIElement element){
   if(grip!=null){grip.MouseLeftButtonDown-=OnDown;grip.MouseMove-=OnMove;grip.MouseLeftButtonUp-=OnUp;grip.LostMouseCapture-=OnLost;}
   grip=element;dragging=false;if(grip==null)return;
   grip.MouseLeftButtonDown+=OnDown;grip.MouseMove+=OnMove;grip.MouseLeftButtonUp+=OnUp;grip.LostMouseCapture+=OnLost;
  }
  void OnDown(object s,MouseButtonEventArgs e){if(e.LeftButton!=MouseButtonState.Pressed)return;GetCursorPos(out cursorStart);GetWindowRect(handle,out rectStart);dragging=true;grip.CaptureMouse();e.Handled=true;}
  void OnMove(object s,MouseEventArgs e){if(!dragging)return;POINT p;GetCursorPos(out p);Move(rectStart.Left+p.X-cursorStart.X,rectStart.Top+p.Y-cursorStart.Y);}
  void OnUp(object s,MouseButtonEventArgs e){if(!dragging)return;dragging=false;grip.ReleaseMouseCapture();SavePosition();}
  void OnLost(object s,MouseEventArgs e){dragging=false;}
  static IntPtr FindDesktop(){IntPtr host=IntPtr.Zero;EnumWindows(delegate(IntPtr h,IntPtr d){if(FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null)!=IntPtr.Zero){host=h;return false;}return true;},IntPtr.Zero);return host==IntPtr.Zero?FindWindow("Progman",null):host;}
  bool IsDesktopActive(IntPtr shell){var active=GetForegroundWindow();return shell!=IntPtr.Zero&&(active==shell||active==FindWindow("Progman",null));}
  void Watch(){
   if(pinned||!window.IsVisible)return;
   var shell=FindDesktop();bool active=IsDesktopActive(shell);
   if(shell==desktop&&active==desktopActive)return;
   desktop=shell;desktopActive=active;UpdateStatus();Place(0,0,0x1|0x2|0x10);
  }
  void UpdateStatus(){Attached=!pinned&&desktop!=IntPtr.Zero;Status=pinned?"始终置顶":Attached?"桌面组件":"桌面暂不可用 · 普通窗口";}
  // When Explorer brings the desktop forward, follow it in the non-topmost band.
  // Otherwise stay immediately above the desktop, below application windows.
  IntPtr InsertAfter(){
   if(pinned)return new IntPtr(-1);
   if(desktopActive||desktop==IntPtr.Zero)return IntPtr.Zero;
   var above=GetWindow(desktop,3); // GW_HWNDPREV
   if(above==handle)above=GetWindow(handle,3);
   if(above==IntPtr.Zero||(GetWindowLong(above,-20)&8)!=0)return IntPtr.Zero;
   return above;
  }
  void Place(int x,int y,uint flags){positioning=true;try{SetWindowPos(handle,InsertAfter(),x,y,0,0,flags);}finally{positioning=false;}}
  public bool IsAt(Preferences settings){return settings.AlwaysOnTop==pinned&&settings.X==appliedX&&settings.Y==appliedY;}
  public void Apply(Preferences settings){
   if(handle==IntPtr.Zero)return;
   appliedX=settings.X;appliedY=settings.Y;pinned=settings.AlwaysOnTop;desktop=FindDesktop();desktopActive=IsDesktopActive(desktop);
   positioning=true;try{window.Topmost=pinned;}finally{positioning=false;}
   UpdateStatus();RECT rect;GetWindowRect(handle,out rect);
   int width=rect.Right-rect.Left,height=rect.Bottom-rect.Top;
   int x=settings.X==-9999?(int)(SystemParameters.WorkArea.Right*System.Windows.Media.VisualTreeHelper.GetDpi(window).DpiScaleX)-width-24:(int)settings.X;
   int y=settings.Y==-9999?60:(int)settings.Y;
   RECT target=new RECT{Left=x,Top=y,Right=x+width,Bottom=y+height};var mi=new MONITORINFO{Size=Marshal.SizeOf(typeof(MONITORINFO))};
   if(GetMonitorInfo(MonitorFromRect(ref target,2),ref mi)){x=Math.Max(mi.Work.Left,Math.Min(x,mi.Work.Right-width));y=Math.Max(mi.Work.Top,Math.Min(y,mi.Work.Bottom-height));}
   Move(x,y);
  }
  void Move(int x,int y){Place(x,y,0x1|0x10);}
  public void SavePosition(){RECT r;GetWindowRect(handle,out r);appliedX=r.Left;appliedY=r.Top;Ui.Guard(delegate{store.Update(s=>{s.Settings.X=r.Left;s.Settings.Y=r.Top;});});}
 }
}
