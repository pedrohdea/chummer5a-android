using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Chummer.UI.Platform;

namespace Chummer.UI.Spikes;

/// <summary>
/// The single most consequential measurement of Etapa 2.5: does
/// <see cref="XslCompiledTransform"/> work on Android?
/// </summary>
/// <remarks>
/// It matters because <c>XslCompiledTransform</c> compiles the stylesheet to IL through
/// <c>System.Reflection.Emit.DynamicMethod</c>. Any runtime configuration without dynamic
/// code — full AOT, an interpreter-only build — cannot do that. If it fails, Etapa 7
/// (character sheets via XSLT → WebView) has to be redesigned around a different engine, so
/// the answer is worth measuring before anything is built on top of it.
///
/// Three levels, from cheapest to most representative:
///   1. is dynamic code even available on this runtime;
///   2. a minimal stylesheet compiled and run from a string — proves the engine works;
///   3. the real "Shadowrun 5.xsl", with its three levels of xsl:import resolved out of the
///      packaged assets — proves the actual Etapa 7 path works.
/// </remarks>
public static class XsltSpike
{
    private const string MinimalStylesheet = """
        <?xml version="1.0" encoding="utf-8"?>
        <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
          <xsl:output method="html" indent="no"/>
          <xsl:template match="/character">
            <html><body><h1><xsl:value-of select="alias"/></h1></body></html>
          </xsl:template>
        </xsl:stylesheet>
        """;

    private const string MinimalInput =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><character><alias>Nêmesis</alias></character>";

    public static SpikeResult Run(string sheetsDirectory, string sheetFileName)
    {
        List<(string, string)> details =
        [
            ("IsDynamicCodeSupported", RuntimeFeature.IsDynamicCodeSupported.ToString()),
            ("IsDynamicCodeCompiled", RuntimeFeature.IsDynamicCodeCompiled.ToString()),
            ("runtime", RuntimeInformationText()),
        ];

        bool minimalOk = RunMinimal(details);
        bool realOk = RunRealSheet(sheetsDirectory, sheetFileName, details);

        string headline = (minimalOk, realOk) switch
        {
            (true, true) => "XSLT funciona, inclusive a folha real com imports",
            (true, false) => "o motor funciona, mas a folha real falhou",
            _ => "XslCompiledTransform NÃO funciona nesta plataforma",
        };

        return new SpikeResult
        {
            Name = "XslCompiledTransform",
            Ok = minimalOk && realOk,
            Headline = headline,
            Details = details,
        };
    }

    private static bool RunMinimal(List<(string, string)> details)
    {
        try
        {
            Stopwatch watch = Stopwatch.StartNew();
            XslCompiledTransform transform = new();
            using (StringReader sheetText = new(MinimalStylesheet))
            using (XmlReader sheetReader = XmlReader.Create(sheetText))
            {
                transform.Load(sheetReader, XsltSettings.Default, null);
            }
            long compileMs = watch.ElapsedMilliseconds;

            StringBuilder output = new();
            using (StringReader inputText = new(MinimalInput))
            using (XmlReader inputReader = XmlReader.Create(inputText))
            using (XmlWriter writer = XmlWriter.Create(output, transform.OutputSettings))
            {
                transform.Transform(inputReader, writer);
            }
            watch.Stop();

            bool produced = output.ToString().Contains("Nêmesis", StringComparison.Ordinal);
            details.Add(("folha mínima — compilação", Measure.Millis(compileMs)));
            details.Add(("folha mínima — total", Measure.Millis(watch.Elapsed.TotalMilliseconds)));
            details.Add(("folha mínima — saída correta", produced ? "sim" : "NÃO"));
            return produced;
        }
        catch (Exception ex)
        {
            details.Add(("folha mínima — exceção", $"{ex.GetType().FullName}: {ex.Message}"));
            return false;
        }
    }

    private static bool RunRealSheet(string sheetsDirectory, string sheetFileName, List<(string, string)> details)
    {
        try
        {
            IAssetSource assets = AppServices.Assets;
            string sheetPath = $"{sheetsDirectory.Trim('/')}/{sheetFileName}";
            if (!assets.Exists(sheetPath))
            {
                details.Add(("folha real", $"não encontrada: {sheetPath}"));
                return false;
            }

            AssetXmlResolver resolver = new(assets, sheetsDirectory);

            Stopwatch watch = Stopwatch.StartNew();
            XslCompiledTransform transform = new();
            using (Stream sheetStream = assets.Open(sheetPath))
            using (XmlReader sheetReader = XmlReader.Create(
                       sheetStream,
                       new XmlReaderSettings { XmlResolver = resolver, DtdProcessing = DtdProcessing.Ignore },
                       resolver.UriFor(sheetFileName).ToString()))
            {
                transform.Load(sheetReader, XsltSettings.Default, resolver);
            }
            watch.Stop();

            details.Add(("folha real", sheetFileName));
            details.Add(("folha real — compilação (com imports)", Measure.Millis(watch.Elapsed.TotalMilliseconds)));

            // A synthetic, nearly empty character: the point is to prove the compiled sheet
            // executes end to end on this runtime, not to produce a meaningful sheet.
            const string skeleton =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><characters><character>" +
                "<name>Spike</name><alias>Spike</alias></character></characters>";

            StringBuilder output = new();
            Stopwatch runWatch = Stopwatch.StartNew();
            using (StringReader inputText = new(skeleton))
            using (XmlReader inputReader = XmlReader.Create(inputText))
            using (XmlWriter writer = XmlWriter.Create(output, transform.OutputSettings))
            {
                transform.Transform(inputReader, writer);
            }
            runWatch.Stop();

            details.Add(("folha real — transformação", Measure.Millis(runWatch.Elapsed.TotalMilliseconds)));
            details.Add(("folha real — bytes de saída", Measure.Bytes(output.Length)));
            return true;
        }
        catch (Exception ex)
        {
            details.Add(("folha real — exceção", $"{ex.GetType().FullName}: {ex.Message}"));
            return false;
        }
    }

    private static string RuntimeInformationText() =>
        $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} / " +
        $"{System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}";
}
