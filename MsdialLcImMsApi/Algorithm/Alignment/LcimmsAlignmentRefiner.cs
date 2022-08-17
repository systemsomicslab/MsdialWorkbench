using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment
{
    public class LcimmsAlignmentRefiner : LcmsAlignmentRefiner
    {
        public LcimmsAlignmentRefiner(MsdialLcImMsParameter parameter, IupacDatabase iupac, IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(parameter, iupac, evaluator, null) { }

        protected override List<int> SetAlignmentID(List<AlignmentSpotProperty> alignments) {
            alignments.Sort((x, y) => x.MassCenter.CompareTo(y.MassCenter));
            foreach (var spot in alignments) {
                spot.AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(spot.AlignmentDriftSpotFeatures.OrderBy(p => p.TimesCenter.Value));
            }

            var masterID = 0;
            var ids = new List<int>();
            for (int i = 0; i < alignments.Count; i++) {
                ids.Add(alignments[i].MasterAlignmentID);
                alignments[i].AlignmentID = i;
                alignments[i].MasterAlignmentID = masterID++;
                var driftSpots = alignments[i].AlignmentDriftSpotFeatures;
                for (int j = 0; j < driftSpots.Count; j++) {
                    ids.Add(driftSpots[j].MasterAlignmentID);
                    driftSpots[j].MasterAlignmentID = masterID++;
                    driftSpots[j].AlignmentID = j;
                    driftSpots[j].ParentAlignmentID = i;
                }
            }

            return ids;
        }
    }
}
