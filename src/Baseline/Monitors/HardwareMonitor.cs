using System.Net.NetworkInformation;
using Baseline.Config;
using LibreHardwareMonitor.Hardware;

namespace Baseline.Monitors;

/// <summary>一次读取的四项指标，均为 0..1 的占用比。</summary>
public readonly record struct Metrics(double Cpu, double Gpu, double Mem, double Net)
{
    /// <summary>按指标种类取值。</summary>
    public double this[MetricKind kind] => kind switch
    {
        MetricKind.Cpu => Cpu,
        MetricKind.Gpu => Gpu,
        MetricKind.Mem => Mem,
        MetricKind.Net => Net,
        _ => 0,
    };
}

/// <summary>
/// 采集 CPU/GPU/内存（LibreHardwareMonitor 的 load%）与网络下载占用比。
/// 网络满格 = BandwidthMbps 换算的字节/秒。
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private double _netMaxBytesPerSec;
    private readonly List<IHardware> _hardware = new();

    private ISensor? _cpuLoad;
    private ISensor? _gpuLoad;
    private ISensor? _memLoad;

    // 按网卡 Id 记录上次的累计接收字节。口径取「增量最大的那块网卡」而非全部相加，
    // 以免 VPN/TUN、VirtualBox、WSL/Hyper-V 等伴生虚拟网卡把同一份下载流量重复计数。
    private readonly Dictionary<string, long> _lastRecvByIf = new();
    private long _lastTicks;

    public HardwareMonitor(double bandwidthMbps)
    {
        // Mbps -> 字节/秒：×1_000_000 bit ÷ 8
        _netMaxBytesPerSec = bandwidthMbps * 1_000_000 / 8.0;

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
        };
        _computer.Open();

        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            _hardware.Add(hw);
            foreach (var sensor in hw.Sensors)
            {
                if (sensor.SensorType != SensorType.Load) continue;

                switch (hw.HardwareType)
                {
                    case HardwareType.Cpu when sensor.Name.Contains("Total"):
                        _cpuLoad = sensor;
                        break;
                    case HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel
                        when sensor.Name.Contains("Core"):
                        _gpuLoad ??= sensor;
                        break;
                    case HardwareType.Memory when sensor.Name == "Memory":
                        _memLoad = sensor;
                        break;
                }
            }
        }

        SeedRecvSnapshot();
        _lastTicks = DateTime.UtcNow.Ticks;
    }

    /// <summary>GPU 是否成功识别（读不到通常是非管理员权限或无独显）。</summary>
    public bool GpuAvailable => _gpuLoad is not null;

    /// <summary>最近一次 Read() 测得的下行速率（字节/秒），供悬浮显示真实网速用。</summary>
    public double LastNetBytesPerSec { get; private set; }

    /// <summary>更新网络满格基准（Mbps），设置变更时调用，无需重建采集器。</summary>
    public void SetBandwidth(double bandwidthMbps)
        => _netMaxBytesPerSec = Math.Max(bandwidthMbps, 0.001) * 1_000_000 / 8.0;

    public Metrics Read()
    {
        foreach (var hw in _hardware) hw.Update();

        var cpu = Fraction(_cpuLoad);
        var gpu = Fraction(_gpuLoad);
        var mem = Fraction(_memLoad);

        long ticks = DateTime.UtcNow.Ticks;
        double seconds = Math.Max((ticks - _lastTicks) / (double)TimeSpan.TicksPerSecond, 0.001);
        _lastTicks = ticks;

        // 取增量最大的网卡作为真实下行速率。第一次见到的网卡（启动后才 Up 的 VPN 等）
        // 没有历史值，只记录不计增量，避免把历史累计字节误当成这一秒的流量。
        double maxBytesPerSec = 0;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            long bytes;
            try { bytes = ni.GetIPv4Statistics().BytesReceived; }
            catch { continue; /* 个别虚拟网卡不支持统计，忽略 */ }

            if (_lastRecvByIf.TryGetValue(ni.Id, out var prev))
            {
                double bps = (bytes - prev) / seconds;
                if (bps > maxBytesPerSec) maxBytesPerSec = bps;
            }
            _lastRecvByIf[ni.Id] = bytes;
        }

        LastNetBytesPerSec = maxBytesPerSec;
        double net = Math.Clamp(maxBytesPerSec / _netMaxBytesPerSec, 0, 1);

        return new Metrics(cpu, gpu, mem, net);
    }

    private static double Fraction(ISensor? sensor)
    {
        var v = sensor?.Value ?? 0f;
        if (float.IsNaN(v)) return 0;
        return Math.Clamp(v / 100.0, 0, 1);
    }

    /// <summary>构造时给每块活动网卡记下初始累计值，作为首次增量的基线。</summary>
    private void SeedRecvSnapshot()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;
            try { _lastRecvByIf[ni.Id] = ni.GetIPv4Statistics().BytesReceived; }
            catch { /* 个别虚拟网卡不支持统计，忽略 */ }
        }
    }

    public void Dispose() => _computer.Close();
}
