using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal class AlignmentResultMassBankRecordExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
    {
        internal const string ContributorIdError = "Enter the contributor ID MassBank assigned to you: 1 to 32 of A-Z, 0-9 and _.";

        private readonly AlignmentResultMassBankRecordExportModel _model;

        public AlignmentResultMassBankRecordExportViewModel(AlignmentResultMassBankRecordExportModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            // The dialog starts from the contributor ID saved with the project, when there is one.
            model.UseProjectContributorID();
            IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
            // The contributor ID is checked only while MassBank is chosen, so an unused option shows no error.
            ContributorID = model.ToReactivePropertyAsSynchronized(m => m.ContributorID)
                .SetValidateNotifyError(id => !IsSelected.Value || MassBankVocabulary.IsValidContributorId(id) ? null : ContributorIdError)
                .AddTo(Disposables);
            IsSelected.Subscribe(_ => ContributorID.ForceValidate()).AddTo(Disposables);
            IncludeSuggested = model.ToReactivePropertySlimAsSynchronized(m => m.IncludeSuggested).AddTo(Disposables);
            IncludeUnknowns = model.ToReactivePropertySlimAsSynchronized(m => m.IncludeUnknowns).AddTo(Disposables);

            // The project metadata cannot change while the export dialog is open, so it is checked once.
            var problems = model.ValidateProjectMetadata();
            HasProjectMetadataProblems = problems.Count > 0;
            ProjectMetadataProblems = MassBankProjectMetadataMessage.Describe(problems, model.IsProjectConstrainedToMassBankFormat);
            ShowProjectMetadataProblems = IsSelected.Select(selected => selected && HasProjectMetadataProblems)
                .ToReadOnlyReactivePropertySlim(IsSelected.Value && HasProjectMetadataProblems)
                .AddTo(Disposables);

            CanExport = IsSelected
                .CombineLatest(ContributorID, (selected, id) => !selected || (MassBankVocabulary.IsValidContributorId(id) && !HasProjectMetadataProblems))
                .ToReadOnlyReactivePropertySlim(!IsSelected.Value || (MassBankVocabulary.IsValidContributorId(ContributorID.Value) && !HasProjectMetadataProblems))
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

        /// <summary>True while MassBank is chosen and the project metadata keeps it from being exported.</summary>
        public ReadOnlyReactivePropertySlim<bool> ShowProjectMetadataProblems { get; }

        public ReadOnlyReactivePropertySlim<bool> CanExport { get; }

        IReadOnlyReactiveProperty<bool> IAlignmentResultExportViewModel.CanExport => CanExport;
    }
}
