using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class SummarizedDataModel : DisposableModelBase
    {
        public SummarizedDataModel(AnalysisDataModel dataModel) {
            AnalysisDataModel = dataModel.AddTo(Disposables) ?? throw new ArgumentNullException(nameof(dataModel));

            MsSpectrumIntensityCheckModelTask = MsSpectrumIntensityCheckModel.CreateAsync(AnalysisDataModel);
            MsPeakSpotsCheckModelTask = MsPeakSpotsCheckModel.CreateAsync(AnalysisDataModel);
            RawMsSpectrumCheckModelTask = RawMsSpectrumCheckModel.CreateAsync(dataModel);
        }

        public AnalysisDataModel AnalysisDataModel { get; }

        public Task<MsSpectrumIntensityCheckModel> MsSpectrumIntensityCheckModelTask { get; }

        public Task<MsPeakSpotsCheckModel> MsPeakSpotsCheckModelTask { get; }

        public Task<RawMsSpectrumCheckModel> RawMsSpectrumCheckModelTask { get; }
    }
}
