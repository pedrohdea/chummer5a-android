using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chummer.UI.Views;

namespace Chummer.UI;

/// <summary>
/// The single Avalonia <see cref="Application"/> shared by every head.
/// </summary>
/// <remarks>
/// Two lifetimes, one app: the desktop head runs a classic window, Android runs single-view
/// inside an Activity. Both host the very same <see cref="MainView"/>, which is what makes
/// Chummer.Desktop a usable stand-in for the device (DEC-002).
/// </remarks>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
