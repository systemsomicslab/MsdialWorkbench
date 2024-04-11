using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Information
{
    public sealed class MatchResultCandidatesModel : DisposableModelBase
    {
        private readonly IObservable<MsScanMatchResultContainerModel?> _containerOx;

        public MatchResultCandidatesModel(IObservable<MsScanMatchResultContainerModel?> containerOx) {
            _containerOx = containerOx ?? throw new ArgumentNullException(nameof(containerOx));
            Representative = _containerOx.Select(ox => ox?.Representative).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Candidates = _containerOx.Select(ox => (IList<MsScanMatchResult>?)ox?.MatchResults ?? new List<MsScanMatchResult>(0)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            SelectedCandidate = Representative.ToReactiveProperty().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<MsScanMatchResult?> Representative { get; }
        public ReactiveProperty<MsScanMatchResult?> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<IList<MsScanMatchResult>> Candidates { get; }

        public IObservable<Ms2ScanMatching?> GetCandidatesScorer(CompoundSearcherCollection compoundSearcherCollection) {
            return SelectedCandidate.Select(candidate => compoundSearcherCollection.GetMs2ScanMatching(candidate));
        }
    }
}
