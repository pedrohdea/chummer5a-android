namespace Chummer.UI.Platform;

/// <summary>
/// Platform facts the shared UI cannot obtain on its own — device model, Android API level,
/// and the native/Java heap figures that <see cref="GC"/> does not see.
/// </summary>
public interface IPlatformProbe
{
    /// <summary>Short platform name, e.g. "Android" or "Desktop".</summary>
    string Name { get; }

    /// <summary>Multi-line description of device, OS and runtime.</summary>
    string Describe();

    /// <summary>
    /// Bytes currently allocated outside the managed heap, or <c>null</c> when the platform
    /// cannot report it. On Android this is the Java heap plus native allocations, which is
    /// what the process actually gets killed for.
    /// </summary>
    long? NativeMemoryBytes { get; }

    /// <summary>Maximum heap the process is allowed, or <c>null</c> when unbounded/unknown.</summary>
    long? MemoryLimitBytes { get; }
}
