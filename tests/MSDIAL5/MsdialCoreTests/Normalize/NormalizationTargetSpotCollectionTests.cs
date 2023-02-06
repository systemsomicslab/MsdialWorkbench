using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Normalize.Tests
{
    [TestClass]
    public class NormalizationTargetSpotCollectionTests
    {
        [TestMethod]
        public void InitializeTest() {
            var spots = new[]
            {
                new AlignmentSpotProperty {
                    InternalStandardAlignmentID = 100,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = 10d, NormalizedPeakAreaAboveBaseline = 10d, NormalizedPeakAreaAboveZero = 10d, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = 10d, NormalizedPeakAreaAboveBaseline = 10d, NormalizedPeakAreaAboveZero = 10d, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = 10d, NormalizedPeakAreaAboveBaseline = 10d, NormalizedPeakAreaAboveZero = 10d, },
                    }
                },
                new AlignmentSpotProperty {
                    InternalStandardAlignmentID = 300,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = 20d, NormalizedPeakAreaAboveBaseline = 20d, NormalizedPeakAreaAboveZero = 20d, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = 20d, NormalizedPeakAreaAboveBaseline = 20d, NormalizedPeakAreaAboveZero = 20d, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = 20d, NormalizedPeakAreaAboveBaseline = 20d, NormalizedPeakAreaAboveZero = 20d, },
                    }
                },
            };
            var targets = new NormalizationTargetSpotCollection(spots);
            targets.Initialize(initializeIntenralStandardId: true);

            Assert.AreEqual(2, spots.Length);
            Assert.AreEqual(-1, spots[0].InternalStandardAlignmentID);
            Assert.AreEqual(3, spots[0].AlignedPeakProperties.Count);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[2].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[2].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[2].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1, spots[1].InternalStandardAlignmentID);
            Assert.AreEqual(3, spots[1].AlignedPeakProperties.Count);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[2].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[2].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[1].AlignedPeakProperties[2].NormalizedPeakAreaAboveZero);
        }

        [TestMethod]
        public void NormalizeInternalStandardSpotTest() {
            var spots = new[]
            {
                new AlignmentSpotProperty {
                    MasterAlignmentID = 10,
                    InternalStandardAlignmentID = -1,
                    IonAbundanceUnit = IonAbundanceUnit.Intensity,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = -1, NormalizedPeakAreaAboveBaseline = -1, NormalizedPeakAreaAboveZero = -1, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = -1, NormalizedPeakAreaAboveBaseline = -1, NormalizedPeakAreaAboveZero = -1, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = -1, NormalizedPeakAreaAboveBaseline = -1, NormalizedPeakAreaAboveZero = -1, },
                    }
                },
                new AlignmentSpotProperty {
                    MasterAlignmentID = 20,
                    InternalStandardAlignmentID = -1,
                    IonAbundanceUnit = IonAbundanceUnit.Intensity,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = -1, NormalizedPeakAreaAboveBaseline = -1, NormalizedPeakAreaAboveZero = -1, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = -1, NormalizedPeakAreaAboveBaseline = -1, NormalizedPeakAreaAboveZero = -1, },
                        new AlignmentChromPeakFeature { NormalizedPeakHeight = -1, NormalizedPeakAreaAboveBaseline = -1, NormalizedPeakAreaAboveZero = -1, },
                    }
                },
            };
            var compound = new StandardCompound
            {
                PeakID = 20,
                Concentration = 100d,
            };

            var targets = new NormalizationTargetSpotCollection(spots);
            targets.NormalizeInternalStandardSpot(compound, IonAbundanceUnit.fmol_per_10E6_cells);

            Assert.AreEqual(2, spots.Length);
            Assert.AreEqual(10, spots[0].MasterAlignmentID);
            Assert.AreEqual(-1, spots[0].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.Intensity, spots[0].IonAbundanceUnit);
            Assert.AreEqual(3, spots[0].AlignedPeakProperties.Count);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[2].NormalizedPeakHeight);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[2].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(-1d, spots[0].AlignedPeakProperties[2].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(20, spots[1].MasterAlignmentID);
            Assert.AreEqual(20, spots[1].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.fmol_per_10E6_cells, spots[1].IonAbundanceUnit);
            Assert.AreEqual(3, spots[1].AlignedPeakProperties.Count);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[2].NormalizedPeakHeight);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[2].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(100d, spots[1].AlignedPeakProperties[2].NormalizedPeakAreaAboveZero);
        }

        [TestMethod]
        public void FindSpot() {
            var spots = new[]
            {
                new AlignmentSpotProperty { MasterAlignmentID = 20, },
                new AlignmentSpotProperty { MasterAlignmentID = 10, },
                new AlignmentSpotProperty { MasterAlignmentID = 30, },
            };
            var targets = new NormalizationTargetSpotCollection(spots);

            Assert.AreEqual(spots[0], targets.FindSpot(20));
            Assert.AreEqual(spots[1], targets.FindSpot(10));
            Assert.AreEqual(spots[2], targets.FindSpot(30));
            Assert.AreEqual(null, targets.FindSpot(40));
        }
    }
}
