using Avalonia;
using Chummer.UI;
using Chummer.UI.Platform;
using Chummer.UI.Spikes;

namespace Chummer.Desktop;

internal static class Program
{
    /// <summary>
    /// Two modes on purpose. Without arguments it opens the window, which is what makes this
    /// project a substitute for the emulator (DEC-002). With <c>--spikes</c> it runs the
    /// platform measurements on the console and exits — that is the control run against which
    /// the device numbers are read, and it works on a machine with no display.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        AppStartup.MarkProcessStart();
        InstallPlatformServices();

        if (args.Contains("--spikes", StringComparer.Ordinal))
            return RunSpikesOnConsole();

        int shot = Array.IndexOf(args, "--screenshot");
        if (shot >= 0 && shot + 1 < args.Length)
        {
            return Screenshot.Capture(
                args[shot + 1],
                width: 420,
                height: 900);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Referenced by name by the Avalonia designer and previewer; do not rename.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void InstallPlatformServices()
    {
        FileSystemAssetSource? assets = FileSystemAssetSource.Discover();
        if (assets is not null)
            AppServices.Install(assets, new DesktopPlatformProbe());
        else
            Console.Error.WriteLine(
                "AVISO: pasta Chummer/ com data/ não encontrada. " +
                "Defina CHUMMER_ASSETS ou rode a partir da árvore do repositório.");
    }

    private static int RunSpikesOnConsole()
    {
        Console.WriteLine($"# Spikes de plataforma — {AppServices.Platform.Name}");
        Console.WriteLine($"# assets: {AppServices.Assets.Description}");
        Console.WriteLine();

        int failed = 0;
        foreach (SpikeResult result in SpikeSuite.RunAll())
        {
            Console.WriteLine($"[{result.Icon}] {result.Name} — {result.Headline}");
            foreach ((string label, string value) in result.Details)
                Console.WriteLine($"      {label,-38} {value}");
            Console.WriteLine();
            if (!result.Ok)
                ++failed;
        }

        Console.WriteLine(failed == 0 ? "Todos os spikes passaram." : $"{failed} spike(s) falharam.");
        return failed == 0 ? 0 : 1;
    }
}
