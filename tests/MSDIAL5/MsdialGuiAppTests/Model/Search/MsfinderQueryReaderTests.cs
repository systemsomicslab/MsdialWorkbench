using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Tests.Search {
    [TestClass]
    public class MsfinderQueryReaderTests {
        [TestMethod]
        public void ReadAllRejectsAnEmptyProjectWithItsFolderPath() {
            var error = Assert.ThrowsException<InvalidDataException>(() =>
                MsfinderQueryReader.ReadAll(Array.Empty<MsfinderQueryFile>(), new AnalysisParamOfMsfinder(), "empty-project"));

            StringAssert.Contains(error.Message, "empty-project");
            StringAssert.Contains(error.Message, "*.mat");
        }

        [TestMethod]
        public void ReadAllReportsTheMissingFileInsteadOfReturningPartialResults() {
            var file = Path.GetTempFileName();
            var missing = file + ".missing.mat";
            try {
                File.WriteAllText(file, "NAME: valid peak\nPRECURSORMZ: 100\n");
                var queries = new[] { new MsfinderQueryFile(file), new MsfinderQueryFile(missing) };
                var error = Assert.ThrowsException<InvalidDataException>(() =>
                    MsfinderQueryReader.ReadAll(queries, new AnalysisParamOfMsfinder(), Path.GetDirectoryName(file)));

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
        public void ReadAllReturnsValidInputsInOrder() {
            var first = Path.GetTempFileName();
            var second = Path.GetTempFileName();
            try {
                File.WriteAllText(first, "NAME: first\nPRECURSORMZ: 100\n");
                File.WriteAllText(second, "NAME: second\nPRECURSORMZ: 200\n");
                var queries = new[] { new MsfinderQueryFile(first), new MsfinderQueryFile(second) };

                var result = MsfinderQueryReader.ReadAll(queries, new AnalysisParamOfMsfinder(), Path.GetDirectoryName(first));

                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("first", result[0].Name);
                Assert.AreEqual("second", result[1].Name);
            }
            finally {
                File.Delete(first);
                File.Delete(second);
            }
        }
    }
}
