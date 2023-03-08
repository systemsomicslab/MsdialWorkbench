using CompMs.App.Msdial.Model.DataObj;
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
        private readonly IObservable<MsScanMatchResultContainerModel> _containerOx;

        public MatchResultCandidatesModel(IObservable<MsScanMatchResultContainerModel> containerOx) {
            _containerOx = containerOx ?? throw new ArgumentNullException(nameof(containerOx));
            Candidates = _containerOx.Select(ox => (IList<MsScanMatchResult>)ox.MatchResults).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            SelectedCandidate = _containerOx.Select(ox => ox.Representative).ToReactiveProperty().AddTo(Disposables);
        }

        public ReactiveProperty<MsScanMatchResult> SelectedCandidate { get; }
        public ReadOnlyReactivePropertySlim<IList<MsScanMatchResult>> Candidates { get; }
    }
}
