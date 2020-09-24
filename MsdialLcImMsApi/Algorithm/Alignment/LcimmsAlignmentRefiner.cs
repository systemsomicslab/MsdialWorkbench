using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment
{
    public class LcimmsAlignmentRefiner : LcmsAlignmentRefiner
    {
        public LcimmsAlignmentRefiner(MsdialLcImMsParameter param) : base(param) { }

        protected override void SetAlignmentID(List<AlignmentSpotProperty> alignments) {
            alignments.Sort((x, y) => x.MassCenter.CompareTo(y.MassCenter));
            foreach (var spot in alignments) {
                spot.AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(spot.AlignmentDriftSpotFeatures.OrderBy(p => p.TimesCenter.Value));
            }

            var masterID = 0;
            for (int i = 0; i < alignments.Count; i++) {
                alignments[i].AlignmentID = i;
                alignments[i].MasterAlignmentID = masterID++;
                var driftSpots = alignments[i].AlignmentDriftSpotFeatures;
                for (int j = 0; j < driftSpots.Count; j++) {
                    driftSpots[j].MasterAlignmentID = masterID++;
                    driftSpots[j].AlignmentID = j;
                    driftSpots[j].ParentAlignmentID = i;
                }
            }
        }
    }
}
