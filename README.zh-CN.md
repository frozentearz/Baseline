# Baseline

[English](README.md) | **简体中文**

![Baseline](assets/hero.png)

屏幕底边一条横贯全宽的细进度条，紧贴任务栏上沿，实时显示资源占用，不用打开任务管理器。

4 段从左到右：**CPU / 内存 / GPU / 带宽**，每段填充长度 = 占用比。常驻置顶、点击穿透（点它等于点桌面）。

## 功能
- CPU / 内存 / GPU / 带宽 实时占用，分段进度条
- **鼠标悬停**在某一段上就地显示精确数值——带宽段显示实时下载网速（如 `4.2 MB/s`），其余段显示占用百分比
- **10 种语言**，默认跟随系统语言（见 [语言](#语言)）
- **设置窗口**（托盘 →「设置…」）：条高、透明度、刷新间隔、宽带、显示哪些段、屏幕边/显示器、语言、开机自启，存到 `%AppData%\Baseline\settings.json`
- 点击穿透、常驻置顶；系统托盘驻留
- 跨分辨率 / DPI 自适应（WPF DIP + PerMonitorV2）

## 设置
从托盘图标 →「设置…」打开（或运行 `Baseline.exe --open-settings`）。

![Baseline 设置](assets/settings-zh-CN.png)

## 下载
到 [Releases](https://github.com/frozentearz/Baseline/releases/latest) 取最新版（二选一）：
- **`Baseline.exe`（约 73 MB，推荐）** — 自包含单文件，双击即用，无需装 .NET。
- **依赖框架版 zip（约 3.5 MB）** — 体积小，但需先装 [.NET 10 桌面运行时](https://dotnet.microsoft.com/download/dotnet/10.0)，解压后运行 `Baseline.exe`。

退出：托盘图标右键 →「退出」。

## 从源码运行
```powershell
dotnet run --project src/Baseline
```
需 .NET 10 SDK。技术栈：C# + WPF，硬件数据用 `LibreHardwareMonitorLib`。

## 语言
内置 10 种语言，首次运行自动跟随 Windows 显示语言，之后可在设置窗口随时切换。

English · 简体中文 · 繁體中文 · 日本語 · 한국어 · Español · Français · Deutsch · Русский · Português

<details>
<summary>各语言界面截图</summary>

| | | |
|---|---|---|
| ![en](assets/settings-en.png) | ![zh-CN](assets/settings-zh-CN.png) | ![zh-TW](assets/settings-zh-TW.png) |
| ![ja](assets/settings-ja.png) | ![ko](assets/settings-ko.png) | ![es](assets/settings-es.png) |
| ![fr](assets/settings-fr.png) | ![de](assets/settings-de.png) | ![ru](assets/settings-ru.png) |
| ![pt](assets/settings-pt.png) | | |

</details>

## 说明
- 带宽满格默认按 **50 Mbps 宽带**（≈ 6.25 MB/s 下载）计算，在设置窗口里改（只统计**下载**、且取流量最大的那块网卡，VPN 虚拟网卡不会重复计数）。
- GPU 段一直为 0 多为权限问题，可以管理员身份运行。
- 颜色、段布局在 `Config/Settings.cs`；界面文案在 `Config/Loc.cs`。

详细规范见 [CLAUDE.md](CLAUDE.md)。
