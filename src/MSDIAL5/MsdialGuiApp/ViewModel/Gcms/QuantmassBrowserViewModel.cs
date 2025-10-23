using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Gcms;

public sealed class QuantmassBrowserViewModel : SettingDialogViewModel
{
    private readonly QuantmassBrowserModel _model;

    public QuantmassBrowserViewModel(QuantmassBrowserModel model) {
        SelectedSpot = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedSpot).AddTo(Disposables);
        _model = model;
    }

    public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Spots => _model.Spots.Items;

    public ReactivePropertySlim<AlignmentSpotPropertyModel?> SelectedSpot { get; }
}
