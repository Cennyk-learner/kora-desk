//! plan.json storage. The wire format stays PascalCase so files written by Kora 1.x keep
//! working; everything added in v2 rides along in `extra` and is validated by the frontend.

use serde::{Deserialize, Serialize};
use serde_json::{Map, Value};
use std::fs;
use std::io::Write;
use std::path::{Path, PathBuf};
use std::sync::Mutex;

pub const MAX_BYTES: usize = 8_000_000;
pub const CURRENT_VERSION: i64 = 2;

fn one() -> i64 {
    1
}
fn group() -> String {
    "学习".into()
}
fn unset() -> f64 {
    -9999.0
}

#[derive(Serialize, Deserialize, Clone, Debug, Default)]
pub struct Task {
    #[serde(rename = "Id", default)]
    pub id: String,
    #[serde(rename = "Title", default)]
    pub title: String,
    #[serde(rename = "Note", default)]
    pub note: String,
    #[serde(rename = "Group", default = "group")]
    pub group: String,
    #[serde(rename = "Days", default)]
    pub days: Vec<i64>,
    #[serde(rename = "OnceDate", default)]
    pub once_date: String,
    #[serde(flatten)]
    pub extra: Map<String, Value>,
}

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct Prefs {
    #[serde(rename = "AlwaysOnTop", default)]
    pub always_on_top: bool,
    #[serde(rename = "X", default = "unset")]
    pub x: f64,
    #[serde(rename = "Y", default = "unset")]
    pub y: f64,
    #[serde(rename = "Theme", default)]
    pub theme: Option<String>,
    #[serde(rename = "Appearance", default)]
    pub appearance: Option<String>,
    #[serde(flatten)]
    pub extra: Map<String, Value>,
}

impl Default for Prefs {
    fn default() -> Self {
        Prefs { always_on_top: false, x: -9999.0, y: -9999.0, theme: None, appearance: None, extra: Map::new() }
    }
}

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct State {
    #[serde(rename = "Version", default = "one")]
    pub version: i64,
    #[serde(rename = "Tasks", default)]
    pub tasks: Vec<Task>,
    #[serde(rename = "Done", default)]
    pub done: Vec<String>,
    #[serde(rename = "Settings", default)]
    pub settings: Prefs,
    #[serde(flatten)]
    pub extra: Map<String, Value>,
}

pub fn preset() -> State {
    let mut s = State { version: CURRENT_VERSION, tasks: vec![], done: vec![], settings: Prefs::default(), extra: Map::new() };
    let all = [1, 2, 3, 4, 5, 6, 0];
    let mut add = |title: &str, note: &str, group: &str, days: &[i64]| {
        s.tasks.push(Task {
            id: uuid::Uuid::new_v4().simple().to_string(),
            title: title.into(),
            note: note.into(),
            group: group.into(),
            days: days.to_vec(),
            once_date: String::new(),
            extra: Map::new(),
        });
    };
    add("词汇真经", "积累词义、搭配与语境。", "学习", &all);
    add("顾家北写作", "跟着 31 天计划，每天留下一点输出。", "学习", &all);
    add("雅思王语料库", "听写、核对，再听一遍易错词。", "学习", &all);
    add("百词斩", "100 个新词 + 200 个复习词；按实际进度调整。", "学习", &all);
    add("听力 + 阅读整套", "完成一套听阅，记录需要复盘的问题。", "学习", &[2, 4, 6]);
    add("听阅复盘", "改错、精读、精听，整理同义替换。", "学习", &[3, 5, 0]);
    add("胸 + 肩 + 三头", "", "健身", &[1]);
    add("健身休息", "给身体留一点恢复空间。", "健身", &[2, 4]);
    add("背 + 二头", "", "健身", &[3]);
    add("腿 + 核心", "", "健身", &[5]);
    add("倒三角专项", "", "健身", &[6]);
    add("纯有氧", "", "健身", &[0]);
    s
}

fn is_date(text: &str) -> bool {
    chrono::NaiveDate::parse_from_str(text, "%Y-%m-%d").is_ok() && text.len() == 10
}

