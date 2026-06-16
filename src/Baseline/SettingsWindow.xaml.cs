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

        string? primary = Forms.Screen.PrimaryScreen?.DeviceName;
        foreach (var s in Forms.Screen.AllScreens)
        {
            var b = s.Bounds;
            string text = (s.Primary ? "主显示器 " : "显示器 ") + $"({b.Width}×{b.Height})";
            var item = new ComboBoxItem { Content = text, Tag = s.DeviceName };
            MonitorBox.Items.Add(item);

            bool selected = string.IsNullOrEmpty(_working.MonitorDeviceName)
                ? s.Primary
                : s.DeviceName == _working.MonitorDeviceName;
            if (selected) MonitorBox.SelectedItem = item;
        }
        if (MonitorBox.SelectedItem is null && MonitorBox.Items.Count > 0)
            MonitorBox.SelectedIndex = 0;

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

        _working.Normalize();
        Autostart.Set(AutostartBox.IsChecked == true);

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
