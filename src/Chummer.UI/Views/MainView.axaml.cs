using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
        AppStartup.MarkFirstFrame();
    }

    private async void OnRunClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            await viewModel.RunSpikesAsync();
    }
}
