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
            Assert.AreEqual(-2, target.PeakID);
            // The link to the file's own peak, and the record that the file had a joined peak, are kept.
            Assert.AreEqual(1, target.MasterPeakID);
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
        public void GapFillStillCentersLaterFilesOnADemotedPeakThatHasAPosition() {
            // Gap filling is forced (the default): with no signal at all the nearest scan is taken, at height 0.
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter()) { Peaks = CreateFlatPeakList(), };
            var (spot, representative, target, missing) = CreateSpot();

            filler.GapFill(null, null, null, spot, fileID: target.FileID);
            Assert.AreEqual(-2, target.PeakID);
            Assert.AreEqual(1.0d, target.ChromXsTop.RT.Value, 1e-9);
            filler.GapFill(null, null, null, spot, fileID: missing.FileID);

            CollectionAssert.AreEquivalent(new[] { representative, target, }, filler.LastCenterPeaks);
            CollectionAssert.AreEquivalent(new[] { representative, target, }, filler.LastWidthPeaks);
        }

        [TestMethod()]
        public void GapFillKeepsTheOwnPositionOfADetectedPeakWhoseReextractionFoundNothing() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, missing) = CreateSpot();

            // No chromatogram: the peak keeps its own retention at height 0, as MS-DIAL 4 set it, and is not reset to
            // RT -1, so later files are still centered on it.
            filler.GapFill(null, null, null, spot, fileID: target.FileID);
            Assert.AreEqual(1.0d, target.ChromXsTop.RT.Value, 1e-9);
            Assert.AreEqual(0d, target.PeakHeightTop);
            Assert.AreEqual(-2, target.PeakID);
            filler.GapFill(null, null, null, spot, fileID: missing.FileID);

            CollectionAssert.AreEquivalent(new[] { representative, target, }, filler.LastCenterPeaks);
            CollectionAssert.AreEquivalent(new[] { representative, target, }, filler.LastWidthPeaks);
        }

        [TestMethod()]
        public void GapFillDoesNotCenterLaterFilesOnAPeakWithoutAPosition() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, missing) = CreateSpot();
            // As read back from an alignment saved by an earlier version, which reset such a peak to RT -1.
            target.PeakID = -2;
            target.ChromXsLeft = target.ChromXsTop = target.ChromXsRight = new ChromXs(0d, ChromXType.Mz, ChromXUnit.Mz);

            filler.GapFill(null, null, null, spot, fileID: missing.FileID);

            CollectionAssert.AreEquivalent(new[] { representative, }, filler.LastCenterPeaks);
            CollectionAssert.AreEquivalent(new[] { representative, }, filler.LastWidthPeaks);
        }

        [TestMethod()]
        public void GapFillCentersOnEveryJoinedPeakWhenNoneHasAPosition() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, missing) = CreateSpot();
            foreach (var peak in new[] { representative, target, }) {
                peak.ChromXsLeft = peak.ChromXsTop = peak.ChromXsRight = new ChromXs(0d, ChromXType.Mz, ChromXUnit.Mz);
            }

            filler.GapFill(null, null, null, spot, fileID: missing.FileID);

            CollectionAssert.AreEquivalent(new[] { representative, target, }, filler.LastCenterPeaks);
        }

        [TestMethod()]
        public void GapFillTakesADetectedPeaksOwnApexNotANeighbourNearerTheSpotCenter() {
            // Files 0 and 1 have the compound at 10.00 min, file 2 at 10.09 (it joined within the 0.075 min tolerance of a
            // 10.03 master). A neighbouring compound on the same ion peaks at 9.99. File 2 is re-extracted at the spot's
            // quant mass: around the spot center (10.03) the nearest apex is the neighbour's; around its own apex it is
            // its own, which MS-DIAL 4 took.
            var parameter = new MsdialGcmsParameter { RetentionTimeAlignmentTolerance = 0.075f, };
            var filler = new SyntheticRtGapFiller(parameter, fileWithoutIon: -1, apexes: new[] { (9.99d, 1e5d), (10.09d, 5e4d), });
            var peaks = new List<AlignmentChromPeakFeature> {
                CreateDetectedAt(fileID: 0, top: 10.00d, mass: 73.0d),
                CreateDetectedAt(fileID: 1, top: 10.00d, mass: 73.0d),
                CreateDetectedAt(fileID: 2, top: 10.09d, mass: 217.0d),
            };
            var spot = new AlignmentSpotProperty { QuantMass = 73.0d, RepresentativeFileID = 0, AlignedPeakProperties = peaks, };

            filler.GapFill(null, null, null, spot, fileID: 2);

            Assert.AreEqual(10.09d, peaks[2].ChromXsTop.RT.Value, 1e-9);
            Assert.AreEqual(2, peaks[2].PeakID);
            Assert.AreEqual(73.0d, peaks[2].Mass);
        }

        [TestMethod()]
        public void WithoutForcingARepresentativeWhoseReextractionFindsNoApexKeepsItsRetention() {
            // Not forced: no apex near the representative's own retention at the new quant mass. It stays detected (the
            // spot takes its name and spectrum from it) at height 0, and keeps its retention, so the spot's average
            // retention is not pulled toward -1 and its main axis stays RT.
            var parameter = new MsdialGcmsParameter { IsForceInsertForGapFilling = false, RetentionTimeAlignmentTolerance = 0.075f, };
            var filler = new SyntheticRtGapFiller(parameter, fileWithoutIon: 0);
            var peaks = new List<AlignmentChromPeakFeature> {
                CreateDetectedAt(fileID: 0, top: 10.00d, mass: 217.0d),
                CreateDetectedAt(fileID: 1, top: 10.00d, mass: 73.0d),
            };
            var spot = new AlignmentSpotProperty { QuantMass = 73.0d, RepresentativeFileID = 0, AlignedPeakProperties = peaks, };

            filler.GapFill(null, null, null, spot, fileID: 0);

            Assert.AreEqual(0, peaks[0].PeakID);
            Assert.AreEqual(0d, peaks[0].PeakHeightTop);
            Assert.AreEqual(10.00d, peaks[0].ChromXsTop.RT.Value, 1e-9);
            Assert.AreEqual(ChromXType.RT, peaks[0].ChromXsTop.MainType);
        }

        [TestMethod()]
        public void TheRiFillerSearchesAtLeastTheMinutesMsDial4UsedConvertedToIndexUnits() {
            var filler = new ExposedRiGapFiller(new MsdialGcmsParameter());
            var peak = new AlignmentChromPeakFeature {
                ChromXsLeft = new ChromXs(new RetentionIndex(1500d)) { RT = new RetentionTime(10.000d), },
                ChromXsTop = new ChromXs(new RetentionIndex(1500.25d)) { RT = new RetentionTime(10.0025d), },
                ChromXsRight = new ChromXs(new RetentionIndex(1500.5d)) { RT = new RetentionTime(10.005d), },
            };

            // 100 index units per minute across the peak: 0.02 min is 2 units.
            Assert.AreEqual(2d, filler.MinimumOwnWidthOf(peak), 1e-9);
        }

        [TestMethod()]
        public void UpdateQuantMassDoesNotCenterOnAPeakWithoutAPosition() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, missing) = CreateSpot();
            // As read back from a saved alignment whose re-extraction of this file found nothing.
            target.PeakID = -2;
            target.ChromXsLeft = target.ChromXsTop = target.ChromXsRight = new ChromXs(0d, ChromXType.Mz, ChromXUnit.Mz);

            filler.UpdateQuantMass(null, spot, fileID: missing.FileID);

            CollectionAssert.AreEquivalent(new[] { representative, }, filler.LastCenterPeaks);
            CollectionAssert.AreEquivalent(new[] { representative, }, filler.LastWidthPeaks);
        }

        [TestMethod()]
        public void GapFillWithoutForcingIsNotDraggedOffByOneFileWithoutTheQuantIon() {
            // Ten files with the same peak at 10.00 min. File 1 has no ion at the quant mass, and gap filling is not
            // forced, so its re-extraction finds nothing and it is reset to RT -1. Averaged into the center, that -1 moved
            // it to 8.9 min for file 2, whose peak at 10.00 then fell outside the window, and so on for every later file.
            var parameter = new MsdialGcmsParameter { IsForceInsertForGapFilling = false, RetentionTimeAlignmentTolerance = 0.075f, };
            var filler = new SyntheticRtGapFiller(parameter, fileWithoutIon: 1);
            var peaks = Enumerable.Range(0, 10).Select(fileID => new AlignmentChromPeakFeature {
                FileID = fileID,
                MasterPeakID = fileID,
                PeakID = fileID,
                Mass = 147.0d,
                PeakHeightTop = 500d,
                ChromXsLeft = new ChromXs(9.95d, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(10.0d, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(10.05d, ChromXType.RT, ChromXUnit.Min),
            }).ToList();
            var spot = new AlignmentSpotProperty {
                QuantMass = 147.066d,
                RepresentativeFileID = 0,
                AlignedPeakProperties = peaks,
            };

            // PeakAligner fills the files one after another.
            foreach (var peak in peaks) {
                filler.GapFill(null, null, null, spot, peak.FileID);
            }

            Assert.AreEqual(-2, peaks[1].PeakID);
            foreach (var peak in peaks.Where(peak => peak.FileID != 1)) {
                Assert.AreEqual(peak.MasterPeakID, peak.PeakID, $"file {peak.FileID}");
                Assert.AreEqual(10.0d, peak.ChromXsTop.RT.Value, 1e-9, $"file {peak.FileID}");
                Assert.IsTrue(peak.PeakHeightTop > 0, $"file {peak.FileID}");
            }
        }

        [TestMethod()]
        public void UpdateQuantMassRestoresADemotedPeakThatCarriesTheNewQuantMass() {
            var (spot, _, target, _) = CreateSpot();
            new StubGcmsGapFiller(new MsdialGcmsParameter()).GapFill(null, null, null, spot, fileID: target.FileID);
            Assert.AreEqual(-2, target.PeakID);

            // The user picks another quant mass, one that this file's peak carries, and re-quantifies: the GUI uses a
            // new gap filler, so nothing but the alignment result records the earlier decision.
            spot.QuantMass = 73.0472d;
            new StubGcmsGapFiller(new MsdialGcmsParameter()) { Peaks = CreatePeakList(), }.UpdateQuantMass(null, spot, fileID: target.FileID);

            Assert.AreEqual(1000d, target.PeakHeightTop);
            Assert.AreEqual(1, target.PeakID);
            Assert.AreEqual(1, target.MasterPeakID);
        }

        [TestMethod()]
        public void UpdateQuantMassDemotesADetectedPeakWithoutSignalAtTheNewQuantMass() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, _, target, _) = CreateSpot();

            spot.QuantMass = 73.0472d;
            filler.UpdateQuantMass(null, spot, fileID: target.FileID);

            Assert.AreEqual(0d, target.PeakHeightTop);
            Assert.AreEqual(-2, target.PeakID);
            Assert.AreEqual(1, target.MasterPeakID);
        }

        [TestMethod()]
        public void UpdateQuantMassKeepsTheRepresentativePeakDetected() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, _, _) = CreateSpot();

            filler.UpdateQuantMass(null, spot, fileID: representative.FileID);

            Assert.AreEqual(0d, representative.PeakHeightTop);
            Assert.AreEqual(0, representative.PeakID);
            Assert.AreEqual(0, representative.MasterPeakID);
        }

        [TestMethod()]
        public void UpdateQuantMassLeavesAGapFilledCellNotDetected() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter()) { Peaks = CreatePeakList(), };
            var (spot, _, _, missing) = CreateSpot();

            filler.UpdateQuantMass(null, spot, fileID: missing.FileID);

            Assert.AreEqual(1000d, missing.PeakHeightTop);
            Assert.AreEqual(-2, missing.PeakID);
            Assert.AreEqual(-2, missing.MasterPeakID);
        }

        [TestMethod()]
        public void UpdateQuantMassCentersOnADemotedPeakAsTheAlignmentDid() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());
            var (spot, representative, target, missing) = CreateSpot();
            // As read back from a saved alignment: the file's peak was joined, then counted as not detected.
            target.PeakID = -2;

            filler.UpdateQuantMass(null, spot, fileID: missing.FileID);

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

        private static AlignmentChromPeakFeature CreateDetectedAt(int fileID, double top, double mass) {
            return new AlignmentChromPeakFeature {
                FileID = fileID,
                MasterPeakID = fileID,
                PeakID = fileID,
                Mass = mass,
                PeakHeightTop = 500d,
                ChromXsLeft = new ChromXs(top - 0.03d, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(top, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(top + 0.03d, ChromXType.RT, ChromXUnit.Min),
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

        private static List<ChromatogramPeak> CreateFlatPeakList() {
            var peaks = new List<ChromatogramPeak>();
            for (int i = 0; i < 7; i++) {
                var rt = 0.97d + 0.01d * i;
                peaks.Add(new ChromatogramPeak(i, 123.456d, 0d, new ChromXs(rt, ChromXType.RT, ChromXUnit.Min)));
            }
            return peaks;
        }

        /// <summary>
        /// The real RT gap filler, reading a synthetic chromatogram: a 0.01 min grid over center +/- 2 x width, the window
        /// GcmsRTGapFiller extracts, with Gaussian peaks (by default one at 10.00 min) in every file except one, which is
        /// flat at 0.
        /// </summary>
        private sealed class SyntheticRtGapFiller : GcmsRTGapFiller {
            private readonly int _fileWithoutIon;
            private readonly (double Rt, double Height)[] _apexes;

            public SyntheticRtGapFiller(MsdialGcmsParameter parameter, int fileWithoutIon, (double Rt, double Height)[] apexes = null) : base(parameter) {
                _fileWithoutIon = fileWithoutIon;
                _apexes = apexes ?? new[] { (10d, 1e5d), };
            }

            protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
                const double step = 0.01d, sigma = 0.02d;
                var first = (int)System.Math.Ceiling((center.RT.Value - peakWidth * 2d) / step - 1e-9);
                var last = (int)System.Math.Floor((center.RT.Value + peakWidth * 2d) / step + 1e-9);
                var peaks = new List<ChromatogramPeak>();
                for (int k = first; k <= last; k++) {
                    var rt = k * step;
                    var intensity = fileID == _fileWithoutIon ? 0d : _apexes.Sum(a => a.Height * System.Math.Exp(-(rt - a.Rt) * (rt - a.Rt) / (2d * sigma * sigma)));
                    peaks.Add(new ChromatogramPeak(peaks.Count, center.Mz.Value, intensity, new ChromXs(rt, ChromXType.RT, ChromXUnit.Min)));
                }
                return peaks;
            }
        }

        private sealed class ExposedRiGapFiller : GcmsRIGapFiller {
            public ExposedRiGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
            }

            public double MinimumOwnWidthOf(AlignmentChromPeakFeature peak) => MinimumOwnWidth(peak);
        }

        private sealed class StubGcmsGapFiller : GcmsGapFiller {
            public StubGcmsGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
            }

            protected override double AxTol => 0.05d;

            public List<ChromatogramPeak> Peaks { get; set; } = new List<ChromatogramPeak>();

            public AlignmentChromPeakFeature[] LastCenterPeaks { get; private set; }

            public AlignmentChromPeakFeature[] LastWidthPeaks { get; private set; }

            protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
                LastCenterPeaks = peaks.ToArray();
                return new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min) {
                    Mz = new MzValue(spot.QuantMass),
                    RI = new RetentionIndex(1000d),
                };
            }

            protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
                LastWidthPeaks = peaks.ToArray();
                return 0.1d;
            }

            protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
                return Peaks;
            }
        }
    }
}
