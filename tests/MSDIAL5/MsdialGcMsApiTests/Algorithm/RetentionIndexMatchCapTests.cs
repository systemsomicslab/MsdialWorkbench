using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Tests
{
    /// <summary>
    /// The retention-index verdict is capped too, on whichever index scale the run actually uses.
    /// </summary>
    /// <remarks>
    /// GC-MS needed no fix for references that carry no retention index: CompareBasicMSScanProperties
    /// already calls the guarded four-argument GetGaussianSimilarity for both axes. What it did need
    /// is the same separation the retention-time side needed, between the search window and the match
    /// verdict. GC-MS is in fact the mode most exposed to it, because
    /// DatasetParameterSettingModel turns IsUseTimeForAnnotationScoring on by default there, and
    /// GetAnnotationCode reads IsRiMatch alone to promote 440 to 340.
    ///
    /// THE TWO SCALES ARE NOT INTERCHANGEABLE, which is why RiCompoundType is a required constructor
    /// argument on CalculateMatchScore rather than a defaulted one. Kovats units run 100 per carbon.
    /// The Fiehn scale is FAME retention in milliseconds -- RetentionIndexHandler's FAME dictionary
    /// runs from 262320 at C8 to 1113100 at C30 -- so it runs near 39,350 per carbon, a factor of
    /// about 390. A cap chosen on one scale is meaningless on the other. (The same mismatch afflicts
    /// the RiTolerance default of 20, which is a sensible Kovats window and 20 milliseconds on the
    /// Fiehn scale. That is a separate defect and is not addressed here.)
    ///
    /// These tests go through CalculateMatchScore rather than calling the policy directly, so that
    /// they also hold the wiring: the cap has to reach CompareEIMSScanProperties, and it has to be
    /// chosen for the right scale.
    ///
    /// Cap values confirmed with the author of MS-DIAL, 2026-09-10.
    /// </remarks>
    [TestClass()]
    public class RetentionIndexMatchCapTests
    {
        [TestMethod()]
        public void OnTheAlkaneScaleFourHundredUnitsIsNotAMatch() {
            // Inside a 1000-unit search window, outside the 150-unit cap.
            var result = Match(RiCompoundType.Alkanes, peakRetentionIndex: 1500d, referenceRetentionIndex: 1900d, riTolerance: 1000f);

            Assert.IsFalse(result.IsRiMatch);
        }

        [TestMethod()]
        public void OnTheAlkaneScaleOneHundredUnitsStillIs() {
            var result = Match(RiCompoundType.Alkanes, peakRetentionIndex: 1500d, referenceRetentionIndex: 1600d, riTolerance: 1000f);

            Assert.IsTrue(result.IsRiMatch, "one carbon apart is inside the cap");
        }

        [TestMethod()]
        public void OnTheFiehnScaleTheSameCapWouldHaveRejectedEverything() {
            // 100,000 Fiehn units is about 2.5 carbons and is correctly refused; 40,000 is about one
            // carbon and is correctly accepted. Under the Kovats cap of 150 both would have failed,
            // which is the mistake this scale distinction exists to prevent.
            var rejected = Match(RiCompoundType.Fames, peakRetentionIndex: 600000d, referenceRetentionIndex: 700000d, riTolerance: 1000000f);
            var accepted = Match(RiCompoundType.Fames, peakRetentionIndex: 600000d, referenceRetentionIndex: 640000d, riTolerance: 1000000f);

            Assert.IsFalse(rejected.IsRiMatch);
            Assert.IsTrue(accepted.IsRiMatch);
        }

        [TestMethod()]
        public void ATightUserToleranceStillGoverns() {
            // The cap is an upper bound on the analyst's setting, never a relaxation of it. The
            // GC-MS default RiTolerance is 20, well under the alkane cap, so default runs are
            // unaffected by this change -- which is the point of asserting it.
            //
            // 30 units, not something larger: CalculateMatchScore.Tolerance widens the SEARCH window
            // to twice RiTolerance when retention filtering is off, so a reference further than 40
            // units away here would not be retrieved at all and there would be no verdict to test.
            var result = Match(RiCompoundType.Alkanes, peakRetentionIndex: 1500d, referenceRetentionIndex: 1530d, riTolerance: 20f);

            Assert.IsFalse(result.IsRiMatch, "30 units exceeds the analyst's own 20-unit tolerance");
            Assert.AreEqual(20d, RetentionMatchPolicy.EffectiveRetentionIndexTolerance(20d, RiCompoundType.Alkanes),
                "and the cap did not widen it");
        }

        [TestMethod()]
        public void TheCapsThemselves() {
            // Stated once, so that changing either constant is a visible decision.
            Assert.AreEqual(150d, RetentionMatchPolicy.EffectiveRetentionIndexTolerance(1e6d, RiCompoundType.Alkanes));
            Assert.AreEqual(50000d, RetentionMatchPolicy.EffectiveRetentionIndexTolerance(1e6d, RiCompoundType.Fames));
        }

        private static MsScanMatchResult Match(RiCompoundType riCompoundType, double peakRetentionIndex, double referenceRetentionIndex, float riTolerance) {
            var parameter = new MsRefSearchParameterBase
            {
                RiTolerance = riTolerance,
                Ms1Tolerance = 0.5f,
                Ms2Tolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
                IsUseTimeForAnnotationFiltering = false,
            };
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a reference",
                InChIKey = "DUMMYINCHIKEY",
                ChromXs = new ChromXs(referenceRetentionIndex, ChromXType.RI, ChromXUnit.None),
                Spectrum = Spectrum(),
            };
            var database = new MoleculeDataBase(
                new List<MoleculeMsReference> { reference, }, "MspDB", DataBaseSource.Msp, SourceType.MspDB, "MspPath");
            var item = new DataBaseItem<MoleculeDataBase>(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>());
            var calculator = new CalculateMatchScore(item, parameter, RetentionType.RI, riCompoundType);

            var scan = new MSScanProperty
            {
                ScanID = 0,
                ChromXs = new ChromXs(peakRetentionIndex, ChromXType.RI, ChromXUnit.None),
                Spectrum = Spectrum(),
            };
            return calculator.CalculateMatches(scan).Single();
        }

        private static List<SpectrumPeak> Spectrum() {
            return new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 73, Intensity = 100, },
                new SpectrumPeak { Mass = 147, Intensity = 60, },
                new SpectrumPeak { Mass = 205, Intensity = 40, },
                new SpectrumPeak { Mass = 291, Intensity = 20, },
            };
        }
    }
}
