using Chummer.UI.Platform;

namespace Chummer.Desktop;

/// <summary>
/// Reads the game data straight from a folder on disk.
/// </summary>
/// <remarks>
/// The desktop head deliberately does <em>not</em> copy the ~20 MB of data into its output:
/// it points at the working copy inside the repository. That keeps the inner loop fast and,
/// more importantly, means the desktop run always exercises the same files the APK packages,
/// with no chance of a stale copy quietly diverging.
/// </remarks>
public sealed class FileSystemAssetSource : IAssetSource
{
    private readonly string _root;

    private FileSystemAssetSource(string root) => _root = root;

    public string Description => $"disco: {_root}";

    /// <summary>
    /// Walks up from the executable looking for the repository's <c>Chummer/</c> folder, which
    /// is where <c>data/</c>, <c>lang/</c>, <c>sheets/</c> and <c>customdata/</c> live today.
    /// </summary>
    public static FileSystemAssetSource? Discover()
    {
        string[] candidates =
        [
            Environment.GetEnvironmentVariable("CHUMMER_ASSETS") ?? string.Empty,
            Path.Combine(AppContext.BaseDirectory, "assets"),
        ];

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && Directory.Exists(Path.Combine(candidate, "data")))
                return new FileSystemAssetSource(Path.GetFullPath(candidate));
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string chummer = Path.Combine(directory.FullName, "Chummer");
            if (Directory.Exists(Path.Combine(chummer, "data")))
                return new FileSystemAssetSource(chummer);
            directory = directory.Parent;
        }

        return null;
    }

    public IReadOnlyList<string> List(string directory)
    {
        string full = Path.Combine(_root, directory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(full))
            return [];
        return Directory.EnumerateFiles(full)
            .Select(f => $"{directory.Trim('/')}/{Path.GetFileName(f)}")
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    public Stream Open(string path) =>
        File.OpenRead(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar)));

    public bool Exists(string path) =>
        File.Exists(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar)));
}
