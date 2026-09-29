using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Kora;
class Tests {
 static int passed;static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);passed++;}
 static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}catch(System.Runtime.Serialization.SerializationException){return true;}}
 static void Extra() {
  var legacy=Encoding.UTF8.GetBytes("{\"Done\":[],\"Settings\":{\"AlwaysOnTop\":false,\"X\":-9999,\"Y\":-9999},\"Tasks\":[{\"Days\":[1],\"Group\":\"学习\",\"Id\":\"a1\",\"Note\":\"\",\"OnceDate\":\"\",\"Title\":\"旧任务\"}],\"Version\":1}");
  var old=Store.Decode(legacy);
  Check(old.Settings.Theme==null&&old.Settings.Appearance==null,"Files saved before themes existed decode with empty theme fields");
  Check(Store.Decode(Store.Encode(old)).Tasks.Count==1,"Legacy state saves again unchanged");
  old.Settings.Theme="dot";old.Settings.Appearance="dark";
  var again=Store.Decode(Store.Encode(old));
  Check(again.Settings.Theme=="dot"&&again.Settings.Appearance=="dark","Theme and appearance survive a save");
  again.Settings.Theme=new string('x',40);
  Check(Reject(()=>Store.Validate(again)),"Oversized theme name rejected");
  var saved=CultureInfo.CurrentCulture;
  try {
   foreach(var name in new[]{"th-TH","ar-SA","zh-CN"}) {
    CultureInfo culture;
    try { culture=new CultureInfo(name); } catch(Exception) { continue; }
    CultureInfo.CurrentCulture=culture;
    var day=new DateTime(2026,9,28);
    Check(Dates.MonthDay(day)=="9月28日"&&Dates.Key(day)=="2026-09-28"&&Dates.Parse("2026-09-28")==day&&Dates.Number(day)=="28","Date text and keys ignore the "+name+" calendar");
   }
  } finally { CultureInfo.CurrentCulture=saved; }
 }
 static int Main(){string root=Path.Combine(Path.GetTempPath(),"KoraTests-"+Guid.NewGuid().ToString("N"));try{var store=new Store(root);var s=store.Read();var monday=new DateTime(2026,9,28);Check(s.Tasks.Count==12,"Full editable study and fitness preset");Check(s.ForDay(monday).Count==5,"Monday has four daily studies and chest workout");foreach(var d in new[]{1,2,3,4,5,6})Check(s.ForDay(monday.AddDays(d)).Count==6,"Six tasks on "+monday.AddDays(d).DayOfWeek);
 Check(s.ForDay(monday.AddDays(1)).Any(t=>t.Title=="听力 + 阅读整套")&&s.ForDay(monday.AddDays(1)).Any(t=>t.Title=="健身休息"),"Tuesday full test and rest day");Check(s.ForDay(monday.AddDays(6)).Any(t=>t.Title=="听阅复盘")&&s.ForDay(monday.AddDays(6)).Any(t=>t.Title=="纯有氧"),"Sunday review and cardio");var task=s.Tasks[0];store.Update(x=>x.SetDone(task.Id,monday,true));Check(new Store(root).Read().IsDone(task,monday),"Checkoff persists across store instances");Check(!store.Read().IsDone(task,monday.AddDays(1)),"Daily rollover keeps next day unchecked");store.Update(x=>x.SetDone(task.Id,monday,true));Check(store.Read().Done.Count==1,"Repeated completion is idempotent");store.Update(x=>x.SetDone(task.Id,monday,false));Check(store.Read().Done.Count==0,"Uncheck persists");
 var once=new TaskItem{Title="一次性任务",Group="生活",OnceDate="2026-10-01"};store.Update(x=>x.Tasks.Add(once));Check(store.Read().ForDay(new DateTime(2026,10,1)).Any(t=>t.Id==once.Id)&&!store.Read().ForDay(new DateTime(2026,10,2)).Any(t=>t.Id==once.Id),"One-off date is exact across month boundary");
 Parallel.For(0,30,i=>{new Store(root).Update(x=>x.SetDone(task.Id,monday.AddDays(i),true));});Check(store.Read().Done.Count==30,"Concurrent writers do not lose checkoffs");store.Update(x=>{x.Tasks[0].Title="改名 ✓ 中文";x.Settings.AlwaysOnTop=true;});Check(store.Read().IsDone(task,monday)&&store.Read().Tasks[0].Title=="改名 ✓ 中文","Editing preserves identity and Unicode");
 string backup=Path.Combine(root,"export.json");store.Export(backup);store.Update(x=>x.Tasks.RemoveAt(0));store.Import(backup);Check(store.Read().Tasks[0].Id==task.Id&&store.Read().Settings.AlwaysOnTop,"Backup roundtrip preserves tasks, marks and settings");var invalid=Store.Decode(File.ReadAllBytes(backup));invalid.Tasks.Add(invalid.Tasks[0]);Check(Reject(()=>Store.Validate(invalid)),"Duplicate task IDs rejected");invalid=Store.Decode(File.ReadAllBytes(backup));invalid.Tasks[0].Days.Clear();Check(Reject(()=>Store.Validate(invalid)),"Empty weekly recurrence rejected");invalid=Store.Decode(File.ReadAllBytes(backup));invalid.Tasks[0].Days.Add(8);Check(Reject(()=>Store.Validate(invalid)),"Invalid weekday rejected");invalid=Store.Decode(File.ReadAllBytes(backup));invalid.Settings.X=double.NaN;Check(Reject(()=>Store.Validate(invalid)),"Invalid window position rejected");
 string bad=Path.Combine(root,"invalid.json");File.WriteAllText(bad,"{}");string before=Convert.ToBase64String(File.ReadAllBytes(store.FilePath));Check(Reject(()=>store.Import(bad))&&before==Convert.ToBase64String(File.ReadAllBytes(store.FilePath)),"Invalid import leaves current data untouched");store.Update(x=>x.Settings.AlwaysOnTop=false);File.WriteAllText(store.FilePath,"broken json");var recovered=new Store(root);Check(recovered.Read().Tasks.Count>0&&recovered.RecoveryNotice!=null&&Directory.GetFiles(root,"*.damaged-*").Length==1,"Corrupt file recovers backup and retains original");Check(Dates.Monday(new DateTime(2026,10,4))==monday,"Sunday maps to preceding Monday");Extra();
#if WPF
SkinTests.Run(Check);
#endif
Console.WriteLine(passed+" checks passed.");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
