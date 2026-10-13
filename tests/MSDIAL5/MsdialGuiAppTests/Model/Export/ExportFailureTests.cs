using CompMs.App.Msdial.Model.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Export.Tests
{
    [TestClass()]
    public class ExportFailureTests
    {
        [TestMethod()]
        public void AMissingRawDataFileNamesTheSampleAndWhatCanStillBeExported() {
            var description = ExportFailure.Describe(new RawDataNotAvailableException("sample", @"C:\data\sample.mzML"));

            StringAssert.StartsWith(description, "The export was not completed because the raw data file of sample was not found:\n  C:\\data\\sample.mzML");
            StringAssert.Contains(description, "choose the deconvoluted spectra to export them");
        }

        [TestMethod()]
        public void AFileThatCouldNotBeReadInsideAnAggregateIsNamed() {
            var error = new AggregateException(new FileLoadException(@"Loading C:\data\sample.wiff failed."));

            var description = ExportFailure.Describe(error);

            Assert.AreEqual("The export was not completed because a file it reads was not found or could not be read:\n  Loading C:\\data\\sample.wiff failed.", description);
        }

        [TestMethod()]
        public void AMissingFileIsNamedByItsFileName() {
            var description = ExportFailure.Describe(new FileNotFoundException("Could not find the file.", @"C:\data\sample.dcl"));

            Assert.AreEqual("The export was not completed because a file it reads was not found or could not be read:\n  C:\\data\\sample.dcl", description);
        }

        [TestMethod()]
        public void APlainIOExceptionIsStillAboutWriting() {
            var description = ExportFailure.Describe(new IOException("There is not enough space on the disk."));

            StringAssert.StartsWith(description, "MS-DIAL could not write the exported files.");
        }
    }
}
