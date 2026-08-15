using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Chummer.UI;
using Chummer.UI.ViewModels;
using Chummer.UI.Views;

namespace Chummer.Desktop;

/// <summary>
/// Renders <see cref="MainView"/> to a PNG without a screen.
/// </summary>
/// <remarks>
/// The build container has no display and no screenshot tool, so "the UI opens" would
/// otherwise be a claim rather than a measurement — and this project does not make claims it
/// has not run. Rendering through Skia into a bitmap exercises the real pipeline: XAML load,
/// styles, layout, text shaping and rasterisation. What it does not exercise is the
/// windowing backend, which is why the run under Xvfb exists as well.
/// </remarks>
internal static class Screenshot
{
    public static int Capture(string path, int width, int height)
    {
        // UsePlatformDetect and not UseSkia alone: Avalonia refuses to start without a
        // runtime platform, and using the real one means this run also proves the X11
        // backend initialises. It therefore needs a display — under Xvfb in the container.
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .SetupWithoutStarting();

        // The view has to live inside a real window, not float on its own: Avalonia resolves
        // styles and theme resources through the visual tree up to a TopLevel. Rendering a
        // detached control produces a technically valid, entirely blank PNG — measured.
        MainWindow window = new()
        {
            Width = width,
            Height = height,
        };
        window.Show();

        Pump(40);

        using RenderTargetBitmap bitmap = new(
            new PixelSize(width, height),
            new Vector(96, 96));
        bitmap.Render(window);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        bitmap.Save(path);

        FileInfo file = new(path);
        Console.WriteLine($"PNG gravado: {file.FullName} ({file.Length} bytes, {width}x{height})");
        return file.Length > 0 ? 0 : 1;
    }

    /// <summary>
    /// Runs pending dispatcher work. Layout, styling and text shaping all happen on
    /// dispatcher jobs — pumping them is what turns "the window exists" into "the window has
    /// content". Without it the PNG comes out valid and entirely blank, which was measured.
    /// </summary>
    private static void Pump(int rounds)
    {
        for (int i = 0; i < rounds; ++i)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(25);
        }
    }
}
