using CompMs.App.RawDataViewer.Model;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public class MsPeakSpotsCheckViewModel : ViewModelBase
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
        }

        public ReadOnlyReactivePropertySlim<AnalysisFileBean> AnalysisFile { get; }
        public ReadOnlyReactivePropertySlim<MachineCategory> MachineCategory { get; }
        public ReadOnlyReactivePropertySlim<MsPeakSpotsSummary> Summary { get; }
    }
}
