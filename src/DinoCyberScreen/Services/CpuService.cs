using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public sealed class CpuService
{
    private readonly string _cpuName;
    private ulong _previousIdle;
    private ulong _previousKernel;
    private ulong _previousUser;

    public CpuService()
    {
        _cpuName = ReadCpuName();
        _ = ReadSystemTimes(out _previousIdle, out _previousKernel, out _previousUser);
    }

    public CpuTelemetry Read(IReadOnlyList<IHardware> hardware)
    {
        var cpuHardware = hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        var sensors = cpuHardware?.Sensors ?? [];
        var totalFromSensor = sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Load && s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))?.Value;
        var cores = sensors
            .Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            .Select(s => Math.Round((double)(s.Value ?? 0), 1))
            .ToArray();
        var temperature = sensors
            .Where(s => s.SensorType == SensorType.Temperature)
            .OrderByDescending(s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Value)
            .FirstOrDefault(v => v.HasValue);

        return new CpuTelemetry
        {
            Name = cpuHardware?.Name ?? _cpuName,
            Usage = Math.Round(totalFromSensor ?? ReadTotalUsage(), 1),
            Temperature = temperature.HasValue ? Math.Round(temperature.Value, 1) : null,
            Cores = cores
        };
    }

    private double ReadTotalUsage()
    {
        if (!ReadSystemTimes(out var idle, out var kernel, out var user)) return 0;
        var idleDelta = idle - _previousIdle;
        var kernelDelta = kernel - _previousKernel;
        var userDelta = user - _previousUser;
        _previousIdle = idle;
        _previousKernel = kernel;
        _previousUser = user;
        var total = kernelDelta + userDelta;
        return total == 0 ? 0 : Math.Clamp(100d * (total - idleDelta) / total, 0, 100);
    }

    private static string ReadCpuName()
    {
        try
        {
            return Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0")
                ?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "CPU";
        }
        catch { return "CPU"; }
    }

    private static bool ReadSystemTimes(out ulong idle, out ulong kernel, out ulong user)
    {
        if (GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            idle = idleTime.Value; kernel = kernelTime.Value; user = userTime.Value;
            return true;
        }
        idle = kernel = user = 0;
        return false;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FileTime
    {
        private readonly uint Low;
        private readonly uint High;
        public ulong Value => ((ulong)High << 32) | Low;
    }
}
