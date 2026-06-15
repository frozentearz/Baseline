using System.Windows;
using Baseline.Config;
using Baseline.Monitors;
using Forms = System.Windows.Forms;

namespace Baseline;

public partial class App : Application
{
    private HardwareMonitor? _monitor;
    private MainWindow? _window;
    private Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _monitor = new HardwareMonitor(Settings.BandwidthMbps);
        _window = new MainWindow(_monitor);
        _window.Show();

        SetupTray();
    }

    private void SetupTray()
    {
        var menu = new Forms.ContextMenuStrip();

        var autostart = new Forms.ToolStripMenuItem("开机自启")
        {
            Checked = Autostart.IsEnabled(),
            CheckOnClick = true,
        };
        autostart.Click += (_, _) => Autostart.Set(autostart.Checked);

        var exit = new Forms.ToolStripMenuItem("退出");
        exit.Click += (_, _) => Shutdown();

        menu.Items.Add(autostart);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exit);

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
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _monitor?.Dispose();
        base.OnExit(e);
    }
}
