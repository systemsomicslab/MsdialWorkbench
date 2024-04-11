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
            AnnotatedSpot = model.AnnotatedSpot;
            AdductType = model.AdductType;
            PeptideSeq = model.PeptideSeq;
            DatabaseOrigin = model.DatabaseOrigin;
            DatabaseOriginID = model.DatabaseOriginID;
            ModifiedSequence = model.ModifiedSequence;
            Position = model.Position;
            Formula = model.Formula;
            ExactMass = model.ExactMass;
            IsProteinNterminal = model.IsProteinNterminal;
            IsProteinCterminal = model.IsProteinCterminal;
            IsDecoy = model.IsDecoy;
            MissedCleavages = model.MissedCleavages;
            SamePeptideNumberInSearchedProteins = model.SamePeptideNumberInSearchedProteins;

        }
        private readonly PeptideModel _model;
        public string PeptideSeq { get; }
        public string? AdductType { get; }
        public string ModifiedSequence { get; }
        public string DatabaseOrigin { get; }
        public int DatabaseOriginID { get; }
        public Formula Formula { get; }
        public double ExactMass { get; }
        public Range Position { get; }
        public bool IsProteinNterminal { get; }
        public bool IsProteinCterminal { get; }
        public bool IsDecoy { get; }
        public int MissedCleavages { get; }
        public int SamePeptideNumberInSearchedProteins { get; }

        //public object AnnotatedSpot => _model.AnnotatedSpot;
        public object? AnnotatedSpot { get; }
    }
}
