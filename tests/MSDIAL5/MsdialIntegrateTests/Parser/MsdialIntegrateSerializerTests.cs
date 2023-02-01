using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialIntegrate.Parser.Tests
{
    [TestClass()]
    public class MsdialIntegrateSerializerTests
    {
        [TestMethod()]
        public async Task DimsLoadTest() {
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
            var serializer = new MsdialIntegrateSerializer();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await serializer.SaveAsync(storage, manager, "Test", "TestFolder");
                manager.Complete();
            }

            using (var manager = ZipStreamManager.OpenGet(memory)) {
                var actual = await serializer.LoadAsync(manager, "Test", null, "TestFolder");

                AreAnalysisFilesEqual(storage.AnalysisFiles, actual.AnalysisFiles);
                AreAlignmentFilesEqual(storage.AlignmentFiles, actual.AlignmentFiles);
                AreDBEqual(storage.MspDB, actual.MspDB);
                AreDBEqual(storage.TextDB, actual.TextDB);
                AreDBEqual(storage.IsotopeTextDB, actual.IsotopeTextDB);
                AreIupacDatabaseEqual(storage.IupacDatabase, actual.IupacDatabase);
                AreDataBaseMapperEqual(storage.DataBaseMapper, actual.DataBaseMapper);
                AreDataBaseStorageEqual(storage.DataBases, actual.DataBases);

                Assert.AreEqual(storage.MsdialDimsParameter.ProjectFileName, actual.Parameter.ProjectFileName);
                Console.WriteLine(actual.Parameter.GetType().FullName);
                Assert.IsTrue(actual.Parameter is MsdialDimsParameter);
                Assert.IsTrue(((MsdialDimsParameter)actual.Parameter).ProviderFactoryParameter is DimsBpiDataProviderFactoryParameter);
                Assert.AreEqual(((DimsBpiDataProviderFactoryParameter)storage.MsdialDimsParameter.ProviderFactoryParameter).TimeBegin, ((DimsBpiDataProviderFactoryParameter)((MsdialDimsParameter)actual.Parameter).ProviderFactoryParameter).TimeBegin);
                Assert.AreEqual(((DimsBpiDataProviderFactoryParameter)storage.MsdialDimsParameter.ProviderFactoryParameter).TimeEnd, ((DimsBpiDataProviderFactoryParameter)((MsdialDimsParameter)actual.Parameter).ProviderFactoryParameter).TimeEnd);
            }
        }

        [TestMethod()]
        public async Task LcmsLoadTest() {
            var storage = new MsdialLcmsDataStorage
            {
                AnalysisFiles = new List<AnalysisFileBean> { new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "TestFile" } },
                AlignmentFiles = new List<AlignmentFileBean> { new AlignmentFileBean { FileID = 2, FileName = "TestAlignmentFile" } },
                MspDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 3, Name = "TestMspRef" } },
                TextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 4, Name = "TestTextRef" } },
                IsotopeTextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 5, Name = "TestIsotopeRef" } },
                MsdialLcmsParameter = new MsdialLcmsParameter { ProjectFileName = "TestProjectPath", },
                IupacDatabase = new IupacDatabase { Id2AtomElementProperties = new Dictionary<int, List<AtomElementProperty>> {
                    { 6, new List<AtomElementProperty> { new AtomElementProperty { ElementName = "TestElement" } } }
                } },
                DataBaseMapper = new DataBaseMapper { }, // No serialization in the future.
                DataBases = DataBaseStorage.CreateEmpty(),
            };
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 7, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB);
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            var dbs = storage.DataBases;
            LcmsMspAnnotator annotator = new LcmsMspAnnotator(db, searchParameter, TargetOmics.Metabolomics, "DummyAnnotator", 8);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, storage.MsdialLcmsParameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true))
                }
            );

            storage.DataBaseMapper.Add(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, dbs.MetabolomicsDataBases[0].DataBase);

            var memory = new MemoryStream();
            var serializer = new MsdialIntegrateSerializer();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await serializer.SaveAsync(storage, manager, "Test", "TestFolder");
                manager.Complete();
            }

            using (var manager = ZipStreamManager.OpenGet(memory)) {
                var actual = await serializer.LoadAsync(manager, "Test", null, "TestFolder");

                AreAnalysisFilesEqual(storage.AnalysisFiles, actual.AnalysisFiles);
                AreAlignmentFilesEqual(storage.AlignmentFiles, actual.AlignmentFiles);
                AreDBEqual(storage.MspDB, actual.MspDB);
                AreDBEqual(storage.TextDB, actual.TextDB);
                AreDBEqual(storage.IsotopeTextDB, actual.IsotopeTextDB);
                AreIupacDatabaseEqual(storage.IupacDatabase, actual.IupacDatabase);
                AreDataBaseMapperEqual(storage.DataBaseMapper, actual.DataBaseMapper);
                AreDataBaseStorageEqual(storage.DataBases, actual.DataBases);

                Assert.AreEqual(storage.MsdialLcmsParameter.ProjectFileName, actual.Parameter.ProjectFileName);
                Console.WriteLine(actual.Parameter.GetType().FullName);
                Assert.IsTrue(actual.Parameter is MsdialLcmsParameter);
            }
        }

        [TestMethod()]
        public async Task ImmsLoadTest() {
            var storage = new MsdialImmsDataStorage
            {
                AnalysisFiles = new List<AnalysisFileBean> { new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "TestFile" } },
                AlignmentFiles = new List<AlignmentFileBean> { new AlignmentFileBean { FileID = 2, FileName = "TestAlignmentFile" } },
                MspDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 3, Name = "TestMspRef" } },
                TextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 4, Name = "TestTextRef" } },
                IsotopeTextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 5, Name = "TestIsotopeRef" } },
                MsdialImmsParameter = new MsdialImmsParameter { ProjectFileName = "TestProjectPath", ProviderFactoryParameter = new ImmsTicDataProviderFactoryParameter(3, 6) },
                IupacDatabase = new IupacDatabase { Id2AtomElementProperties = new Dictionary<int, List<AtomElementProperty>> { { 6, new List<AtomElementProperty> { new AtomElementProperty { ElementName = "TestElement" } } } } },
                DataBaseMapper = new DataBaseMapper { },
                DataBases = DataBaseStorage.CreateEmpty(),
            };
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 7, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB);
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            var dbs = storage.DataBases;
            ImmsMspAnnotator annotator = new ImmsMspAnnotator(db, searchParameter, TargetOmics.Metabolomics, "DummyAnnotator", 8);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, storage.MsdialImmsParameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true))
                }
            );
            storage.DataBaseMapper.Add(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, dbs.MetabolomicsDataBases[0].DataBase);

            var memory = new MemoryStream();
            var serializer = new MsdialIntegrateSerializer();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await serializer.SaveAsync(storage, manager, "Test", "TestFolder");
                manager.Complete();
            }

            using (var manager = ZipStreamManager.OpenGet(memory)) {
                var actual = await serializer.LoadAsync(manager, "Test", null, "TestFolder");

                AreAnalysisFilesEqual(storage.AnalysisFiles, actual.AnalysisFiles);
                AreAlignmentFilesEqual(storage.AlignmentFiles, actual.AlignmentFiles);
                AreDBEqual(storage.MspDB, actual.MspDB);
                AreDBEqual(storage.TextDB, actual.TextDB);
                AreDBEqual(storage.IsotopeTextDB, actual.IsotopeTextDB);
                AreIupacDatabaseEqual(storage.IupacDatabase, actual.IupacDatabase);
                AreDataBaseMapperEqual(storage.DataBaseMapper, actual.DataBaseMapper);
                AreDataBaseStorageEqual(storage.DataBases, actual.DataBases);

                Assert.AreEqual(storage.MsdialImmsParameter.ProjectFileName, actual.Parameter.ProjectFileName);
                Console.WriteLine(actual.Parameter.GetType().FullName);
                Assert.IsTrue(actual.Parameter is MsdialImmsParameter);
                Assert.IsTrue(((MsdialImmsParameter)actual.Parameter).ProviderFactoryParameter is ImmsTicDataProviderFactoryParameter);
                Assert.AreEqual(((ImmsTicDataProviderFactoryParameter)storage.MsdialImmsParameter.ProviderFactoryParameter).TimeBegin, ((ImmsTicDataProviderFactoryParameter)((MsdialImmsParameter)actual.Parameter).ProviderFactoryParameter).TimeBegin);
                Assert.AreEqual(((ImmsTicDataProviderFactoryParameter)storage.MsdialImmsParameter.ProviderFactoryParameter).TimeEnd, ((ImmsTicDataProviderFactoryParameter)((MsdialImmsParameter)actual.Parameter).ProviderFactoryParameter).TimeEnd);
            }
        }

        [TestMethod()]
        public async Task LcImMsLoadTest() {
            var storage = new MsdialLcImMsDataStorage
            {
                AnalysisFiles = new List<AnalysisFileBean> { new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "TestFile" } },
                AlignmentFiles = new List<AlignmentFileBean> { new AlignmentFileBean { FileID = 2, FileName = "TestAlignmentFile" } },
                MspDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 3, Name = "TestMspRef" } },
                TextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 4, Name = "TestTextRef" } },
                IsotopeTextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 5, Name = "TestIsotopeRef" } },
                MsdialLcImMsParameter = new MsdialLcImMsParameter { ProjectFileName = "TestProjectPath", DriftTimeBegin = 3f },
                IupacDatabase = new IupacDatabase { Id2AtomElementProperties = new Dictionary<int, List<AtomElementProperty>> { { 6, new List<AtomElementProperty> { new AtomElementProperty { ElementName = "TestElement" } } } } },
                DataBaseMapper = new DataBaseMapper { },
                DataBases = DataBaseStorage.CreateEmpty(),
            };
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 7, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB);
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            var dbs = storage.DataBases;
            var annotator = new LcimmsMspAnnotator(db, searchParameter, TargetOmics.Metabolomics, "DummyAnnotator", 8);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, storage.MsdialLcImMsParameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true))
                }
            );

            storage.DataBaseMapper.Add(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, dbs.MetabolomicsDataBases[0].DataBase);

            var memory = new MemoryStream();
            var serializer = new MsdialIntegrateSerializer();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await serializer.SaveAsync(storage, manager, "Test", "TestFolder");
                manager.Complete();
            }

            using (var manager = ZipStreamManager.OpenGet(memory)) {
                var actual = await serializer.LoadAsync(manager, "Test", null, "TestFolder");

                AreAnalysisFilesEqual(storage.AnalysisFiles, actual.AnalysisFiles);
                AreAlignmentFilesEqual(storage.AlignmentFiles, actual.AlignmentFiles);
                AreDBEqual(storage.MspDB, actual.MspDB);
                AreDBEqual(storage.TextDB, actual.TextDB);
                AreDBEqual(storage.IsotopeTextDB, actual.IsotopeTextDB);
                AreIupacDatabaseEqual(storage.IupacDatabase, actual.IupacDatabase);
                AreDataBaseMapperEqual(storage.DataBaseMapper, actual.DataBaseMapper);
                AreDataBaseStorageEqual(storage.DataBases, actual.DataBases);

                Assert.AreEqual(storage.MsdialLcImMsParameter.ProjectFileName, actual.Parameter.ProjectFileName);
                Console.WriteLine(actual.Parameter.GetType().FullName);
                Assert.IsTrue(actual.Parameter is MsdialLcImMsParameter);
                Assert.AreEqual(storage.MsdialLcImMsParameter.DriftTimeBegin, ((MsdialLcImMsParameter)actual.Parameter).DriftTimeBegin);
            }
        }

        [TestMethod()]
        public async Task GcmsLoadTest() {
            var storage = new MsdialGcmsDataStorage
            {
                AnalysisFiles = new List<AnalysisFileBean> { new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "TestFile" } },
                AlignmentFiles = new List<AlignmentFileBean> { new AlignmentFileBean { FileID = 2, FileName = "TestAlignmentFile" } },
                MspDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 3, Name = "TestMspRef" } },
                TextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 4, Name = "TestTextRef" } },
                IsotopeTextDB = new List<MoleculeMsReference> { new MoleculeMsReference { DatabaseID = 5, Name = "TestIsotopeRef" } },
                MsdialGcmsParameter = new MsdialGcmsParameter { ProjectFileName = "TestProjectPath", RetentionIndexAlignmentTolerance = 30f, },
                IupacDatabase = new IupacDatabase { Id2AtomElementProperties = new Dictionary<int, List<AtomElementProperty>> { { 6, new List<AtomElementProperty> { new AtomElementProperty { ElementName = "TestElement" } } } } },
                DataBaseMapper = new DataBaseMapper { },
                DataBases = DataBaseStorage.CreateEmpty(),
            };
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 7, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB);
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            var dbs = storage.DataBases;
            MassAnnotator annotator = new MassAnnotator(db, searchParameter, TargetOmics.Metabolomics, SourceType.MspDB, "DummyAnnotator", 8);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, storage.MsdialGcmsParameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true))
                }
            );

            storage.DataBaseMapper.Add(dbs.MetabolomicsDataBases[0].Pairs[0].AnnotatorID, dbs.MetabolomicsDataBases[0].DataBase);

            var memory = new MemoryStream();
            var serializer = new MsdialIntegrateSerializer();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await serializer.SaveAsync(storage, manager, "Test", "TestFolder");
                manager.Complete();
            }

            using (var manager = ZipStreamManager.OpenGet(memory)) {
                var actual = await serializer.LoadAsync(manager, "Test", null, "TestFolder");

                AreAnalysisFilesEqual(storage.AnalysisFiles, actual.AnalysisFiles);
                AreAlignmentFilesEqual(storage.AlignmentFiles, actual.AlignmentFiles);
                AreDBEqual(storage.MspDB, actual.MspDB);
                AreDBEqual(storage.TextDB, actual.TextDB);
                AreDBEqual(storage.IsotopeTextDB, actual.IsotopeTextDB);
                AreIupacDatabaseEqual(storage.IupacDatabase, actual.IupacDatabase);
                AreDataBaseMapperEqual(storage.DataBaseMapper, actual.DataBaseMapper);
                AreDataBaseStorageEqual(storage.DataBases, actual.DataBases);

                Assert.AreEqual(storage.MsdialGcmsParameter.ProjectFileName, actual.Parameter.ProjectFileName);
                Console.WriteLine(actual.Parameter.GetType().FullName);
                Assert.IsTrue(actual.Parameter is MsdialGcmsParameter);
                Assert.AreEqual(storage.MsdialGcmsParameter.RetentionIndexAlignmentTolerance, ((MsdialGcmsParameter)actual.Parameter).RetentionIndexAlignmentTolerance);
            }
        }



        private static void AreAnalysisFilesEqual(List<AnalysisFileBean> expected, List<AnalysisFileBean> actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach ((var exp, var act) in expected.Zip(actual)) {
                Assert.AreEqual(exp.AnalysisFileId, act.AnalysisFileId);
                Assert.AreEqual(exp.AnalysisFileName, act.AnalysisFileName);
            }
        }

        private static void AreAlignmentFilesEqual(List<AlignmentFileBean> expected, List<AlignmentFileBean> actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach ((var exp, var act) in expected.Zip(actual)) {
                Assert.AreEqual(exp.FileID, act.FileID);
                Assert.AreEqual(exp.FileName, act.FileName);
            }
        }

        private static void AreDBEqual(List<MoleculeMsReference> expected, List<MoleculeMsReference> actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach ((var exp, var act) in expected.Zip(actual)) {
                Assert.AreEqual(exp.DatabaseID, act.DatabaseID);
                Assert.AreEqual(exp.Name, act.Name);
            }
        }

        private static void AreIupacDatabaseEqual(IupacDatabase expected, IupacDatabase actual) {
            CollectionAssert.AreEquivalent(expected.Id2AtomElementProperties.Keys, actual.Id2AtomElementProperties.Keys);
            Assert.IsTrue(actual.Id2AtomElementProperties.ContainsKey(6));
            Assert.AreEqual(expected.Id2AtomElementProperties[6].Count, actual.Id2AtomElementProperties[6].Count);
            Assert.AreEqual(expected.Id2AtomElementProperties[6][0].ElementName, actual.Id2AtomElementProperties[6][0].ElementName);
            foreach (var key in expected.Id2AtomElementProperties.Keys) {
                Assert.AreEqual(expected.Id2AtomElementProperties[key].Count, actual.Id2AtomElementProperties[key].Count);
                foreach ((var exp, var act) in expected.Id2AtomElementProperties[key].Zip(actual.Id2AtomElementProperties[key])) {
                    Assert.AreEqual(exp.ElementName, act.ElementName);
                }
            }
        }

        private static void AreDataBaseMapperEqual(DataBaseMapper expected, DataBaseMapper actual) {

        }

        private static void AreDataBaseStorageEqual(DataBaseStorage expected, DataBaseStorage actual) {
            Assert.AreEqual(expected.MetabolomicsDataBases.Count, actual.MetabolomicsDataBases.Count);
            foreach ((var exp, var act) in expected.MetabolomicsDataBases.Zip(actual.MetabolomicsDataBases)) {
                Assert.AreEqual(exp.DataBaseID, act.DataBaseID);
                Assert.AreEqual(exp.DataBase.Id, act.DataBase.Id);
                Assert.AreEqual(exp.DataBase.SourceType, act.DataBase.SourceType);
                Assert.AreEqual(exp.DataBase.DataBaseSource, act.DataBase.DataBaseSource);
                foreach ((var expItem, var actItem) in exp.DataBase.Database.Zip(act.DataBase.Database)) {
                    Assert.AreEqual(expItem.DatabaseID, actItem.DatabaseID);
                    Assert.AreEqual(expItem.Name, actItem.Name);
                }
                Assert.AreEqual(exp.Pairs.Count, act.Pairs.Count);
                foreach ((var expPair, var actPair) in exp.Pairs.Zip(act.Pairs)) {
                    Assert.AreEqual(expPair.AnnotatorID, actPair.AnnotatorID);
                    Assert.AreEqual(expPair.AnnotationQueryFactory.GetType().FullName, actPair.AnnotationQueryFactory.GetType().FullName);
                }
            }
        }
    }
}