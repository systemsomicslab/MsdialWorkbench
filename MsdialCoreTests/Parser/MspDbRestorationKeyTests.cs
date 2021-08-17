using CompMs.Common.Interfaces;
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
            IReferRestorationKey<MoleculeDataBase> key = new MspDbRestorationKey("MspKey");
            IReferRestorationKey<MoleculeDataBase> expected = key;
            IReferRestorationKey<MoleculeDataBase> actual;

            using (var stream = new MemoryStream()) {
                Common.MessagePack.MessagePackDefaultHandler.SaveToStream<IReferRestorationKey<MoleculeDataBase>>(key, stream);
                actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<IReferRestorationKey<MoleculeDataBase>>(stream);
            }

            Assert.AreEqual(expected.Key, actual.Key);
        }

        [TestMethod()]
        public void AcceptTest() {
            var key = new MspDbRestorationKey("MspKey");
            var visitor = new MockLoadAnnotator(key);
            key.Accept(visitor, null);
            Assert.IsTrue(visitor.Called);
        }
    }

    class MockLoadAnnotator : ILoadAnnotatorVisitor
    {
        private readonly IReferRestorationKey<MoleculeDataBase> expected;

        public MockLoadAnnotator(IReferRestorationKey<MoleculeDataBase> expected) {
            this.expected = expected;
        }

        public bool Called { get; private set; } = false;

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            Assert.AreEqual(expected, key);
            Called = true;
            return null;
        }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            throw new NotImplementedException();
        }
    }
}