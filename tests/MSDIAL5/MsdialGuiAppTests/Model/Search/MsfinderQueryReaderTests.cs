using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.App.Msdial.Model.Tests.Search {
    [TestClass]
    public class MsfinderQueryReaderTests {
        [TestMethod]
        public void EnsureNotEmptyRejectsAnEmptyProjectWithItsFolderPath() {
            var error = Assert.ThrowsException<InvalidDataException>(() =>
                MsfinderQueryReader.EnsureNotEmpty(0, "empty-project"));

            StringAssert.Contains(error.Message, "empty-project");
            StringAssert.Contains(error.Message, "*.mat");
        }

        [TestMethod]
        public void ReadReportsTheMissingFile() {
            var file = Path.GetTempFileName();
            var missing = file + ".missing.mat";
            try {
                File.WriteAllText(file, "NAME: valid peak\nPRECURSORMZ: 100\n");
                var error = Assert.ThrowsException<InvalidDataException>(() =>
                    MsfinderQueryReader.Read(new MsfinderQueryFile(missing), new AnalysisParamOfMsfinder()));

                StringAssert.Contains(error.Message, missing);
                Assert.IsInstanceOfType(error.InnerException, typeof(FileNotFoundException));
            }
            finally {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void ReadReportsMalformedInputAndPreservesTheCause() {
            var file = Path.GetTempFileName();
            try {
                File.WriteAllText(file, "NAME:");
                var error = Assert.ThrowsException<InvalidDataException>(() =>
                    MsfinderQueryReader.Read(new MsfinderQueryFile(file), new AnalysisParamOfMsfinder()));

                StringAssert.Contains(error.Message, file);
                Assert.IsNotNull(error.InnerException);
            }
            finally {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void ReadReturnsValidInput() {
            var first = Path.GetTempFileName();
            try {
                File.WriteAllText(first, "NAME: first\nPRECURSORMZ: 100\n");
                var result = MsfinderQueryReader.Read(new MsfinderQueryFile(first), new AnalysisParamOfMsfinder());
                Assert.AreEqual("first", result.Name);
            }
            finally {
                File.Delete(first);
            }
        }
    }
}
