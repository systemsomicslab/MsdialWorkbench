using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
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

        public MatchResultCandidatesModel(IObservable<MsScanMatchResultContainerModel?> containerOx, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            _containerOx = containerOx ?? throw new ArgumentNullException(nameof(containerOx));
            Representative = _containerOx.Select(ox => ox?.Representative).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Candidates = _containerOx.Select(ox => ox?.MatchResults.Select(r => Refer(r, refer)).ToList() ?? []).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            SelectedCandidate = Observable.CombineLatest(Representative, Candidates, (res, rr) => rr.FirstOrDefault(r => r.MatchResult == res)).ToReactiveProperty().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<MsScanMatchResult?> Representative { get; }
        public ReactiveProperty<ReferedReference?> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<List<ReferedReference>> Candidates { get; }

        public IObservable<Ms2ScanMatching?> GetCandidatesScorer(CompoundSearcherCollection compoundSearcherCollection) {
            return SelectedCandidate.Select(candidate => compoundSearcherCollection.GetMs2ScanMatching(candidate?.MatchResult));
        }

        public IObservable<T> RetryRefer<T>(IMatchResultRefer<T, MsScanMatchResult?> refer) where T: class? {
            return SelectedCandidate.Select(rr => (rr?.Reference as T) ?? refer.Refer(rr?.MatchResult));
        }

        private static ReferedReference Refer(MsScanMatchResult result, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new ReferedReference(result, refer.Refer(result));
        }
    }
}
