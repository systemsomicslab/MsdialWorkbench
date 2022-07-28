using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class ProteinResultContainerModel : BindableBase
    {
        private readonly ProteinResultContainer _resultContainer;
        private readonly ObservableCollection<ProteinGroupModel> _proteinGroups;

        public ProteinResultContainerModel(ProteinResultContainer resultContainer, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            _resultContainer = resultContainer;
            _proteinGroups = new ObservableCollection<ProteinGroupModel>(resultContainer.ProteinGroups.Select(group => new ProteinGroupModel(group, spots)));

        }

        public ProteinResultContainerModel(ProteinResultContainer resultContainer, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            _resultContainer = resultContainer;
            _proteinGroups = new ObservableCollection<ProteinGroupModel>(resultContainer.ProteinGroups.Select(group => new ProteinGroupModel(group, spots)));
        }

        public ReadOnlyObservableCollection<ProteinGroupModel> ProteinGroups => new ReadOnlyObservableCollection<ProteinGroupModel>(_proteinGroups);
    }
}
