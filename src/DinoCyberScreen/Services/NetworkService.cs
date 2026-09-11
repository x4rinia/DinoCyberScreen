using System.Net.NetworkInformation;
using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public sealed class NetworkService
{
    private long _lastReceived;
    private long _lastSent;
    private DateTime _lastRead = DateTime.UtcNow;
    private string _lastAdapter = "NO ACTIVE ADAPTER";

    public NetworkTelemetry Read()
    {
        try
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
                .ToArray();
            if (adapters.Length == 0) return new NetworkTelemetry();

            var received = adapters.Sum(a => a.GetIPv4Statistics().BytesReceived);
            var sent = adapters.Sum(a => a.GetIPv4Statistics().BytesSent);
            var elapsed = Math.Max(0.1, (DateTime.UtcNow - _lastRead).TotalSeconds);
            var download = _lastReceived == 0 ? 0 : Math.Max(0, (received - _lastReceived) * 8 / elapsed / 1_000_000d);
            var upload = _lastSent == 0 ? 0 : Math.Max(0, (sent - _lastSent) * 8 / elapsed / 1_000_000d);

            _lastReceived = received;
            _lastSent = sent;
            _lastRead = DateTime.UtcNow;
            _lastAdapter = string.Join(" + ", adapters.Select(a => a.Name).Take(2));

            return new NetworkTelemetry
            {
                Adapter = _lastAdapter,
                Download = Math.Round(download, 2),
                Upload = Math.Round(upload, 2)
            };
        }
        catch
        {
            return new NetworkTelemetry { Adapter = _lastAdapter };
        }
    }
}
