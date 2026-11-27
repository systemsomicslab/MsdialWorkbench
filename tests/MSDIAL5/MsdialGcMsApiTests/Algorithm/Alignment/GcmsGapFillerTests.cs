using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment.Tests {
    [TestClass()]
    public class GcmsGapFillerTests {
        [TestMethod()]
        public void UpdateQuantMassSetsTargetMassToSpotQuantMass() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());

            var target = new AlignmentChromPeakFeature {
                FileID = 0,
                MasterPeakID = -1,
                PeakID = -1,
                Mass = 999d,
            };
            var detected = new AlignmentChromPeakFeature {
                FileID = 1,
                MasterPeakID = 1,
                PeakID = 1,
                ChromXsLeft = new ChromXs(0.9d, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(1.1d, ChromXType.RT, ChromXUnit.Min),
            };
            var spot = new AlignmentSpotProperty {
                QuantMass = 123.456d,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature> { target, detected },
            };

            filler.UpdateQuantMass(rawSpectra: null, spot, fileID: target.FileID);

            Assert.AreEqual(spot.QuantMass, target.Mass);
        }

        [TestMethod()]
        public void NeedsGapFillKeepsAccurateMassPeaksWithinTheCentroidTolerance() {
            // Two files detect the same ion at 73.0468 and 73.0471; the spot's quant mass is their average.
            var filler = new StubGcmsGapFiller(AccurateParameter(centroidMs1Tolerance: 0.01f));
            var peaks = new[] { DetectedPeak(fileId: 0, mass: 73.0468d), DetectedPeak(fileId: 1, mass: 73.0471d), };
            var spot = Spot(quantMass: (73.0468d + 73.0471d) / 2d, peaks);

            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(0)));
            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(1)));
        }

        [TestMethod()]
        public void NeedsGapFillReextractsAccurateMassPeakOutsideTheCentroidTolerance() {
            var filler = new StubGcmsGapFiller(AccurateParameter(centroidMs1Tolerance: 0.01f));
            var peaks = new[] { DetectedPeak(fileId: 0, mass: 73.0468d), DetectedPeak(fileId: 1, mass: 73.0650d), };
            var spot = Spot(quantMass: 73.0470d, peaks);

            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(0)));
            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(1)));
        }

        [TestMethod()]
        public void NeedsGapFillCountsADistanceEqualToTheToleranceAsTheSameIon() {
            // 0.0625 and the masses are exact in binary, so the distance equals the tolerance exactly.
            var filler = new StubGcmsGapFiller(AccurateParameter(centroidMs1Tolerance: 0.0625f));
            var peaks = new[] { DetectedPeak(fileId: 0, mass: 100.0625d), DetectedPeak(fileId: 1, mass: 100.125d), };
            var spot = Spot(quantMass: 100d, peaks);

            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(0)));
            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(1)));
        }

        [TestMethod()]
        public void NeedsGapFillAlwaysFillsAFileWithNoDetectedPeak() {
            var filler = new StubGcmsGapFiller(AccurateParameter(centroidMs1Tolerance: 0.01f));
            var undetected = new AlignmentChromPeakFeature { FileID = 0, MasterPeakID = -1, PeakID = -1, };
            var spot = Spot(quantMass: 73.0470d, new[] { undetected, DetectedPeak(fileId: 1, mass: 73.0470d), });

            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(0)));
        }

        [DataTestMethod()]
        [DataRow(0.5f)] // what the Console and the GUI set for nominal data
        [DataRow(0.01f)] // what the Kovats method file writes before the Console overrides it
        public void NeedsGapFillOnWholeNumberNominalMassesIsAnExactComparison(float centroidMs1Tolerance) {
            var parameter = new MsdialGcmsParameter { AccuracyType = AccuracyType.IsNominal, };
            parameter.PeakPickBaseParam.CentroidMs1Tolerance = centroidMs1Tolerance;
            var filler = new StubGcmsGapFiller(parameter);
            var undetected = new AlignmentChromPeakFeature { FileID = 3, MasterPeakID = -1, PeakID = -1, };
            var peaks = new[] { DetectedPeak(fileId: 0, mass: 73d), DetectedPeak(fileId: 1, mass: 74d), DetectedPeak(fileId: 2, mass: 72d), undetected, };
            var spot = Spot(quantMass: 73d, peaks);

            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(0)));
            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(1)));
            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(2)));
            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(3)));
        }

        [TestMethod()]
        public void NeedsGapFillKeepsNominalPeaksRecordedInFinerStepsOnTheSameNominalIon() {
            // Nominal data whose m/z is recorded in finer steps (the Kovats demo ABF files): the quant-mass vote
            // groups 103.9 and 104.0 into nominal 104 and averages them to 103.95.
            var parameter = new MsdialGcmsParameter { AccuracyType = AccuracyType.IsNominal, };
            parameter.PeakPickBaseParam.CentroidMs1Tolerance = 0.5f;
            var filler = new StubGcmsGapFiller(parameter);
            var peaks = new[] { DetectedPeak(fileId: 0, mass: 103.9d), DetectedPeak(fileId: 1, mass: 104.0d), DetectedPeak(fileId: 2, mass: 105.0d), };
            var spot = Spot(quantMass: (103.9d + 104.0d) / 2d, peaks);

            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(0)));
            Assert.IsFalse(filler.NeedsGapFill(spot, FileWithId(1)));
            Assert.IsTrue(filler.NeedsGapFill(spot, FileWithId(2)));
        }

        [TestMethod()]
        public void DetectedAccurateMassPeakWithinToleranceKeepsItsHeight() {
            // PeakAligner gap fills a file only when NeedsGapFill says so. The stub's re-extraction finds no
            // chromatogram, so a re-extracted detected peak would come back with height 0 while still detected.
            var filler = new StubGcmsGapFiller(AccurateParameter(centroidMs1Tolerance: 0.01f));
            var peak = DetectedPeak(fileId: 0, mass: 73.0468d);
            peak.PeakHeightTop = 1000d;
            var spot = Spot(quantMass: (73.0468d + 73.0471d) / 2d, new[] { peak, DetectedPeak(fileId: 1, mass: 73.0471d), });

            if (filler.NeedsGapFill(spot, FileWithId(0))) {
                filler.GapFill(ms1Spectra: null, rawSpectra: null, spectra: null, spot, fileID: 0);
            }

            Assert.AreEqual(1000d, peak.PeakHeightTop);
            Assert.AreEqual(73.0468d, peak.Mass);
            Assert.IsTrue(peak.PeakID >= 0);
        }

        private static MsdialGcmsParameter AccurateParameter(float centroidMs1Tolerance) {
            var parameter = new MsdialGcmsParameter { AccuracyType = AccuracyType.IsAccurate, };
            parameter.PeakPickBaseParam.CentroidMs1Tolerance = centroidMs1Tolerance;
            return parameter;
        }

        private static AlignmentChromPeakFeature DetectedPeak(int fileId, double mass) {
            return new AlignmentChromPeakFeature {
                FileID = fileId,
                MasterPeakID = fileId,
                PeakID = fileId,
                Mass = mass,
                ChromXsLeft = new ChromXs(0.9d, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(1.1d, ChromXType.RT, ChromXUnit.Min),
            };
        }

        private static AlignmentSpotProperty Spot(double quantMass, IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new AlignmentSpotProperty {
                QuantMass = quantMass,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>(peaks),
            };
        }

        private static AnalysisFileBean FileWithId(int fileId) {
            return new AnalysisFileBean { AnalysisFileId = fileId, };
        }

        private sealed class StubGcmsGapFiller : GcmsGapFiller {
            public StubGcmsGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
            }

            protected override double AxTol => 0.05d;

            protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
                return new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min) {
                    Mz = new MzValue(spot.QuantMass),
                    RI = new RetentionIndex(1000d),
                };
            }

            protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
                return 0.1d;
            }

            protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
                return new List<ChromatogramPeak>();
            }
        }
    }
}
