using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Baseline.Config;
using Baseline.Monitors;

namespace Baseline;

public partial class MainWindow : Window
{
    private readonly HardwareMonitor _monitor;
    private readonly DispatcherTimer _timer;
    private Rectangle[] _fills = Array.Empty<Rectangle>();
    private double _segmentWidth;

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
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        // 贴在工作区底边（任务栏上沿），横贯全宽
        var area = SystemParameters.WorkArea;
        Left = area.Left;
        Top = area.Bottom - Settings.BarHeight;
        Width = area.Width;
        Height = Settings.BarHeight;

        BuildBars();
        Refresh();
        _timer.Start();
    }

    private void BuildBars()
    {
        RootCanvas.Children.Clear();
        var segs = Settings.Segments;
        _segmentWidth = Width / segs.Length;
        _fills = new Rectangle[segs.Length];

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
            var label = new Border
            {
                Width = _segmentWidth,
                Height = Settings.BarHeight,
                Background = Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = segs[i].Label,
                    FontSize = fontSize,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HAlign.Left,
                    VerticalAlignment = VAlign.Center,
                    Margin = new Thickness(Settings.LabelLeftPadding, 0, 0, 0),
                },
            };
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, 0);
            RootCanvas.Children.Add(label);
        }
    }

    private void Refresh()
    {
        var m = _monitor.Read();
        var segs = Settings.Segments;
        for (int i = 0; i < _fills.Length && i < segs.Length; i++)
            _fills[i].Width = Math.Max(0, _segmentWidth * Value(m, segs[i].Kind));
    }

    private static double Value(Metrics m, MetricKind kind) => kind switch
    {
        MetricKind.Cpu => m.Cpu,
        MetricKind.Gpu => m.Gpu,
        MetricKind.Mem => m.Mem,
        MetricKind.Net => m.Net,
        _ => 0,
    };

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
