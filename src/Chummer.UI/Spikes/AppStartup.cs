using System.Diagnostics;

namespace Chummer.UI.Spikes;

/// <summary>
/// Measures cold start: from the head's entry point to the first frame the user can see.
/// </summary>
/// <remarks>
/// Not a curiosity — Android kills an app that takes too long to draw, and the whole point of
/// packaging ~20 MB of game data is that it must not be touched before the first frame. This
/// number is what proves the data loading stayed off the start-up path.
/// </remarks>
public static class AppStartup
{
    private static readonly Stopwatch Watch = new();

    /// <summary>Called by each head as early as it can run managed code.</summary>
    public static void MarkProcessStart() => Watch.Restart();

    /// <summary>Called once, when the first view is attached to the visual tree.</summary>
    public static void MarkFirstFrame()
    {
        if (Watch.IsRunning)
        {
            Watch.Stop();
            TimeToFirstFrame = Watch.Elapsed;
        }
    }

    /// <summary>Elapsed time until the first frame, or <c>null</c> before it is drawn.</summary>
    public static TimeSpan? TimeToFirstFrame { get; private set; }
}
