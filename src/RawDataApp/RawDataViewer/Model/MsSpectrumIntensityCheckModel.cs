using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsSpectrumIntensityCheckModel : DisposableModelBase
    {
        private MsSpectrumIntensityCheckModel(AnalysisFileBean analysisFile, MachineCategory machineCategory, MsSpectrumSummary[] summaries) {
            Summaries = summaries;
            foreach (var summary in summaries) {
                Disposables.Add(summary);
            }
            AnalysisFile = analysisFile;
            MachineCategory = machineCategory;
        }

        public MsSpectrumSummary[] Summaries { get; }

        public AnalysisFileBean AnalysisFile { get; }
        public MachineCategory MachineCategory { get; }

        public static async Task<MsSpectrumIntensityCheckModel> CreateAsync(AnalysisDataModel dataModel, CancellationToken token = default) {
            var summarizer = new MsSpectrumIntensitySummarizer();
            var dataProvider = await summarizer.SummarizeAsync(dataModel.CreateDataProvider(token)).ConfigureAwait(false);
            return new MsSpectrumIntensityCheckModel(dataModel.AnalysisFile, dataModel.MachineCategory, dataProvider);
        }
    }
}
