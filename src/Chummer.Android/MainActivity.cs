using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Chummer.UI;
using Chummer.UI.Platform;
using Chummer.UI.Spikes;

namespace Chummer.Android;

/// <summary>
/// The single Activity of the skeleton APK. It only installs the platform services and hands
/// control to the shared Avalonia <see cref="App"/>.
/// </summary>
/// <remarks>
/// <c>ConfigChanges</c> lists everything Android would otherwise recreate the Activity for.
/// Avalonia handles those itself; without this the app is torn down and rebuilt on every
/// rotation, which on a data-heavy app means re-paying the whole start-up cost.
/// </remarks>
[Activity(
    Label = "Chummer 5",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@android:drawable/sym_def_app_icon",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation
                           | ConfigChanges.ScreenSize
                           | ConfigChanges.UiMode
                           | ConfigChanges.Density
                           | ConfigChanges.Keyboard
                           | ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        builder.WithInterFont();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AppStartup.MarkProcessStart();
        AppServices.Install(
            new AndroidAssetSource(Assets!),
            new AndroidPlatformProbe(this));
        base.OnCreate(savedInstanceState);
    }
}
