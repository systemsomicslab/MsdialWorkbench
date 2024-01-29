using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Utility;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Information
{
    public sealed class MatchResultCandidatesViewModel : ViewModelBase
    {
        private readonly MatchResultCandidatesModel _model;

        public MatchResultCandidatesViewModel(MatchResultCandidatesModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Representative = model.Representative;
            Candidates = model.Candidates;
            SelectedCandidate = model.SelectedCandidate
                .ToReactivePropertyAsSynchronized(
                    m => m.Value,
                    oxp => oxp,
                    oxr => Candidates.DefaultIfNull(cs => oxr.Where(r => r is not null && cs.Contains(r)), Observable.Never<MsScanMatchResult>()).Switch(),
                    ignoreValidationErrorValue: true
                ).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<MsScanMatchResult?> Representative { get; }
        public ReactiveProperty<MsScanMatchResult?> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<IList<MsScanMatchResult>> Candidates { get; }
    }
}
