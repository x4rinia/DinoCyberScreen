using System.Text.Json.Serialization;

namespace DinoCyberScreen.Models;

public sealed class SystemTelemetry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string ComputerName { get; init; } = Environment.MachineName;
    public string WindowsVersion { get; init; } = Environment.OSVersion.VersionString;
    public string Uptime { get; init; } = "00:00:00";
    public CpuTelemetry Cpu { get; init; } = new();
    public GpuTelemetry Gpu { get; init; } = new();
    public RamTelemetry Ram { get; init; } = new();
    public NetworkTelemetry Network { get; init; } = new();
    public DiskTelemetry Disk { get; init; } = new();
    public IReadOnlyList<ProcessTelemetry> Processes { get; init; } = [];
}

public sealed class CpuTelemetry
{
    public string Name { get; init; } = "CPU";
    public double? Usage { get; init; }
    public double? Temperature { get; init; }
    public IReadOnlyList<double> Cores { get; init; } = [];
}

public sealed class GpuTelemetry
{
    public string Name { get; init; } = "GPU NOT DETECTED";
    public double? Usage { get; init; }
    public double? Temperature { get; init; }
    public double? VramUsed { get; init; }
    public double? VramTotal { get; init; }
}

public sealed class RamTelemetry
{
    public double Used { get; init; }
    public double Total { get; init; }
    public double Usage { get; init; }
}

public sealed class NetworkTelemetry
{
    public string Adapter { get; init; } = "NO ACTIVE ADAPTER";
    public double Download { get; init; }
    public double Upload { get; init; }
}

public sealed class DiskTelemetry
{
    public string Name { get; init; } = "SYSTEM DISK";
    public double? Read { get; init; }
    public double? Write { get; init; }
    public double Free { get; init; }
    public double Total { get; init; }
    public double Usage { get; init; }
}

public sealed class ProcessTelemetry
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double Cpu { get; init; }
    public double Memory { get; init; }
    public string Status { get; init; } = "RUNNING";
}
