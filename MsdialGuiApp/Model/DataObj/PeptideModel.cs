using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeptideModel {
        public object AnnotatedSpot { get; }
        //public object PeptideSeq { get; }
        private readonly PeptideMsResult _peptideMsResult;

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.InnerModel == peptideMsResult.ChromatogramPeakFeature);
            //PeptideSeq = peptideMsResult.Peptide.Sequence;
            //results = PeptideMsResult;
            //Console.WriteLine(results);
        }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.innerModel == peptideMsResult.AlignmentSpotProperty);
            //PeptideSeq = peptideMsResult.Peptide.Sequence;
            //results = PeptideMsResult;
            //Console.WriteLine(results);
        }

        public string PeptideSeq => _peptideMsResult.Peptide.Sequence;
    }
}
