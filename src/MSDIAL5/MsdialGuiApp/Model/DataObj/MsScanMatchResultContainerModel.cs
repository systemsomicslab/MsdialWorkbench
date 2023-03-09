using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class MsScanMatchResultContainerModel : BindableBase
    {
        private readonly MsScanMatchResultContainer _container;
        private readonly ObservableCollection<MsScanMatchResult> _matchResults;

        public MsScanMatchResultContainerModel(MsScanMatchResultContainer container) {
            _container = container ?? throw new System.ArgumentNullException(nameof(container));
            _matchResults = new ObservableCollection<MsScanMatchResult>(container.MatchResults);
            MatchResults = new ReadOnlyObservableCollection<MsScanMatchResult>(_matchResults);
        }

        public ReadOnlyObservableCollection<MsScanMatchResult> MatchResults { get; }

        public MsScanMatchResult Representative => _container.Representative;

        public void RemoveManuallyResults() {
            _container.RemoveManuallyResults();
            var removedResults = new List<MsScanMatchResult>();
            foreach (var result in _matchResults) {
                if (!_container.MatchResults.Contains(result)) {
                    removedResults.Add(result);
                }
            }
            foreach (var result in removedResults) {
                _matchResults.Remove(result);
            }
        }

        public void AddResult(MsScanMatchResult result) {
            _container.AddResult(result);
            _matchResults.Add(result);
        }
    }
}
