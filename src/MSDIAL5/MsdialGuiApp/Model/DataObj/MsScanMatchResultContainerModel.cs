using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class MsScanMatchResultContainerModel : DisposableModelBase
    {
        private readonly MsScanMatchResultContainer _container;
        private readonly ObservableCollection<MsScanMatchResult> _matchResults;

        public MsScanMatchResultContainerModel(MsScanMatchResultContainer container) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _matchResults = new ObservableCollection<MsScanMatchResult>(container.MatchResults.Where(r => !r.IsUnknown));
            MatchResults = new ReadOnlyObservableCollection<MsScanMatchResult>(_matchResults);
            _matchResults.CollectionChangedAsObservable()
                .Subscribe(_ => OnPropertyChanged(nameof(Representative)))
                .AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<MsScanMatchResult> MatchResults { get; }

        public MsScanMatchResult Representative => _container.Representative;

        public List<MsScanMatchResult> GetManuallyResults() {
            return _container.GetManuallyResults();
        }

        public void RemoveResult(MsScanMatchResult result) {
            if (!_container.MatchResults.Contains(result)) {
                return;
            }
            _container.RemoveResult(result);
            _matchResults.Remove(result);
        }

        public void RemoveManuallyResults() {
            var removedResults = _container.GetManuallyResults();
            _container.RemoveManuallyResults();
            foreach (var result in removedResults) {
                _matchResults.Remove(result);
            }
        }

        public void AddResult(MsScanMatchResult result) {
            _container.AddResult(result);
            _matchResults.Add(result);
        }

        public void AddResults(IReadOnlyList<MsScanMatchResult> results) {
            _container.AddResults(results);
            foreach (var result in results) {
                _matchResults.Add(result);
            }
        }
    }
}
