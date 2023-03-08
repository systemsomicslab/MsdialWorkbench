using CompMs.App.Msdial.Model.Information;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Information
{
    public sealed class MatchResultCandidatesViewModel : ViewModelBase
    {
        private readonly MatchResultCandidatesModel _model;

        public MatchResultCandidatesViewModel(MatchResultCandidatesModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Representative = model.Representative;
            SelectedCandidate = model.SelectedCandidate;
            Candidates = model.Candidates;
        }

        public ReadOnlyReactivePropertySlim<MsScanMatchResult> Representative { get; }
        public ReactiveProperty<MsScanMatchResult> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<IList<MsScanMatchResult>> Candidates { get; }
    }
}
