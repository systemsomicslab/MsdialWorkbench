using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IMethodModel : INotifyPropertyChanged, IDisposable {
        AnalysisModelBase AnalysisModel { get; set; }

        AlignmentModelBase AlignmentModel { get; set; }

        ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        AnalysisFileBean AnalysisFile { get; }

        ObservableCollection<AlignmentFileBean> AlignmentFiles { get; }

        AlignmentFileBean AlignmentFile { get; }

        void Run(ProcessOption process);

        void LoadAnalysisFile(AnalysisFileBean analysisFile);

        Task SaveAsync();
    }
}
