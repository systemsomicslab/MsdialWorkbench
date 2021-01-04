using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class RepresentativeSetter : ISpotAction
    {
        public void Process(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                foreach (var child in spot.AlignmentDriftSpotFeatures) {
                    DataObjConverter.SetRepresentativeProperty(child);
                }
                DataObjConverter.SetRepresentativeProperty(spot);
            }
        }
    }
}
