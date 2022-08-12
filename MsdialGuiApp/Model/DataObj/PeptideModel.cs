using CompMs.MsdialCore.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeptideModel {
        public object AnnotatedSpot { get; }
        //public string PeptideSeq => _peptideMsResult.Peptide.Sequence;
        public string PeptideSeq { get; }
        public string DatabaseOrigin { get; }
        public int DatabaseOriginID { get; }
        public string ModifiedSequence { get; }
        public Range Position { get; }
        public double ExactMass { get; }
        public Formula Formula { get; }

        //public object PeptideSeq { get; }
        private readonly PeptideMsResult _peptideMsResult;

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.InnerModel.MasterPeakID == peptideMsResult.ChromatogramPeakFeature.MasterPeakID);
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
            DatabaseOriginID = peptideMsResult.Peptide.DatabaseOriginID;
            ModifiedSequence = peptideMsResult.Peptide.ModifiedSequence;
            Formula = peptideMsResult.Peptide.Formula;
            Position = peptideMsResult.Peptide.Position;
            ExactMass = peptideMsResult.Peptide.ExactMass;
        }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.innerModel.MasterAlignmentID == peptideMsResult.AlignmentSpotProperty.MasterAlignmentID);
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
            DatabaseOriginID = peptideMsResult.Peptide.DatabaseOriginID;
            ModifiedSequence = peptideMsResult.Peptide.ModifiedSequence;
            Formula = peptideMsResult.Peptide.Formula;
            Position = peptideMsResult.Peptide.Position;
            ExactMass = peptideMsResult.Peptide.ExactMass;
        }

    }
}
