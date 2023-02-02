using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.Common.DataObj.Database;
using System.IO;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.Common.Parameter;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.Common.Enum;

namespace CompMs.MsdialDimsCore.DataObj.Tests
{
    [TestClass()]
    public class MsdialDimsDataStorageTests
    {
        [TestMethod()]
        public async Task SaveTest() {
            var storage = new MsdialDimsDataStorage
            {
                AnalysisFiles = new List<AnalysisFileBean> { new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "TestFile" } },
                AlignmentFiles = new List<AlignmentFileBean> { new AlignmentFileBean { FileID = 2, FileName = "TestAlignmentFile" } },
                MspDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 3, Name = "TestMspRef" } },
                TextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 4, Name = "TestTextRef" } },
                IsotopeTextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 5, Name = "TestIsotopeRef" } },
                MsdialDimsParameter = new MsdialDimsParameter { ProjectFileName = "TestProjectPath", ProviderFactoryParameter = new DimsBpiDataProviderFactoryParameter(3, 6) },
                IupacDatabase = new IupacDatabase { Id2AtomElementProperties = new Dictionary<int, List<AtomElementProperty>> { { 6, new List<AtomElementProperty> { new AtomElementProperty { ElementName = "TestElement" } } } } },
                DataBaseMapper = new DataBaseMapper { },
                DataBases = DataBaseStorage.CreateEmpty(),
            };
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 7, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB);
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            var dbs = storage.DataBases;
            DimsMspAnnotator annotator = new DimsMspAnnotator(db, searchParameter, TargetOmics.Metabolomics, "DummyAnnotator", 8);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryWithoutIsotopeFactory(annotator, searchParameter))
                }
            );
            storage.DataBaseMapper.Add(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, dbs.MetabolomicsDataBases[0].DataBase);

            var memory = new MemoryStream();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await storage.SaveAsync(manager, "Test", "TestFolder");
                manager.Complete();
            }

            using (var manager = ZipStreamManager.OpenGet(memory)) {
                var actual = await MsdialDimsDataStorage.Serializer.LoadAsync(manager, "Test", null, "TestFolder");

                Assert.AreEqual(storage.AnalysisFiles.Count, actual.AnalysisFiles.Count);
                Assert.AreEqual(storage.AnalysisFiles[0].AnalysisFileId, actual.AnalysisFiles[0].AnalysisFileId);
                Assert.AreEqual(storage.AnalysisFiles[0].AnalysisFileName, actual.AnalysisFiles[0].AnalysisFileName);

                Assert.AreEqual(storage.AlignmentFiles.Count, actual.AlignmentFiles.Count);
                Assert.AreEqual(storage.AlignmentFiles[0].FileID, actual.AlignmentFiles[0].FileID);
                Assert.AreEqual(storage.AlignmentFiles[0].FileName, actual.AlignmentFiles[0].FileName);

                Assert.AreEqual(storage.MspDB.Count, actual.MspDB.Count);
                Assert.AreEqual(storage.MspDB[0].DatabaseID, actual.MspDB[0].DatabaseID);
                Assert.AreEqual(storage.MspDB[0].Name, actual.MspDB[0].Name);

                Assert.AreEqual(storage.TextDB.Count, actual.TextDB.Count);
                Assert.AreEqual(storage.TextDB[0].DatabaseID, actual.TextDB[0].DatabaseID);
                Assert.AreEqual(storage.TextDB[0].Name, actual.TextDB[0].Name);

                Assert.AreEqual(storage.IsotopeTextDB.Count, actual.IsotopeTextDB.Count);
                Assert.AreEqual(storage.IsotopeTextDB[0].DatabaseID, actual.IsotopeTextDB[0].DatabaseID);
                Assert.AreEqual(storage.IsotopeTextDB[0].Name, actual.IsotopeTextDB[0].Name);

                Assert.AreEqual(storage.MsdialDimsParameter.ProjectFileName, actual.Parameter.ProjectFileName);
                Console.WriteLine(actual.Parameter.GetType().FullName);
                Assert.IsTrue(actual.Parameter is MsdialDimsParameter);
                Assert.IsTrue(((MsdialDimsParameter)actual.Parameter).ProviderFactoryParameter is DimsBpiDataProviderFactoryParameter);
                Assert.AreEqual(((DimsBpiDataProviderFactoryParameter)storage.MsdialDimsParameter.ProviderFactoryParameter).TimeBegin, ((DimsBpiDataProviderFactoryParameter)((MsdialDimsParameter)actual.Parameter).ProviderFactoryParameter).TimeBegin);
                Assert.AreEqual(((DimsBpiDataProviderFactoryParameter)storage.MsdialDimsParameter.ProviderFactoryParameter).TimeEnd, ((DimsBpiDataProviderFactoryParameter)((MsdialDimsParameter)actual.Parameter).ProviderFactoryParameter).TimeEnd);

                Assert.AreEqual(storage.IupacDatabase.Id2AtomElementProperties.Count, actual.IupacDatabase.Id2AtomElementProperties.Count);
                Assert.IsTrue(actual.IupacDatabase.Id2AtomElementProperties.ContainsKey(6));
                Assert.AreEqual(storage.IupacDatabase.Id2AtomElementProperties[6].Count, actual.IupacDatabase.Id2AtomElementProperties[6].Count);
                Assert.AreEqual(storage.IupacDatabase.Id2AtomElementProperties[6][0].ElementName, actual.IupacDatabase.Id2AtomElementProperties[6][0].ElementName);

                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases.Count, actual.DataBases.MetabolomicsDataBases.Count);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].DataBaseID, actual.DataBases.MetabolomicsDataBases[0].DataBaseID);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].DataBase.Id, actual.DataBases.MetabolomicsDataBases[0].DataBase.Id);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].DataBase.SourceType, actual.DataBases.MetabolomicsDataBases[0].DataBase.SourceType);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].DataBase.DataBaseSource, actual.DataBases.MetabolomicsDataBases[0].DataBase.DataBaseSource);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].DataBase.Database[0].DatabaseID, actual.DataBases.MetabolomicsDataBases[0].DataBase.Database[0].DatabaseID);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].DataBase.Database[0].Name, actual.DataBases.MetabolomicsDataBases[0].DataBase.Database[0].Name);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].Pairs.Count, actual.DataBases.MetabolomicsDataBases[0].Pairs.Count);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, actual.DataBases.MetabolomicsDataBases[0].Pairs[0].AnnotatorID);
                Assert.AreEqual(storage.DataBases.MetabolomicsDataBases[0].Pairs[0].AnnotationQueryFactory.GetType().FullName, actual.DataBases.MetabolomicsDataBases[0].Pairs[0].AnnotationQueryFactory.GetType().FullName);
            }
        }
    }
}