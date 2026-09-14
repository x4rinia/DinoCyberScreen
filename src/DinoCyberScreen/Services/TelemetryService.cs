using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public sealed class TelemetryService : IDisposable
{
    private readonly HardwareMonitorService? _hardware;
    private readonly CpuService _cpu = new();
    private readonly GpuService _gpu = new();
    private readonly NetworkService _network = new();
    private readonly DiskService _disk = new();
    private readonly ProcessService _processes = new();

    public TelemetryService(bool initializeHardware = true)
    {
        if (initializeHardware)
            _hardware = new HardwareMonitorService();
    }

    public SystemTelemetry Read(AppSettings settings)
    {
        var hardware = settings.ShowRealData ? _hardware?.Snapshot() ?? [] : [];
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        if (!settings.ShowRealData)
            return SimulatedTelemetry(uptime);

        return new SystemTelemetry
        {
            Timestamp = DateTime.Now,
            ComputerName = Environment.MachineName,
            WindowsVersion = Environment.OSVersion.VersionString,
            Uptime = $"{(int)uptime.TotalHours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}",
            Cpu = _cpu.Read(hardware),
            Gpu = settings.ShowGpuData ? _gpu.Read(hardware) : new GpuTelemetry(),
            Ram = MemoryService.Read(),
            Network = settings.ShowNetworkData ? _network.Read() : new NetworkTelemetry(),
            Disk = _disk.Read(),
            Processes = _processes.Read()
        };
    }

    private static SystemTelemetry SimulatedTelemetry(TimeSpan uptime)
    {
        var t = DateTime.UtcNow.TimeOfDay.TotalSeconds;
        double Wave(double middle, double spread, double rate) => Math.Clamp(middle + Math.Sin(t * rate) * spread + Random.Shared.NextDouble() * 3, 0, 100);
        var cpu = Wave(38, 17, .17);
        var gpu = Wave(54, 22, .11);
        return new SystemTelemetry
        {
            Timestamp = DateTime.Now,
            ComputerName = "SIMULATION-NODE",
            WindowsVersion = "WINDOWS // SIMULATION",
            Uptime = $"{(int)uptime.TotalHours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}",
            Cpu = new CpuTelemetry { Name = "DINO VIRTUAL CPU", Usage = cpu, Temperature = 46 + cpu * .22, Cores = Enumerable.Range(0, 8).Select(i => Wave(cpu, 12, .13 + i * .01)).ToArray() },
            Gpu = new GpuTelemetry { Name = "DINO GRAPHICS ARRAY", Usage = gpu, Temperature = 45 + gpu * .24, VramUsed = 7.4, VramTotal = 16 },
            Ram = new RamTelemetry { Used = 13.8, Total = 32, Usage = 43.1 },
            Network = new NetworkTelemetry { Adapter = "SIMULATED UPLINK", Download = Wave(120, 90, .21), Upload = Wave(32, 24, .15) },
            Disk = new DiskTelemetry { Name = "NVME CORE", Read = Wave(180, 130, .3), Write = Wave(75, 50, .27), Free = 681, Total = 1024, Usage = 33.5 },
            Processes = [
                new() { Id = 4720, Name = "DinoCore", Cpu = 12.4, Memory = 1228 },
                new() { Id = 3184, Name = "System", Cpu = 3.1, Memory = 330 },
                new() { Id = 9048, Name = "WebView2", Cpu = 6.7, Memory = 512 },
                new() { Id = 7612, Name = "telemetry", Cpu = 0.9, Memory = 76 }
            ]
        };
    }

    public void Dispose()
    {
        _disk.Dispose();
        _hardware?.Dispose();
    }
}
