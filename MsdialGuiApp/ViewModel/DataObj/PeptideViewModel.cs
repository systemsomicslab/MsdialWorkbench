using CompMs.Common.DataObj;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Property;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    internal class PeptideViewModel
    {
        public PeptideViewModel(PeptideModel model)
        {
            _model = model;
            PeptideSeq = model.PeptideSeq;
            DatabaseOrigin = model.DatabaseOrigin;
            DatabaseOriginID = model.DatabaseOriginID;
            Formula = model.Formula;
            ExactMass = model.ExactMass;
            Position = model.Position;
        }
        private readonly PeptideModel _model;
        public string PeptideSeq { get; }
        public string DatabaseOrigin { get; }

        public object AnnotatedSpot => _model.AnnotatedSpot;

        public int DatabaseOriginID { get; }
        public Formula Formula { get; }
        public double ExactMass { get; }
        public Range Position { get; }

    }
}
