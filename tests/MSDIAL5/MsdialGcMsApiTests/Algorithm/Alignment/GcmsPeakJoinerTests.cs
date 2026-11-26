using CompMs.Common.Components;
using CompMs.Common.Enum;
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

        private List<AlignmentSpotProperty> Join(List<List<SpectrumFeature>> data, AlignmentIndexType indexType = AlignmentIndexType.RI) {
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
            var joiner = indexType == AlignmentIndexType.RI
                ? GcmsPeakJoiner.CreateRIJoiner(RiTolerance, parameter, evaluator: null, refer: null, new StubAccessor(data))
                : GcmsPeakJoiner.CreateRTJoiner(parameter, evaluator: null, refer: null, new StubAccessor(data));
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
