# Baseline — 贴边资源进度条

## 这是什么

一条横贯屏幕底边、紧贴任务栏上沿的细进度条。等分 4 段，从左到右：
**CPU / 内存 / GPU / 带宽**，每段填充长度 = 该指标实时占用比。常驻置顶、鼠标点击穿透。

替代「为看占用而打开任务管理器」的场景，瞄一眼底边就知道资源状况。
名字一语双关：它就贴在屏幕最底那条「基准线(base line)」上，baseline 本身又是「基础指标」。

## 技术栈

- **C# + WPF**（.NET 10，`net10.0-windows`）
- 硬件数据：`LibreHardwareMonitorLib`（CPU/GPU/内存 load%）
- 网络速度：`System.Net.NetworkInformation`（不依赖第三方）
- 托盘菜单：WinForms `NotifyIcon`（`UseWindowsForms=true`）

## 指标口径（重要，别改错）

| 段 | 数据来源 | 满格(100%)定义 |
|---|---|---|
| CPU | LHM `CPU Total` load | 100% 占用 |
| 内存 | LHM `Memory` load | 物理内存 100% 占用 |
| GPU | LHM GPU `Core` load（NVIDIA 优先） | 100% 占用 |
| 带宽 | 增量最大的活动网卡下载字节/秒（非全部相加，避免 VPN/虚拟网卡重复计数） | **50 Mbps 宽带 = 6.25 MB/s**（`Settings.BandwidthMbps`） |

- 「50M 宽带」是 50 **Mbps**，÷8 = 6.25 MB/s 才是满格速度，不要写成 50 MB/s。
- 网络只取**下载**速度。要改带宽改 `Settings.BandwidthMbps`。

## 目录结构

```
statusline/
├── CLAUDE.md
├── README.md
├── Baseline.slnx
└── src/Baseline/
    ├── Baseline.csproj
    ├── app.manifest          # asInvoker + PerMonitorV2 DPI
    ├── GlobalUsings.cs        # WPF/WinForms 同名类型的全局别名
    ├── App.xaml(.cs)         # 启动、托盘、生命周期
    ├── MainWindow.xaml(.cs)  # 贴边窗口 + 渲染 + 点击穿透
    ├── Monitors/
    │   └── HardwareMonitor.cs # 4 指标采集
    └── Config/
        ├── Settings.cs        # 段定义（指标+颜色）、字号/边距等细粒度常量
        ├── AppSettings.cs     # 用户可调并持久化的配置（条高/透明度/带宽/语言/位置…）
        ├── Loc.cs             # i18n 字符串表：所有面向用户文案集中于此，10 种语言
        └── Autostart.cs       # HKCU Run 开机自启
```

## 约定

- 权限：默认**普通权限**（只读 load%，无需管理员）。GPU 读不到再以管理员运行。
- 命名：类型/文件英文 PascalCase。
- 颜色、条高、刷新间隔、带宽都集中在 `Settings.cs`，不要散落到各处硬编码。
- 退出只能通过托盘「退出」（`ShutdownMode=OnExplicitShutdown`）。
- 文件编码：源码一律 UTF-8。**禁止用 PowerShell 5.1 的 `Set-Content`/`Out-File` 批量改含中文的文件**（会按 ANSI 重写导致乱码），改文件用编辑器/Write 工具。

## 多语言（硬性要求）

**每次开发必须兼容多语言。任何新增或改动的、面向用户可见的文案，都要同步支持全部语言，绝不允许只写中文或英文的字面量。**

- 新增任何用户可见字符串：先在 `Config/Loc.cs` 字符串表加一行 key，并补齐全部 10 种语言译文（数组顺序严格对应 `AppLanguage` 枚举），再在代码里用 `Loc.T(key)` 取用。
- 禁止在 XAML、代码、托盘菜单中写死任何一种语言的字面量。
- 进度条段名走 `Loc.SegLabel(kind)`（CPU/GPU 全语言通用，内存/带宽随语言变）。
- 新增语言：在 `AppLanguage` 末尾追加枚举值、`Loc.Count` 与字符串表每行同步加列、`Loc.Choices` 补一项。
- 自检：交付前切到至少一种非默认语言（如英文）跑一遍，确认没有残留未翻译的文案。

## 验证

```powershell
dotnet build src/Baseline/Baseline.csproj   # 编译必须通过
dotnet run --project src/Baseline             # 底边出现进度条，跑满 CPU 看第一段涨满
```

## 本次范围边界（未做）

不做：曲线图、历史统计、换肤、磁盘段、多屏分别显示。先把单屏一条能动的进度条跑通。
