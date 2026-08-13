namespace Chummer.UI.Spikes;

/// <summary>Outcome of one platform-risk measurement.</summary>
public sealed class SpikeResult
{
    public required string Name { get; init; }

    /// <summary>False when the measured capability does not work on this platform.</summary>
    public required bool Ok { get; init; }

    /// <summary>One-line verdict, shown big.</summary>
    public required string Headline { get; init; }

    /// <summary>Measured numbers, in display order.</summary>
    public IReadOnlyList<(string Label, string Value)> Details { get; init; } =
        Array.Empty<(string, string)>();

    public string Icon => Ok ? "OK" : "FALHOU";

    public string DetailText => string.Join(
        Environment.NewLine,
        Details.Select(d => $"{d.Label}: {d.Value}"));
}
