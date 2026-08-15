namespace Chummer.UI.Platform;

/// <summary>
/// Static facade holding the platform implementations each head installs at startup.
/// </summary>
/// <remarks>
/// Same pattern as <c>UserInteraction</c> in the core (DEC-026) and
/// <c>Timekeeper.ActivityFactory</c> (DEC-025): a static facade with an installable
/// implementation, and a null object as the initial value so that nothing throws when a
/// head forgets to install — the UI degrades to "no assets" instead of crashing on start.
/// </remarks>
public static class AppServices
{
    public static IAssetSource Assets { get; private set; } = new EmptyAssetSource();

    public static IPlatformProbe Platform { get; private set; } = new UnknownPlatformProbe();

    public static void Install(IAssetSource assets, IPlatformProbe platform)
    {
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        Platform = platform ?? throw new ArgumentNullException(nameof(platform));
    }

    private sealed class EmptyAssetSource : IAssetSource
    {
        public string Description => "nenhuma fonte de assets instalada";

        public IReadOnlyList<string> List(string directory) => Array.Empty<string>();

        public Stream Open(string path) => throw new FileNotFoundException(
            "Nenhuma fonte de assets foi instalada em AppServices.", path);

        public bool Exists(string path) => false;
    }

    private sealed class UnknownPlatformProbe : IPlatformProbe
    {
        public string Name => "Desconhecida";

        public string Describe() => "Nenhuma sonda de plataforma instalada.";

        public long? NativeMemoryBytes => null;

        public long? MemoryLimitBytes => null;
    }
}
