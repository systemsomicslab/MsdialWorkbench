using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IMethodModel : IDisposable {
        AnalysisModelBase AnalysisModel { get; set; }

        AlignmentModelBase AlignmentModel { get; set; }

        ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        ObservableCollection<AlignmentFileBean> AlignmentFiles { get; }

        void Run(ProcessOption process);

        void LoadAnalysisFile(AnalysisFileBean analysisFile);

        Task SaveAsync();
    }
}
