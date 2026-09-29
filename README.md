# Kora Desk · 刻序

<p align="center"><img src="docs/assets/hero.svg" alt="Kora Desk — Your day. A quieter desktop." width="100%"></p>
<p align="center">
<a href="https://github.com/Cennyk-learner/kora-desk/releases/latest"><img alt="Download for Windows" src="https://img.shields.io/badge/Download-Windows_x64-557665?style=flat-square&logo=windows"></a>
<a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-718278?style=flat-square"></a>
<img alt="Offline, local storage" src="https://img.shields.io/badge/offline-local_storage-718278?style=flat-square">
<a href="https://github.com/Cennyk-learner/kora-desk/stargazers"><img alt="GitHub stars" src="https://img.shields.io/github/stars/Cennyk-learner/kora-desk?style=flat-square&color=557665"></a>
</p>
<p align="center"><b>把每天的事，轻轻放在桌面上。</b><br>A minimal Windows task widget for daily checklists and weekly routines.</p>
<p align="center"><a href="#快速开始">下载与使用</a> · <a href="#每周有节奏每天有清单">功能</a> · <a href="README.en.md">English</a> · <a href="https://github.com/Cennyk-learner/kora-desk/issues">反馈问题</a></p>

Kora Desk 是一款原生 Windows 桌面清单工具。一个安静的小组件，显示今天要做的事；一个独立的管理窗口，整理每周重复的安排。没有时间格子，没有账号，也不需要保持管理窗口开启。

内置**五种界面风格**，每种都有浅色和深色：macOS、Windows 11 Fluent、Material 3 Expressive、IBM Carbon、点阵仪表。在偏好设置里随时切换，管理窗口和桌面组件会一起更换。名称保留字母 **K**，中文 **刻序**，意为把日常一点点安排好。

## 界面预览

![macOS 浅色界面](docs/assets/themes/mac-light.png)

[查看五套风格的浅色、深色与桌面组件预览 →](docs/THEMES.md)

## 界面风格

| 风格 | 特点 |
|---|---|
| **macOS** | 彩色图标的侧栏、圆形复选框、大标题加待办数字、分组圆角列表；组件是浅色卡片。默认风格 |
| **Fluent · Mica** | Windows 11 原生语言：云母底、圆角内容层、导航栏底部固定设置、摘要卡里的进度环 |
| **Material 3 Expressive** | 粗体大标题、波浪进度条和曲奇形状徽章、选中变胶囊的星期条、分段清单 |
| **IBM Carbon** | 顶栏加侧栏，今日、学习、健身三个数据块，任务以表格呈现，方角，信息密度高 |
| **点阵仪表** | 点阵字体的大日期，一排圆点表示进度，单一红色信号，点线分隔；组件是黑色卡片 |

外观可选「跟随系统」「浅色」「深色」。跟随系统时，Windows 切换浅色或深色后会自动更新。

在 Windows 上运行 `scripts\preview.ps1`，可以把每种风格、每种外观下的管理窗口（三个页面）和组件渲染成图片，输出到 `dist\previews`。

## 快速开始