pub fn validate(s: &State) -> Result<(), String> {
    let bad = |m: &str| Err(m.to_string());
    if (s.version != 1 && s.version != 2) || s.tasks.len() > 1000 || s.done.len() > 200_000 {
        return bad("计划格式不受支持。");
    }
    let mut ids = std::collections::HashSet::new();
    for t in &s.tasks {
        if t.id.trim().is_empty()
            || t.id.chars().count() > 64
            || t.id.contains('/')
            || !ids.insert(t.id.clone())
            || t.title.trim().is_empty()
            || t.title.chars().count() > 80
            || t.note.chars().count() > 300
            || t.group.chars().count() > 20
            || t.days.iter().any(|d| *d < 0 || *d > 6)
        {
            return bad("任务内容不完整或超出限制。");
        }
        let mut days = t.days.clone();
        days.sort();
        days.dedup();
        if days.len() != t.days.len() {
            return bad("任务内容不完整或超出限制。");
        }
        if t.once_date.is_empty() {
            if t.days.is_empty() {
                return bad("请至少选择一天。");
            }
        } else if !is_date(&t.once_date) {
            return bad("任务日期无效。");
        }
    }
    for key in &s.done {
        let n = key.chars().count();
        if !(12..=80).contains(&n) || !key.is_char_boundary(10) || key.as_bytes()[10] != b'/' || !is_date(&key[..10]) {
            return bad("完成记录格式无效。");
        }
    }
    if !s.settings.x.is_finite() || !s.settings.y.is_finite() {
        return bad("组件位置无效。");
    }
    let long = |o: &Option<String>| o.as_ref().map_or(false, |v| v.chars().count() > 24);
    if long(&s.settings.theme) || long(&s.settings.appearance) {
        return bad("外观设置无效。");
    }
    for (_, v) in s.extra.iter() {
        if let Value::Array(a) = v {
            if a.len() > 200_000 {
                return bad("记录数量超出限制。");
            }
        }
    }
    Ok(())
}

pub fn decode(bytes: &[u8]) -> Result<State, String> {
    if bytes.len() > MAX_BYTES {
        return Err("备份文件过大。".into());
    }
    let bytes = bytes.strip_prefix(&[0xEF, 0xBB, 0xBF]).unwrap_or(bytes);
    let mut s: State = serde_json::from_slice(bytes).map_err(|e| format!("计划格式不受支持：{e}"))?;
    validate(&s)?;
    s.version = CURRENT_VERSION;
    Ok(s)
}

pub fn encode(s: &State) -> Vec<u8> {
    serde_json::to_vec(s).expect("state serializes")
}

pub struct Store {
    dir: PathBuf,
    lock: Mutex<()>,
    notice: Mutex<Option<String>>,
}

impl Store {
    pub fn new(dir: impl AsRef<Path>) -> std::io::Result<Store> {
        fs::create_dir_all(dir.as_ref())?;
        Ok(Store { dir: dir.as_ref().to_path_buf(), lock: Mutex::new(()), notice: Mutex::new(None) })
    }

    pub fn default_dir() -> PathBuf {
        if let Ok(p) = std::env::var("KORA_DATA_DIR") {
            if !p.is_empty() {
                return PathBuf::from(p);
            }
        }
        let base = std::env::var("LOCALAPPDATA").map(PathBuf::from).unwrap_or_else(|_| std::env::temp_dir());
        base.join("KoraDesk")
    }

    pub fn dir(&self) -> &Path {
        &self.dir
    }
    fn file(&self) -> PathBuf {
        self.dir.join("plan.json")
    }
    fn backup(&self) -> PathBuf {
        self.dir.join("plan.json.bak")
    }
    pub fn take_notice(&self) -> Option<String> {
        self.notice.lock().unwrap().take()
    }

    pub fn read(&self) -> Result<State, String> {
        let _g = self.lock.lock().unwrap_or_else(|e| e.into_inner());
        self.read_locked()
    }

