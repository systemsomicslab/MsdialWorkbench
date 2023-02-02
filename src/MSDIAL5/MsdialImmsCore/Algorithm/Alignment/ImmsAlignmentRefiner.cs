using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Alignment
{
    public class ImmsAlignmentRefiner : AlignmentRefiner
    {
        private readonly MsdialImmsParameter parameter;
        public ImmsAlignmentRefiner(MsdialImmsParameter parameter, IupacDatabase iupac, IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(parameter, iupac, evaluator) {
            this.parameter = parameter;
        }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments) {
            var cSpots = new List<AlignmentSpotProperty>();
            var donelist = new HashSet<int>();

            foreach (var spot in alignments.Where(spot => spot.MspID >= 0 && spot.IsReferenceMatched(evaluator)).OrderByDescending(n => n.MspBasedMatchResult.TotalScore)) {
                TryMergeToMaster(spot, cSpots, donelist, parameter);
            }

            foreach (var spot in alignments.Where(spot => spot.IsReferenceMatched(evaluator)).OrderByDescending(spot => spot.MatchResults.Representative.TotalScore)) {
                TryMergeToMaster(spot, cSpots, donelist, parameter);
            }

            foreach (var spot in alignments.Where(spot => spot.TextDbID >= 0 && spot.IsReferenceMatched(evaluator)).OrderByDescending(n => n.TextDbBasedMatchResult.TotalScore)) {
                TryMergeToMaster(spot, cSpots, donelist, parameter);
            }

            foreach (var spot in alignments.OrderByDescending(n => n.HeightAverage)) {
                if (spot.IsReferenceMatched(evaluator)) continue;
                if (spot.PeakCharacter.IsotopeWeightNumber > 0) continue;
                TryMergeToMaster(spot, cSpots, donelist, parameter);
            }

            return cSpots;
        }

        private static void TryMergeToMaster(AlignmentSpotProperty spot, List<AlignmentSpotProperty> cSpots, HashSet<int> donelist, MsdialImmsParameter param) {
            if (donelist.Contains(spot.AlignmentID)) return;
            var spotDt = spot.TimesCenter.Value;
            var spotMz = spot.MassCenter;

            var dtTol = param.DriftTimeAlignmentTolerance;
            var ms1Tol = param.Ms1AlignmentTolerance;
            #region // practical parameter changes
            if (spotMz > 500) {
                var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(spotMz, ppm);
            }
            #endregion
            foreach (var cSpot in cSpots.Where(n => Math.Abs(n.MassCenter - spotMz) < ms1Tol)) {
                var cSpotDt = cSpot.TimesCenter.Value;
                if (Math.Abs(cSpotDt - spotDt) < dtTol * 0.5) return;
            }
            cSpots.Add(spot);
            donelist.Add(spot.AlignmentID);
        }

        protected override List<int> SetAlignmentID(List<AlignmentSpotProperty> alignments) {
            alignments.Sort((x, y) => x.MassCenter.CompareTo(y.MassCenter));

            var ids = new List<int>(alignments.Count);
            for (int i = 0; i < alignments.Count; i++) {
                ids.Add(alignments[i].MasterAlignmentID);
                alignments[i].MasterAlignmentID = alignments[i].AlignmentID = i;
            }

            return ids;
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) {
            //checking alignment spot variable correlations
            var dtMargin = 0.001F;
            AssignLinksByIonAbundanceCorrelations(alignments, dtMargin);

            // assigning peak characters from the identified spots
            AssignLinksByIdentifiedIonFeatures(alignments);

            // assigning peak characters from the representative file information
            alignments.Sort((x, y) => x.HeightAverage.CompareTo(y.HeightAverage));
            alignments.Reverse();
            AssignLinksByRepresentativeIonFeatures(alignments);

            // assign putative group IDs
            alignments.Sort((x, y) => x.AlignmentID.CompareTo(y.AlignmentID));
            AssignPutativePeakgroupIDs(alignments);
        }

        private static void AssignLinksByIonAbundanceCorrelations(List<AlignmentSpotProperty> alignSpots, float dtMargin) {
            if (alignSpots == null || alignSpots.Count == 0) return;
            if (alignSpots[0].AlignedPeakProperties == null || alignSpots[0].AlignedPeakProperties.Count == 0) return;

            if (alignSpots[0].AlignedPeakProperties.Count > 9) {
                alignSpots = alignSpots.OrderBy(n => n.TimesCenter.Value).ToList();
                foreach (var spot in alignSpots) {
                    if (spot.PeakCharacter.IsotopeWeightNumber > 0) continue;
                    var spotDt = spot.TimesCenter.Value;
                    var startScanIndex = SearchCollection.LowerBound(
                        alignSpots,
                        new AlignmentSpotProperty { TimesCenter = new ChromXs(spotDt - dtMargin - 0.0002f, ChromXType.Drift, ChromXUnit.Msec) },
                        (a, b) => a.TimesCenter.Value.CompareTo(b.TimesCenter.Value)
                        );

                    var searchedSpots = new List<AlignmentSpotProperty>();

                    for (int i = startScanIndex; i < alignSpots.Count; i++) {
                        if (spot.AlignmentID == alignSpots[i].AlignmentID) continue;
                        if (alignSpots[i].TimesCenter.Value < spotDt - dtMargin) continue;
                        if (alignSpots[i].PeakCharacter.IsotopeWeightNumber > 0) continue;
                        if (alignSpots[i].TimesCenter.Value > spotDt + dtMargin) break;

                        searchedSpots.Add(alignSpots[i]);
                    }

                    AlignmentSpotVariableCorrelationSearcher(spot, searchedSpots);
                }
            }
        }

        private static void AlignmentSpotVariableCorrelationSearcher(AlignmentSpotProperty spot, List<AlignmentSpotProperty> searchedSpots)
        {
            var sampleCount = spot.AlignedPeakProperties.Count;
            var spotPeaks = spot.AlignedPeakProperties;
            var peaks = spotPeaks.Select(peak => peak.PeakHeightTop).ToArray();

            foreach (var searchSpot in searchedSpots) {

                var searchedSpotPeaks = searchSpot.AlignedPeakProperties;
                var correlation = BasicMathematics.Coefficient(peaks, searchedSpotPeaks.Select(peak => peak.PeakHeightTop).ToArray());
                if (correlation >= 0.95) {
                    spot.AlignmentSpotVariableCorrelations.Add(
                        new AlignmentSpotVariableCorrelation() {
                            CorrelateAlignmentID = searchSpot.AlignmentID,
                            CorrelationScore = (float)correlation
                        });
                    spot.PeakCharacter.IsLinked = true;
                    spot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                        LinkedPeakID = searchSpot.AlignmentID,
                        Character = PeakLinkFeatureEnum.CorrelSimilar
                    });
                }
            }

            if (spot.AlignmentSpotVariableCorrelations.Count > 1)
                spot.AlignmentSpotVariableCorrelations = spot.AlignmentSpotVariableCorrelations.OrderBy(n => n.CorrelateAlignmentID).ToList();
        }
    }
}
