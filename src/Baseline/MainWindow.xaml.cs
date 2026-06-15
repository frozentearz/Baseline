using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Baseline.Config;
using Baseline.Monitors;
using Forms = System.Windows.Forms;

namespace Baseline;

public partial class MainWindow : Window
{
    private readonly HardwareMonitor _monitor;
    private readonly DispatcherTimer _timer;
    private Rectangle[] _fills = Array.Empty<Rectangle>();
    private TextBlock[] _labels = Array.Empty<TextBlock>();
    private double _segmentWidth;

    // 悬停读数
    private Metrics _last;
    private DispatcherTimer? _hoverTimer;
    private int _hoveredIndex = -1;
    private double _dpiX = 1, _dpiY = 1;

    public MainWindow(HardwareMonitor monitor)
    {
        InitializeComponent();
        _monitor = monitor;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Settings.RefreshSeconds) };
        _timer.Tick += (_, _) => Refresh();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 点击穿透 + 不抢焦点 + 不在 Alt-Tab/任务栏出现
        Native.MakeOverlay(new WindowInteropHelper(this).Handle);

        // 贴在工作区底边（任务栏上沿），横贯全宽
        var area = SystemParameters.WorkArea;
        Left = area.Left;
        Top = area.Bottom - Settings.BarHeight;
        Width = area.Width;
        Height = Settings.BarHeight;

        // 记录 DPI 缩放，用于把物理像素的光标坐标换算成 DIP
        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget is { } ct)
        {
            _dpiX = ct.TransformToDevice.M11;
            _dpiY = ct.TransformToDevice.M22;
        }

        BuildBars();
        Refresh();
        _timer.Start();

        // 光标轮询：命中哪一段就在该段文字后追加百分比
        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _hoverTimer.Tick += (_, _) => UpdateHover();
        _hoverTimer.Start();
    }

    private void BuildBars()
    {
        RootCanvas.Children.Clear();
        var segs = Settings.Segments;
        _segmentWidth = Width / segs.Length;
        _fills = new Rectangle[segs.Length];
        _labels = new TextBlock[segs.Length];

        double fontSize = Settings.BarHeight * Settings.LabelFontScale;

        for (int i = 0; i < segs.Length; i++)
        {
            var color = segs[i].Color;
            double left = i * _segmentWidth;

            var track = new Rectangle
            {
                Width = _segmentWidth,
                Height = Settings.BarHeight,
                Fill = new SolidColorBrush(Color.FromArgb(0x40, color.R, color.G, color.B)),
            };
            Canvas.SetLeft(track, left);
            Canvas.SetTop(track, 0);
            RootCanvas.Children.Add(track);

            var fill = new Rectangle
            {
                Width = 0,
                Height = Settings.BarHeight,
                Fill = new SolidColorBrush(color),
            };
            Canvas.SetLeft(fill, left);
            Canvas.SetTop(fill, 0);
            RootCanvas.Children.Add(fill);
            _fills[i] = fill;

            // 段内文字：水平左对齐、垂直居中
            var text = new TextBlock
            {
                Text = segs[i].Label,
                FontSize = fontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HAlign.Left,
                VerticalAlignment = VAlign.Center,
                Margin = new Thickness(Settings.LabelLeftPadding, 0, 0, 0),
            };
            _labels[i] = text;

            var label = new Border
            {
                Width = _segmentWidth,
                Height = Settings.BarHeight,
                Background = Brushes.Transparent,
                Child = text,
            };
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, 0);
            RootCanvas.Children.Add(label);
        }
    }

    private void Refresh()
    {
        _last = _monitor.Read();
        var segs = Settings.Segments;
        for (int i = 0; i < _fills.Length && i < segs.Length; i++)
            _fills[i].Width = Math.Max(0, _segmentWidth * _last[segs[i].Kind]);

        UpdateLabels(); // 悬停中也让百分比随刷新跳动
    }

    /// <summary>光标落在底边进度条上时，算出悬停的是哪一段。</summary>
    private void UpdateHover()
    {
        var c = Forms.Cursor.Position;          // 物理像素
        var wa = Forms.Screen.PrimaryScreen!.WorkingArea;
        int barPx = (int)Math.Ceiling(Settings.BarHeight * _dpiY);
        bool over = c.X >= wa.Left && c.X <= wa.Right
                    && c.Y >= wa.Bottom - Math.Max(barPx, 6) && c.Y < wa.Bottom;

        int idx = -1;
        if (over && _segmentWidth > 0)
        {
            idx = (int)Math.Floor((c.X / _dpiX - Left) / _segmentWidth);
            if (idx < 0 || idx >= _labels.Length) idx = -1;
        }

        if (idx != _hoveredIndex)
        {
            _hoveredIndex = idx;
            UpdateLabels();
        }
    }

    /// <summary>命中段显示「名称 百分比」，其余段只显示名称。</summary>
    private void UpdateLabels()
    {
        var segs = Settings.Segments;
        for (int i = 0; i < _labels.Length && i < segs.Length; i++)
        {
            _labels[i].Text = i == _hoveredIndex
                ? $"{segs[i].Label} {Math.Round(_last[segs[i].Kind] * 100)}%"
                : segs[i].Label;
        }
    }
}
