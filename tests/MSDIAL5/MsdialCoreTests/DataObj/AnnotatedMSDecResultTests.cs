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
            var msdecResult = new MSDecResult();
            var reference = new MoleculeMsReference();
            var obj = new AnnotatedMSDecResult(msdecResult, matchResultContainer, reference);

            var memory = new MemoryStream();
            obj.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = AnnotatedMSDecResult.Load(memory);

            Assert.AreEqual(obj, actual);
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