using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsAlignmentRefiner : AlignmentRefiner
    {
        public DimsAlignmentRefiner(MsdialDimsParameter param, IupacDatabase iupac) : base(param, iupac) { }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> spots) {
            var master = new Dictionary<int, List<AlignmentSpotProperty>>();
            var ms1Tol = _param.Ms1AlignmentTolerance;

            MergeToMaster(spots.Where(spot => spot.MspID >= 0 && spot.IsReferenceMatched).OrderByDescending(n => n.MspBasedMatchResult.TotalScore), master, ms1Tol);
            MergeToMaster(spots.Where(spot => spot.TextDbID >= 0 && spot.IsReferenceMatched).OrderByDescending(n => n.TextDbBasedMatchResult.TotalScore), master, ms1Tol);
            MergeToMaster(spots.Where(spot => !spot.IsReferenceMatched && spot.PeakCharacter.IsotopeWeightNumber <= 0).OrderByDescending(n => n.HeightAverage), master, ms1Tol);

            return master.Values.SelectMany(props => props).OrderBy(spot => spot.MassCenter).ToList();
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

        private static void MergeToMaster(IEnumerable<AlignmentSpotProperty> spots, Dictionary<int, List<AlignmentSpotProperty>> master, double ms1Tol) {
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            var tol = ms1Tol;
            foreach (var spot in spots) {

                #region // practical parameter changes
                if (spot.MassCenter > 500) {
                    tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(spot.MassCenter, ppm);
                }
                #endregion

                if (!ExistsSimilar(spot.MassCenter, master, tol)) {
                    var massKey = (int)spot.MassCenter;
                    if (!master.ContainsKey(massKey))
                        master[massKey] = new List<AlignmentSpotProperty>();
                    master[massKey].Add(spot);
                }
            }
        }

        private static bool ExistsSimilar(double mass, Dictionary<int, List<AlignmentSpotProperty>> master, double tol) {
            var massKey = (int)mass;
            var t = (int)Math.Ceiling(tol);
            foreach (var keyTol in Enumerable.Range(-t, t * 2 + 1)) { // -t to + t
                if (!master.ContainsKey(massKey + keyTol))
                    continue;
                foreach (var prop in master[massKey + keyTol]) {
                    if (prop.MassCenter - tol <= mass && mass <= prop.MassCenter + tol) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
