using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialDimsCore.DataObj
{
    public class DimsAlignmentRefiner : AlignmentRefiner
    {
        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments) {
            var spots = alignments.OrderBy(spot => spot.MassCenter).ToList();
            var master = new List<AlignmentSpotProperty> { new AlignmentSpotProperty { MassCenter = double.MaxValue } }; // add sentinel
            var ms1Tol = _param.Ms1AlignmentTolerance;

            master = MergeToMaster(spots.Where(spot => spot.MspID >= 0 && spot.IsReferenceMatched), master, ms1Tol);
            master = MergeToMaster(spots.Where(spot => spot.TextDbID >= 0 && spot.IsReferenceMatched), master, ms1Tol);
            master = MergeToMaster(spots.Where(spot => !spot.IsReferenceMatched && spot.PeakCharacter.IsotopeWeightNumber == 0), master, ms1Tol);

            master.RemoveAt(master.Count - 1); // remove sentinel

            return master.ToList();
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) {
            foreach ((var spot, var idx) in alignments.WithIndex())
                spot.MasterAlignmentID = spot.AlignmentID = idx;

            // AssignLinksByIdentifiedIonFeatures()

            AssignLinksByIdentifiedIonFeatures(alignments);

            alignments.Sort((x, y) => x.HeightAverage.CompareTo(y.HeightAverage)); // ?
            alignments.Reverse();
            AssignLinksByRepresentativeIonFeatures(alignments);

            alignments.Sort((x, y) => x.AlignmentID.CompareTo(y.AlignmentID));
            AssignPutativePeakgroupIDs(alignments);
        }

        protected override void PostProcess(List<AlignmentSpotProperty> alignments) { }

        private List<AlignmentSpotProperty> MergeToMaster(IEnumerable<AlignmentSpotProperty> spots, List<AlignmentSpotProperty> master, double ms1Tol) {
            var merged = new List<AlignmentSpotProperty>(master.Count);
            int i = 0;
            foreach (var spot in spots) {
                while (i < master.Count && master[i].MassCenter < spot.MassCenter)
                    merged.Add(master[i++]);
                if (merged[merged.Count - 1].MassCenter + ms1Tol <= spot.MassCenter && spot.MassCenter <= master[i].MassCenter - ms1Tol)
                    merged.Add(spot);
            }
            while (i < master.Count)
                merged.Add(master[i++]);
            return merged;
        }
    }
}
