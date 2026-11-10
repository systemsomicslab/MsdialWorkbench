using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.Service;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms;

public sealed class QuantmassBrowserViewModel : SettingDialogViewModel
{
    private readonly QuantmassBrowserModel _model;

    public QuantmassBrowserViewModel(QuantmassBrowserModel model, IMessageBroker broker) {
        SelectedSpot = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedSpot).AddTo(Disposables);
        _model = model;
        // Browsing the quant masses reads no raw data; recalculating them reads the raw data of every sample.
        FinishCommand = new AsyncReactiveCommand().WithSubscribe(() => RawDataGuard.EnsureAll(model.AnalysisFiles, broker, "Quant mass recalculation")
            ? model.RunQuantificationAsync()
            : Task.CompletedTask).AddTo(Disposables);
    }

    public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Spots => _model.Spots.Items;

    public ReactivePropertySlim<AlignmentSpotPropertyModel?> SelectedSpot { get; }

    public override ICommand? FinishCommand { get; }
}
