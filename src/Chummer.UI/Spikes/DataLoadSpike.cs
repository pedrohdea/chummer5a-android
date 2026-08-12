using System.Diagnostics;
using System.Xml;
using Chummer.UI.Platform;

namespace Chummer.UI.Spikes;

/// <summary>
/// Answers the open question from Etapa 1.4: what does it cost, on the device, to load the
/// ~20 MB of game data the way the legacy <c>XmlManager</c> does — one cached
/// <see cref="XmlDocument"/> per file, all kept alive for the life of the process.
/// </summary>
/// <remarks>
/// The retained figure is the one that matters, not the allocation total: XmlManager caches
/// the documents, so whatever they weigh is weight the app carries forever. That is what
/// PREM-003 is 🟡 about.
/// </remarks>
public static class DataLoadSpike
{
    public static SpikeResult Run(string directory)
    {
        IAssetSource assets = AppServices.Assets;
        IReadOnlyList<string> files = assets.List(directory)
            .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (files.Count == 0)
        {
            return new SpikeResult
            {
                Name = $"Carga de {directory}/",
                Ok = false,
                Headline = $"nenhum .xml encontrado em {directory}/",
                Details = new[] { ("fonte de assets", assets.Description) },
            };
        }

        // Baseline taken with a blocking collection so the delta measures what the documents
        // actually retain, not whatever garbage happened to be floating around.
        long managedBefore = GC.GetTotalMemory(forceFullCollection: true);
        long nativeBefore = AppServices.Platform.NativeMemoryBytes ?? 0;
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);

        List<XmlDocument> retained = new(files.Count);
        long bytesRead = 0;
        long nodeCount = 0;
        int failures = 0;
        string? firstError = null;

        Stopwatch watch = Stopwatch.StartNew();
        foreach (string file in files)
        {
            try
            {
                using Stream stream = assets.Open(file);
                using MemoryStream buffer = new();
                stream.CopyTo(buffer);
                bytesRead += buffer.Length;
                buffer.Position = 0;

                XmlDocument document = new() { XmlResolver = null };
                using (XmlReader reader = XmlReader.Create(
                           buffer,
                           new XmlReaderSettings { XmlResolver = null, DtdProcessing = DtdProcessing.Ignore }))
                {
                    document.Load(reader);
                }

                nodeCount += CountNodes(document.DocumentElement);
                retained.Add(document);
            }
            catch (Exception ex)
            {
                ++failures;
                firstError ??= $"{file}: {ex.GetType().Name}: {ex.Message}";
            }
        }
        watch.Stop();

        long allocatedAfter = GC.GetTotalAllocatedBytes(precise: false);
        long managedAfter = GC.GetTotalMemory(forceFullCollection: true);
        long nativeAfter = AppServices.Platform.NativeMemoryBytes ?? 0;

        // Keeps the documents alive across the measurement above; without this the JIT is
        // free to consider `retained` dead and collect the very thing being measured.
        GC.KeepAlive(retained);

        List<(string, string)> details =
        [
            ("arquivos", Measure.Count(files.Count)),
            ("bytes lidos", Measure.Bytes(bytesRead)),
            ("nós XML", Measure.Count(nodeCount)),
            ("tempo total", Measure.Millis(watch.Elapsed.TotalMilliseconds)),
            ("tempo por MiB", Measure.Millis(
                bytesRead > 0 ? watch.Elapsed.TotalMilliseconds / (bytesRead / 1048576d) : 0)),
            ("heap gerenciado retido", Measure.Bytes(managedAfter - managedBefore)),
            ("heap gerenciado total", Measure.Bytes(managedAfter)),
            ("alocado durante a carga", Measure.Bytes(allocatedAfter - allocatedBefore)),
        ];

        if (AppServices.Platform.NativeMemoryBytes.HasValue)
            details.Add(("memória nativa (delta)", Measure.Bytes(nativeAfter - nativeBefore)));
        if (AppServices.Platform.MemoryLimitBytes.HasValue)
            details.Add(("limite de heap do processo", Measure.Bytes(AppServices.Platform.MemoryLimitBytes)));
        if (failures > 0)
            details.Add(("falhas", $"{failures} — {firstError}"));

        double amplification = bytesRead > 0 ? (managedAfter - managedBefore) / (double)bytesRead : 0;
        details.Add(("amplificação XML→memória", amplification.ToString("0.0×")));

        return new SpikeResult
        {
            Name = $"Carga de {directory}/",
            Ok = failures == 0,
            Headline = failures == 0
                ? $"{files.Count} arquivos, {Measure.Bytes(bytesRead)} em {Measure.Millis(watch.Elapsed.TotalMilliseconds)}"
                : $"{failures} de {files.Count} arquivos falharam",
            Details = details,
        };
    }

    private static long CountNodes(XmlNode? node)
    {
        if (node is null)
            return 0;
        long total = 1;
        for (XmlNode? child = node.FirstChild; child is not null; child = child.NextSibling)
            total += CountNodes(child);
        return total;
    }
}
