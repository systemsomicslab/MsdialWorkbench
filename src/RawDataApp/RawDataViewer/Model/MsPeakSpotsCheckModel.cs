using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsPeakSpotsCheckModel : DisposableModelBase
    {
        private MsPeakSpotsCheckModel(AnalysisDataModel dataModel, MsPeakSpotsSummary summary) {
            AnalysisFile = dataModel.AnalysisFile;
            MachineCategory = dataModel.MachineCategory;
            Summary = summary.AddTo(Disposables) ?? throw new ArgumentNullException(nameof(summary));
        }

        public AnalysisFileBean AnalysisFile { get; }
        public MachineCategory MachineCategory { get; }

        public MsPeakSpotsSummary Summary { get; }

        public static async Task<MsPeakSpotsCheckModel> CreateAsync(AnalysisDataModel dataModel, CancellationToken token = default) {
            var detector = new MsPeakSpotsDetector();
            var summary = await detector.DetectAsync(dataModel, token).ConfigureAwait(false);
            return new MsPeakSpotsCheckModel(dataModel, summary);
        }
    }
}
