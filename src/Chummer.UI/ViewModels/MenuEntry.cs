namespace Chummer.UI.ViewModels;

/// <summary>
/// One entry of the main menu. The skeleton ships every entry disabled, so the type carries
/// the reason: a greyed-out button with no explanation reads as a bug during QA.
/// </summary>
/// <param name="Label">What the entry will do once it is built.</param>
/// <param name="Note">Why it is unavailable right now.</param>
public sealed record MenuEntry(string Label, string Note);
