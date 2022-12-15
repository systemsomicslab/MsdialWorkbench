using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Normalize.Tests
{
    [TestClass]
    public class NormalizationTargetSpotTest
    {
        [TestMethod]
        public void IsNormalizedTest() {
            var spot1 = new AlignmentSpotProperty { InternalStandardAlignmentID = 1, };
            var actual1 = new NormalizationTargetSpot(spot1).IsNormalized();
            Assert.IsTrue(actual1);

            var spot2 = new AlignmentSpotProperty { InternalStandardAlignmentID = -1, };
            var actual2 = new NormalizationTargetSpot(spot2).IsNormalized();
            Assert.IsFalse(actual2);
        }

        [TestMethod]
        public void NormalizeByIternalStandardTest() {
            var spot = new AlignmentSpotProperty {
                MasterAlignmentID = 10,
                InternalStandardAlignmentID = -1,
                IonAbundanceUnit = IonAbundanceUnit.Intensity,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { PeakHeightTop = 10d, PeakAreaAboveBaseline = 20d, PeakAreaAboveZero = 30d, },
                    new AlignmentChromPeakFeature { PeakHeightTop = 10d, PeakAreaAboveBaseline = 20d, PeakAreaAboveZero = 30d, },
                },
            };
            var isSpot = new AlignmentSpotProperty
            {
                MasterAlignmentID = 20,
                InternalStandardAlignmentID = 20,
                IonAbundanceUnit = IonAbundanceUnit.fmol_per_10E6_cells,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { PeakHeightTop = 10d, PeakAreaAboveBaseline = 10d, PeakAreaAboveZero = 10d, },
                    new AlignmentChromPeakFeature { PeakHeightTop = -1d, PeakAreaAboveBaseline = -1d, PeakAreaAboveZero = -1d, },
                },
            };
            var compound = new StandardCompound
            {
                PeakID = 20,
                Concentration = 100d,
            };

            var target = new NormalizationTargetSpot(spot);
            target.NormalizeByInternalStandard(isSpot, compound, IonAbundanceUnit.fmol_per_10E6_cells);

            Assert.AreEqual(20, spot.InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.fmol_per_10E6_cells, spot.IonAbundanceUnit);
            Assert.AreEqual(100d, spot.AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(200d, spot.AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(300d, spot.AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(1000d, spot.AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(2000d, spot.AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(3000d, spot.AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
        }

        [TestMethod]
        [DataRow("PC", "PC", true)]
        [DataRow("OxPC", "OxPC", true)]
        [DataRow("EtherPC", "EtherPC", true)]
        [DataRow("LPC", "LPC", true)]
        [DataRow("Unknown", "PC", false)]
        public void GetAnnotatedLipidClass(string expected, string lipid, bool isRefMatched) {
            var spot = new AlignmentSpotProperty { Name = "some lipid", };
            var target = new NormalizationTargetSpot(spot);
            var stubEvaluator = new FakeEvaluator(isRefMatched);
            var stubRefer = new FakeRefer(lipid);
            var stubLipids = new HashSet<string>();

            var actual = target.GetAnnotatedLipidClass(stubEvaluator, stubRefer, stubLipids);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FillNormalizePropertiesTest() {
            var spot = new AlignmentSpotProperty
            {
                IonAbundanceUnit = IonAbundanceUnit.Intensity,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { PeakHeightTop = 100d, PeakAreaAboveBaseline = 200d, PeakAreaAboveZero = 300d, },
                    new AlignmentChromPeakFeature { PeakHeightTop = 400d, PeakAreaAboveBaseline = 500d, PeakAreaAboveZero = 600d, },
                    new AlignmentChromPeakFeature { PeakHeightTop = 700d, PeakAreaAboveBaseline = 800d, PeakAreaAboveZero = 900d, },
                },
            };
            var target = new NormalizationTargetSpot(spot);
            target.FillNormalizeProperties();
            Assert.AreEqual(IonAbundanceUnit.Intensity, spot.IonAbundanceUnit);
            foreach (var peak in spot.AlignedPeakProperties) {
                Assert.AreEqual(peak.PeakHeightTop, peak.NormalizedPeakHeight);
                Assert.AreEqual(peak.PeakAreaAboveBaseline, peak.NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(peak.PeakAreaAboveZero, peak.NormalizedPeakAreaAboveZero);
            }
        }

        class FakeEvaluator : IMatchResultEvaluator<MsScanMatchResult>
        {
            private readonly bool _isRefmatched;

            public FakeEvaluator(bool isRefmatched) {
                _isRefmatched = isRefmatched;
            }

            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
                throw new System.NotImplementedException();
            }

            public bool IsAnnotationSuggested(MsScanMatchResult result) {
                return false;
            }

            public bool IsReferenceMatched(MsScanMatchResult result) {
                return _isRefmatched;
            }

            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
                throw new System.NotImplementedException();
            }

            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
                throw new System.NotImplementedException();
            }
        }

        class FakeRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            private readonly string _lipidClass;

            public FakeRefer(string lipidClass) {
                _lipidClass = lipidClass;
            }

            public string Key => throw new System.NotImplementedException();

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference { CompoundClass = _lipidClass, };
            }
        }
    }
}