    fn read_locked(&self) -> Result<State, String> {
        let file = self.file();
        if !file.exists() {
            let initial = preset();
            self.save_locked(&initial)?;
            return Ok(initial);
        }
        let bytes = fs::read(&file).map_err(|e| e.to_string())?;
        match decode(&bytes) {
            Ok(state) => {
                // First open of a Kora 1.x file: keep the untouched original, then upgrade.
                if serde_json::from_slice::<Value>(&bytes).ok().and_then(|v| v["Version"].as_i64()).unwrap_or(1) < CURRENT_VERSION {
                    let v1 = self.dir.join("plan.json.v1.bak");
                    if !v1.exists() {
                        let _ = fs::copy(&file, &v1);
                    }
                    self.save_locked(&state)?;
                }
                Ok(state)
            }
            Err(_) => {
                let backup = self.backup();
                if !backup.exists() {
                    return Err("计划文件无法读取。原文件已保留，请从备份导入或手动恢复。".into());
                }
                let recovered = fs::read(&backup)
                    .map_err(|e| e.to_string())
                    .and_then(|b| decode(&b))
                    .map_err(|_| "计划及自动备份均无法读取，原文件已保留。".to_string())?;
                let stamp = chrono::Local::now().format("%Y%m%d%H%M%S%3f");
                let _ = fs::copy(&file, self.dir.join(format!("plan.json.damaged-{stamp}")));
                fs::copy(&backup, &file).map_err(|e| e.to_string())?;
                *self.notice.lock().unwrap() = Some("计划文件异常，已从上一次自动备份恢复。原文件已保留在数据文件夹。".into());
                Ok(recovered)
            }
        }
    }

    pub fn update<F: FnOnce(&mut State)>(&self, edit: F) -> Result<State, String> {
        let _g = self.lock.lock().unwrap_or_else(|e| e.into_inner());
        let mut state = self.read_locked()?;
        edit(&mut state);
        self.save_locked(&state)?;
        Ok(state)
    }

    pub fn replace(&self, mut state: State) -> Result<State, String> {
        state.version = CURRENT_VERSION;
        let _g = self.lock.lock().unwrap_or_else(|e| e.into_inner());
        self.save_locked(&state)?;
        Ok(state)
    }

    pub fn import(&self, path: &Path) -> Result<State, String> {
        let bytes = fs::read(path).map_err(|e| e.to_string())?;
        let state = decode(&bytes)?;
        self.replace(state)
    }

    pub fn export(&self, path: &Path) -> Result<(), String> {
        let state = self.read()?;
        fs::write(path, serde_json::to_vec_pretty(&state).unwrap()).map_err(|e| e.to_string())
    }

