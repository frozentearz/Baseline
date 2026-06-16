using System.IO;
using System.Text.Json;

namespace Baseline.Config;

/// <summary>贴边位置：屏幕底边或顶边。</summary>
public enum EdgePosition { Bottom, Top }

/// <summary>用户可在设置窗口里调整、并持久化到 JSON 的配置。</summary>
public sealed class AppSettings
{
    public double BarHeight { get; set; } = 10;     // DIP，4–24
    public double Opacity { get; set; } = 1.0;       // 整条不透明度，0.2–1.0
    public double RefreshSeconds { get; set; } = 1;  // 0.5 / 1 / 2
    public double BandwidthMbps { get; set; } = 50;  // 网络满格基准

    public bool ShowCpu { get; set; } = true;
    public bool ShowMem { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowNet { get; set; } = true;

    public EdgePosition Position { get; set; } = EdgePosition.Bottom;

    /// <summary>目标显示器的设备名；null/空 表示主显示器。</summary>
    public string? MonitorDeviceName { get; set; }

    public bool IsEnabled(MetricKind kind) => kind switch
    {
        MetricKind.Cpu => ShowCpu,
        MetricKind.Mem => ShowMem,
        MetricKind.Gpu => ShowGpu,
        MetricKind.Net => ShowNet,
        _ => false,
    };

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    /// <summary>把越界值收敛到合理范围。</summary>
    public void Normalize()
    {
        BarHeight = Math.Clamp(BarHeight, 4, 24);
        Opacity = double.IsNaN(Opacity) ? 1.0 : Math.Clamp(Opacity, 0.2, 1.0);
        if (RefreshSeconds is not (0.5 or 1 or 2)) RefreshSeconds = 1;
        if (BandwidthMbps <= 0 || double.IsNaN(BandwidthMbps)) BandwidthMbps = 50;
        // 至少保留一个段，全关时回退为全开
        if (!ShowCpu && !ShowMem && !ShowGpu && !ShowNet)
            ShowCpu = ShowMem = ShowGpu = ShowNet = true;
    }

    // ---- 持久化 ----
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Baseline", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (s is not null) { s.Normalize(); return s; }
            }
        }
        catch { /* 损坏/不可读则用默认值 */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch { /* 写入失败不影响运行 */ }
    }
}
