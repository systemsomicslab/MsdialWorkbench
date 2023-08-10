using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Normalize.Tests
{
    [TestClass()]
    public class NormalizationTests
    {
        [TestMethod()]
        public void SplashNormalizeTest() {
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean(),
                new AnalysisFileBean(),
            };
            var spots = new AlignmentSpotProperty[9];
            for (int i = 0; i < 9; i++) {
                spots[i] = new AlignmentSpotProperty
                {
                    MasterAlignmentID = i,
                };
                spots[i].MatchResults.AddResult(new MsScanMatchResult
                {
                    LibraryID = i,
                    Name = $"Compound {i}",
                    Source = SourceType.Manual
                });
                spots[i].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature
                    {
                        PeakHeightTop = 10 + i,
                        PeakAreaAboveZero = 20 + i,
                        PeakAreaAboveBaseline = 30 + i,
                    },
                    new AlignmentChromPeakFeature
                    {
                        PeakHeightTop = 15 + i,
                        PeakAreaAboveZero = 25 + i,
                        PeakAreaAboveBaseline = 35 + i,
                    },
                };
            }
            spots[0].Name = spots[1].Name = spots[2].Name = spots[3].Name = spots[4].Name = spots[5].Name = "Annotated";

            var refer = new MockRefer(new List<MoleculeMsReference>
            {
                new MoleculeMsReference { CompoundClass = "PC" },
                new MoleculeMsReference { Ontology = "PC" },
                new MoleculeMsReference { CompoundClass = "PE" },
                new MoleculeMsReference { Ontology = "PE" },
                new MoleculeMsReference { CompoundClass = "PA" },
                new MoleculeMsReference { Ontology = "PA" },
            });

            var lipids = new List<StandardCompound>
            {
                new StandardCompound { Concentration = 10, TargetClass = "PC", PeakID = 7 },
                new StandardCompound { Concentration = 5, TargetClass = "PE", PeakID = 8 },
            };

            Normalization.SplashNormalize(files, spots, refer, lipids, IonAbundanceUnit.nmol_per_microL_plasma, new FacadeMatchResultEvaluator(), applyDilutionFactor: false);

            // PC
            for (int i = 0; i < 2; i++) {
                Assert.AreEqual(lipids[0].PeakID, spots[i].InternalStandardAlignmentID);
                Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[i].IonAbundanceUnit);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[0].PeakHeightTop / spots[lipids[0].PeakID].AlignedPeakProperties[0].PeakHeightTop,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[1].PeakHeightTop / spots[lipids[0].PeakID].AlignedPeakProperties[1].PeakHeightTop,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveBaseline / spots[lipids[0].PeakID].AlignedPeakProperties[0].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveBaseline / spots[lipids[0].PeakID].AlignedPeakProperties[1].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveZero / spots[lipids[0].PeakID].AlignedPeakProperties[0].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveZero / spots[lipids[0].PeakID].AlignedPeakProperties[1].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            }
            // PE
            for (int i = 2; i < 4; i++) {
                Assert.AreEqual(lipids[1].PeakID, spots[i].InternalStandardAlignmentID);
                Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[i].IonAbundanceUnit);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[0].PeakHeightTop / spots[lipids[1].PeakID].AlignedPeakProperties[0].PeakHeightTop,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[1].PeakHeightTop / spots[lipids[1].PeakID].AlignedPeakProperties[1].PeakHeightTop,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveBaseline / spots[lipids[1].PeakID].AlignedPeakProperties[0].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveBaseline / spots[lipids[1].PeakID].AlignedPeakProperties[1].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveZero / spots[lipids[1].PeakID].AlignedPeakProperties[0].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveZero / spots[lipids[1].PeakID].AlignedPeakProperties[1].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            }
            // PA and unknown
            for (int i = 4; i < 7; i++) {
                Assert.AreEqual(-1, spots[i].InternalStandardAlignmentID);
                Assert.AreEqual(IonAbundanceUnit.Intensity, spots[i].IonAbundanceUnit);
                Assert.AreEqual(spots[i].AlignedPeakProperties[0].PeakHeightTop, spots[i].AlignedPeakProperties[0].NormalizedPeakHeight);
                Assert.AreEqual(spots[i].AlignedPeakProperties[1].PeakHeightTop, spots[i].AlignedPeakProperties[1].NormalizedPeakHeight);
                Assert.AreEqual(spots[i].AlignedPeakProperties[0].PeakAreaAboveBaseline, spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(spots[i].AlignedPeakProperties[1].PeakAreaAboveBaseline, spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(spots[i].AlignedPeakProperties[0].PeakAreaAboveZero, spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
                Assert.AreEqual(spots[i].AlignedPeakProperties[1].PeakAreaAboveZero, spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            }
            // Internal standard
            Assert.AreEqual(lipids[0].PeakID, spots[7].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[7].IonAbundanceUnit);
            Assert.AreEqual(lipids[0].Concentration, spots[7].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(lipids[0].Concentration, spots[7].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(lipids[0].Concentration, spots[7].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[0].Concentration, spots[7].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[0].Concentration, spots[7].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(lipids[0].Concentration, spots[7].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);

            Assert.AreEqual(lipids[1].PeakID, spots[8].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[8].IonAbundanceUnit);
            Assert.AreEqual(lipids[1].Concentration, spots[8].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(lipids[1].Concentration, spots[8].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(lipids[1].Concentration, spots[8].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[1].Concentration, spots[8].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[1].Concentration, spots[8].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(lipids[1].Concentration, spots[8].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
        }

        [TestMethod()]
        public void SplashNormalizeTest2() {
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean(),
                new AnalysisFileBean(),
            };
            var spots = new AlignmentSpotProperty[8];
            for (int i = 0; i < 8; i++) {
                spots[i] = new AlignmentSpotProperty
                {
                    MasterAlignmentID = i,
                };
                spots[i].MatchResults.AddResult(new MsScanMatchResult
                {
                    LibraryID = i,
                    Name = $"Compound {i}",
                    Source = SourceType.Manual
                });
                spots[i].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature
                    {
                        PeakHeightTop = 10 + i,
                        PeakAreaAboveZero = 20 + i,
                        PeakAreaAboveBaseline = 30 + i,
                    },
                    new AlignmentChromPeakFeature
                    {
                        PeakHeightTop = 15 + i,
                        PeakAreaAboveZero = 25 + i,
                        PeakAreaAboveBaseline = 35 + i,
                    },
                };
            }
            spots[0].Name = spots[1].Name = spots[2].Name = spots[3].Name  = "Annotated";

            var refer = new MockRefer(new List<MoleculeMsReference>
            {
                new MoleculeMsReference { CompoundClass = "PC" },
                new MoleculeMsReference { Ontology = "PC" },
                new MoleculeMsReference { CompoundClass = "PE" },
                new MoleculeMsReference { Ontology = "PE" },
            });

            var lipids = new List<StandardCompound>
            {
                new StandardCompound { Concentration = 10, TargetClass = "PC", PeakID = 5 },
                new StandardCompound { Concentration = 5, TargetClass = StandardCompound.AnyOthers, PeakID = 6 },
                new StandardCompound { Concentration = 3, TargetClass = StandardCompound.AnyOthers, PeakID = 7 },
            };

            Normalization.SplashNormalize(files, spots, refer, lipids, IonAbundanceUnit.nmol_per_microL_plasma, new FacadeMatchResultEvaluator(), applyDilutionFactor: false);

            // PC
            for (int i = 0; i < 2; i++) {
                Assert.AreEqual(lipids[0].PeakID, spots[i].InternalStandardAlignmentID);
                Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[i].IonAbundanceUnit);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[0].PeakHeightTop / spots[lipids[0].PeakID].AlignedPeakProperties[0].PeakHeightTop,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[1].PeakHeightTop / spots[lipids[0].PeakID].AlignedPeakProperties[1].PeakHeightTop,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveBaseline / spots[lipids[0].PeakID].AlignedPeakProperties[0].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveBaseline / spots[lipids[0].PeakID].AlignedPeakProperties[1].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveZero / spots[lipids[0].PeakID].AlignedPeakProperties[0].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
                Assert.AreEqual(
                    lipids[0].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveZero / spots[lipids[0].PeakID].AlignedPeakProperties[1].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            }
            // PE and unknown
            for (int i = 2; i < 5; i++) {
                Assert.AreEqual(lipids[1].PeakID, spots[i].InternalStandardAlignmentID);
                Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[i].IonAbundanceUnit);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[0].PeakHeightTop / spots[lipids[1].PeakID].AlignedPeakProperties[0].PeakHeightTop,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[1].PeakHeightTop / spots[lipids[1].PeakID].AlignedPeakProperties[1].PeakHeightTop,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakHeight);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveBaseline / spots[lipids[1].PeakID].AlignedPeakProperties[0].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveBaseline / spots[lipids[1].PeakID].AlignedPeakProperties[1].PeakAreaAboveBaseline,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[0].PeakAreaAboveZero / spots[lipids[1].PeakID].AlignedPeakProperties[0].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
                Assert.AreEqual(
                    lipids[1].Concentration * spots[i].AlignedPeakProperties[1].PeakAreaAboveZero / spots[lipids[1].PeakID].AlignedPeakProperties[1].PeakAreaAboveZero,
                    spots[i].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
            }
            // Internal standard
            Assert.AreEqual(lipids[0].PeakID, spots[5].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[5].IonAbundanceUnit);
            Assert.AreEqual(lipids[0].Concentration, spots[5].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(lipids[0].Concentration, spots[5].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(lipids[0].Concentration, spots[5].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[0].Concentration, spots[5].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[0].Concentration, spots[5].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(lipids[0].Concentration, spots[5].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);

            Assert.AreEqual(lipids[1].PeakID, spots[6].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[6].IonAbundanceUnit);
            Assert.AreEqual(lipids[1].Concentration, spots[6].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(lipids[1].Concentration, spots[6].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(lipids[1].Concentration, spots[6].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[1].Concentration, spots[6].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[1].Concentration, spots[6].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(lipids[1].Concentration, spots[6].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);

            Assert.AreEqual(lipids[2].PeakID, spots[7].InternalStandardAlignmentID);
            Assert.AreEqual(IonAbundanceUnit.nmol_per_microL_plasma, spots[7].IonAbundanceUnit);
            Assert.AreEqual(lipids[2].Concentration, spots[7].AlignedPeakProperties[0].NormalizedPeakHeight);
            Assert.AreEqual(lipids[2].Concentration, spots[7].AlignedPeakProperties[1].NormalizedPeakHeight);
            Assert.AreEqual(lipids[2].Concentration, spots[7].AlignedPeakProperties[0].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[2].Concentration, spots[7].AlignedPeakProperties[1].NormalizedPeakAreaAboveBaseline);
            Assert.AreEqual(lipids[2].Concentration, spots[7].AlignedPeakProperties[0].NormalizedPeakAreaAboveZero);
            Assert.AreEqual(lipids[2].Concentration, spots[7].AlignedPeakProperties[1].NormalizedPeakAreaAboveZero);
        }
    }

    class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public MockRefer(List<MoleculeMsReference> db) {
            this.db = db;
        }

        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key { get; } = string.Empty;
        private List<MoleculeMsReference> db;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (result.LibraryID < db.Count)
                return db[result.LibraryID];
            return null;
        }
    }
}