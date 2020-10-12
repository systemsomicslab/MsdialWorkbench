using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsAlignmentRefiner : AlignmentRefiner
    {
        public DimsAlignmentRefiner(MsdialDimsParameter param) : base(param) { }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments) {
            var spots = alignments.OrderBy(spot => spot.MassCenter).ToList();
            var master = new List<AlignmentSpotProperty> {
                new AlignmentSpotProperty { MassCenter = double.MinValue },
                new AlignmentSpotProperty { MassCenter = double.MaxValue } }; // add sentinel
            var ms1Tol = _param.Ms1AlignmentTolerance;

            master = MergeToMaster(spots.Where(spot => spot.MspID >= 0 && spot.IsReferenceMatched), master, ms1Tol);
            master = MergeToMaster(spots.Where(spot => spot.TextDbID >= 0 && spot.IsReferenceMatched), master, ms1Tol);
            master = MergeToMaster(spots.Where(spot => !spot.IsReferenceMatched && spot.PeakCharacter.IsotopeWeightNumber == 0), master, ms1Tol);

            return master.Skip(1).Take(master.Count - 2).ToList(); // skip sentinel
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
