using System.Diagnostics;
using System.IO;
using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public sealed class DiskService : IDisposable
{
    private PerformanceCounter? _readCounter;
    private PerformanceCounter? _writeCounter;

    public DiskService()
    {
        try
        {
            _readCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _writeCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            _ = _readCounter.NextValue();
            _ = _writeCounter.NextValue();
        }
        catch
        {
            _readCounter?.Dispose();
            _writeCounter?.Dispose();
            _readCounter = _writeCounter = null;
        }
    }

    public DiskTelemetry Read()
    {
        DriveInfo? drive = null;
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory);
            drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.RootDirectory.FullName.Equals(root, StringComparison.OrdinalIgnoreCase))
                    ?? DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);
        }
        catch { }

        var total = drive?.TotalSize / 1024d / 1024d / 1024d ?? 0;
        var free = drive?.AvailableFreeSpace / 1024d / 1024d / 1024d ?? 0;
        return new DiskTelemetry
        {
            Name = drive is null ? "SYSTEM DISK" : $"{drive.Name.TrimEnd('\\')} {drive.DriveFormat}",
            Read = ReadCounter(_readCounter),
            Write = ReadCounter(_writeCounter),
            Free = Math.Round(free, 1),
            Total = Math.Round(total, 1),
            Usage = total <= 0 ? 0 : Math.Round((total - free) / total * 100, 1)
        };
    }

    private static double? ReadCounter(PerformanceCounter? counter)
    {
        try { return counter is null ? null : Math.Round(counter.NextValue() / 1024d / 1024d, 2); }
        catch { return null; }
    }

    public void Dispose()
    {
        _readCounter?.Dispose();
        _writeCounter?.Dispose();
    }
}
