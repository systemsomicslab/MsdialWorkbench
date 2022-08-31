using CompMs.MsdialCore.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeptideModel {
        public object AnnotatedSpot { get; }
        public string AdductType { get; }
        //public string PeptideSeq => _peptideMsResult.Peptide.Sequence;
        public string PeptideSeq { get; }
        public string DatabaseOrigin { get; }
        public int DatabaseOriginID { get; }
        public string ModifiedSequence { get; }
        public Range Position { get; }
        public double ExactMass { get; }
        public Formula Formula { get; }
        public bool IsProteinNterminal { get; }
        public bool IsProteinCterminal { get; }
        public bool IsDecoy { get; }
        public int MissedCleavages { get; }
        public int SamePeptideNumberInSearchedProteins { get; }
        public int CountModifiedAminoAcids { get; }


        //public object PeptideSeq { get; }
        private readonly PeptideMsResult _peptideMsResult;

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            var peak = spots.FirstOrDefault(spot => spot.InnerModel.MasterPeakID == peptideMsResult.ChromatogramPeakFeature.MasterPeakID);
            AnnotatedSpot = peak;
            AdductType = peak.AdductIonName;
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
            DatabaseOriginID = peptideMsResult.Peptide.DatabaseOriginID;
            ModifiedSequence = peptideMsResult.Peptide.ModifiedSequence;
            Formula = peptideMsResult.Peptide.Formula;
            Position = peptideMsResult.Peptide.Position;
            ExactMass = peptideMsResult.Peptide.ExactMass;
            IsProteinNterminal = peptideMsResult.Peptide.IsProteinNterminal;
            IsProteinCterminal = peptideMsResult.Peptide.IsProteinCterminal;
            IsDecoy = peptideMsResult.Peptide.IsDecoy;
            MissedCleavages = peptideMsResult.Peptide.MissedCleavages;
            SamePeptideNumberInSearchedProteins = peptideMsResult.Peptide.SamePeptideNumberInSearchedProteins;
            CountModifiedAminoAcids = peptideMsResult.Peptide.CountModifiedAminoAcids();
        }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            var peak = spots.FirstOrDefault(spot => spot.innerModel.MasterAlignmentID == peptideMsResult.AlignmentSpotProperty.MasterAlignmentID);
            AnnotatedSpot = peak;
            AdductType = peak.AdductIonName;
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
            DatabaseOriginID = peptideMsResult.Peptide.DatabaseOriginID;
            ModifiedSequence = peptideMsResult.Peptide.ModifiedSequence;
            Formula = peptideMsResult.Peptide.Formula;
            Position = peptideMsResult.Peptide.Position;
            ExactMass = peptideMsResult.Peptide.ExactMass;
            IsProteinNterminal = peptideMsResult.Peptide.IsProteinNterminal;
            IsProteinCterminal = peptideMsResult.Peptide.IsProteinCterminal;
            IsDecoy = peptideMsResult.Peptide.IsDecoy;
            MissedCleavages = peptideMsResult.Peptide.MissedCleavages;
            SamePeptideNumberInSearchedProteins = peptideMsResult.Peptide.SamePeptideNumberInSearchedProteins;
            CountModifiedAminoAcids = peptideMsResult.Peptide.CountModifiedAminoAcids();
        }
    }
}
