using CompMs.App.Msdial.Model.DataObj;

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
        }

        public int GroupId { get; }
        private readonly ProteinGroupModel _model;
        public int NumberOfProteins { get; }
        public int NumberOfUniquePeptides { get; }
        public int NumberOfPeptides { get; }
    }

}
