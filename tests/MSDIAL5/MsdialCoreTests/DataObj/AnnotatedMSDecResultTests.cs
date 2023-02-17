using CompMs.Common.Components;
using CompMs.Common.Interfaces.Tests;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.MSDec.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class AnnotatedMSDecResultTests
    {
        [TestMethod()]
        public void SaveAndLoadTest() {
            var matchResultContainer = new MsScanMatchResultContainer();
            matchResultContainer.AddResult(new Common.DataObj.Result.MsScanMatchResult { LibraryID = 1, });
            var msdecResult = new MSDecResult
            {
                ScanID = 1,
            };
            var reference = new MoleculeMsReference
            {
                ScanID = 1,
            };
            var obj = new AnnotatedMSDecResult(msdecResult, matchResultContainer, reference);

            var memory = new MemoryStream();
            obj.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = AnnotatedMSDecResult.Load(memory);

            Assert.That.AreEqual(obj, actual);
        }
    }

    public static class AnnotatedMSDecResultTestHelper {
        public static void AreEqual(this Assert assert, AnnotatedMSDecResult expected, AnnotatedMSDecResult actual) {
            assert.AreEqual(expected.MSDecResult, actual.MSDecResult);
            assert.AreEqual(expected.MatchResults, actual.MatchResults);
            assert.AreEqual(expected.Molecule, actual.Molecule);
            Assert.AreEqual(expected.QuantMass, actual.QuantMass);
        }
    }
}