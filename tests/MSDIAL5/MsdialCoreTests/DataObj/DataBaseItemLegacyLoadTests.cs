using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class DataBaseItemLegacyLoadTests
    {
        [TestMethod]
        public void LoadsLegacyNestedDatabaseArchiveLayout() {
            var dataBase = new MockReferenceDataBase("mock-db");
            var item = new DataBaseItem<MockReferenceDataBase>(dataBase, new List<IAnnotatorParameterPair<MockReferenceDataBase>>());

            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
                var legacyArchiveEntry = archive.CreateEntry("root/mock-db");
                using var legacyStream = legacyArchiveEntry.Open();
                using (var nested = new ZipArchive(legacyStream, ZipArchiveMode.Create, leaveOpen: true)) {
                    var dataBaseEntry = nested.CreateEntry("DataBase");
                    using (var dbStream = dataBaseEntry.Open()) {
                        dataBase.Save(dbStream, false);
                    }
                }
            }

            stream.Position = 0;
            using var readArchive = new ZipArchive(stream, ZipArchiveMode.Read);

            item.Load(readArchive, "root", new NoopLoadAnnotatorVisitor(), new NoopAnnotationQueryFactoryGenerationVisitor(), "project");

            Assert.AreEqual(dataBase.Id, item.DataBase.Id);
        }

        private sealed class MockReferenceDataBase : IReferenceDataBase
        {
            public MockReferenceDataBase(string id) {
                Id = id;
            }

            public string Id { get; }

            public void Save(Stream stream, bool forceSerialize = false) {
                using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, leaveOpen: true);
                writer.Write(Id);
            }

            public void Load(Stream stream, string folderpath) {
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, false, 1024, leaveOpen: true);
                var actual = reader.ReadToEnd();
                Assert.AreEqual(Id, actual);
            }
        }

        private sealed class NoopLoadAnnotatorVisitor : ILoadAnnotatorVisitor
        {
            public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) => throw new NotImplementedException();
            public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) => throw new NotImplementedException();
            public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) => throw new NotImplementedException();
            public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) => throw new NotImplementedException();
            public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) => throw new NotImplementedException();
        }

        private sealed class NoopAnnotationQueryFactoryGenerationVisitor : IAnnotationQueryFactoryGenerationVisitor
        {
            public IAnnotationQueryFactory<MsScanMatchResult> Visit(StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) => throw new NotImplementedException();
            public IAnnotationQueryFactory<MsScanMatchResult> Visit(MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) => throw new NotImplementedException();
            public IAnnotationQueryFactory<MsScanMatchResult> Visit(TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) => throw new NotImplementedException();
            public IAnnotationQueryFactory<MsScanMatchResult> Visit(ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder) => throw new NotImplementedException();
            public IAnnotationQueryFactory<MsScanMatchResult> Visit(EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder) => throw new NotImplementedException();
        }
    }
}
