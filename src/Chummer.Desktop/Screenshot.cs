using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Chummer.UI;
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
        // SetupWithoutStarting brings up Skia and the styling system without a window and
        // without a dispatcher loop — exactly what is needed to rasterise one control.
        AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .SetupWithoutStarting();

        MainView view = new()
        {
            Width = width,
            Height = height,
        };

        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();

        using RenderTargetBitmap bitmap = new(
            new PixelSize(width, height),
            new Vector(96, 96));
        bitmap.Render(view);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        bitmap.Save(path);

        FileInfo file = new(path);
        Console.WriteLine($"PNG gravado: {file.FullName} ({file.Length} bytes, {width}x{height})");
        return file.Length > 0 ? 0 : 1;
    }
}
