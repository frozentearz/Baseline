using System.Windows.Media;

namespace Baseline.Config;

/// <summary>四项指标，决定每段读哪个值。</summary>
public enum MetricKind { Cpu, Gpu, Mem, Net }

/// <summary>集中配置：改外观/行为只动这里。</summary>
public static class Settings
{
    /// <summary>宽带上限（Mbps）。运营商口径，÷8 = 满格下载速度(MB/s)。</summary>
    public const double BandwidthMbps = 50;

    /// <summary>刷新间隔（秒）。</summary>
    public const double RefreshSeconds = 1;

    /// <summary>
    /// 进度条高度（DIP，设备无关单位，非物理像素）。
    /// 系统缩放 125%/150% 时 WPF 自动按比例放大，跨分辨率/DPI 自适应。
    /// </summary>
    public const double BarHeight = 10;

    /// <summary>段内文字字号 = 条高 × 此比例（字随条高同步缩放）。</summary>
    public const double LabelFontScale = 0.7;

    /// <summary>段内文字左边距（DIP）。</summary>
    public const double LabelLeftPadding = 4;

    /// <summary>段定义：指标 + 文字 + 颜色，顺序即从左到右的显示顺序。</summary>
    public static readonly (MetricKind Kind, string Label, Color Color)[] Segments =
    {
        (MetricKind.Cpu, "CPU",  Color.FromRgb(0x4F, 0xC3, 0xF7)), // 蓝
        (MetricKind.Mem, "内存", Color.FromRgb(0xFF, 0xB7, 0x4D)), // 橙
        (MetricKind.Gpu, "GPU",  Color.FromRgb(0x81, 0xC7, 0x84)), // 绿
        (MetricKind.Net, "带宽", Color.FromRgb(0xBA, 0x68, 0xC8)), // 紫
    };
}
