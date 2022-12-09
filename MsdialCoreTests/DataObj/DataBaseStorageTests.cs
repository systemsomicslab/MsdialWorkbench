using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class DataBaseStorageTests
    {
        [TestMethod()]
        public void DataBaseStorageTest() {
            var dbs = DataBaseStorage.CreateEmpty();
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 0, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB);
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Metabolomics, };
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            MassAnnotator annotator = new MassAnnotator(db, searchParameter, TargetOmics.Metabolomics, SourceType.MspDB, "DummyAnnotator", 1);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true))
                }
            );

            var memory = new MemoryStream();
            dbs.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = DataBaseStorage.Load(memory, new StandardLoadAnnotatorVisitor(parameter), new StandardAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam), null);

            Assert.AreEqual(dbs.MetabolomicsDataBases.Count, actual.MetabolomicsDataBases.Count);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].DataBaseID, actual.MetabolomicsDataBases[0].DataBaseID);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].DataBase.Id, actual.MetabolomicsDataBases[0].DataBase.Id);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].DataBase.SourceType, actual.MetabolomicsDataBases[0].DataBase.SourceType);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].DataBase.DataBaseSource, actual.MetabolomicsDataBases[0].DataBase.DataBaseSource);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].DataBase.Database[0].DatabaseID, actual.MetabolomicsDataBases[0].DataBase.Database[0].DatabaseID);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].DataBase.Database[0].Name, actual.MetabolomicsDataBases[0].DataBase.Database[0].Name);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].Pairs.Count, actual.MetabolomicsDataBases[0].Pairs.Count);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, actual.MetabolomicsDataBases[0].Pairs[0].AnnotatorID);
            Assert.AreEqual(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotationQueryFactory.GetType().FullName, actual.MetabolomicsDataBases[0].Pairs[0].AnnotationQueryFactory.GetType().FullName);
        }
    }
}