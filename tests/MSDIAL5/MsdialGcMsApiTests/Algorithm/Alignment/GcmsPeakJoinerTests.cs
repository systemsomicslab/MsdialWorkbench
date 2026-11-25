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
        public void SpectrumAInPartScoresBelowTheDefaultOnEIAloneAndAboveItInTotal(bool isUseRetentionIndex) {
            // The premise of the threshold tests below. 2 RI units (0.02 min) apart, a retention
            // similarity of 0.98 for either tolerance; with the default factors of 0.5 the total
            // score is about 0.82, while the integrated EI similarity alone is about 0.67.
            var parameter = new MsRefSearchParameterBase { Ms1Tolerance = .5f, Ms2Tolerance = .5f, MassRangeBegin = 0, MassRangeEnd = 2000, };
            var a = CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA).AnnotatedMSDecResult.MSDecResult;
            var b = CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart).AnnotatedMSDecResult.MSDecResult;

            var result = MsScanMatching.CompareEIMSScanProperties(a, b, parameter, rtTol: .1, riTol: RiTolerance, eiFactor: .5, riFactor: .5, isUseRetentionIndex: isUseRetentionIndex);

            var ei = MsScanMatching.GetIntegratedSpectraSimilarity(result);
            Assert.IsTrue(ei > .6 && ei < .7, $"integrated EI similarity {ei}");
            Assert.IsTrue(result.TotalScore > .8 && result.TotalScore < .85, $"total score {result.TotalScore}");
            Assert.IsTrue(result.IsRiMatch && result.IsRtMatch);
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
        public void JoinerJoinsByTheTotalScoreAboveTheSpectrumSimilarityTolerance(AlignmentIndexType indexType) {
            // Total score about 0.82, integrated EI similarity about 0.67: joined at 0.75, which a
            // threshold on the EI similarity alone, or on the MS1 tolerance (0.9 here), would not do.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.Ms1AlignmentTolerance = .9f;
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .75f;
            });

            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void JoinerKeepsTheSameSpectrumApartWhenTheRetentionPullsTheTotalScoreBelowTheTolerance(AlignmentIndexType indexType) {
            // The same spectrum 9 RI units (0.09 min) away, inside the retention tolerance: retention
            // similarity 0.67, total score 0.83. A threshold of 0.9 on the EI similarity alone (1.0)
            // would join the two.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1009d, height: 1000d, SpectrumA), },
            };

            var spots = Join(data, indexType, p => {
                p.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .9f;
            });

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots.All(s => s.AlignedPeakProperties.Count(p => p.MasterPeakID >= 0) == 1));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI)]
        [DataRow(AlignmentIndexType.RT)]
        public void TheGcmsDefaultJoinsAPartlyMatchingSpectrumOfTheSameRetention(AlignmentIndexType indexType) {
            // Integrated EI similarity about 0.67, as two spectra of one compound in two files often
            // score: the GC-MS default (0.7 on the total score) joins them.
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1000d, height: 1000d, SpectrumA), },
                new List<SpectrumFeature> { CreateFeature(scanId: 0, ri: 1002d, height: 1000d, SpectrumAInPart), },
            };

            var spots = Join(data, indexType, _ => { });

            Assert.AreEqual(1, spots.Count);
            Assert.IsTrue(spots[0].AlignedPeakProperties.All(p => p.MasterPeakID >= 0));
        }

        [TestMethod]
        [DataRow(AlignmentIndexType.RI, .9f, 1008d)]
        [DataRow(AlignmentIndexType.RI, .1f, 1000d)]
        [DataRow(AlignmentIndexType.RT, .9f, 1008d)]
        [DataRow(AlignmentIndexType.RT, .1f, 1000d)]
        public void JoinerWeighsTheEISimilarityBySpectrumSimilarityFactor(AlignmentIndexType indexType, float eiFactor, double expectedMasterRi) {
            // The peak of file 1 is similar to both masters: one at the same retention whose spectrum
            // matches in part, one 8 RI units (0.08 min) away with the same spectrum. Which one it
            // joins depends on how the EI similarity is weighed against the retention similarity.
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

        private List<AlignmentSpotProperty> Join(List<List<SpectrumFeature>> data) {
            return Join(data, AlignmentIndexType.RI, _ => { });
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
            configure(parameter);
            var accessor = new StubAccessor(data);
            var joiner = indexType == AlignmentIndexType.RI
                ? GcmsPeakJoiner.CreateRIJoiner(RiTolerance, parameter, evaluator: null, refer: null, accessor)
                : GcmsPeakJoiner.CreateRTJoiner(parameter, evaluator: null, refer: null, accessor);
            return joiner.Join(files, referenceId: 0, null);
        }

        private static SpectrumFeature CreateFeature(int scanId, double ri, double height, (double Mz, double Intensity)[] spectrum) {
            var quantMass = spectrum[0].Mz;
            ChromXs Times() => new ChromXs(new RetentionIndex(ri)) { RT = new RetentionTime(ri / 100d), };
            var msdec = new MSDecResult
            {
                ScanID = scanId,
                RawSpectrumID = scanId,
                ChromXs = Times(),
                ModelPeakMz = quantMass,
                ModelPeakHeight = height,
                Spectrum = spectrum.Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList(),
            };
            var peak = new BaseChromatogramPeakFeature
            {
                ChromXsLeft = Times(),
                ChromXsTop = Times(),
                ChromXsRight = Times(),
                PeakHeightTop = height,
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
