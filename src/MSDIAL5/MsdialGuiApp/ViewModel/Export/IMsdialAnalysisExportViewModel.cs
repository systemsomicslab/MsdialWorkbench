using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal interface IMsdialAnalysisExportViewModel : INotifyDataErrorInfo, IDisposable {
        IObservable<bool> CanExport { get; }
        IObservable<bool> ShouldExport { get; }
    }

    internal static class MsdialAnalysisExportViewModelFactory {
        public static IMsdialAnalysisExportViewModel Create(IMsdialAnalysisExport model) {
            switch (model) {
                case MsdialAnalysisExportModel exportModel:
                    return new MsdialAnalysisExportViewModel(exportModel);
                case MsdialAnalysisTableExportModel tableModel:
                    return new MsdialAnalysisTableExportViewModel(tableModel);
                default:
                    return null;
            }
        }
    }

    internal sealed class MsdialAnalysisExportViewModel : ViewModelBase, IMsdialAnalysisExportViewModel {
        private readonly MsdialAnalysisExportModel _model;

        public MsdialAnalysisExportViewModel(MsdialAnalysisExportModel model) {
            _model = model;
            ShouldExport = model.ToReactivePropertySlimAsSynchronized(m => m.ShouldExport).AddTo(Disposables);
        }

        public string Label => _model.Label;

        public ReactivePropertySlim<bool> ShouldExport { get; }

        IObservable<bool> IMsdialAnalysisExportViewModel.CanExport => Observable.Return(true);
        IObservable<bool> IMsdialAnalysisExportViewModel.ShouldExport => ShouldExport;
    }

}
