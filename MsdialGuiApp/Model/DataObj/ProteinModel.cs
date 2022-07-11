using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class ProteinModel : BindableBase
    {
        private readonly ObservableCollection<PeptideModel> _peptides;

        public ProteinModel(ProteinMsResult proteinResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            _peptides = new ObservableCollection<PeptideModel>(proteinResult.MatchedPeptideResults.Select(result => new PeptideModel(result, spots)));
        }

        public ProteinModel(ProteinMsResult proteinResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            _peptides = new ObservableCollection<PeptideModel>(proteinResult.MatchedPeptideResults.Select(result => new PeptideModel(result, spots)));
        }
    }
}