1. 前往 [Releases](https://github.com/Cennyk-learner/kora-desk/releases/latest)，下载最新版的 `KoraDesk-v*-win-x64.zip`。
2. 将压缩包**完整解压**到一个固定文件夹。
3. 双击 **`Kora.exe`**，管理窗口与桌面组件会一起打开。
4. 在组件上直接勾选任务。关闭管理窗口，组件仍会继续运行。

**运行环境：** Windows 10 / 11 x64，.NET Framework 4.8 或更高版本。较新的 Windows 通常已包含该运行时；若系统提示缺少运行时，可从 [Microsoft 官方页面](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)获取。

软件免安装。请把两个 EXE、`Kora.Core.dll`、配置文件、图标和 `fonts` 文件夹保留在同一目录。当前版本未进行商业代码签名；发行页提供 SHA-256 校验值。

## 每周有节奏，每天有清单

| 功能 | 你可以做什么 |
|---|---|
| 五种界面风格 | macOS、Fluent、Material 3、Carbon、点阵；浅色、深色或跟随系统 |
| 独立桌面组件 | 仅看今天的任务，直接完成或取消勾选 |
| 桌面层 / 始终置顶 | 默认让其他应用盖住组件，需要时再开启置顶 |
| 两个独立进程 | 关闭管理窗口后，组件继续使用；也可单独启动 `Kora.Widget.exe` |
| 每周重复 | 自选周一到周日；全选即每天重复 |
| 指定日期任务 | 给某一天添加一次性的安排 |
| 可编辑清单 | 修改名称、备注、分组、重复日，或删除任务 |
| 每日完成记录 | 每天独立计数，翻到其他日期查看与勾选 |
| 本地备份 | 导出 / 导入 JSON，保存时自动保留上一版 |
| 托盘入口 | 打开管理窗口、隐藏组件、切换置顶、退出组件 |

组件与管理窗口通常在一秒左右同步。拖动组件顶部，可以移动到桌面上合适的位置；多于可见区域的任务可滚动查看。

### 内置的学习与运动计划

第一次启动会生成以下**可自由编辑**的示例。你可以把它改成自己的工作、学习或生活清单。

**每天：** 词汇真经、顾家北写作、雅思王语料库、百词斩。

| 星期 | 听阅安排 | 运动安排 |
|---|---|---|
| 周一 | — | 胸 + 肩 + 三头 |
| 周二 | 听力 + 阅读整套 | 健身休息 |
| 周三 | 听阅复盘 | 背 + 二头 |
| 周四 | 听力 + 阅读整套 | 健身休息 |
| 周五 | 听阅复盘 | 腿 + 核心 |
| 周六 | 听力 + 阅读整套 | 倒三角专项 |
| 周日 | 听阅复盘 | 纯有氧 |

任务只表示当天的事项，不限定开始时间。教材名称仅用于示例任务，不包含教材内容，也不代表与相应出版方存在关联。

## 常用操作

- **增加 / 修改任务：** 点击「添加任务」，或任务右侧的 `···`；在「每周计划」中可以看到全部任务。
- **切换风格：** 偏好设置 → 界面风格 / 外观。
- **切换置顶：** 组件右上角 `···` → 切换置顶，或在管理窗口「偏好设置」中调整。
- **找回组件：** 托盘图标 → 显示 / 隐藏组件；也可以在偏好设置中重置组件位置。
- **退出：** 关闭主窗口只结束管理程序。右键托盘图标 →「退出组件」结束组件。
- **升级：** 退出两个程序，解压新版到原目录。任务数据不在程序目录，正常升级不会覆盖它。
- **备份：** 偏好设置 → 导出备份。导入会替换现有计划与完成记录，操作前会再次确认。

## 隐私与数据

应用不发送网络请求，没有遥测、账号、云同步或广告。任务、偏好和每日勾选保存在：

```text
%LOCALAPPDATA%\KoraDesk\plan.json
```

每次保存通过进程间互斥锁和原子替换写入，上一版位于 `plan.json.bak`。发现损坏且备份有效时，会恢复备份并保留损坏文件。数据是普通 JSON；导出的备份可能包含你的私人安排，请自行妥善保管。

## 常见问题

**桌面组件和普通置顶窗口有什么区别？**
默认模式把小组件挂载到 Windows 桌面窗口。普通软件会覆盖它。置顶模式则作为一个小型独立窗口显示在其他窗口之上。组件不占用正常任务栏入口。

**为什么在某些电脑上桌面模式表现不同？**
桌面挂载依赖 Explorer 的窗口结构，不是 Windows 的正式小组件扩展 API。替代桌面、部分桌面美化工具和未来系统更新可能影响它。组件会尝试重新挂载；不可用时提示普通窗口状态，可切换为置顶使用。Explorer 重启后若组件没有恢复，请重新运行 `Kora.Widget.exe`。

**编辑每周规则会改变过去吗？**
日期列表根据当前规则生成，因此规则修改也会影响过去日期显示的任务。完成记录按任务 ID 和日期保留，但不保存历史任务标题或历史规则版本。请在大幅调整计划前导出备份。

**有提醒、番茄钟、云同步和自动开机启动吗？**
当前版本聚焦清单与桌面组件，未包含上述功能。需要开机启动时，可手动把 `Kora.Widget.exe` 的快捷方式放入 Windows 启动文件夹。

## 从源码构建

无需下载 NuGet 包或安装浏览器运行时。使用 Windows PowerShell 和 .NET Framework 自带的 C# 编译器：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Test
```

输出在 `dist/`。构建脚本运行核心行为测试，涵盖重复规则、跨日状态、并发写入、导入校验和损坏恢复。更多结构与验证说明见 [开发说明](docs/DEVELOPMENT.md)。

## 参与改进

欢迎在 [Issues](https://github.com/Cennyk-learner/kora-desk/issues) 报告问题或提出建议。反馈桌面显示问题时，请附上 Windows 版本、屏幕缩放比例，以及是否使用第三方桌面工具，避免上传包含私人任务的数据文件。

如果这个安静的小工具对你有帮助，欢迎点一个 **Star**，让更多需要它的人看到。

[MIT License](LICENSE) · Made for a quieter desktop.
