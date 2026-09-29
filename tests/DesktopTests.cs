#if WPF
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Kora;

static class DesktopTests {
 [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h,uint flags);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 static void Pump(){var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);}
  static void PumpFor(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 public static void Run(Store store,Action<bool,string> check){
  store.Update(s=>{s.Settings.AlwaysOnTop=false;s.Settings.X=100;s.Settings.Y=100;});
  foreach(var info in Skins.Catalog)foreach(bool dark in new[]{false,true}){
   var skin=Skins.Create(info.Id,dark);Ui.Apply(skin);
   var win=new Window{ShowInTaskbar=false,ShowActivated=false,Width=skin.WidgetWidth+24,Height=600};Ui.Chromeless(win);
   var host=new DesktopHost(win,store);
   var state=Preset.Create();var context=new WidgetContext{Skin=skin,State=state,Day=DateTime.Today,Toggle=(t,v)=>{},OpenPlan=()=>{},OpenMenu=e=>{},Grip=host.Attach};
   win.Content=Ui.Frame(skin.BuildWidget(context),skin.WidgetRadius,skin.WidgetFill,skin.WidgetBorder,24,skin.WidgetShadow);
   try{
    win.Show();Pump();var hwnd=new WindowInteropHelper(win).Handle;var visual=win.Content;bool valid=true;
    var preferences=store.Read().Settings;
    for(int i=0;i<8;i++){
     preferences.AlwaysOnTop=i%2==0;host.Apply(preferences);Pump();
     valid &= hwnd==new WindowInteropHelper(win).Handle && GetAncestor(hwnd,2)==hwnd && (GetWindowLong(hwnd,-16)&0x40000000)==0 && IsWindowVisible(hwnd);
     valid &= ((GetWindowLong(hwnd,-20)&8)!=0)==preferences.AlwaysOnTop && ReferenceEquals(visual,win.Content);
    }
    check(valid,"Stable top-level HWND and visual across eight mode switches: "+info.Id+(dark?" dark":" light"));
    win.Hide();Pump();check(!IsWindowVisible(hwnd),"Hide removes native visibility: "+info.Id+(dark?" dark":" light"));
    win.Show();host.Apply(preferences);Pump();check(IsWindowVisible(hwnd)&&new WindowInteropHelper(win).Handle==hwnd,"Show restores same native window: "+info.Id+(dark?" dark":" light"));
   }finally{win.Close();}
  }
    int reports=0;PollGuard guard=null;
  guard=new PollGuard(e=>{reports++;guard.Run(()=>{throw new IOException("nested");});});
  for(int i=0;i<10;i++)guard.Run(()=>{throw new IOException("same error");});
  check(reports==1,"Polling reports repeated errors once and suppresses modal reentrancy");
  guard.Run(()=>{});guard.Run(()=>{throw new IOException("same error");});
  check(reports==2,"Polling recovers after a successful refresh");
  string saved=Environment.GetEnvironmentVariable("KORA_DATA_DIR");
  try{
   Environment.SetEnvironmentVariable("KORA_DATA_DIR",store.Folder);
   var first=Activation.Name("Widget");
   Environment.SetEnvironmentVariable("KORA_DATA_DIR",Path.Combine(store.Folder,"other"));
   check(first!=Activation.Name("Widget"),"Independent test data has independent process activation");
   Environment.SetEnvironmentVariable("KORA_DATA_DIR",store.Folder);
   using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,Activation.Name("Widget")+".Wake")){
    bool signaled=true;for(int i=0;i<20;i++){Launch.App("Kora.Widget.exe");signaled &= signal.WaitOne(0);}
    check(signaled,"Twenty show requests wake existing widget without starting an executable");
   }
      check(!Activation.Wake("Absent"),"Missing process activation is non-throwing");
   store.Update(s=>{s.Settings.AlwaysOnTop=true;s.Settings.X=100;s.Settings.Y=100;});
   var live=new Widget(store,false);
   try{
    live.Show();PumpFor(1300);check(live.Topmost,"Live widget starts pinned");
    typeof(Widget).GetMethod("ToggleTop",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(live,null);PumpFor(1400);check(!live.Topmost,"Live widget observes desktop mode from manager data");
    store.Update(s=>s.Settings.AlwaysOnTop=true);PumpFor(1400);check(live.Topmost,"Live widget observes pinned mode again without theme change");
    live.Hide();Launch.App("Kora.Widget.exe");PumpFor(1400);check(live.IsVisible,"Live hidden widget processes wake event");
   }finally{live.Close();}
  }finally{Environment.SetEnvironmentVariable("KORA_DATA_DIR",saved);}
 }
}
#endif
