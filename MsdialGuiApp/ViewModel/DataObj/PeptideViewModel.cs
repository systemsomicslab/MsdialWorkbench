using CompMs.App.Msdial.Model.DataObj;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    internal class PeptideViewModel
    {
        public PeptideViewModel(PeptideModel model)
        {
            _model = model;
            PeptideSeq = model.PeptideSeq;
            DatabaseOrigin = model.DatabaseOrigin;
        }
        private readonly PeptideModel _model;
        public string PeptideSeq { get; }
        public string DatabaseOrigin { get; }

        public object AnnotatedSpot => _model.AnnotatedSpot;

    }
}
