using Chummer.UI.Platform;

namespace Chummer.UI.Spikes;

/// <summary>
/// The Etapa 2.5 risk measurements, in the order they answer the open questions of Etapa 1.4.
/// </summary>
public static class SpikeSuite
{
    /// <summary>Sheet used for the XSLT spike — the default character sheet of the app.</summary>
    public const string ReferenceSheet = "Shadowrun 5.xsl";

    public static IReadOnlyList<SpikeResult> RunAll()
    {
        List<SpikeResult> results =
        [
            Environment(),
            XsltSpike.Run("sheets", ReferenceSheet),
            DataLoadSpike.Run("data"),
        ];
        return results;
    }

    private static SpikeResult Environment()
    {
        IPlatformProbe probe = AppServices.Platform;
        List<(string, string)> details =
        [
            ("plataforma", probe.Name),
            ("fonte de assets", AppServices.Assets.Description),
            ("até o primeiro quadro", AppStartup.TimeToFirstFrame is { } t
                ? Measure.Millis(t.TotalMilliseconds)
                : "ainda não desenhado"),
            ("processadores", Measure.Count(System.Environment.ProcessorCount)),
            ("heap gerenciado", Measure.Bytes(GC.GetTotalMemory(forceFullCollection: false))),
            ("memória nativa", Measure.Bytes(probe.NativeMemoryBytes)),
            ("limite de heap", Measure.Bytes(probe.MemoryLimitBytes)),
        ];

        foreach (string line in probe.Describe().Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = line.IndexOf('=');
            if (separator > 0)
                details.Add((line[..separator].Trim(), line[(separator + 1)..].Trim()));
        }

        return new SpikeResult
        {
            Name = "Ambiente",
            Ok = true,
            Headline = probe.Name,
            Details = details,
        };
    }
}
