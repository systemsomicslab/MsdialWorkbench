using CompMs.App.Msdial.Model.DataObj;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    internal class ProteinGroupViewModel
    {
        public ProteinGroupViewModel(ProteinGroupModel model) {
            _model = model;
            GroupId = model.GroupID;
        }

        public int GroupId { get; }
        private readonly ProteinGroupModel _model;
    }

}
