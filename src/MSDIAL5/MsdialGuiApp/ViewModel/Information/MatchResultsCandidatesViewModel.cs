using CompMs.App.Msdial.Model.Information;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Information
{
    public sealed class MatchResultsCandidatesViewModel : ViewModelBase
    {
        private readonly MatchResultCandidatesModel _model;

        public MatchResultsCandidatesViewModel(MatchResultCandidatesModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            SelectedCandidate = model.SelectedCandidate;
            Candidates = model.Candidates;
        }

        public ReactiveProperty<MsScanMatchResult> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<IList<MsScanMatchResult>> Candidates { get; }
    }
}
