using Avalonia.Controls;

namespace Chummer.UI.Views;

/// <summary>
/// Desktop shell around <see cref="MainView"/>. The window is sized like a phone on purpose:
/// the point of Chummer.Desktop is to debug the Android layout without an emulator (DEC-002),
/// which only works if the desktop window has roughly the aspect ratio of the device.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}
