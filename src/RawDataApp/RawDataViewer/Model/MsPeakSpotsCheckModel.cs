using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public sealed class MsPeakSpotsCheckModel : DisposableModelBase
    {
        private MsPeakSpotsCheckModel(AnalysisDataModel dataModel, MsPeakSpotsSummary summary, MsSnDistribution distribution) {
            AnalysisFile = dataModel.AnalysisFile;
            MachineCategory = dataModel.MachineCategory;
            Summary = summary.AddTo(Disposables) ?? throw new ArgumentNullException(nameof(summary));
            Distribution = distribution.AddTo(Disposables) ?? throw new ArgumentNullException(nameof(distribution));
        }

        public AnalysisFileBean AnalysisFile { get; }
        public MachineCategory MachineCategory { get; }

        public MsPeakSpotsSummary Summary { get; }
        public MsSnDistribution Distribution { get; }

        public static async Task<MsPeakSpotsCheckModel> CreateAsync(AnalysisDataModel dataModel, CancellationToken token = default) {
            var detector = new MsPeakSpotsDetector();
            var (summary, distribution) = await detector.DetectAsync(dataModel, token).ConfigureAwait(false);
            return new MsPeakSpotsCheckModel(dataModel, summary, distribution);
        }
    }
}
