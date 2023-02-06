using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class MoleculeDataBaseTests
    {
        [TestMethod()]
        public void MoleculeDataBaseTest() {
            var db = new MoleculeDataBase(Array.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB);
            var expected = db;
            MoleculeDataBase actual;

            using (var stream = new MemoryStream()) {
                Common.MessagePack.MessagePackDefaultHandler.SaveToStream<MoleculeDataBase>(db, stream);
                actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<MoleculeDataBase>(stream);
            }

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.SourceType, actual.SourceType);
            Assert.AreEqual(expected.DataBaseSource, actual.DataBaseSource);
        }

        [TestMethod()]
        public void SaveLoadTest() {
            var references = new[]
            {
                new MoleculeMsReference { ScanID = 0, Name = "A", Comment = "a", },
                new MoleculeMsReference { ScanID = 1, Name = "B", Comment = "b", },
                new MoleculeMsReference { ScanID = 2, Name = "C", Comment = "c", },
                new MoleculeMsReference { ScanID = 3, Name = "D", Comment = "d", },
                new MoleculeMsReference { ScanID = 4, Name = "E", Comment = "e", },
            };
            var db = new MoleculeDataBase(references.ToList(), "DB", DataBaseSource.Msp, SourceType.MspDB);
            var expected = references;

            using (var stream = new MemoryStream()) {
                db.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                db.Load(stream, null);
            }

            CollectionAssert.AreEqual(references.Select(item => item.ScanID).ToArray(), db.Database.Select(item => item.ScanID).ToArray());
            CollectionAssert.AreEqual(references.Select(item => item.Name).ToArray(), db.Database.Select(item => item.Name).ToArray());
            CollectionAssert.AreEqual(references.Select(item => item.Comment).ToArray(), db.Database.Select(item => item.Comment).ToArray());
        }

        [TestMethod()]
        public void ReferenceTest() {
            var references = new[]
            {
                new MoleculeMsReference { ScanID = 0, Name = "A", Comment = "a", },
                new MoleculeMsReference { ScanID = 1, Name = "B", Comment = "b", },
                new MoleculeMsReference { ScanID = 2, Name = "C", Comment = "c", },
                new MoleculeMsReference { ScanID = 4, Name = "E", Comment = "e", },
            };
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> db = new MoleculeDataBase(references.ToList(), "DB", DataBaseSource.Msp, SourceType.MspDB);

            var actual = db.Refer(new MsScanMatchResult { LibraryID = 2, });
            var expected = references[2];
            Assert.AreEqual(expected, actual);

            actual = db.Refer(new MsScanMatchResult { LibraryID = 4, });
            expected = references[3];
            Assert.AreEqual(expected, actual);
        }
    }
}