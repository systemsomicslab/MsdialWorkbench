using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeptideModel {
        public object AnnotatedSpot { get; }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<ChromatogramPeakFeatureModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.InnerModel == peptideMsResult.ChromatogramPeakFeature);
            //results = PeptideMsResult;
            //Console.WriteLine(results);
        }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<AlignmentSpotPropertyModel> spots)
        {
            AnnotatedSpot = spots.FirstOrDefault(spot => spot.innerModel == peptideMsResult.AlignmentSpotProperty);
            //results = PeptideMsResult;
            //Console.WriteLine(results);
        }
    }
}
