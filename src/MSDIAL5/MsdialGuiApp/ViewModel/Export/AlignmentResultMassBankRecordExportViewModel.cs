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
    internal class AlignmentResultMassBankRecordExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
    {
        internal const string ContributorIdError = "Enter the contributor ID MassBank assigned to you: 1 to 32 of A-Z, 0-9 and _.";

        private readonly AlignmentResultMassBankRecordExportModel _model;

        public AlignmentResultMassBankRecordExportViewModel(AlignmentResultMassBankRecordExportModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
            ContributorID = model.ToReactivePropertyAsSynchronized(m => m.ContributorID)
                .SetValidateNotifyError(id => MassBankVocabulary.IsValidContributorId(id) ? null : ContributorIdError)
                .AddTo(Disposables);
            IncludeSuggested = model.ToReactivePropertySlimAsSynchronized(m => m.IncludeSuggested).AddTo(Disposables);
            IncludeUnknowns = model.ToReactivePropertySlimAsSynchronized(m => m.IncludeUnknowns).AddTo(Disposables);

            // The project metadata cannot change while the export dialog is open, so it is checked once.
            var problems = model.ValidateProjectMetadata();
            HasProjectMetadataProblems = problems.Count > 0;
            ProjectMetadataProblems = HasProjectMetadataProblems
                ? "MassBank records cannot be exported until these are set in Option > Project property setting:\n" + string.Join("\n", problems.Select(p => "- " + p))
                : string.Empty;

            CanExport = IsSelected
                .CombineLatest(ContributorID.ObserveHasErrors.StartWith(ContributorID.HasErrors), (selected, hasErrors) => !selected || (!hasErrors && !HasProjectMetadataProblems))
                .ToReadOnlyReactivePropertySlim(!IsSelected.Value || (!ContributorID.HasErrors && !HasProjectMetadataProblems))
                .AddTo(Disposables);
        }

        public bool IsExpanded {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        private bool _isExpanded;

        public ReactivePropertySlim<bool> IsSelected { get; }

        public ReactiveProperty<string> ContributorID { get; }

        /// <summary>Also export suggested annotations, marked as such in the record.</summary>
        public ReactivePropertySlim<bool> IncludeSuggested { get; }

        /// <summary>Also export unknowns, with the name Unknown and no structure.</summary>
        public ReactivePropertySlim<bool> IncludeUnknowns { get; }

        public bool HasProjectMetadataProblems { get; }

        public string ProjectMetadataProblems { get; }

        public ReadOnlyReactivePropertySlim<bool> CanExport { get; }

        IReadOnlyReactiveProperty<bool> IAlignmentResultExportViewModel.CanExport => CanExport;
    }
}
