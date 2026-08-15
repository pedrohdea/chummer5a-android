using System.Diagnostics;
using System.Runtime.InteropServices;
using Chummer.UI.Platform;

namespace Chummer.Desktop;

/// <summary>Desktop counterpart of the Android probe, so the two runs are comparable.</summary>
public sealed class DesktopPlatformProbe : IPlatformProbe
{
    public string Name => $"Desktop ({RuntimeInformation.OSDescription.Trim()})";

    public long? NativeMemoryBytes
    {
        get
        {
            try
            {
                using Process process = Process.GetCurrentProcess();
                process.Refresh();
                return process.WorkingSet64;
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }
    }

    public long? MemoryLimitBytes => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

    public string Describe() =>
        $"SO = {RuntimeInformation.OSDescription.Trim()}\n" +
        $"arquitetura = {RuntimeInformation.OSArchitecture} / processo {RuntimeInformation.ProcessArchitecture}\n" +
        $"framework = {RuntimeInformation.FrameworkDescription}\n" +
        $"servidor GC = {System.Runtime.GCSettings.IsServerGC}";
}
