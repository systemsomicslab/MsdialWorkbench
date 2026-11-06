using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class MsdialAnalysisMassBankRecordExportViewModel : ViewModelBase, IMsdialAnalysisExportViewModel {
        private readonly MsdialAnalysisMassBankRecordExportModel _model;

        public MsdialAnalysisMassBankRecordExportViewModel(MsdialAnalysisMassBankRecordExportModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ShouldExport = model.ToReactivePropertySlimAsSynchronized(m => m.ShouldExport).AddTo(Disposables);
            ContributorID = model.ToReactivePropertyAsSynchronized(m => m.ContributorID)
                .SetValidateNotifyError(id => MassBankVocabulary.IsValidContributorId(id) ? null : AlignmentResultMassBankRecordExportViewModel.ContributorIdError)
                .AddTo(Disposables);

            // The project metadata cannot change while the export dialog is open, so it is checked once.
            var problems = model.ValidateProjectMetadata();
            HasProjectMetadataProblems = problems.Count > 0;
            ProjectMetadataProblems = HasProjectMetadataProblems
                ? "MassBank records cannot be exported until these are set in Option > Project property setting:\n" + string.Join("\n", problems.Select(p => "- " + p))
                : string.Empty;

            CanExport = ShouldExport
                .CombineLatest(ContributorID.ObserveHasErrors.StartWith(ContributorID.HasErrors), (selected, hasErrors) => !selected || (!hasErrors && !HasProjectMetadataProblems))
                .ToReadOnlyReactivePropertySlim(!ShouldExport.Value || (!ContributorID.HasErrors && !HasProjectMetadataProblems))
                .AddTo(Disposables);
        }

        public string Label => _model.Label;

        public ReactiveProperty<string> ContributorID { get; }

        public ReactivePropertySlim<bool> ShouldExport { get; }

        public bool HasProjectMetadataProblems { get; }

        public string ProjectMetadataProblems { get; }

        public ReadOnlyReactivePropertySlim<bool> CanExport { get; }

        IObservable<bool> IMsdialAnalysisExportViewModel.CanExport => CanExport;
        IObservable<bool> IMsdialAnalysisExportViewModel.ShouldExport => ShouldExport;
    }
}
