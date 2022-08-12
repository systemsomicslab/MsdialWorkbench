using CompMs.App.Msdial.Model.DataObj;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    internal class ProteinGroupViewModel
    {
        public ProteinGroupViewModel(ProteinGroupModel model) {
            _model = model;
            GroupId = model.GroupID;
            NumberOfProteins = model.NoOfProteins;
            NumberOfUniquePeptides = model.NoOfUniquePeptides;
            NumberOfPeptides = model.NoOfPeptides;
            //ProteinViewModels = model.ProteinModels
            ProteinViewModels = new ObservableCollection<ProteinViewModel>(model.ProteinModels.Select(protein => new ProteinViewModel(protein)));
            PeptideViewModels = new ObservableCollection<PeptideViewModel>(model.PeptideModels.Select(peptide => new PeptideViewModel(peptide)));
        }

        public int GroupId { get; }
        private readonly ProteinGroupModel _model;
        public int NumberOfProteins { get; }
        public int NumberOfUniquePeptides { get; }
        public int NumberOfPeptides { get; }
        public ObservableCollection<ProteinViewModel> ProteinViewModels { get; }
        public ObservableCollection<PeptideViewModel> PeptideViewModels { get; }
    }

}
