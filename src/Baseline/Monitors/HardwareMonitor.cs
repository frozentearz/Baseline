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
    private readonly double _netMaxBytesPerSec;
    private readonly List<IHardware> _hardware = new();

    private ISensor? _cpuLoad;
    private ISensor? _gpuLoad;
    private ISensor? _memLoad;

    private long _lastRecvBytes;
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

        _lastRecvBytes = TotalBytesReceived();
        _lastTicks = DateTime.UtcNow.Ticks;
    }

    /// <summary>GPU 是否成功识别（读不到通常是非管理员权限或无独显）。</summary>
    public bool GpuAvailable => _gpuLoad is not null;

    public Metrics Read()
    {
        foreach (var hw in _hardware) hw.Update();

        var cpu = Fraction(_cpuLoad);
        var gpu = Fraction(_gpuLoad);
        var mem = Fraction(_memLoad);

        long recv = TotalBytesReceived();
        long ticks = DateTime.UtcNow.Ticks;
        double seconds = Math.Max((ticks - _lastTicks) / (double)TimeSpan.TicksPerSecond, 0.001);
        double bytesPerSec = (recv - _lastRecvBytes) / seconds;
        _lastRecvBytes = recv;
        _lastTicks = ticks;

        double net = Math.Clamp(bytesPerSec / _netMaxBytesPerSec, 0, 1);

        return new Metrics(cpu, gpu, mem, net);
    }

    private static double Fraction(ISensor? sensor)
    {
        var v = sensor?.Value ?? 0f;
        if (float.IsNaN(v)) return 0;
        return Math.Clamp(v / 100.0, 0, 1);
    }

    private static long TotalBytesReceived()
    {
        long sum = 0;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;
            try { sum += ni.GetIPv4Statistics().BytesReceived; }
            catch { /* 个别虚拟网卡不支持统计，忽略 */ }
        }
        return sum;
    }

    public void Dispose() => _computer.Close();
}
