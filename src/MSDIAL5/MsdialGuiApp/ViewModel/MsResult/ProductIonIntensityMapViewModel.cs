using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.MsResult;

internal sealed class ProductIonIntensityMapViewModel : ViewModelBase
{
    private readonly ProductIonIntensityMapModel _model;

    public ProductIonIntensityMapViewModel(ProductIonIntensityMapModel model) {
        _model = model;

        MsSpectrumViewModel = new MsSpectrumViewModel(model.MsSpectrumModel).AddTo(Disposables);
        var haxis = model.MsSpectrumModel.HorizontalAxis.First(); // temp

        SelectedMzRange = model.ToReactivePropertySlimAsSynchronized(
            m => m.SelectedRange,
            m => m is null ? null : new AxisRange(haxis.TranslateToAxisValue(m.Mz - m.Tolerance), haxis.TranslateToAxisValue(m.Mz + m.Tolerance)),
            vm => vm is null ? null : MzRange.FromRange(new RangeSelection(vm).ConvertBy(haxis))).AddTo(Disposables);

        LoadedIons = model.ObserveProperty(m => m.LoadedIons).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        IntensityMapHorizontalAxis = LoadedIons.Select(ions => ions?.Select(ion => ion.ID).ToArray() ?? [])
            .ToReactiveCategoryAxisManager().AddTo(Disposables);
        IntensityMapVerticalAxis = LoadedIons.Select(ions => ions?.Select(ion => ion.ExperimentID).ToArray() ?? [])
            .ToReactiveCategoryAxisManager().AddTo(Disposables);

        LoadProductIonsMapCommand = SelectedMzRange.Select(r => r is not null)
            .ToReactiveCommand()
            .WithSubscribe(async () => {
                await _model.LoadIonsAsync().ConfigureAwait(false);
            }).AddTo(Disposables);
    }

    public MsSpectrumViewModel MsSpectrumViewModel { get; }

    public ReadOnlyReactivePropertySlim<List<MappedIon>?> LoadedIons { get; }

    public IAxisManager<string> IntensityMapHorizontalAxis { get; }
    public IAxisManager<int> IntensityMapVerticalAxis { get; }

    public ReactivePropertySlim<AxisRange?> SelectedMzRange { get; }

    public ReactiveCommand LoadProductIonsMapCommand { get; }
}
