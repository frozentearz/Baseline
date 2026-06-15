using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Baseline.Config;
using Baseline.Monitors;

namespace Baseline;

/// <summary>鼠标悬停时弹出的读数浮窗：显示 4 个指标的实时百分比。点击穿透、不抢焦点。</summary>
public sealed class ReadoutWindow : Window
{
    private readonly TextBlock[] _lines;

    public ReadoutWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        IsHitTestVisible = false;

        var panel = new StackPanel { Margin = new Thickness(11, 7, 13, 7) };
        var segs = Settings.Segments;
        _lines = new TextBlock[segs.Length];
        for (int i = 0; i < segs.Length; i++)
        {
            var line = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(segs[i].Color),
                Margin = new Thickness(0, 1, 0, 1),
            };
            _lines[i] = line;
            panel.Children.Add(line);
        }

        Content = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0xE6, 0x12, 0x16, 0x22)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Child = panel,
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Native.MakeOverlay(new WindowInteropHelper(this).Handle);
    }

    public void SetValues(Metrics m)
    {
        var segs = Settings.Segments;
        for (int i = 0; i < _lines.Length && i < segs.Length; i++)
            _lines[i].Text = $"{segs[i].Label}   {Math.Round(m[segs[i].Kind] * 100)}%";
    }
}
