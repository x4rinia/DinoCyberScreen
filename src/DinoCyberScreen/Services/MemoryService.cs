using System.Runtime.InteropServices;
using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public static class MemoryService
{
    public static RamTelemetry Read()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref status)) return new RamTelemetry();
        var total = status.TotalPhysical / 1024d / 1024d / 1024d;
        var available = status.AvailablePhysical / 1024d / 1024d / 1024d;
        var used = Math.Max(0, total - available);
        return new RamTelemetry
        {
            Used = Math.Round(used, 2),
            Total = Math.Round(total, 2),
            Usage = total <= 0 ? 0 : Math.Round(used / total * 100, 1)
        };
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
