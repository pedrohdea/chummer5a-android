using Android.App;
using Android.Content;
using Android.OS;
using Chummer.UI.Platform;

namespace Chummer.Android;

/// <summary>
/// Reports the numbers that decide whether the app survives on a phone.
/// </summary>
/// <remarks>
/// <see cref="GC"/> only sees the managed heap. What gets an Android process killed is the
/// total footprint — Java heap plus native allocations, measured against a per-app limit that
/// the device, not the app, chooses. Those are the figures collected here.
/// </remarks>
public sealed class AndroidPlatformProbe : IPlatformProbe
{
    private readonly Context _context;

    public AndroidPlatformProbe(Context context) => _context = context;

    public string Name => $"Android {Build.VERSION.Release} (API {(int)Build.VERSION.SdkInt})";

    public long? NativeMemoryBytes => Debug.NativeHeapAllocatedSize;

    public long? MemoryLimitBytes
    {
        get
        {
            if (_context.GetSystemService(Context.ActivityService) is ActivityManager manager)
                return manager.MemoryClass * 1024L * 1024L;
            return null;
        }
    }

    public string Describe()
    {
        Java.Lang.Runtime? runtime = Java.Lang.Runtime.GetRuntime();
        ActivityManager? manager = _context.GetSystemService(Context.ActivityService) as ActivityManager;

        return string.Join('\n',
            $"aparelho = {Build.Manufacturer} {Build.Model}",
            $"Android = {Build.VERSION.Release} (API {(int)Build.VERSION.SdkInt})",
            $"ABIs = {string.Join(", ", Build.SupportedAbis ?? new List<string>())}",
            $"framework = {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            $"RID = {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}",
            $"heap Java em uso = {(runtime is null ? "n/d" : (runtime.TotalMemory() - runtime.FreeMemory()).ToString())} B",
            $"heap Java máximo = {(runtime is null ? "n/d" : runtime.MaxMemory().ToString())} B",
            $"classe de memória = {(manager is null ? "n/d" : manager.MemoryClass + " MiB")}",
            $"classe de memória grande = {(manager is null ? "n/d" : manager.LargeMemoryClass + " MiB")}");
    }
}
