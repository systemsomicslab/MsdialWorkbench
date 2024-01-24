using CompMs.App.RawDataViewer.Model;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public sealed class MsPeakSpotsCheckViewModel : ViewModelBase
    {
        public MsPeakSpotsCheckViewModel(MsPeakSpotsCheckModel model) {
            AnalysisFile = model.ObserveProperty(m => m.AnalysisFile)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MachineCategory = model.ObserveProperty(m => m.MachineCategory)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            Summary = model.ObserveProperty(m => m.Summary)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            Distribution = model.ObserveProperty(m => m.Distribution)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            SelectedPeak = new ReactivePropertySlim<ChromatogramPeakFeature>().AddTo(Disposables);
            SelectedPeakSpectrumID = SelectedPeak.Select(p => p?.MS1RawSpectrumIdTop ?? 0).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<AnalysisFileBean> AnalysisFile { get; }
        public ReadOnlyReactivePropertySlim<MachineCategory> MachineCategory { get; }
        public ReadOnlyReactivePropertySlim<MsPeakSpotsSummary> Summary { get; }
        public ReadOnlyReactivePropertySlim<MsSnDistribution> Distribution { get; }
        public ReactivePropertySlim<ChromatogramPeakFeature> SelectedPeak { get; }
        public ReadOnlyReactivePropertySlim<int> SelectedPeakSpectrumID { get; }
    }
}
