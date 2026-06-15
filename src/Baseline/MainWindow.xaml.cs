using System.Linq;
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
    private AppSettings _settings;

    private (MetricKind Kind, string Label, Color Color)[] _shown =
        Array.Empty<(MetricKind, string, Color)>();
    private Rectangle[] _fills = Array.Empty<Rectangle>();
    private TextBlock[] _labels = Array.Empty<TextBlock>();
    private double _segmentWidth;

    private Metrics _last;
    private DispatcherTimer? _hoverTimer;
    private int _hoveredIndex = -1;
    private double _dpiX = 1, _dpiY = 1;

    public MainWindow(HardwareMonitor monitor, AppSettings settings)
    {
        InitializeComponent();
        _monitor = monitor;
        _settings = settings;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.RefreshSeconds) };
        _timer.Tick += (_, _) => Refresh();
    }

    /// <summary>当前生效的配置（设置窗口克隆它来编辑）。</summary>
    public AppSettings CurrentSettings => _settings;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        Native.MakeOverlay(new WindowInteropHelper(this).Handle);

        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget is { } ct)
        {
            _dpiX = ct.TransformToDevice.M11;
            _dpiY = ct.TransformToDevice.M22;
        }

        ApplyLayout();
        BuildBars();
        Refresh();
        _timer.Start();

        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _hoverTimer.Tick += (_, _) => UpdateHover();
        _hoverTimer.Start();
    }

    /// <summary>设置窗口「确定」后整体应用（不负责存盘，由调用方存）。</summary>
    public void ApplySettings(AppSettings s)
    {
        _settings = s;
        _monitor.SetBandwidth(s.BandwidthMbps);
        _timer.Interval = TimeSpan.FromSeconds(s.RefreshSeconds);
        ApplyLayout();
        BuildBars();
        Refresh();
    }

    /// <summary>设置窗口里拖动条高滑块时的实时预览（不存盘）。</summary>
    public void PreviewBarHeight(double height)
    {
        _settings.BarHeight = Math.Clamp(height, 4, 24);
        ApplyLayout();
        BuildBars();
        Refresh();
    }

    private Forms.Screen ResolveScreen()
    {
        if (!string.IsNullOrEmpty(_settings.MonitorDeviceName))
            foreach (var s in Forms.Screen.AllScreens)
                if (s.DeviceName == _settings.MonitorDeviceName)
                    return s;
        return Forms.Screen.PrimaryScreen!;
    }

    private void ApplyLayout()
    {
        var wa = ResolveScreen().WorkingArea; // 物理像素
        Left = wa.Left / _dpiX;
        Width = wa.Width / _dpiX;
        Height = _settings.BarHeight;
        Top = _settings.Position == EdgePosition.Top
            ? wa.Top / _dpiY
            : wa.Bottom / _dpiY - _settings.BarHeight;
    }

    private void BuildBars()
    {
        RootCanvas.Children.Clear();
        _shown = Settings.Segments.Where(s => _settings.IsEnabled(s.Kind)).ToArray();
        int n = Math.Max(_shown.Length, 1);
        _segmentWidth = Width / n;
        _fills = new Rectangle[_shown.Length];
        _labels = new TextBlock[_shown.Length];
        _hoveredIndex = -1;

        double fontSize = _settings.BarHeight * Settings.LabelFontScale;

        for (int i = 0; i < _shown.Length; i++)
        {
            var color = _shown[i].Color;
            double left = i * _segmentWidth;

            var track = new Rectangle
            {
                Width = _segmentWidth,
                Height = _settings.BarHeight,
                Fill = new SolidColorBrush(Color.FromArgb(0x40, color.R, color.G, color.B)),
            };
            Canvas.SetLeft(track, left);
            Canvas.SetTop(track, 0);
            RootCanvas.Children.Add(track);

            var fill = new Rectangle
            {
                Width = 0,
                Height = _settings.BarHeight,
                Fill = new SolidColorBrush(color),
            };
            Canvas.SetLeft(fill, left);
            Canvas.SetTop(fill, 0);
            RootCanvas.Children.Add(fill);
            _fills[i] = fill;

            var text = new TextBlock
            {
                Text = _shown[i].Label,
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
                Height = _settings.BarHeight,
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
        for (int i = 0; i < _fills.Length; i++)
            _fills[i].Width = Math.Max(0, _segmentWidth * _last[_shown[i].Kind]);
        UpdateLabels();
    }

    private void UpdateHover()
    {
        var c = Forms.Cursor.Position; // 物理像素
        var wa = ResolveScreen().WorkingArea;
        int barPx = (int)Math.Ceiling(_settings.BarHeight * _dpiY);
        int band = Math.Max(barPx, 6);
        bool inY = _settings.Position == EdgePosition.Top
            ? (c.Y >= wa.Top && c.Y < wa.Top + band)
            : (c.Y >= wa.Bottom - band && c.Y < wa.Bottom);
        bool over = c.X >= wa.Left && c.X <= wa.Right && inY;

        int idx = -1;
        if (over && _segmentWidth > 0)
        {
            idx = (int)Math.Floor((c.X / _dpiX - Left) / _segmentWidth);
            if (idx < 0 || idx >= _shown.Length) idx = -1;
        }

        if (idx != _hoveredIndex)
        {
            _hoveredIndex = idx;
            UpdateLabels();
        }
    }

    private void UpdateLabels()
    {
        for (int i = 0; i < _labels.Length; i++)
        {
            _labels[i].Text = i == _hoveredIndex
                ? $"{_shown[i].Label} {Math.Round(_last[_shown[i].Kind] * 100)}%"
                : _shown[i].Label;
        }
    }
}
