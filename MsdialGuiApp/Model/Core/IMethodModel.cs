using CompMs.App.Msdial.Model.DataObj;
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
        AnalysisFileBeanModelCollection AnalysisFileModelCollection { get; }

        AnalysisFileBeanModel AnalysisFileModel { get; }

        ObservableCollection<AlignmentFileBean> AlignmentFiles { get; }

        AlignmentFileBean AlignmentFile { get; }

        Task LoadAnalysisFileAsync(AnalysisFileBeanModel analysisFile, CancellationToken token);

        Task LoadAsync(CancellationToken token);
        Task SaveAsync();
        Task RunAsync(ProcessOption option, CancellationToken token);
    }
}
