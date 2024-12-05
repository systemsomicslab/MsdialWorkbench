using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class MsdialAnalysisMassBankRecordExportViewModel : ViewModelBase, IMsdialAnalysisExportViewModel {
        private readonly MsdialAnalysisMassBankRecordExportModel _model;

        public MsdialAnalysisMassBankRecordExportViewModel(MsdialAnalysisMassBankRecordExportModel model) {
            _model = model;
            ShouldExport = model.ToReactivePropertySlimAsSynchronized(m => m.ShouldExport).AddTo(Disposables);
            ContributorID = model.ToReactivePropertyAsSynchronized(m => m.ContributorID).AddTo(Disposables);
        }

        public string Label => _model.Label;

        public ReactiveProperty<string?> ContributorID { get; }

        public ReactivePropertySlim<bool> ShouldExport { get; }

        IObservable<bool> IMsdialAnalysisExportViewModel.CanExport => Observable.Return(true);
        IObservable<bool> IMsdialAnalysisExportViewModel.ShouldExport => ShouldExport;
    }
}
