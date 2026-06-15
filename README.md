# Baseline

![Baseline](assets/hero.png)

屏幕底边一条横贯全宽的细进度条，紧贴任务栏上沿，实时显示资源占用：

```
[██████░░ CPU][█████░░░ 内存][███░░░░░ GPU][██░░░░░░ 带宽]
```

4 段从左到右：**CPU / 内存 / GPU / 带宽**，每段填充长度 = 占用比。常驻置顶、点击穿透（点它等于点桌面）。

## 运行

```powershell
dotnet run --project src/Baseline
```

或编译后直接运行 `src/Baseline/bin/Debug/net10.0-windows/Baseline.exe`。

- 退出：托盘图标右键 → 退出。
- 开机自启：托盘图标右键 → 开机自启（写当前用户启动项，可随时取消）。

## 说明

- **网络满格** = 50 Mbps 宽带（≈ 6.25 MB/s 下载）。改 `Config/Settings.cs` 的 `BandwidthMbps`。
- **GPU 段为 0**：多为非管理员权限或无独显。以管理员身份运行可解。
- 外观（颜色、条高、刷新间隔）全在 `Config/Settings.cs`。

详细规范见 [CLAUDE.md](CLAUDE.md)。
