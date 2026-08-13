namespace Chummer.UI.Platform;

/// <summary>
/// Read-only access to the packaged game data (data/, lang/, sheets/, customdata/).
/// </summary>
/// <remarks>
/// Every head packages those folders differently: Android puts them in the APK and reads
/// them through <c>AssetManager</c>, the desktop head reads them straight from disk. The
/// shared UI only ever sees this interface, so nothing above it needs to know which.
/// Paths are relative to the asset root and always use forward slashes ("data/skills.xml").
/// </remarks>
public interface IAssetSource
{
    /// <summary>Human-readable description of where the assets come from.</summary>
    string Description { get; }

    /// <summary>Lists the files directly inside <paramref name="directory"/>, as full asset paths.</summary>
    IReadOnlyList<string> List(string directory);

    /// <summary>Opens an asset for reading. Throws if it does not exist.</summary>
    Stream Open(string path);

    /// <summary>Whether the asset exists.</summary>
    bool Exists(string path);
}
