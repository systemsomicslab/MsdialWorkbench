using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class ProteinModel : BindableBase
    {
        //private readonly ObservableCollection<PeptideModel> _peptides;
        public ObservableCollection<PeptideModel> UniquePeptides { get; }
        public ObservableCollection<PeptideModel> Peptides { get; }
        public string DatabaseId { get; }
        

        public ProteinModel(ProteinMsResult proteinResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            UniquePeptides = new ObservableCollection<PeptideModel>(proteinResult.GetUniquePeptides().Select(result => new PeptideModel(result, spots)));
            Peptides = new ObservableCollection<PeptideModel>(proteinResult.MatchedPeptideResults.Select(result => new PeptideModel(result, spots)));
            DatabaseId = proteinResult.DatabaseID;
        }

        public ProteinModel(ProteinMsResult proteinResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            UniquePeptides = new ObservableCollection<PeptideModel>(proteinResult.GetUniquePeptides().Select(result => new PeptideModel(result, spots)));
            Peptides = new ObservableCollection<PeptideModel>(proteinResult.MatchedPeptideResults.Select(result => new PeptideModel(result, spots)));
            DatabaseId = proteinResult.DatabaseID;
        }
    }
}
