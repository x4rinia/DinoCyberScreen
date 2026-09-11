using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DinoCyberScreen.Launcher;

internal static class Program
{
    private const string ProductName = "DinoCyberScreen";

    [STAThread]
    private static int Main(string[] args)
    {
        var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR");
        if (string.IsNullOrWhiteSpace(windowsDirectory))
            windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        var installDirectory = Path.Combine(windowsDirectory, "Dino_SCR");
        var executable = Path.Combine(installDirectory, "DinoCyberScreen.exe");
        if (!File.Exists(executable))
        {
            MessageBoxW(IntPtr.Zero,
                $"Die DinoCyberScreen-Installation wurde nicht gefunden.\n\nErwarteter Pfad:\n{executable}\n\nBitte Install-Screensaver.bat erneut ausführen.",
                ProductName, 0x10);
            return 2;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = installDirectory,
                UseShellExecute = false
            };
            foreach (var argument in args)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process is null) return 3;
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception exception)
        {
            MessageBoxW(IntPtr.Zero,
                $"DinoCyberScreen konnte nicht gestartet werden.\n\n{exception.Message}",
                ProductName, 0x10);
            return 4;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr window, string text, string caption, uint type);
}
