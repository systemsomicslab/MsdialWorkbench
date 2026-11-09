using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class SummarizedDataModel : DisposableModelBase
    {
        public SummarizedDataModel(AnalysisDataModel dataModel) {
            AnalysisDataModel = dataModel.AddTo(Disposables) ?? throw new ArgumentNullException(nameof(dataModel));

            MsSpectrumIntensityCheckModelTask = ObserveFault(MsSpectrumIntensityCheckModel.CreateAsync(AnalysisDataModel));
            MsPeakSpotsCheckModelTask = ObserveFault(MsPeakSpotsCheckModel.CreateAsync(AnalysisDataModel));
            RawMsSpectrumCheckModelTask = ObserveFault(RawMsSpectrumCheckModel.CreateAsync(dataModel));
        }

        public AnalysisDataModel AnalysisDataModel { get; }

        public Task<MsSpectrumIntensityCheckModel> MsSpectrumIntensityCheckModelTask { get; }

        public Task<MsPeakSpotsCheckModel> MsPeakSpotsCheckModelTask { get; }

        public Task<RawMsSpectrumCheckModel> RawMsSpectrumCheckModelTask { get; }

        // The summary window shows the fault of a task in its panel, and reading it there observes it, but only while the
        // view model is subscribed: the faults of a file removed from the list while it loads, which disposes its view
        // model, would end as unobserved task exceptions. Reading the exception here observes it whatever the view does.
        private static Task<T> ObserveFault<T>(Task<T> task) {
            task.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return task;
        }
    }
}
