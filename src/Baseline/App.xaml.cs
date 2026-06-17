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

    // 托盘菜单项引用，供语言切换时重新本地化
    private Forms.ToolStripMenuItem? _miSettings, _miAutostart, _miRestart, _miExit;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = AppSettings.Load();
        Loc.SetLanguage(_settings.Language);
        Loc.Changed += LocalizeTray;

        _monitor = new HardwareMonitor(_settings.BandwidthMbps);
        _window = new MainWindow(_monitor, _settings);
        _window.Show();

        SetupTray();

        // 直接打开设置窗口（用于快捷方式/截图），托盘照常驻留
        if (Array.Exists(e.Args, a => a == "--open-settings")) OpenSettings();
    }

    private void SetupTray()
    {
        var menu = new Forms.ContextMenuStrip();

        _miSettings = new Forms.ToolStripMenuItem();
        _miSettings.Click += (_, _) => OpenSettings();

        _miAutostart = new Forms.ToolStripMenuItem
        {
            Checked = Autostart.IsEnabled(),
            CheckOnClick = true,
        };
        _miAutostart.Click += (_, _) => Autostart.Set(_miAutostart.Checked);

        _miRestart = new Forms.ToolStripMenuItem();
        _miRestart.Click += (_, _) => Restart();

        _miExit = new Forms.ToolStripMenuItem();
        _miExit.Click += (_, _) => Shutdown();

        menu.Items.Add(_miSettings);
        menu.Items.Add(_miAutostart);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_miRestart);
        menu.Items.Add(_miExit);
        // 打开菜单时同步「开机自启」勾选（设置窗口里可能改过）
        menu.Opening += (_, _) => _miAutostart.Checked = Autostart.IsEnabled();

        var exePath = Environment.ProcessPath;
        var trayIcon = !string.IsNullOrEmpty(exePath)
            ? System.Drawing.Icon.ExtractAssociatedIcon(exePath)
            : null;

        _tray = new Forms.NotifyIcon
        {
            Icon = trayIcon ?? System.Drawing.SystemIcons.Information,
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => OpenSettings();

        LocalizeTray();
    }

    /// <summary>按当前语言刷新托盘文案，启动与切换语言时调用。</summary>
    private void LocalizeTray()
    {
        if (_tray is null) return;
        _tray.Text = Loc.T("tray.tooltip");
        if (_miSettings is not null) _miSettings.Text = Loc.T("tray.settings");
        if (_miAutostart is not null) _miAutostart.Text = Loc.T("field.autostart");
        if (_miRestart is not null) _miRestart.Text = Loc.T("tray.restart");
        if (_miExit is not null) _miExit.Text = Loc.T("tray.exit");
    }

    private void Restart()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exe))
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = true });
        Shutdown();
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
