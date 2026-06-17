using System.Windows.Media;

namespace Baseline.Config;

/// <summary>四项指标，决定每段读哪个值。</summary>
public enum MetricKind { Cpu, Gpu, Mem, Net }

/// <summary>静态目录：段的名称/颜色/顺序，以及不进设置窗口的细粒度常量。</summary>
public static class Settings
{
    /// <summary>段内文字字号 = 条高 × 此比例（字随条高同步缩放）。</summary>
    public const double LabelFontScale = 0.7;

    /// <summary>段内文字左边距（DIP）。</summary>
    public const double LabelLeftPadding = 4;

    /// <summary>段定义：指标 + 颜色，顺序即从左到右的显示顺序。段名走 <see cref="Loc.SegLabel"/>。</summary>
    public static readonly (MetricKind Kind, Color Color)[] Segments =
    {
        (MetricKind.Cpu, Color.FromRgb(0x4F, 0xC3, 0xF7)), // 蓝
        (MetricKind.Mem, Color.FromRgb(0xFF, 0xB7, 0x4D)), // 橙
        (MetricKind.Gpu, Color.FromRgb(0x81, 0xC7, 0x84)), // 绿
        (MetricKind.Net, Color.FromRgb(0xBA, 0x68, 0xC8)), // 紫
    };
}
