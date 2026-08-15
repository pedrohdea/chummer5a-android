using System.Collections.ObjectModel;
using System.Reflection;
using Chummer.UI.Platform;

namespace Chummer.UI.ViewModels;

/// <summary>
/// The single screen of the skeleton: a presentation header plus the menu the real app grows
/// into. Every entry is disabled on purpose.
/// </summary>
/// <remarks>
/// The screen deliberately does nothing. The skeleton exists to answer one question — does the
/// app install, start, draw a frame and stay up on a real device — and anything the screen did
/// on its own would be one more thing that could fail for an unrelated reason, turning a clear
/// answer into a diagnosis.
/// </remarks>
public sealed class MainViewModel : ViewModelBase
{
    public string Title => "Chummer 5";

    public string Subtitle => "Shadowrun 5e — fichas de personagem";

    /// <summary>
    /// Build identity, on screen rather than in a log. QA happens on a device with no debugger
    /// attached, and without this line there is no way to tell which APK is installed.
    /// </summary>
    public string BuildLine
    {
        get
        {
            string version = typeof(MainViewModel).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(MainViewModel).Assembly.GetName().Version?.ToString()
                ?? "?";

            // The '+' suffix is the commit hash that the SDK appends; it is noise on a phone
            // screen and the version alone already identifies the build.
            int plus = version.IndexOf('+');
            if (plus >= 0)
                version = version[..plus];

            return $"esqueleto {version} · {AppServices.Platform.Name}";
        }
    }

    public ObservableCollection<MenuEntry> Menu { get; } =
    [
        new("Abrir personagem", "em breve"),
        new("Novo personagem", "em breve"),
        new("Configurações", "em breve"),
        new("Sobre", "em breve"),
    ];
}
