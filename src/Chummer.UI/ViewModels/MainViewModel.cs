using System.Collections.ObjectModel;
using Chummer.UI.Platform;
using Chummer.UI.Spikes;

namespace Chummer.UI.ViewModels;

/// <summary>
/// The one screen of the skeleton APK: it exists to run the platform-risk measurements on
/// the device and show the numbers, since no emulator is available in the build container.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private bool _running;
    private string _status = "Toque em Medir para rodar os spikes de plataforma.";

    public ObservableCollection<SpikeResult> Results { get; } = [];

    public string Title => "Chummer 5 — esqueleto";

    public string Subtitle =>
        $"{AppServices.Platform.Name} · {AppServices.Assets.Description}";

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public bool Running
    {
        get => _running;
        private set
        {
            if (SetField(ref _running, value))
                OnPropertyChanged(nameof(CanRun));
        }
    }

    public bool CanRun => !Running;

    /// <summary>
    /// Runs the suite off the UI thread. The data spike reads ~7 MB of XML; doing that on the
    /// UI thread is exactly the ANR the port is supposed to avoid, so the skeleton models the
    /// right behaviour from the first screen.
    /// </summary>
    public async Task RunSpikesAsync()
    {
        if (Running)
            return;

        Running = true;
        Status = "Medindo…";
        Results.Clear();

        try
        {
            IReadOnlyList<SpikeResult> results = await Task.Run(SpikeSuite.RunAll).ConfigureAwait(true);
            foreach (SpikeResult result in results)
                Results.Add(result);

            int failed = results.Count(r => !r.Ok);
            Status = failed == 0
                ? $"{results.Count} spikes, todos passaram."
                : $"{results.Count} spikes, {failed} com falha — ver detalhes.";
        }
        catch (Exception ex)
        {
            Status = $"Os spikes explodiram: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            Running = false;
        }
    }
}
