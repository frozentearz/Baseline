using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Baseline.Config;
using Forms = System.Windows.Forms;

namespace Baseline;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _main;
    private readonly AppSettings _working;
    private readonly AppSettings _original;
    private bool _committed;

    public SettingsWindow(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        _original = main.CurrentSettings.Clone();
        _working = main.CurrentSettings.Clone();

        Localize();
        LoadInto();

        HeightSlider.ValueChanged += (_, e) =>
        {
            HeightValue.Text = $"{(int)Math.Round(e.NewValue)} px";
            _main.PreviewBarHeight(e.NewValue); // 实时预览
        };
        OpacitySlider.ValueChanged += (_, e) =>
        {
            OpacityValue.Text = $"{(int)Math.Round(e.NewValue * 100)} %";
            _main.PreviewOpacity(e.NewValue); // 实时预览
        };
        OkButton.Click += OnOk;
        CancelButton.Click += (_, _) => Close();
    }

    /// <summary>按当前语言填充所有静态文案（标题、分组标题、字段名、按钮）。</summary>
    private void Localize()
    {
        Title = Loc.T("settings.title");
        AppearanceHeader.Text = Loc.T("group.appearance");
        BarHeightLabel.Text = Loc.T("field.barHeight");
        OpacityLabel.Text = Loc.T("field.opacity");
        RefreshLabel.Text = Loc.T("field.refresh");

        string sec = Loc.T("unit.sec");
        Refresh05.Content = $"0.5 {sec}";
        Refresh1.Content = $"1 {sec}";
        Refresh2.Content = $"2 {sec}";

        NetworkHeader.Text = Loc.T("group.network");
        BandwidthLabel.Text = Loc.T("field.bandwidth");

        SegmentsHeader.Text = Loc.T("group.segments");
        ShowCpu.Content = Loc.SegLabel(MetricKind.Cpu);
        ShowMem.Content = Loc.SegLabel(MetricKind.Mem);
        ShowGpu.Content = Loc.SegLabel(MetricKind.Gpu);
        ShowNet.Content = Loc.SegLabel(MetricKind.Net);

        PositionHeader.Text = Loc.T("group.position");
        PosBottom.Content = Loc.T("pos.bottom");
        PosTop.Content = Loc.T("pos.top");
        MonitorLabel.Text = Loc.T("field.monitor");

        LanguageLabel.Text = Loc.T("field.language");
        AutostartBox.Content = Loc.T("field.autostart");
        CancelButton.Content = Loc.T("btn.cancel");
        OkButton.Content = Loc.T("btn.ok");
    }

    private void LoadInto()
    {
        HeightSlider.Value = _working.BarHeight;
        HeightValue.Text = $"{(int)Math.Round(_working.BarHeight)} px";

        OpacitySlider.Value = _working.Opacity;
        OpacityValue.Text = $"{(int)Math.Round(_working.Opacity * 100)} %";

        Refresh05.IsChecked = _working.RefreshSeconds == 0.5;
        Refresh1.IsChecked = _working.RefreshSeconds == 1;
        Refresh2.IsChecked = _working.RefreshSeconds == 2;

        BandwidthBox.Text = _working.BandwidthMbps.ToString(CultureInfo.InvariantCulture);

        ShowCpu.IsChecked = _working.ShowCpu;
        ShowMem.IsChecked = _working.ShowMem;
        ShowGpu.IsChecked = _working.ShowGpu;
        ShowNet.IsChecked = _working.ShowNet;

        PosBottom.IsChecked = _working.Position == EdgePosition.Bottom;
        PosTop.IsChecked = _working.Position == EdgePosition.Top;

        foreach (var s in Forms.Screen.AllScreens)
        {
            var b = s.Bounds;
            string text = (s.Primary ? Loc.T("monitor.primary") : Loc.T("monitor.secondary"))
                          + $" ({b.Width}×{b.Height})";
            var item = new ComboBoxItem { Content = text, Tag = s.DeviceName };
            MonitorBox.Items.Add(item);

            bool selected = string.IsNullOrEmpty(_working.MonitorDeviceName)
                ? s.Primary
                : s.DeviceName == _working.MonitorDeviceName;
            if (selected) MonitorBox.SelectedItem = item;
        }
        if (MonitorBox.SelectedItem is null && MonitorBox.Items.Count > 0)
            MonitorBox.SelectedIndex = 0;

        foreach (var (lang, display) in Loc.Choices)
        {
            // System 项随当前语言显示「跟随系统」，其余用各语言自身写法
            string text = lang == AppLanguage.System ? Loc.T("lang.system") : display;
            var item = new ComboBoxItem { Content = text, Tag = lang };
            LanguageBox.Items.Add(item);
            if (lang == _working.Language) LanguageBox.SelectedItem = item;
        }
        if (LanguageBox.SelectedItem is null && LanguageBox.Items.Count > 0)
            LanguageBox.SelectedIndex = 0;

        AutostartBox.IsChecked = Autostart.IsEnabled();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        _working.BarHeight = HeightSlider.Value;
        _working.Opacity = OpacitySlider.Value;
        _working.RefreshSeconds = Refresh05.IsChecked == true ? 0.5
                                : Refresh2.IsChecked == true ? 2 : 1;
        if (double.TryParse(BandwidthBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var bw) && bw > 0)
            _working.BandwidthMbps = bw;
        _working.ShowCpu = ShowCpu.IsChecked == true;
        _working.ShowMem = ShowMem.IsChecked == true;
        _working.ShowGpu = ShowGpu.IsChecked == true;
        _working.ShowNet = ShowNet.IsChecked == true;
        _working.Position = PosTop.IsChecked == true ? EdgePosition.Top : EdgePosition.Bottom;

        var dev = (MonitorBox.SelectedItem as ComboBoxItem)?.Tag as string;
        _working.MonitorDeviceName = dev == Forms.Screen.PrimaryScreen?.DeviceName ? null : dev;

        if ((LanguageBox.SelectedItem as ComboBoxItem)?.Tag is AppLanguage lang)
            _working.Language = lang;

        _working.Normalize();
        Autostart.Set(AutostartBox.IsChecked == true);

        // 先切语言再 ApplySettings：进度条段名会在 BuildBars 里用新语言重建，
        // 托盘菜单经 Loc.Changed 事件同步刷新。
        Loc.SetLanguage(_working.Language);
        _main.ApplySettings(_working);
        _working.Save();
        _committed = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (!_committed)
            _main.ApplySettings(_original); // 取消时还原条高等预览
    }
}
