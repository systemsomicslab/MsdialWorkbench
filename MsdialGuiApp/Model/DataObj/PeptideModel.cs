using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeptideModel {
        public object AnnotatedSpot { get; }
        //public string PeptideSeq => _peptideMsResult.Peptide.Sequence;
        public string PeptideSeq { get; }
        public string DatabaseOrigin { get; }


        //public object PeptideSeq { get; }
        private readonly PeptideMsResult _peptideMsResult;

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.InnerModel.MasterPeakID == peptideMsResult.ChromatogramPeakFeature.MasterPeakID);
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
        }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.innerModel.MasterAlignmentID == peptideMsResult.AlignmentSpotProperty.MasterAlignmentID);
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
        }

    }
}
