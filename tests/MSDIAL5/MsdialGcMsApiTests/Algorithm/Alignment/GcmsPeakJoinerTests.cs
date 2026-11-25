using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment.Tests {
    [TestClass()]
    public class GcmsPeakJoinerTests {
        private const double RiTolerance = 10d;

        private static readonly (double Mz, double Intensity)[] SpectrumA = [(73d, 1000d), (147d, 600d), (221d, 300d), (295d, 150d)];
        private static readonly (double Mz, double Intensity)[] SpectrumB = [(86d, 1000d), (130d, 500d), (174d, 250d), (218d, 120d)];
        // Shares two of SpectrumA's four peaks: an integrated EI similarity to SpectrumA of about 0.67,
        // and the default dot-product cutoffs alone do not call it a match (see the premise test).
        private static readonly (double Mz, double Intensity)[] SpectrumAInPart = [(73d, 1000d), (147d, 600d), (117d, 900d), (191d, 700d), (265d, 400d)];
        // SpectrumA's four peaks with other intensities: the default dot-product cutoffs call it a match.
        private static readonly (double Mz, double Intensity)[] SpectrumAReweighed = [(73d, 1000d), (147d, 250d), (221d, 700d), (295d, 40d)];

        private string _directory = string.Empty;

        [TestInitialize]
        public void Initialize() {
            _directory = Path.Combine(Path.GetTempPath(), "GcmsPeakJoinerTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_directory)) {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [TestMethod]
        public void RIJoinerAlignsEveryPeakWhenTheReferenceFileIsNotInRetentionIndexOrder() {
            // The reference file lists the later compound first, so the master list, built from
            // the reference file's RI buckets in insertion order, is not in (RI, Mass) order unless
            // the joiner sorts it before its binary search.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1500d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1000d, height: 1000d, SpectrumB),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumB),
                    CreateFeature(scanId: 1, ri: 1501d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(2, spots.Count);
            var byRi = spots.OrderBy(s => s.AlignedPeakProperties[0].ChromXsTop.RI.Value).ToArray();
            Assert.AreEqual(1000d, byRi[0].AlignedPeakProperties[0].ChromXsTop.RI.Value);
            Assert.AreEqual(1002d, byRi[0].AlignedPeakProperties[1].ChromXsTop.RI.Value);
            Assert.AreEqual(1500d, byRi[1].AlignedPeakProperties[0].ChromXsTop.RI.Value);
            Assert.AreEqual(1501d, byRi[1].AlignedPeakProperties[1].ChromXsTop.RI.Value);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.All(p => p.MasterPeakID >= 0)));
        }

        [TestMethod]
        public void RIJoinerKeepsTheBestMatchingPeakOfAFileForASpot() {
            // Both peaks of file 1 match the same master; the first is the closer one in RI and so
            // scores higher. A later, weaker peak must not overwrite it.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1001d, height: 5000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1008d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(1, spots.Count);
            var peak = spots[0].AlignedPeakProperties[1];
            Assert.AreEqual(0, peak.PeakID);
            Assert.AreEqual(1001d, peak.ChromXsTop.RI.Value);
            Assert.AreEqual(5000d, peak.PeakHeightTop);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SpectrumAInPartScoresAbout067OnEIAloneAndPassesNoDotProductCutoff(bool isUseRetentionIndex) {
            // The premise of the threshold tests below. 2 RI units (0.02 min) apart, a retention
            // similarity of 0.98 for either tolerance; with the default factors of 0.5 the total
            // score is about 0.82, while the integrated EI similarity alone is about 0.67. Only two
            // peaks are shared, fewer than the three the dot-product cutoffs need, so only the
            // threshold on the integrated EI similarity decides whether the two match.
            var parameter = new MsRefSearchParameterBase { Ms1Tolerance = .5f, Ms2Tolerance = .5f, MassRangeBegin = 0, MassRangeEnd = 2000, };
            var a = CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA).AnnotatedMSDecResult.MSDecResult;
            var b = CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart).AnnotatedMSDecResult.MSDecResult;

            var result = MsScanMatching.CompareEIMSScanProperties(a, b, parameter, rtTol: .1, riTol: RiTolerance, eiFactor: .5, riFactor: .5, isUseRetentionIndex: isUseRetentionIndex);

            var ei = MsScanMatching.GetIntegratedSpectraSimilarity(result);
            Assert.IsTrue(ei > .6 && ei < .7, $"integrated EI similarity {ei}");
            Assert.IsTrue(result.TotalScore > .8 && result.TotalScore < .85, $"total score {result.TotalScore}");
            Assert.IsTrue(result.IsRiMatch && result.IsRtMatch);
            Assert.IsFalse(result.IsSpectrumMatch, "the dot-product cutoffs alone do not call it a match");
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void JoinerKeepsSpectraBelowTheSpectrumSimilarityToleranceApart(AlignmentIndexType indexType) {
            // A method file written like an LC-MS one carries an m/z tolerance in "MS1 tolerance for
            // alignment" (0.015). The joiner took its threshold from there, which admits any spectrum
            // within the retention window.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.Ms1AlignmentTolerance = .015f;
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .9f;
            });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 1));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void JoinerJoinsByTheEISimilarityAboveTheSpectrumSimilarityTolerance(AlignmentIndexType indexType) {
            // Integrated EI similarity about 0.67: joined at 0.6, which a threshold taken from the MS1
            // tolerance (0.9 here) would not do.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.Ms1AlignmentTolerance = .9f;
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .6f;
            });

            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void TheToleranceAppliesToTheEISimilarityNotToTheTotalScore(AlignmentIndexType indexType) {
            // Total score about 0.82, integrated EI similarity about 0.67: kept apart at 0.7, which a
            // threshold on the total score would join.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .7f;
            });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 1));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void RetentionIsJudgedByItsToleranceAloneAndDoesNotLowerTheSpectrumMatch(AlignmentIndexType indexType) {
            // The same spectrum 9 RI units (0.09 min) away, inside the retention tolerance: retention
            // similarity 0.67, total score 0.83, integrated EI similarity 1.0. Joined at 0.9, which a
            // threshold on the total score would not do.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1009d, height: 1000d, SpectrumA), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .9f;
            });

            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void TheSameSpectrumBeyondTwiceTheRetentionToleranceIsKeptApart(AlignmentIndexType indexType) {
            // 25 RI units (0.25 min) away: outside both retention tolerances (10 RI units, 0.1 min),
            // and beyond twice them, within which two spots split by retention drift are joined.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1025d, height: 1000d, SpectrumA), },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 1));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void SpotsOfOneCompoundSplitByRetentionDriftAreJoined(AlignmentIndexType indexType) {
            // 12 RI units (0.12 min) away, outside the retention tolerance: the joiner makes a master
            // of each, and the two spots, with the same spectrum and no file in common, are joined.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1012d, height: 1000d, SpectrumA), },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
            Assert.AreEqual(1000d, spots[0].AlignedPeakProperties[0].ChromXsTop.RI.Value);
            Assert.AreEqual(1012d, spots[0].AlignedPeakProperties[1].ChromXsTop.RI.Value);
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void ACompoundSplitThreeWaysIsJoinedIntoOneSpot(AlignmentIndexType indexType) {
            // Each file 12 RI units (0.12 min) later than the one before: three masters, joined in two
            // rounds (the first two spots, then their joined spot, centred at 1006, with the third).
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1012d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1024d, height: 1000d, SpectrumA), },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void SpotsWithPeaksOfOneFileInBothAreNotJoined(AlignmentIndexType indexType) {
            // File 2 has a peak of this spectrum in each spot: two compounds of one spectrum (isomers),
            // or two components of one, and joining the spots would drop one of file 2's peaks.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1012d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1012d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 2));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void SpotsOfSpectraBelowTheDotProductCutoffsAreNotJoined(AlignmentIndexType indexType) {
            // Integrated EI similarity about 0.67, above the GC-MS spectrum similarity tolerance (0.5)
            // that joins features within the retention tolerance, but below the dot-product cutoffs
            // that joining two spots asks for.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1012d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 1));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void TheGcmsDefaultJoinsAPartlyMatchingSpectrumOfTheSameRetention(AlignmentIndexType indexType) {
            // Integrated EI similarity about 0.67, as two spectra of one compound in two files often
            // score: the GC-MS default (0.5 on the integrated EI similarity) joins them.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(.5f, new MsdialGcmsParameter().AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void TheDotProductCutoffsDoNotGrantAMatchBelowTheTolerance(AlignmentIndexType indexType) {
            // SpectrumA's peaks with other intensities pass the fixed dot-product cutoffs, which set
            // IsSpectrumMatch whatever the tolerance. The joiner reads only the integrated EI similarity
            // (about 0.92 here), so at a tolerance of 0.95 the two stay apart; with the cutoffs OR'ed in they joined.
            var parameter = new MsRefSearchParameterBase { Ms1Tolerance = .5f, Ms2Tolerance = .5f, MassRangeBegin = 0, MassRangeEnd = 2000, };
            var a = CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA).AnnotatedMSDecResult.MSDecResult;
            var b = CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAReweighed).AnnotatedMSDecResult.MSDecResult;
            var premise = MsScanMatching.CompareEIMSScanProperties(a, b, parameter, rtTol: .1, riTol: RiTolerance, eiFactor: .5, riFactor: .5, isUseRetentionIndex: indexType == AlignmentIndexType.RI);
            var ei = MsScanMatching.GetIntegratedSpectraSimilarity(premise);
            Assert.IsTrue(premise.IsSpectrumMatch, $"the dot-product cutoffs pass: weighted {premise.WeightedDotProduct}, simple {premise.SimpleDotProduct}, reverse {premise.ReverseDotProduct}, matched {premise.MatchedPeaksPercentage} / {premise.MatchedPeaksCount}");
            Assert.IsTrue(ei < .95, $"integrated EI similarity {ei}");
            var selfA = MsScanMatching.GetIntegratedSpectraSimilarity(MsScanMatching.CompareEIMSScanProperties(a, a, parameter, rtTol: .1, riTol: RiTolerance, eiFactor: .5, riFactor: .5, isUseRetentionIndex: indexType == AlignmentIndexType.RI));
            var selfB = MsScanMatching.GetIntegratedSpectraSimilarity(MsScanMatching.CompareEIMSScanProperties(b, b, parameter, rtTol: .1, riTol: RiTolerance, eiFactor: .5, riFactor: .5, isUseRetentionIndex: indexType == AlignmentIndexType.RI));
            // A spectrum compared with itself scores below 1 on the integrated similarity (about 0.99 and 0.97 here),
            // so each feature still joins its own master at 0.95.
            Assert.IsTrue(selfA > .95 && selfB > .95, $"self-similarity {selfA}, {selfB}");
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAReweighed), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .95f;
            });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 1));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI, .9f, 1008d)]
        [DataRow(AlignmentIndexType.RI, .1f, 1000d)]
        [DataRow(AlignmentIndexType.RT, .9f, 1008d)]
        [DataRow(AlignmentIndexType.RT, .1f, 1000d)]
        public void JoinerWeighsTheEISimilarityBySpectrumSimilarityFactor(AlignmentIndexType indexType, float eiFactor, double expectedMasterRi) {
            // The peak of file 1 is similar to both masters: one at the same retention whose spectrum
            // matches in part, one 8 RI units (0.08 min) away with the same spectrum. Which one it
            // joins depends on how the EI similarity is weighed against the retention similarity in
            // the total score that chooses between similar masters.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumAInPart),
                    CreateFeature(scanId: 1, ri: 1008d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.Ms1AlignmentTolerance = .5f;
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .5f;
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = eiFactor;
                p.AlignmentBaseParam.RetentionTimeAlignmentFactor = 1f - eiFactor;
                // At the other extreme, so that a joiner weighing by the MS1 factor joins the other master.
                p.AlignmentBaseParam.Ms1AlignmentFactor = eiFactor > .5f ? 0f : 1f;
            });

            Assert.AreEqual(2, spots.Count);
            var joined = spots.Single(s => s.AlignedPeakProperties[1].MasterPeakID >= 0);
            Assert.AreEqual(expectedMasterRi, joined.AlignedPeakProperties[0].ChromXsTop.RI.Value);
        }

        [TestMethod]
        public void RIJoinerLetsAPeakJoinAMasterAnEarlierPeakOnlyPassedOver() {
            // Two masters with the same spectrum, 6 RI apart. File 1's first peak lies near both and
            // joins the closer one (RI 1006); on its way it scores the farther one (RI 1000) too. Its
            // second peak, at RI 994, can only join the master at RI 1000, with a lower score than the
            // first peak had there. That master must still take it: the first peak did not join it.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1006d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1005d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 994d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(2, spots.Count);
            var byRi = spots.OrderBy(s => s.AlignedPeakProperties[0].ChromXsTop.RI.Value).ToArray();
            Assert.AreEqual(1, byRi[0].AlignedPeakProperties[1].PeakID, "the peak at RI 994 joins the master at RI 1000");
            Assert.AreEqual(994d, byRi[0].AlignedPeakProperties[1].ChromXsTop.RI.Value);
            Assert.AreEqual(0, byRi[1].AlignedPeakProperties[1].PeakID, "the peak at RI 1005 joins the master at RI 1006");
        }

        [TestMethod]
        public void RTJoinerLetsAPeakJoinAMasterAnEarlierPeakOnlyPassedOver() {
            // The retention-time version of the case above, 0.06 min apart with a 0.1 min tolerance.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1006d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1005d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 994d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data, AlignmentIndexType.RT);

            Assert.AreEqual(2, spots.Count);
            var byRt = spots.OrderBy(s => s.AlignedPeakProperties[0].ChromXsTop.RT.Value).ToArray();
            Assert.AreEqual(1, byRt[0].AlignedPeakProperties[1].PeakID, "the peak at 9.94 min joins the master at 10.00 min");
            Assert.AreEqual(0, byRt[1].AlignedPeakProperties[1].PeakID, "the peak at 10.05 min joins the master at 10.06 min");
        }

        [TestMethod]
        public void RIJoinerLetsAPeakJoinAMasterAnEarlierPeakOnlyPassedOverInRetentionOrder() {
            // The case above with file 1's peaks in ascending RI, the order a file lists them in.
            // The peak at RI 1005 scores both masters and joins the closer one (RI 1006). The peak at
            // RI 1007 scores the master at RI 1006 no higher than the peak that joined it, so it can
            // only join the master at RI 1000, with a lower score there than the first peak had. That
            // master must take it: the first peak did not join it.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1006d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1005d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1007d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(2, spots.Count);
            var byRi = spots.OrderBy(s => s.TimesCenter.RI.Value).ToArray();
            Assert.AreEqual(1, byRi[0].AlignedPeakProperties[1].PeakID, "the peak at RI 1007 joins the master at RI 1000");
            Assert.AreEqual(1007d, byRi[0].AlignedPeakProperties[1].ChromXsTop.RI.Value);
            Assert.AreEqual(0, byRi[1].AlignedPeakProperties[1].PeakID, "the peak at RI 1005 joins the master at RI 1006");
            Assert.AreEqual(1005d, byRi[1].AlignedPeakProperties[1].ChromXsTop.RI.Value);
        }

        [TestMethod]
        public void RTJoinerLetsAPeakJoinAMasterAnEarlierPeakOnlyPassedOverInRetentionOrder() {
            // The retention-time version of the case above, with a 0.1 min tolerance.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1006d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1005d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1007d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data, AlignmentIndexType.RT);

            Assert.AreEqual(2, spots.Count);
            var byRt = spots.OrderBy(s => s.TimesCenter.RT.Value).ToArray();
            Assert.AreEqual(1, byRt[0].AlignedPeakProperties[1].PeakID, "the peak at 10.07 min joins the master at 10.00 min");
            Assert.AreEqual(0, byRt[1].AlignedPeakProperties[1].PeakID, "the peak at 10.05 min joins the master at 10.06 min");
        }

        [TestMethod]
        public void RIJoinerMakesEachOfTwoSimilarPeaksFirstSeenInANonReferenceFileAMaster() {
            // File 1 has two peaks with the same spectrum, 6 RI apart, that the reference file lacks.
            // Compared with the masters from earlier files only, neither is covered, so both become
            // masters. Had the first become a master at once, the second would have looked covered by
            // it, become no master, and joined no spot.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1500d, height: 1000d, SpectrumB),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1006d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 2, ri: 1500d, height: 1000d, SpectrumB),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(3, spots.Count);
            var byRi = spots.OrderBy(s => s.TimesCenter.RI.Value).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, byRi.Select(s => s.AlignedPeakProperties[1].PeakID).ToArray());
            CollectionAssert.AreEqual(new[] { -1, -1, 0 }, byRi.Select(s => s.AlignedPeakProperties[0].PeakID).ToArray());
        }

        [TestMethod]
        public void RTJoinerMakesEachOfTwoSimilarPeaksFirstSeenInANonReferenceFileAMaster() {
            // The retention-time version of the case above, 0.06 min apart with a 0.1 min tolerance.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1500d, height: 1000d, SpectrumB),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1006d, height: 1000d, SpectrumA),
                    CreateFeature(scanId: 2, ri: 1500d, height: 1000d, SpectrumB),
                },
            };

            var spots = Join(data, AlignmentIndexType.RT);

            Assert.AreEqual(3, spots.Count);
            var byRt = spots.OrderBy(s => s.TimesCenter.RT.Value).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, byRt.Select(s => s.AlignedPeakProperties[1].PeakID).ToArray());
            CollectionAssert.AreEqual(new[] { -1, -1, 0 }, byRt.Select(s => s.AlignedPeakProperties[0].PeakID).ToArray());
        }

        [TestMethod]
        public void RIJoinerJoinsAPeakWhoseQuantPeakTopIsOutsideTheToleranceWhenItsMSDecRetentionIsInside() {
            // File 1's peak has its MSDec result at RI 1008, within the tolerance (10) of the master at RI 1000, but
            // the top of its quant-mass peak at RI 1012, outside it. The joiner scores the MSDec retention, so the
            // peak covers the master and makes no master of its own; the search window must open on the same
            // retention, or the peak is never compared with that master and joins no spot.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1008d, height: 1000d, SpectrumA, quantTopRi: 1012d),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[1].PeakID, "the peak joins the master at RI 1000");
            Assert.AreEqual(1012d, spots[0].AlignedPeakProperties[1].ChromXsTop.RI.Value, "the aligned peak keeps its quant-peak top");
        }

        [TestMethod]
        public void RTJoinerJoinsAPeakWhoseQuantPeakTopIsOutsideTheToleranceWhenItsMSDecRetentionIsInside() {
            // The retention-time version of the case above: MSDec result at 10.08 min, quant-peak top at 10.12 min,
            // master at 10.00 min, tolerance 0.1 min.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1008d, height: 1000d, SpectrumA, quantTopRi: 1012d),
                },
            };

            var spots = Join(data, AlignmentIndexType.RT);

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[1].PeakID, "the peak joins the master at 10.00 min");
        }

        [TestMethod]
        public void RIJoinerMakesNoSecondMasterOfAPeakWhoseMSDecRetentionMatchesAMaster() {
            // File 1's peak has its MSDec result at RI 1009, within the tolerance of the master at RI 1000, and the
            // top of its quant-mass peak at RI 1025. Looked up by the quant-peak top it was not near the master,
            // became a second master, and the two files' peaks of one compound ended in two spots.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1009d, height: 1000d, SpectrumA, quantTopRi: 1025d),
                },
            };

            var spots = Join(data);

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[1].PeakID);
        }

        [TestMethod]
        public void RTJoinerMakesNoSecondMasterOfAPeakWhoseMSDecRetentionMatchesAMaster() {
            // The retention-time version of the case above: MSDec result at 10.09 min, quant-peak top at 10.25 min,
            // master at 10.00 min, tolerance 0.1 min.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1009d, height: 1000d, SpectrumA, quantTopRi: 1025d),
                },
            };

            var spots = Join(data, AlignmentIndexType.RT);

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[1].PeakID);
        }

        [DataTestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void JoinerSortsTheMastersOnTheRetentionItSearches(AlignmentIndexType indexType) {
            // Master P: MSDec RI 1000, quant top 1030. Master Q: MSDec RI 1040, quant top 1005, another spectrum.
            // Sorted on the quant top the list is [Q, P]; the search for file 1's peak (MSDec RI 1002) would then start
            // at Q, find it beyond the window and stop before P. Sorted on the MSDec retention it reaches P.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA, quantTopRi: 1030d),
                    CreateFeature(scanId: 1, ri: 1040d, height: 1000d, SpectrumB, quantTopRi: 1005d),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumA),
                },
            };

            var spots = Join(data, indexType);

            Assert.AreEqual(2, spots.Count);
            var p = spots.Single(s => s.AlignedPeakProperties[0].PeakID == 0);
            Assert.AreEqual(0, p.AlignedPeakProperties[1].PeakID, "file 1's peak joins master P");
        }

        [DataTestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void JoinerDoesNotStopItsScanOnAQuantPeakTopBeforeTheMSDecRetention(AlignmentIndexType indexType) {
            // File 1's peak has its MSDec result at RI 1008 (within 10 of the master at RI 1000) but its quant-peak top
            // at RI 985. A scan that stopped on the quant top (985 + 10 < 1000) would never compare the master.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1008d, height: 1000d, SpectrumA, quantTopRi: 985d),
                },
            };

            var spots = Join(data, indexType);

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[1].PeakID, "the peak joins the master");
        }

        [TestMethod]
        public void RIJoinerMakesAPeakANewMasterOnlyAboveATenthOfAPercentOfTheTallestMaster() {
            AssertMasterHeightFloor(Join(MasterHeightFloorData()));
        }

        [TestMethod]
        public void RTJoinerMakesAPeakANewMasterOnlyAboveATenthOfAPercentOfTheTallestMaster() {
            AssertMasterHeightFloor(Join(MasterHeightFloorData(), AlignmentIndexType.RT));
        }

        // The tallest master before file 1 is the reference file's peak at RI 1000 (height 1,000,000), so the floor
        // for file 1 is 1000. File 1's peak at RI 1500 (height 1000) is not above it and becomes no master; its peaks
        // at RI 2000 (height 10,000,000) and RI 2500 (height 5000) are, and become masters. The floor is not raised by
        // the file's own new masters, so the peak at RI 2500 stays a master although it is below 0.1% of the peak at
        // RI 2000. It is raised for file 2: its peak at RI 3000 (height 5000) is below 10,000 and becomes no master.
        // The floor only bounds new masters: file 1's peak at RI 1001 (height 10) still joins the master at RI 1000,
        // and the reference file's peak at RI 4000 (height 1) is a master. The height compared is the MSDec model
        // peak height, MS-DIAL 4's BasepeakHeight, not the height of the quantified peak (1 for the peak at RI 2500).
        private static List<List<SpectrumFeature>> MasterHeightFloorData() {
            return new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1000d, height: 1_000_000d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 4000d, height: 1d, SpectrumB),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 1001d, height: 10d, SpectrumA),
                    CreateFeature(scanId: 1, ri: 1500d, height: 1000d, SpectrumB),
                    CreateFeature(scanId: 2, ri: 2000d, height: 10_000_000d, SpectrumA),
                    CreateFeature(scanId: 3, ri: 2500d, height: 5000d, SpectrumB, quantPeakHeight: 1d),
                },
                new List<SpectrumFeature>
                {
                    CreateFeature(scanId: 0, ri: 3000d, height: 5000d, SpectrumA),
                },
            };
        }

        private static void AssertMasterHeightFloor(List<AlignmentSpotProperty> spots) {
            var byRetention = spots.OrderBy(s => s.TimesCenter.RI.Value).ToArray();
            CollectionAssert.AreEqual(new[] { 1000d, 2000d, 2500d, 4000d }, byRetention.Select(s => s.TimesCenter.RI.Value).ToArray(),
                "masters: the reference file's two peaks and file 1's peaks at RI 2000 and 2500");
            CollectionAssert.AreEqual(new[] { 0, -1, -1, 1 }, byRetention.Select(s => s.AlignedPeakProperties[0].PeakID).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 2, 3, -1 }, byRetention.Select(s => s.AlignedPeakProperties[1].PeakID).ToArray(),
                "file 1's peak at RI 1001 joins the master at RI 1000");
            CollectionAssert.AreEqual(new[] { -1, -1, -1, -1 }, byRetention.Select(s => s.AlignedPeakProperties[2].PeakID).ToArray());
        }

        private List<AlignmentSpotProperty> Join(List<List<SpectrumFeature>> data, AlignmentIndexType indexType = AlignmentIndexType.RI) {
            return Join(data, indexType, _ => { });
        }

        private List<AlignmentSpotProperty> Join(List<List<SpectrumFeature>> data, AlignmentIndexType indexType, Action<MsdialGcmsParameter> configure) {
            var files = new List<AnalysisFileBean>();
            for (int i = 0; i < data.Count; i++) {
                var dcl = Path.Combine(_directory, $"file{i}.dcl");
                MsdecResultsWriter.Write(dcl, data[i].Select(f => f.AnnotatedMSDecResult.MSDecResult).ToList());
                files.Add(new AnalysisFileBean { AnalysisFileId = i, AnalysisFileName = $"file{i}", DeconvolutionFilePath = dcl, });
            }
            var parameter = new MsdialGcmsParameter
            {
                AlignmentIndexType = indexType,
                RetentionIndexAlignmentTolerance = (float)RiTolerance,
            };
            parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance = (float)(RiTolerance / 100d);
            configure(parameter);
            var accessor = new StubAccessor(data);
            var joiner = indexType == AlignmentIndexType.RI
                ? GcmsPeakJoiner.CreateRIJoiner(RiTolerance, parameter, evaluator: null, refer: null, accessor)
                : GcmsPeakJoiner.CreateRTJoiner(parameter, evaluator: null, refer: null, accessor);
            return joiner.Join(files, referenceId: 0, null);
        }

        // ri is the retention of the MSDec result (RT is ri / 100 min). quantTopRi is the top of the quantified
        // (quant-mass) chromatogram peak; it is the same as ri unless given. height is the MSDec model peak height;
        // quantPeakHeight, the height of the quantified peak, is the same unless given.
        private static SpectrumFeature CreateFeature(int scanId, double ri, double height, (double Mz, double Intensity)[] spectrum, double? quantTopRi = null, double? quantPeakHeight = null) {
            var quantMass = spectrum[0].Mz;
            var top = quantTopRi ?? ri;
            ChromXs Times(double value) => new ChromXs(new RetentionIndex(value)) { RT = new RetentionTime(value / 100d), };
            var msdec = new MSDecResult
            {
                ScanID = scanId,
                RawSpectrumID = scanId,
                ChromXs = Times(ri),
                ModelPeakMz = quantMass,
                ModelPeakHeight = height,
                Spectrum = spectrum.Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList(),
            };
            var peak = new BaseChromatogramPeakFeature
            {
                ChromXsLeft = Times(top),
                ChromXsTop = Times(top),
                ChromXsRight = Times(top),
                PeakHeightTop = quantPeakHeight ?? height,
                Mass = quantMass,
            };
            return new SpectrumFeature(
                new AnnotatedMSDecResult(msdec, new MsScanMatchResultContainer(), new MoleculeProperty(), quantMass),
                new QuantifiedChromatogramPeak(peak, new ChromatogramPeakShape(), scanId, scanId, scanId));
        }

        private sealed class StubAccessor : IFeatureAccessor<SpectrumFeature> {
            private readonly List<List<SpectrumFeature>> _data;

            public StubAccessor(List<List<SpectrumFeature>> data) {
                _data = data;
            }

            public List<SpectrumFeature> GetMSScanProperties(AnalysisFileBean analysisFile) {
                return _data[analysisFile.AnalysisFileId];
            }

            public ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, float ms1MassTolerance) {
                throw new NotSupportedException();
            }
        }
    }
}
