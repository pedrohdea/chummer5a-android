using Android.Content.Res;
using Chummer.UI.Platform;

namespace Chummer.Android;

/// <summary>
/// Reads the game data out of the APK through <see cref="AssetManager"/>.
/// </summary>
/// <remarks>
/// Assets inside an APK are entries in a zip, not files: there is no directory to enumerate
/// and no <c>Length</c> to ask for until the entry is opened. <see cref="AssetManager.List"/>
/// answers the first problem; the second is why <see cref="Exists"/> has to open the entry
/// and close it again, since the platform offers no cheaper way to ask.
/// </remarks>
public sealed class AndroidAssetSource : IAssetSource
{
    private readonly AssetManager _assets;

    public AndroidAssetSource(AssetManager assets) => _assets = assets;

    public string Description => "assets do APK";

    public IReadOnlyList<string> List(string directory)
    {
        string normalized = directory.Trim('/');
        string[]? names = _assets.List(normalized);
        if (names is null || names.Length == 0)
            return Array.Empty<string>();

        List<string> paths = new(names.Length);
        foreach (string name in names)
            paths.Add(normalized.Length == 0 ? name : $"{normalized}/{name}");
        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    public Stream Open(string path) => _assets.Open(path.TrimStart('/'));

    public bool Exists(string path)
    {
        try
        {
            using Stream stream = _assets.Open(path.TrimStart('/'));
            return true;
        }
        catch (Java.IO.FileNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
