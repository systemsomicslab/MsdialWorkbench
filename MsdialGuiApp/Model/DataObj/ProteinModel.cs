using CompMs.Common.Components;
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
        public FastaProperty fastaProperty { get; }

        public ProteinModel(ProteinMsResult proteinResult, IReadOnlyList<ChromatogramPeakFeatureModel> orderedPeaks)
        {
            var pairs = proteinResult.MatchedPeptideResults.Select(result => (result, model: new PeptideModel(result, orderedPeaks))).ToList();
            var dictionary = pairs.ToDictionary(pair => pair.result, pair => pair.model);
            Peptides = new ObservableCollection<PeptideModel>(pairs.Select(pair => pair.model));
            UniquePeptides = new ObservableCollection<PeptideModel>(proteinResult.UniquePeptides.Select(result => dictionary[result]));
            DatabaseId = proteinResult.DatabaseID;
            fastaProperty = proteinResult.FastaProperty;
        }

        public ProteinModel(ProteinMsResult proteinResult, IReadOnlyList<AlignmentSpotPropertyModel> orderedSpots)
        {
            var pairs = proteinResult.MatchedPeptideResults.Select(result => (result, model: new PeptideModel(result, orderedSpots))).ToList();
            var dictionary = pairs.ToDictionary(pair => pair.result, pair => pair.model);
            Peptides = new ObservableCollection<PeptideModel>(pairs.Select(pair => pair.model));
            UniquePeptides = new ObservableCollection<PeptideModel>(proteinResult.UniquePeptides.Select(result => dictionary[result]));
            DatabaseId = proteinResult.DatabaseID;
            fastaProperty = proteinResult.FastaProperty;
        }
    }
}
