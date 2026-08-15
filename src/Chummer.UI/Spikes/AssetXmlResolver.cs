using System.Xml;
using Chummer.UI.Platform;

namespace Chummer.UI.Spikes;

/// <summary>
/// Resolves <c>xsl:import</c> / <c>xsl:include</c> hrefs against the packaged assets.
/// </summary>
/// <remarks>
/// The sheets import each other by plain relative file name ("Shadowrun 5 set.xslt"), which
/// on Windows resolves against the file system. Inside an APK there is no file system to
/// resolve against, so imports are given a synthetic <c>asset:</c> authority and mapped back
/// to asset paths here. Names contain spaces, hence the unescaping — without it the lookup
/// asks for "Shadowrun%205%20set.xslt" and misses.
/// </remarks>
public sealed class AssetXmlResolver : XmlResolver
{
    public const string Scheme = "asset";

    private readonly IAssetSource _assets;
    private readonly string _root;

    public AssetXmlResolver(IAssetSource assets, string root)
    {
        _assets = assets;
        _root = root.Trim('/');
    }

    /// <summary>Base URI to hand to <see cref="XmlReader"/> for a file inside the root.</summary>
    public Uri UriFor(string fileName) =>
        new($"{Scheme}:///{_root}/{Uri.EscapeDataString(fileName)}");

    public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
    {
        if (baseUri is not null && !string.IsNullOrEmpty(relativeUri))
            return new Uri(baseUri, Uri.EscapeDataString(relativeUri));
        return base.ResolveUri(baseUri, relativeUri);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        ArgumentNullException.ThrowIfNull(absoluteUri);
        string path = Uri.UnescapeDataString(absoluteUri.AbsolutePath).TrimStart('/');
        if (!_assets.Exists(path))
            throw new FileNotFoundException($"Asset não encontrado: {path}", path);
        return _assets.Open(path);
    }
}
