# Kora Desk · 刻序

<p align="center"><img src="docs/assets/hero.svg" alt="Kora Desk — Your day. A quieter desktop." width="100%"></p>
<p align="center">
<a href="https://github.com/Cennyk-learner/kora-desk/releases/latest"><img alt="Download for Windows" src="https://img.shields.io/badge/Download-Windows_x64-557665?style=flat-square&logo=windows"></a>
<a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-718278?style=flat-square"></a>
<img alt="Offline, local storage" src="https://img.shields.io/badge/offline-local_storage-718278?style=flat-square">
</p>
<p align="center"><b>把每天的事，轻轻放在桌面上；也把自己照顾好。</b><br>A quiet Windows desktop planner for students: daily checklist, focus timer, and a kinder way to keep going.</p>
<p align="center"><a href="#快速开始">下载与使用</a> · <a href="#功能">功能</a> · <a href="README.en.md">English</a> · <a href="CHANGELOG.md">更新记录</a></p>

Kora Desk 2.0 是一个**单文件、单进程**的 Windows 桌面清单：一个安静的桌面小组件显示今天要做的事，管理窗口用来整理每周安排、看统计、开番茄钟。数据只存在这台电脑上，没有账号，不联网。

2.0 基于 Tauri 2（Rust + Web 前端），可执行文件约 **100 MB**（其中约 95 MB 是全屏专注的 100 张 4K 风景图，代码本体仅 3–4 MB），免安装，双击即用。

## 快速开始

1. 从 [Releases](https://github.com/Cennyk-learner/kora-desk/releases/latest) 下载 `Kora.exe`（或自行构建，见 [DEVELOPMENT.md](docs/DEVELOPMENT.md)）。
2. 放在任意固定位置，双击运行。关闭管理窗口后程序留在系统托盘，桌面组件继续工作。
3. 右键托盘图标：打开计划、开始/暂停番茄钟、显示/隐藏组件、切换置顶、退出。

**运行环境：** Windows 10/11 x64，需要 Microsoft Edge WebView2 运行时（Windows 11 与较新的 Windows 10 自带；缺少时首次运行会引导安装）。

**从 1.x 升级：** 直接运行即可。`%LOCALAPPDATA%\KoraDesk\plan.json` 会自动升级到新格式，原文件备份为 `plan.json.v1.bak`，字段只增不删。

## 功能

**每天的清单**
- 每周重复 / 指定日期任务；分组；预计用时；桌面组件上直接勾选。
- 五种界面风格（macOS、Fluent、Material 3、IBM Carbon、点阵仪表），各有浅色与深色，可跟随系统。
- 桌面层 / 始终置顶两种组件模式，可拖动，多显示器与高 DPI 下位置自动修正。

**让人想坚持的正反馈**
- 勾选有对勾动画、进度环平滑增长、数字滚动和彩纸；连续快速打卡音高逐级上升（连击）；当天全部完成有大庆祝。
- 所有声音由 WebAudio 合成，没有音频文件；可静音、调音量，并遵守系统「减少动态效果」。
- **每日一句名言**：内置 420+ 条，洗牌袋算法保证一轮之内不重复，同一天重开不变，可收藏。来源不确定的一律标「佚名」。
- **每日一词**（雅思向），同样不重复，可收藏。

**番茄钟**
- 每次开始前在专注页自己选：专注 / 短休息 / 长休息时长（可自定义分钟）、每几个番茄长休息、绑定任务、完成 N 个番茄自动勾选、背景声、严格模式；会记住上一次的选择。
- **全屏专注**：铺满屏幕的沉浸界面，背景是 100 张 4K 风景照（冰岛为主，另有挪威、北欧、意大利多洛米蒂、印度尼西亚、新西兰、澳大利亚的自然风光，以及纽约、洛杉矶、奥克兰、拉斯维加斯、旧金山、芝加哥等美国城市天际线），缓慢淡入淡出（手动切换时较快，文字与图片同步渐变），默认每 5 小时换一张（可改为 1 小时 / 15 分钟 / 仅手动），也可随时手动上一张/下一张；每次打开都从不同的一张开始，看完一轮才会重复，左下角标注地点和摄影师（图片来自 Wikimedia Commons，版权见 [docs/IMAGE_CREDITS.md](docs/IMAGE_CREDITS.md)）。可在里面开始/暂停/结束，用右上角按钮或 Esc 退出。
- **防休眠**：专注期间保持屏幕常亮，不会熄屏或休眠。
- 抗重启：保存的是结束时间；程序关闭期间结束的番茄，下次打开会被正确记入。
- 小组件内嵌迷你倒计时，托盘提示显示剩余时间；极简视图；棕噪音 / 白噪音 / 雨声（合成）；系统通知；全局快捷键。

**放弃库与请假**
- **没有“放弃今天”按钮，一切自动结算**：第二天打开程序时，前一天只要有任务没完成就自动记一次放弃，**每天最多记一次，每次 0.1 天**。
- 放弃库页：累计、**一年推算**（日均放弃 × 0.1 × 365；活跃天数不足 7 天会标注）、按月柱状图、最常没完成的任务、可事后标注原因（累 / 没状态 / 太忙 / 拖延）。
- **请假**：在今天页点「请假」并写下理由。请假的日子不计入放弃库，也不打断连续天数；理由和记录会在「放弃与请假」页汇总展示。**一周请假 ≥ 3 天会预警。**

**统计与成长**
- 连续打卡与冻结卡（每月 N 次漏做不断连）、全年热力图、周 / 月完成率、分组占比、近 14 天专注、预估与实际用时对比。
- 经验值与等级（由历史推算，不会重复计入）、32 枚分组成就徽章（坚持、完成、专注、韧性、生活、学习、成长）。
- 每周小结：一键保存为图片。导出 JSON 备份或 CSV。

**学习方法**
- 考试倒计时（组件常驻显示），按剩余天数给出每日建议量。
- 每日承诺 + 晚间复盘（做到 / 部分 / 没做到 + 心情 + 一句话）。
- 间隔复习：任务勾选「间隔复习」，完成后自动在第 1/2/4/7/15 天生成复习待办。

**照顾自己**
- 心情记录、每日三件小确幸、喝水记录、4-4-4-4 与 4-7-8 呼吸练习、给自己的奖励清单。
- 20-20-20 护眼、久坐、就寝提醒（都可在偏好里开关）。
- 开机自启（启动后只在托盘运行）。

## 数据与隐私

任务、记录与设置保存在 `%LOCALAPPDATA%\KoraDesk\plan.json`（可用环境变量 `KORA_DATA_DIR` 改位置）。每次保存前自动保留上一版为 `plan.json.bak`；文件损坏时自动从备份恢复并保留损坏文件。应用不联网，没有遥测。

## 许可

MIT。点阵字体见 [`assets/fonts/OFL.txt`](assets/fonts/OFL.txt)。

[查看界面风格预览（1.1 版渲染）→](docs/THEMES.md)
