using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class StagedLipidDatabaseTests
    {
        private static readonly AdductIon ProtonAdduct = AdductIon.GetAdductIon("[M+H]+");

        [TestMethod()]
        public void SaveAndLoadKeepSeedsAndGeneratedReferences() {
            var database = new StagedLipidDatabase("DB", new[] {
                new LipidSeed("PC 34:1", FacadeLipidParser.Default.Parse("PC 34:1"), 5.0),
                new LipidSeed("PE 36:2", FacadeLipidParser.Default.Parse("PE 36:2"), 6.0, AdductIon.GetAdductIon("[M-H]-"), 742.5),
            });
            var lipid = FacadeLipidParser.Default.Parse("PC 16:0_18:1");
            var reference = database.GetOrAddReference(lipid, ProtonAdduct, 760.585, 5.0, new[] { new SpectrumPeak(184.07, 100) });

            var memory = new MemoryStream();
            database.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var loaded = new StagedLipidDatabase("DB", StagedLipidDatabase.CidCategory);
            loaded.Load(memory, null);

            Assert.AreEqual(2, loaded.Seeds.Count);
            Assert.AreEqual("PC 34:1", loaded.Seeds[0].Name);
            Assert.AreEqual(5.0, loaded.Seeds[0].RetentionTime);
            Assert.IsNull(loaded.Seeds[0].Adduct);
            Assert.AreEqual("[M-H]-", loaded.Seeds[1].Adduct.AdductIonName);
            Assert.AreEqual(742.5, loaded.Seeds[1].PrecursorMz);

            var referred = loaded.Refer(new MsScanMatchResult { LibraryID = reference.ScanID });
            Assert.AreEqual("PC 16:0_18:1", referred.Name);
            Assert.AreEqual(1, referred.Spectrum.Count);
            // The loaded reference is reused rather than generated again.
            Assert.AreSame(referred, loaded.GetOrAddReference(lipid, ProtonAdduct, 760.585, 5.0, new SpectrumPeak[0]));
        }

        [TestMethod()]
        public void DataBaseStorageRestoresTheAnnotator() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics, };
            var searchParameter = new MsRefSearchParameterBase { Ms1Tolerance = 0.01f, };
            var database = new StagedLipidDatabase("DB", new[] { new LipidSeed("PC 34:1", FacadeLipidParser.Default.Parse("PC 34:1"), 5.0) });
            var annotator = new StagedLipidDatabaseAnnotator(database, "StagedLipid", 2, searchParameter);
            var storage = DataBaseStorage.CreateEmpty();
            storage.AddStagedLipidDataBase(database, new List<IAnnotatorParameterPair<StagedLipidDatabase>> {
                new StagedLipidAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true)),
            });

            var memory = new MemoryStream();
            storage.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = DataBaseStorage.Load(memory, new StandardLoadAnnotatorVisitor(parameter), new StandardAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam), null);

            var item = actual.StagedLipidDatabases.Single();
            Assert.AreEqual("DB", item.DataBase.Id);
            Assert.AreEqual(1, item.DataBase.Seeds.Count);
            var pair = item.Pairs.Single();
            Assert.AreEqual("StagedLipid", pair.AnnotatorID);
            Assert.IsInstanceOfType(pair.AnnotationQueryFactory, typeof(AnnotationQueryFactory));
            Assert.AreEqual(2, pair.AnnotationQueryFactory.Priority);
            // The staged lipid factories run with the molecule ones (StandardAnnotationProcess).
            Assert.IsTrue(actual.CreateQueryFactories().MoleculeQueryFactories.Any(f => f.AnnotatorId == "StagedLipid"));
        }

        [TestMethod()]
        public void StorageWithoutStagedLipidDatabasesStillLoads() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics, };
            var storage = DataBaseStorage.CreateEmpty();

            var memory = new MemoryStream();
            storage.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = DataBaseStorage.Load(memory, new StandardLoadAnnotatorVisitor(parameter), new StandardAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam), null);

            Assert.AreEqual(0, actual.StagedLipidDatabases.Count);
        }
    }
}
