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
                    oxr => Candidates.DefaultIfNull(cs => oxr.Where(rr => rr is not null && cs.Contains(rr)), Observable.Never<ReferedReference>()).Switch(),
                    ignoreValidationErrorValue: true
                ).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<MsScanMatchResult?> Representative { get; }
        public ReactiveProperty<ReferedReference?> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<List<ReferedReference>> Candidates { get; }
    }
}
