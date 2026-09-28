using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace Kora {
 [DataContract] public class TaskItem {
  [DataMember] public string Id = Guid.NewGuid().ToString("N");
  [DataMember] public string Title = "";
  [DataMember] public string Note = "";
  [DataMember] public string Group = "学习";
  [DataMember] public List<int> Days = new List<int>();
  [DataMember] public string OnceDate = "";
  public bool Due(DateTime day) { return string.IsNullOrEmpty(OnceDate) ? Days.Contains((int)day.DayOfWeek) : OnceDate == Dates.Key(day); }
  public string Frequency { get { if (!string.IsNullOrEmpty(OnceDate)) return OnceDate + " · 仅一次"; if (Days.Count == 7) return "每天"; return string.Join(" / ", Dates.Order.Where(Days.Contains).Select(i => Dates.Short[i])); } }
 }
 [DataContract] public class Preferences {
  [DataMember] public bool AlwaysOnTop;
  [DataMember] public double X = -9999;
  [DataMember] public double Y = -9999;
 }
 [DataContract] public class State {
  [DataMember] public int Version = 1;
  [DataMember] public List<TaskItem> Tasks = new List<TaskItem>();
  [DataMember] public List<string> Done = new List<string>();
  [DataMember] public Preferences Settings = new Preferences();
  public List<TaskItem> ForDay(DateTime day) { return Tasks.Where(t => t.Due(day)).ToList(); }
  public bool IsDone(TaskItem task, DateTime day) { return Done.Contains(Dates.Key(day) + "/" + task.Id); }
  public void SetDone(string id, DateTime day, bool value) { var key = Dates.Key(day) + "/" + id; Done.RemoveAll(x => x == key); if (value) Done.Add(key); }
 }
 public static class Dates {
  public static readonly int[] Order = {1,2,3,4,5,6,0};
  public static readonly string[] Short = {"周日","周一","周二","周三","周四","周五","周六"};
  public static string Key(DateTime day) { return day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
  public static DateTime Monday(DateTime day) { return day.Date.AddDays(-((int)day.DayOfWeek + 6) % 7); }
 }
 public static class Preset {
  public static State Create() {
   var s = new State();
   Add(s,"词汇真经","积累词义、搭配与语境。","学习",Dates.Order);
   Add(s,"顾家北写作","跟着 31 天计划，每天留下一点输出。","学习",Dates.Order);
   Add(s,"雅思王语料库","听写、核对，再听一遍易错词。","学习",Dates.Order);
   Add(s,"百词斩","100 个新词 + 200 个复习词；按实际进度调整。","学习",Dates.Order);
   Add(s,"听力 + 阅读整套","完成一套听阅，记录需要复盘的问题。","学习",2,4,6);
   Add(s,"听阅复盘","改错、精读、精听，整理同义替换。","学习",3,5,0);
   Add(s,"胸 + 肩 + 三头","","健身",1);
   Add(s,"健身休息","给身体留一点恢复空间。","健身",2,4);
   Add(s,"背 + 二头","","健身",3);
   Add(s,"腿 + 核心","","健身",5);
   Add(s,"倒三角专项","","健身",6);
   Add(s,"纯有氧","","健身",0);
   return s;
  }
  static void Add(State s,string title,string note,string group,params int[] days) { s.Tasks.Add(new TaskItem {Title=title,Note=note,Group=group,Days=days.ToList()}); }
 }
 public sealed class Store {
  public readonly string Folder;
  public string FilePath { get { return Path.Combine(Folder,"plan.json"); } }
  readonly string mutexName;
  public string RecoveryNotice {get; private set;}
  public Store() : this(Environment.GetEnvironmentVariable("KORA_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KoraDesk")) {}
  public Store(string path) {
   Folder = Path.GetFullPath(path); Directory.CreateDirectory(Folder);
   using(var sha=System.Security.Cryptography.SHA256.Create()) mutexName="Local\\Kora.Store."+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Folder.ToUpperInvariant()))).Replace("-","").Substring(0,24);
  }
  T Locked<T>(Func<T> work) { using(var mutex=new Mutex(false,mutexName)) { bool held=false; try { try { held=mutex.WaitOne(10000); } catch(AbandonedMutexException) { held=true; } if(!held) throw new IOException("数据正在被使用，请稍后重试。"); return work(); } finally { if(held) mutex.ReleaseMutex(); } } }
  public State Read() { return Locked(delegate { return ReadInternal(); }); }
  State ReadInternal() {
   if(!File.Exists(FilePath)) { var initial=Preset.Create(); SaveInternal(initial); return initial; }
   try { return Decode(File.ReadAllBytes(FilePath)); }
   catch(Exception e) {
    if(!(e is SerializationException || e is InvalidDataException || e is System.Xml.XmlException || e is FormatException)) throw;
    var backup=FilePath+".bak";
    if(!File.Exists(backup)) throw new InvalidDataException("计划文件无法读取。原文件已保留，请从备份导入或手动恢复。",e);
    State recovered;
    try { recovered=Decode(File.ReadAllBytes(backup)); } catch(Exception inner) { throw new InvalidDataException("计划及自动备份均无法读取，原文件已保留。",inner); }
    File.Copy(FilePath,FilePath+".damaged-"+DateTime.Now.ToString("yyyyMMddHHmmssfff"));
    File.Copy(backup,FilePath,true);
    RecoveryNotice="计划文件异常，已从上一次自动备份恢复。原文件已保留在数据文件夹。";
    return recovered;
   }
  }
  public State Update(Action<State> edit) { return Locked(delegate { var state=ReadInternal(); edit(state); Validate(state); SaveInternal(state); return state; }); }
  public void Import(string path) { var state=Decode(File.ReadAllBytes(path)); Locked(delegate { SaveInternal(state); return true; }); }
  public void Export(string path) { var bytes=Encode(Read()); File.WriteAllBytes(path,bytes); }
  void SaveInternal(State state) {
   Validate(state);
   string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
   try {
    var bytes=Encode(state);
    using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
    if(File.Exists(FilePath)) File.Replace(temp,FilePath,FilePath+".bak"); else File.Move(temp,FilePath);
   } finally { if(File.Exists(temp)) File.Delete(temp); }
  }
  public static byte[] Encode(State state) { using(var stream=new MemoryStream()) { new DataContractJsonSerializer(typeof(State)).WriteObject(stream,state); return stream.ToArray(); } }
  public static State Decode(byte[] bytes) { if(bytes.Length>8000000) throw new InvalidDataException("备份文件过大。"); using(var stream=new MemoryStream(bytes)) { var s=(State)new DataContractJsonSerializer(typeof(State)).ReadObject(stream); Validate(s); return s; } }
  public static void Validate(State s) {
   if(s==null || s.Version!=1 || s.Tasks==null || s.Done==null || s.Settings==null || s.Tasks.Count>1000 || s.Done.Count>200000) throw new InvalidDataException("计划格式不受支持。");
   var ids=new HashSet<string>(); DateTime date;
   foreach(var t in s.Tasks) {
    if(t==null || string.IsNullOrWhiteSpace(t.Id) || t.Id.Length>64 || t.Id.Contains("/") || !ids.Add(t.Id) || string.IsNullOrWhiteSpace(t.Title) || t.Title.Length>80 || t.Note==null || t.Note.Length>300 || t.Group==null || t.Group.Length>20 || t.Days==null || t.Days.Any(d=>d<0||d>6) || t.Days.Distinct().Count()!=t.Days.Count) throw new InvalidDataException("任务内容不完整或超出限制。");
    if(string.IsNullOrEmpty(t.OnceDate)) { if(t.Days.Count==0) throw new InvalidDataException("请至少选择一天。"); }
    else if(!DateTime.TryParseExact(t.OnceDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date)) throw new InvalidDataException("任务日期无效。");
   }
   foreach(var key in s.Done) if(key==null || key.Length>80 || key.Length<12 || key[10]!='/' || !DateTime.TryParseExact(key.Substring(0,10),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date)) throw new InvalidDataException("完成记录格式无效。");
   if(double.IsNaN(s.Settings.X)||double.IsInfinity(s.Settings.X)||double.IsNaN(s.Settings.Y)||double.IsInfinity(s.Settings.Y)) throw new InvalidDataException("组件位置无效。");
  }
 }
 public static class Launch {
  public static void App(string name) { Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name)){UseShellExecute=true}); }
  public static void Folder(string path) { Process.Start(new ProcessStartInfo("explorer.exe", "\""+path+"\""){UseShellExecute=true}); }
 }
}
