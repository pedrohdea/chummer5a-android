using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chummer.UI.ViewModels;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base.
/// </summary>
/// <remarks>
/// Deliberately hand-rolled instead of pulling in an MVVM framework: the domain already
/// implements INotifyPropertyChanged rigorously, with declarative dependency-graph
/// propagation (DEC-002), so the ViewModels of the MVP are mostly thin projections. Adding
/// CommunityToolkit.Mvvm or ReactiveUI would put weight in the APK — the very thing this
/// stage is measuring — in exchange for source generators this layer does not need yet.
/// </remarks>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
