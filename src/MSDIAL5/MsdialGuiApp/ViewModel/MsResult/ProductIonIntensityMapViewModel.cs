using CompMs.App.Msdial.Model.MsResult;
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

        IntensityMapHorizontalAxis = LoadedIons.Select(ions => ions?.Distinct(IDComparer.Default).ToArray() ?? [])
            .ToReactiveCategoryAxisManager(ion => ion.ID, ion => ion.Time.ToString()).AddTo(Disposables);
        ((BaseAxisManager<MappedIon>)IntensityMapHorizontalAxis).ChartMargin = new ConstantMargin(0d);
        IntensityMapVerticalAxis = LoadedIons.Select(ions => ions?.Distinct(ExperimentIDComparer.Default).OrderBy(v => v.ExperimentID).ToArray() ?? [])
            .ToReactiveCategoryAxisManager(ion => ion.ExperimentID, ion => ion.Mz.ToString("F2")).AddTo(Disposables);
        ((BaseAxisManager<MappedIon>)IntensityMapVerticalAxis).ChartMargin = new ConstantMargin(0d);

        LoadProductIonsMapCommand = SelectedMzRange.Select(r => r is not null)
            .ToReactiveCommand()
            .WithSubscribe(async () => {
                await _model.LoadIonsAsync().ConfigureAwait(false);
            }).AddTo(Disposables);
    }

    public MsSpectrumViewModel MsSpectrumViewModel { get; }

    public ReadOnlyReactivePropertySlim<List<MappedIon>?> LoadedIons { get; }

    public IAxisManager<MappedIon> IntensityMapHorizontalAxis { get; }
    public IAxisManager<MappedIon> IntensityMapVerticalAxis { get; }

    public ReactivePropertySlim<AxisRange?> SelectedMzRange { get; }

    public ReactiveCommand LoadProductIonsMapCommand { get; }

    class IDComparer : IEqualityComparer<MappedIon>
    {
        public static readonly IDComparer Default = new();
        public bool Equals(MappedIon x, MappedIon y) => x.ID == y.ID;
        public int GetHashCode(MappedIon obj) => obj.ID.GetHashCode();
    }

    class ExperimentIDComparer : IEqualityComparer<MappedIon>
    {
        public static readonly ExperimentIDComparer Default = new();
        public bool Equals(MappedIon x, MappedIon y) => x.ExperimentID == y.ExperimentID;
        public int GetHashCode(MappedIon obj) => obj.ExperimentID.GetHashCode();
    }
}
