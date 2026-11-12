using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class MspDbRestorationKeyTests
    {
        [TestMethod()]
        public void MspDbRestorationKeyTest() {
            IReferRestorationKey key = new MspDbRestorationKey("MspKey", -1);
            IReferRestorationKey expected = key;
            IReferRestorationKey actual;

            using (var stream = new MemoryStream()) {
                Common.MessagePack.MessagePackDefaultHandler.SaveToStream<IReferRestorationKey>(key, stream);
                stream.Seek(0, SeekOrigin.Begin);
                actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<IReferRestorationKey>(stream);
            }

            Assert.AreEqual(expected.Key, actual.Key);
        }

        [TestMethod()]
        public void MspDbRestorationKeySerializedBytesTest() {
            IReferRestorationKey key = new MspDbRestorationKey("MspKey", -1);
            IReferRestorationKey expected = key;
            IReferRestorationKey actual;

            var bytes = Convert.FromBase64String("kgGCo0tleaZNc3BLZXmoUHJpb3JpdHn/");
            using (var stream = new MemoryStream(bytes)) {
                actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<IReferRestorationKey>(stream);
            }

            Assert.AreEqual(expected.Key, actual.Key);
        }

        [TestMethod()]
        public void TextDbRestorationKeyTest() {
            IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> key = new TextDbRestorationKey("TextKey", 3);
            IReferRestorationKey actual;

            using (var stream = new MemoryStream()) {
                Common.MessagePack.MessagePackDefaultHandler.SaveToStream<IReferRestorationKey>(key, stream);
                stream.Position = 0;
                actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<IReferRestorationKey>(stream);
            }

            Assert.AreEqual(key.Key, actual.Key);
            Assert.AreEqual(key.Priority, actual.Priority);
            Assert.IsInstanceOfType(actual, typeof(TextDbRestorationKey));
        }

        [TestMethod()]
        public void ShotgunProteomicsRestorationKeyTest() {
            var parameter = new CompMs.Common.Parameter.MsRefSearchParameterBase();
            var proteomicsParameter = RoundTrip(new CompMs.MsdialCore.Parameter.ProteomicsParameter());
            IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> key = new ShotgunProteomicsRestorationKey("ShotgunKey", 5, parameter, proteomicsParameter, CompMs.Common.DataObj.Result.SourceType.MspDB);
            var actual = RoundTrip<IReferRestorationKey>(key);

            Assert.AreEqual(key.Key, actual.Key);
            Assert.AreEqual(key.Priority, actual.Priority);
            Assert.AreEqual(CompMs.Common.DataObj.Result.SourceType.MspDB, ((ShotgunProteomicsRestorationKey)actual).SourceType);
            Assert.IsNotNull(((ShotgunProteomicsRestorationKey)actual).MsRefSearchParameter);
            Assert.IsNotNull(((ShotgunProteomicsRestorationKey)actual).ProteomicsParameter);
            Assert.IsInstanceOfType(actual, typeof(ShotgunProteomicsRestorationKey));
        }

        [TestMethod()]
        public void ProteomicsParameterRoundTripsDefaultPayload() {
            var actual = RoundTrip(new CompMs.MsdialCore.Parameter.ProteomicsParameter());

            Assert.IsNotNull(actual.VariableModifications);
            Assert.IsNotNull(actual.FixedModifications);
            Assert.IsTrue(actual.MaxNumberOfModificationsPerPeptide >= 0);
        }

        [TestMethod()]
        public void EadLipidDatabaseRestorationKeyTest() {
            var parameter = new CompMs.Common.Parameter.MsRefSearchParameterBase();
            IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> key = new EadLipidDatabaseRestorationKey("EadKey", 7, parameter, CompMs.Common.DataObj.Result.SourceType.MspDB);
            var actual = RoundTrip<IReferRestorationKey>(key);

            Assert.AreEqual(key.Key, actual.Key);
            Assert.AreEqual(key.Priority, actual.Priority);
            Assert.IsInstanceOfType(actual, typeof(EadLipidDatabaseRestorationKey));
        }

        [TestMethod()]
        public void AcceptTest() {
            var key = new MspDbRestorationKey("MspKey", -1);
            var visitor = new MockLoadAnnotator(key);
            key.Accept(visitor, null);
            Assert.IsTrue(visitor.Called);
        }

        private static T RoundTrip<T>(T value) {
            using var stream = new MemoryStream();
            Common.MessagePack.MessagePackDefaultHandler.SaveToStream(value, stream);
            stream.Position = 0;
            return Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<T>(stream);
        }
    }

    class MockLoadAnnotator : ILoadAnnotatorVisitor
    {
        private readonly IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> expected;

        public MockLoadAnnotator(IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> expected) {
            this.expected = expected;
        }

        public bool Called { get; private set; } = false;

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            Assert.AreEqual(expected, key);
            Called = true;
            return null;
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(TextDbRestorationKey key, EadLipidDatabase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) {
            throw new NotImplementedException();
        }
    }
}