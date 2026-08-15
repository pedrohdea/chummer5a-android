using Avalonia;
using Avalonia.Controls;
using Chummer.UI.Spikes;
using Chummer.UI.ViewModels;

namespace Chummer.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The one instrument the skeleton keeps: it marks the moment a frame actually reached
        // the screen, which is precisely what the skeleton is meant to prove on the device.
        AppStartup.MarkFirstFrame();
    }
}
