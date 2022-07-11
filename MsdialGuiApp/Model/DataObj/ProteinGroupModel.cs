using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class ProteinGroupModel : BindableBase
    {
    {
        private readonly ObservableCollection<ProteinModel> _proteins;
        private readonly ObservableCollection<PeptideModel> _peptides;
        private readonly ProteinGroup _proteinGroup;

        public ProteinGroupModel(ProteinGroup proteinGroup, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            _proteinGroup = proteinGroup ?? throw new System.ArgumentNullException(nameof(proteinGroup));
            _proteins = new ObservableCollection<ProteinModel>(proteinGroup.ProteinMsResults.Select(result => new ProteinModel(result, spots)));
        }

        public ProteinGroupModel(ProteinGroup proteinGroup, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            _proteinGroup = proteinGroup ?? throw new System.ArgumentNullException(nameof(proteinGroup));
            _proteins = new ObservableCollection<ProteinModel>(proteinGroup.ProteinMsResults.Select(result => new ProteinModel(result, spots)));
        }

        public int GroupID => _proteinGroup.GroupID;
    }
}
