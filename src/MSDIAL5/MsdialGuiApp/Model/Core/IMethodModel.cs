using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IMethodModel : INotifyPropertyChanged, IDisposable {
        AnalysisFileBeanModelCollection AnalysisFileModelCollection { get; }

        AnalysisFileBeanModel? AnalysisFileModel { get; }

        AlignmentFileBeanModelCollection AlignmentFiles { get; }

        AlignmentFileBeanModel? AlignmentFile { get; }

        Task LoadAnalysisFileAsync(AnalysisFileBeanModel analysisFile, CancellationToken token);

        Task LoadAsync(CancellationToken token);
        Task SaveAsync();
        Task RunAsync(ProcessOption option, CancellationToken token);
        void InvokeMsfinder(IResultModel model);
    }
}
