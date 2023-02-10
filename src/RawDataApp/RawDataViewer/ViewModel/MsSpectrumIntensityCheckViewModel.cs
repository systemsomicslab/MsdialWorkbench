using CompMs.App.RawDataViewer.Model;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public class MsSpectrumIntensityCheckViewModel : ViewModelBase
    {
        public MsSpectrumIntensityCheckViewModel(MsSpectrumIntensityCheckModel model) {
            Summaries = model.ObserveProperty(m => m.Summaries)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AnalysisFile = model.ObserveProperty(m => m.AnalysisFile)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MachineCategory = model.ObserveProperty(m => m.MachineCategory)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<MsSpectrumSummary[]> Summaries { get; }

        public ReadOnlyReactivePropertySlim<AnalysisFileBean> AnalysisFile { get; }

        public ReadOnlyReactivePropertySlim<MachineCategory> MachineCategory { get; }
    }
}
