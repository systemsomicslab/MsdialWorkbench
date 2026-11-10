using CompMs.App.RawDataViewer.Model;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public class SummarizedDataViewModel : ViewModelBase
    {

        public SummarizedDataViewModel(SummarizedDataModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            AnalysisDataModel = Model.AnalysisDataModel;

            // Each panel is null while its model is being built, and then holds either its view model or why it has none.
            MsSpectrumIntensityCheckPanel = PanelLoadResult.FromTask(Model.MsSpectrumIntensityCheckModelTask, m => new MsSpectrumIntensityCheckViewModel(m))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MsSpectrumIntensityCheckViewModel = MsSpectrumIntensityCheckPanel
                .Select(panel => panel?.Value)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            MsPeakSpotsCheckPanel = PanelLoadResult.FromTask(Model.MsPeakSpotsCheckModelTask, m => new MsPeakSpotsCheckViewModel(m))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MsPeakSpotsCheckViewModel = MsPeakSpotsCheckPanel
                .Select(panel => panel?.Value)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            RawMsSpectrumCheckPanel = PanelLoadResult.FromTask(Model.RawMsSpectrumCheckModelTask, m => new RawMsSpectrumCheckViewModel(m))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            RawMsSpectrumCheckViewModel = RawMsSpectrumCheckPanel
                .Select(panel => panel?.Value)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            MsPeakSpotsCheckViewModel.Where(vm => vm != null).Select(vm => vm.SelectedPeakSpectrumID).Switch()
                .CombineLatest(RawMsSpectrumCheckViewModel.Where(vm => vm != null), (id, vm) => {
                    var spec = vm.Spectra.FirstOrDefault(s => s.ScanNumber == id);
                    vm.SelectedSpectrum.Value = spec;
                    return Unit.Default;
                }).Subscribe().AddTo(Disposables);
        }

        public SummarizedDataModel Model { get; }

        public AnalysisDataModel AnalysisDataModel { get; }

        public ReadOnlyReactivePropertySlim<PanelLoadResult<MsSpectrumIntensityCheckViewModel>> MsSpectrumIntensityCheckPanel { get; }

        public ReadOnlyReactivePropertySlim<MsSpectrumIntensityCheckViewModel> MsSpectrumIntensityCheckViewModel { get; }

        public ReadOnlyReactivePropertySlim<PanelLoadResult<MsPeakSpotsCheckViewModel>> MsPeakSpotsCheckPanel { get; }

        public ReadOnlyReactivePropertySlim<MsPeakSpotsCheckViewModel> MsPeakSpotsCheckViewModel { get; }

        public ReadOnlyReactivePropertySlim<PanelLoadResult<RawMsSpectrumCheckViewModel>> RawMsSpectrumCheckPanel { get; }

        public ReadOnlyReactivePropertySlim<RawMsSpectrumCheckViewModel> RawMsSpectrumCheckViewModel { get; }
    }
}
