using System.Windows;
using Baseline.Config;
using Baseline.Monitors;
using Forms = System.Windows.Forms;

namespace Baseline;

public partial class App : Application
{
    private AppSettings? _settings;
    private HardwareMonitor? _monitor;
    private MainWindow? _window;
    private Forms.NotifyIcon? _tray;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = AppSettings.Load();
        _monitor = new HardwareMonitor(_settings.BandwidthMbps);
        _window = new MainWindow(_monitor, _settings);
        _window.Show();

        SetupTray();
    }

    private void SetupTray()
    {
        var menu = new Forms.ContextMenuStrip();

        var settings = new Forms.ToolStripMenuItem("设置…");
        settings.Click += (_, _) => OpenSettings();

        var autostart = new Forms.ToolStripMenuItem("开机自启")
        {
            Checked = Autostart.IsEnabled(),
            CheckOnClick = true,
        };
        autostart.Click += (_, _) => Autostart.Set(autostart.Checked);

        var exit = new Forms.ToolStripMenuItem("退出");
        exit.Click += (_, _) => Shutdown();

        menu.Items.Add(settings);
        menu.Items.Add(autostart);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exit);
        // 打开菜单时同步「开机自启」勾选（设置窗口里可能改过）
        menu.Opening += (_, _) => autostart.Checked = Autostart.IsEnabled();

        var exePath = Environment.ProcessPath;
        var trayIcon = !string.IsNullOrEmpty(exePath)
            ? System.Drawing.Icon.ExtractAssociatedIcon(exePath)
            : null;

        _tray = new Forms.NotifyIcon
        {
            Icon = trayIcon ?? System.Drawing.SystemIcons.Information,
            Text = "Baseline 资源进度条",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => OpenSettings();
    }

    private void OpenSettings()
    {
        if (_window is null) return;

        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_window);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _monitor?.Dispose();
        base.OnExit(e);
    }
}
