using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class MsScanMatchResultContainerModel : BindableBase
    {
        private readonly MsScanMatchResultContainer _container;
        private readonly ObservableCollection<MsScanMatchResult> _matchResults;

        public MsScanMatchResultContainerModel(MsScanMatchResultContainer container) {
            _container = container ?? throw new System.ArgumentNullException(nameof(container));
            _matchResults = new ObservableCollection<MsScanMatchResult>(container.MatchResults);
            MatchResults = new ReadOnlyObservableCollection<MsScanMatchResult>(_matchResults);
        }

        public ReadOnlyObservableCollection<MsScanMatchResult> MatchResults { get; }
    }
}
