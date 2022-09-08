using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IMethodModel : INotifyPropertyChanged, IDisposable {
        ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        AnalysisFileBean AnalysisFile { get; }

        ObservableCollection<AlignmentFileBean> AlignmentFiles { get; }

        AlignmentFileBean AlignmentFile { get; }

        Task LoadAnalysisFileAsync(AnalysisFileBean analysisFile, CancellationToken token);

        Task SaveAsync();
        Task RunAsync(ProcessOption option, CancellationToken token);
    }
}
