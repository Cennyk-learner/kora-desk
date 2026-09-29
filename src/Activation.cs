using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Kora {
 public static class Activation {
  static readonly Dictionary<string,Process> pending=new Dictionary<string,Process>();
  static readonly HashSet<string> launching=new HashSet<string>();
  static readonly Dictionary<string,DateTime> attempts=new Dictionary<string,DateTime>();
  public static string Name(string role){
   string path=Environment.GetEnvironmentVariable("KORA_DATA_DIR");
   if(string.IsNullOrEmpty(path))return "Local\\KoraDesk."+role;
   using(var sha=SHA256.Create())return "Local\\KoraDesk."+role+"."+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).TrimEnd('\\').ToUpperInvariant()))).Replace("-","").Substring(0,24);
  }
  public static bool Wake(string role){
   try{using(var e=EventWaitHandle.OpenExisting(Name(role)+".Wake")){e.Set();return true;}}
   catch(WaitHandleCannotBeOpenedException){return false;}
  }
  public static void WakeStarting(string role){
   for(int i=0;i<40;i++){if(Wake(role))return;Thread.Sleep(50);}
  }
  public static void Open(string name){
   string role=name=="Kora.exe"?"Manager":name=="Kora.Widget.exe"?"Widget":null;
   if(role==null)throw new ArgumentException("Unknown Kora application.","name");
   string key=Name(role);
   if(Wake(role))return;
   // Shell execution may display a system prompt and pump messages. Protect against
   // reentrant clicks as well as a second click while the child is starting.
   if(launching.Contains(key))return;
   Process child;
   if(pending.TryGetValue(key,out child)){
    if(!child.HasExited)return;
    child.Dispose();pending.Remove(key);
   }
   DateTime previous;
   if(attempts.TryGetValue(key,out previous)&&(DateTime.UtcNow-previous).TotalSeconds<2)return;
   launching.Add(key);attempts[key]=DateTime.UtcNow;
   try{
    if(Wake(role))return;
    child=Process.Start(new ProcessStartInfo(Path.Combine(Launch.BaseDirectory,name)){UseShellExecute=true,WorkingDirectory=Launch.BaseDirectory});
    if(child!=null)pending[key]=child;
   }finally{launching.Remove(key);}
  }
 }
}
