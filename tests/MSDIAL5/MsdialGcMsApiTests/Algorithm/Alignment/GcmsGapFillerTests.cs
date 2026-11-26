using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

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
        public void GapFillCountsADetectedPeakWithoutSignalAsNotDetected() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, _) = CreateSpot();

            filler.GapFill(null, null, null, spot, fileID: target.FileID);

            Assert.AreEqual(0d, target.PeakHeightTop);
            Assert.AreEqual(-2, target.MasterPeakID);
            Assert.AreEqual(-2, target.PeakID);
            Assert.AreEqual(0, representative.PeakID);
        }

        [TestMethod()]
        public void GapFillKeepsTheRepresentativePeakDetected() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, _, _) = CreateSpot();

            filler.GapFill(null, null, null, spot, fileID: representative.FileID);

            Assert.AreEqual(0d, representative.PeakHeightTop);
            Assert.AreEqual(0, representative.MasterPeakID);
            Assert.AreEqual(0, representative.PeakID);
        }

        [TestMethod()]
        public void GapFillKeepsADetectedPeakWithSignalDetected() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter()) { Peaks = CreatePeakList(), };
            var (spot, _, target, _) = CreateSpot();

            filler.GapFill(null, null, null, spot, fileID: target.FileID);

            Assert.AreEqual(1000d, target.PeakHeightTop);
            Assert.AreEqual(1, target.MasterPeakID);
            Assert.AreEqual(1, target.PeakID);
        }

        [TestMethod()]
        public void GapFillStillCentersLaterFilesOnADemotedPeak() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, missing) = CreateSpot();

            filler.GapFill(null, null, null, spot, fileID: target.FileID);
            filler.GapFill(null, null, null, spot, fileID: missing.FileID);

            CollectionAssert.AreEquivalent(new[] { representative, target, }, filler.LastCenterPeaks);
        }

        private static (AlignmentSpotProperty Spot, AlignmentChromPeakFeature Representative, AlignmentChromPeakFeature Target, AlignmentChromPeakFeature Missing) CreateSpot() {
            var representative = CreateDetected(fileID: 0, peakID: 0);
            var target = CreateDetected(fileID: 1, peakID: 1);
            var missing = new AlignmentChromPeakFeature {
                FileID = 2,
                MasterPeakID = -1,
                PeakID = -1,
            };
            var spot = new AlignmentSpotProperty {
                QuantMass = 123.456d,
                RepresentativeFileID = 0,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature> { representative, target, missing, },
            };
            return (spot, representative, target, missing);
        }

        private static AlignmentChromPeakFeature CreateDetected(int fileID, int peakID) {
            return new AlignmentChromPeakFeature {
                FileID = fileID,
                MasterPeakID = peakID,
                PeakID = peakID,
                Mass = 123.4d,
                PeakHeightTop = 500d,
                ChromXsLeft = new ChromXs(0.9d, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(1.1d, ChromXType.RT, ChromXUnit.Min),
            };
        }

        private static List<ChromatogramPeak> CreatePeakList() {
            var intensities = new[] { 0d, 100d, 400d, 1000d, 400d, 100d, 0d, };
            var peaks = new List<ChromatogramPeak>();
            for (int i = 0; i < intensities.Length; i++) {
                var rt = 0.97d + 0.01d * i;
                peaks.Add(new ChromatogramPeak(i, 123.456d, intensities[i], new ChromXs(rt, ChromXType.RT, ChromXUnit.Min)));
            }
            return peaks;
        }

        private sealed class StubGcmsGapFiller : GcmsGapFiller {
            public StubGcmsGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
            }

            protected override double AxTol => 0.05d;

            public List<ChromatogramPeak> Peaks { get; set; } = new List<ChromatogramPeak>();

            public AlignmentChromPeakFeature[] LastCenterPeaks { get; private set; }

            protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
                LastCenterPeaks = peaks.ToArray();
                return new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min) {
                    Mz = new MzValue(spot.QuantMass),
                    RI = new RetentionIndex(1000d),
                };
            }

            protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
                return 0.1d;
            }

            protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
                return Peaks;
            }
        }
    }
}
