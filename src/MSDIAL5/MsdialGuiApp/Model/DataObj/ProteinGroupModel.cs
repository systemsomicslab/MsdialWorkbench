using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class ProteinGroupModel : BindableBase
    {
        private readonly ObservableCollection<ProteinModel> _proteins;
        private readonly ObservableCollection<PeptideModel> _uniquePeptides;
        private readonly ObservableCollection<PeptideModel> _peptides;
        private readonly ProteinGroup _proteinGroup;

        public ProteinGroupModel(ProteinGroup proteinGroup, IReadOnlyList<ChromatogramPeakFeatureModel> orderedPeaks)
        {
            _proteinGroup = proteinGroup ?? throw new System.ArgumentNullException(nameof(proteinGroup));
            _proteins = new ObservableCollection<ProteinModel>(proteinGroup.ProteinMsResults.Select(result => new ProteinModel(result, orderedPeaks)));
            //_peptides = new PeptideGroupModel(proteinGroup.ProteinMsResults.Select(result => new PeptideGroupModel(result, spots)));
            _peptides = new ObservableCollection<PeptideModel>(_proteins.SelectMany(protein => protein.Peptides));

            _uniquePeptides = new ObservableCollection<PeptideModel>(_proteins.SelectMany(protein => protein.UniquePeptides));
            //_peptides = new ObservableCollection<PeptideGroupModel>(_proteins.SelectMany(protein => protein.Peptides));

            //_peptides = new ObservableCollection<PeptideModel>();
            //foreach(var protein in _proteins)
            //{
            //    foreach(var peptide in protein.Peptides)
            //    {
            //        _peptides.Add(peptide);
            //    }
            //}

        }

        public ProteinGroupModel(ProteinGroup proteinGroup, IReadOnlyList<AlignmentSpotPropertyModel> orderedSpots)
        {
            _proteinGroup = proteinGroup ?? throw new System.ArgumentNullException(nameof(proteinGroup));
            _proteins = new ObservableCollection<ProteinModel>(proteinGroup.ProteinMsResults.Select(result => new ProteinModel(result, orderedSpots)));
            _uniquePeptides = new ObservableCollection<PeptideModel>(_proteins.SelectMany(protein => protein.UniquePeptides));
            //_peptides = new ObservableCollection<PeptideGroupModel>(_proteins.SelectMany(protein => protein.Peptides));
            //_peptides = new ObservableCollection<PeptideModel>(proteinGroup.ProteinMsResults.Select(result => new PeptideGroupModel(result, spots).Peptides));
            _peptides = new ObservableCollection<PeptideModel>(_proteins.SelectMany(protein => protein.Peptides));
        }

        public int GroupID => _proteinGroup.GroupID;
        public int NoOfProteins => _proteins.Count;
        public int NoOfUniquePeptides => _uniquePeptides.Count;
        public int NoOfPeptides => _peptides.Count;
        public ObservableCollection<ProteinModel> ProteinModels => _proteins;
        public ObservableCollection<PeptideModel> PeptideModels => _peptides;

    }
}
