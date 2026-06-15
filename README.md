# Baseline

**English** | [简体中文](README.zh-CN.md)

![Baseline](assets/hero.png)

A thin, always-on-top progress bar pinned to the bottom edge of your screen — right above the taskbar. It shows live system resource usage at a glance, so you never have to open Task Manager.

Four segments, left to right: **CPU / RAM / GPU / Network**. Each segment fills in proportion to its usage. The bar is click-through (clicks pass to whatever is behind it) and never steals focus.

## Features
- Live **CPU / RAM / GPU / Network** usage as a segmented bar
- **Hover** over a segment to show its exact percentage inline
- **Settings window** (tray → Settings): bar height, refresh rate, bandwidth, which segments to show, screen edge / monitor, autostart — saved to `%AppData%\Baseline\settings.json`
- Click-through & always-on-top; lives in the system tray
- Resolution / DPI aware (WPF DIP + PerMonitorV2)

## Download
Grab the latest build from [Releases](https://github.com/frozentearz/Baseline/releases/latest):
- **`Baseline.exe`** (~73 MB, recommended) — self-contained, just download and run. No .NET install required.
- **framework-dependent zip** (~3.5 MB) — smaller, but requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0); unzip and run `Baseline.exe`.

Exit via the tray icon → Exit.

## Build from source
```powershell
dotnet run --project src/Baseline
```
Requires the .NET 10 SDK. Tech stack: C# + WPF; hardware data via `LibreHardwareMonitorLib`.

## Notes
- The Network segment is full at **50 Mbps** (≈ 6.25 MB/s download). Change `BandwidthMbps` in `Config/Settings.cs`.
- If the GPU segment stays at 0, it's usually a permissions issue — run as administrator.
- Colors, bar height and refresh interval all live in `Config/Settings.cs`.

See [CLAUDE.md](CLAUDE.md) for project conventions (Chinese).
