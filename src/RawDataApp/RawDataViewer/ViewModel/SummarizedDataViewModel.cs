using CompMs.App.RawDataViewer.Model;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public class SummarizedDataViewModel : ViewModelBase
    {

        public SummarizedDataViewModel(SummarizedDataModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            AnalysisDataModel = Model.AnalysisDataModel;

            MsSpectrumIntensityCheckViewModel = Model.MsSpectrumIntensityCheckModelTask
                .ToObservable()
                .Select(m => new MsSpectrumIntensityCheckViewModel(m))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            MsPeakSpotsCheckViewModel = Model.MsPeakSpotsCheckModelTask
                .ToObservable()
                .Select(m => new MsPeakSpotsCheckViewModel(m))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            RawMsSpectrumCheckViewModel = Model.RawMsSpectrumCheckModelTask
                .ToObservable()
                .Select(m => new RawMsSpectrumCheckViewModel(m))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public SummarizedDataModel Model { get; }

        public AnalysisDataModel AnalysisDataModel { get; }

        public ReadOnlyReactivePropertySlim<MsSpectrumIntensityCheckViewModel> MsSpectrumIntensityCheckViewModel { get; }

        public ReadOnlyReactivePropertySlim<MsPeakSpotsCheckViewModel> MsPeakSpotsCheckViewModel { get; }

        public ReadOnlyReactivePropertySlim<RawMsSpectrumCheckViewModel> RawMsSpectrumCheckViewModel { get; }
    }
}