    fn save_locked(&self, state: &State) -> Result<(), String> {
        validate(state)?;
        let bytes = encode(state);
        if bytes.len() > MAX_BYTES {
            return Err("数据过大，请导出备份后清理旧记录。".into());
        }
        let file = self.file();
        let temp = self.dir.join(format!("plan.json.{}.tmp", uuid::Uuid::new_v4().simple()));
        let write = || -> std::io::Result<()> {
            let mut f = fs::OpenOptions::new().write(true).create_new(true).open(&temp)?;
            f.write_all(&bytes)?;
            f.sync_all()?;
            drop(f);
            // Only a readable current file may become the backup; never overwrite a good one with junk.
            if file.exists() && fs::read(&file).ok().map_or(false, |b| decode(&b).is_ok()) {
                let _ = fs::copy(&file, self.backup());
            }
            fs::rename(&temp, &file)
        };
        let result = write();
        if temp.exists() {
            let _ = fs::remove_file(&temp);
        }
        result.map_err(|e| e.to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::sync::Arc;

    fn store() -> (tempfile::TempDir, Store) {
        let dir = tempfile::tempdir().unwrap();
        let s = Store::new(dir.path()).unwrap();
        (dir, s)
    }

    #[test]
    fn creates_preset_on_first_read() {
        let (_d, s) = store();
        let st = s.read().unwrap();
        assert_eq!(st.tasks.len(), 12);
        assert_eq!(st.version, 2);
        assert!(s.dir().join("plan.json").exists());
    }

    #[test]
    fn upgrades_v1_file_and_keeps_original() {
        let (d, s) = store();
        let v1 = r#"{"Version":1,"Tasks":[{"Id":"a","Title":"读书","Note":"","Group":"学习","Days":[1,2],"OnceDate":""}],"Done":["2025-01-06/a"],"Settings":{"AlwaysOnTop":true,"X":10,"Y":20}}"#;
        fs::write(d.path().join("plan.json"), v1).unwrap();
        let st = s.read().unwrap();
        assert_eq!(st.version, 2);
        assert_eq!(st.done, vec!["2025-01-06/a"]);
        assert!(st.settings.always_on_top && st.settings.theme.is_none());
        assert_eq!(fs::read_to_string(d.path().join("plan.json.v1.bak")).unwrap(), v1);
    }

    #[test]
    fn null_theme_and_missing_fields_are_tolerated() {
        let s = decode(br#"{"Version":1,"Tasks":[],"Done":[],"Settings":{"Theme":null,"Appearance":null}}"#).unwrap();
        assert!(s.settings.theme.is_none());
    }

    #[test]
    fn unknown_v2_fields_survive_a_roundtrip() {
        let (_d, s) = store();
        let mut st = s.read().unwrap();
        st.extra.insert("Giveups".into(), serde_json::json!([{"ts":1,"date":"2026-01-01","remaining":3}]));
        s.replace(st).unwrap();
        let back = s.read().unwrap();
        assert_eq!(back.extra["Giveups"][0]["remaining"], 3);
    }

    #[test]
    fn rejects_invalid_content() {
        assert!(decode(br#"{"Version":9,"Tasks":[],"Done":[],"Settings":{}}"#).is_err());
        assert!(decode(br#"{"Version":1,"Tasks":[{"Id":"a/b","Title":"x","Days":[1]}],"Done":[]}"#).is_err());
        assert!(decode(br#"{"Version":1,"Tasks":[{"Id":"a","Title":"x","Days":[1,1]}],"Done":[]}"#).is_err());
        assert!(decode(br#"{"Version":1,"Tasks":[{"Id":"a","Title":"x","Days":[]}],"Done":[]}"#).is_err());
        assert!(decode(br#"{"Version":1,"Tasks":[],"Done":["2025-13-40/a"]}"#).is_err());
        assert!(decode(&vec![b' '; MAX_BYTES + 1]).is_err());
    }

    #[test]
    fn recovers_from_corrupt_file_using_backup() {
        let (d, s) = store();
        s.read().unwrap();
        s.update(|st| st.done.push("2025-01-06/x".into())).unwrap();
        s.update(|st| st.done.push("2025-01-07/x".into())).unwrap();
        fs::write(d.path().join("plan.json"), b"{ not json").unwrap();
        let st = s.read().unwrap();
        assert!(!st.done.is_empty());
        assert!(s.take_notice().is_some());
        assert!(fs::read_dir(d.path()).unwrap().any(|e| e.unwrap().file_name().to_string_lossy().starts_with("plan.json.damaged-")));
    }

    #[test]
    fn corrupt_file_without_backup_is_kept() {
        let (d, s) = store();
        fs::write(d.path().join("plan.json"), b"garbage").unwrap();
        assert!(s.read().is_err());
        assert_eq!(fs::read(d.path().join("plan.json")).unwrap(), b"garbage");
    }

    #[test]
    fn concurrent_updates_do_not_lose_writes() {
        let (_d, s) = store();
        let s = Arc::new(s);
        s.read().unwrap();
        let handles: Vec<_> = (0..8)
            .map(|t| {
                let s = s.clone();
                std::thread::spawn(move || {
                    for i in 0..15 {
                        s.update(|st| st.done.push(format!("2025-01-{:02}/t{t}n{i}", (i % 28) + 1))).unwrap();
                    }
                })
            })
            .collect();
        for h in handles {
            h.join().unwrap();
        }
        assert_eq!(s.read().unwrap().done.len(), 8 * 15);
    }

    #[test]
    fn export_import_roundtrip_and_unicode() {
        let (d, s) = store();
        s.update(|st| st.tasks[0].title = "背单词 📚 — 日本語".into()).unwrap();
        let out = d.path().join("backup.json");
        s.export(&out).unwrap();
        s.update(|st| st.tasks[0].title = "x".into()).unwrap();
        s.import(&out).unwrap();
        assert_eq!(s.read().unwrap().tasks[0].title, "背单词 📚 — 日本語");
    }

    #[test]
    fn import_of_bad_file_leaves_data_untouched() {
        let (d, s) = store();
        let before = s.read().unwrap().tasks.len();
        let bad = d.path().join("bad.json");
        fs::write(&bad, b"{\"Version\":7}").unwrap();
        assert!(s.import(&bad).is_err());
        assert_eq!(s.read().unwrap().tasks.len(), before);
    }

    #[test]
    fn limits_are_enforced_on_save() {
        let (_d, s) = store();
        let mut st = s.read().unwrap();
        for i in 0..1001 {
            st.tasks.push(Task { id: format!("t{i}"), title: "x".into(), days: vec![1], ..Default::default() });
        }
        assert!(s.replace(st).is_err());
    }
}
