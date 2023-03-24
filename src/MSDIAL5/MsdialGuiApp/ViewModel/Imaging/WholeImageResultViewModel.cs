using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class WholeImageResultViewModel : ViewModelBase
    {
        private readonly WholeImageResultModel _model;

        public WholeImageResultViewModel(WholeImageResultModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            PeakPlotViewModel = new AnalysisPeakPlotViewModel(model.PeakPlotModel, () => { }, Observable.Never<bool>());

            Intensities = model.Intensities.ToReadOnlyReactiveCollection(intensity => new IntensityImageViewModel(intensity)).AddTo(Disposables);
            SelectedPeakIntensities = model.ToReactivePropertyAsSynchronized(
                m => m.SelectedPeakIntensities,
                mox => mox.Select(m => Intensities.FirstOrDefault(vm => vm.Model == m)),
                vmox => vmox.Select(vm => vm?.Model))
                .AddTo(Disposables);
            ImagingRoiViewModel = new ImagingRoiViewModel(model.ImagingRoiModel).AddTo(Disposables);
        }

        public AnalysisPeakPlotViewModel PeakPlotViewModel { get; }
        public ReactiveProperty<ChromatogramPeakFeatureModel> Target => _model.Target;
        public ReadOnlyReactiveCollection<IntensityImageViewModel> Intensities { get; }
        public ReactiveProperty<IntensityImageViewModel> SelectedPeakIntensities { get; }
        public ImagingRoiViewModel ImagingRoiViewModel { get; }
    }
}
