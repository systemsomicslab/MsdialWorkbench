using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    [TestClass()]
    public class AnalysisFileBeanModelTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-analysisfile-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void IsRawDataAvailableFollowsTheRawDataPath() {
            var file = Path.Combine(_folder, "sample.mzML");
            File.WriteAllBytes(file, new byte[0]);
            var vendorFolder = Path.Combine(_folder, "sample.d");
            Directory.CreateDirectory(vendorFolder);

            Assert.IsTrue(Create(file).IsRawDataAvailable);
            Assert.IsTrue(Create(vendorFolder).IsRawDataAvailable);
            Assert.IsFalse(Create(Path.Combine(_folder, "missing.mzML")).IsRawDataAvailable);
            Assert.IsFalse(Create(string.Empty).IsRawDataAvailable);
        }

        [TestMethod()]
        public void RefreshRawDataAvailabilityRaisesPropertyChangedOnlyOnAChange() {
            var path = Path.Combine(_folder, "sample.mzML");
            var model = Create(path);
            Assert.IsFalse(model.IsRawDataAvailable);
            var changes = new List<string>();
            model.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            Assert.IsFalse(model.RefreshRawDataAvailability());
            Assert.AreEqual(0, changes.Count);

            File.WriteAllBytes(path, new byte[0]);
            Assert.IsTrue(model.RefreshRawDataAvailability());
            Assert.IsTrue(model.IsRawDataAvailable);
            CollectionAssert.AreEqual(new[] { nameof(AnalysisFileBeanModel.IsRawDataAvailable) }, changes);

            Assert.IsTrue(model.RefreshRawDataAvailability());
            Assert.AreEqual(1, changes.Count);
        }

        [TestMethod()]
        public void SettingTheRawDataPathChecksAgain() {
            var present = Path.Combine(_folder, "present.mzML");
            File.WriteAllBytes(present, new byte[0]);
            var model = Create(Path.Combine(_folder, "missing.mzML"));
            Assert.IsFalse(model.IsRawDataAvailable);
            var changes = new List<string>();
            model.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            model.AnalysisFilePath = present;

            CollectionAssert.Contains(changes, nameof(AnalysisFileBeanModel.IsRawDataAvailable));
            Assert.IsTrue(model.IsRawDataAvailable);
        }

        [TestMethod()]
        [DataRow(@"C:\data\sample_0.pai", @"C:\data\sample_0.pai2")]
        [DataRow(@"C:\data\sample_0.pai2", @"C:\data\sample_0.pai2")]
        [DataRow("", "")]
        public void PeakListFilePathIsTheFileOpeningReads(string recorded, string expected) {
            var model = AnalysisFileBeanModelTestHelper.Create();
            model.File.PeakAreaBeanInformationFilePath = recorded;

            Assert.AreEqual(expected, model.PeakListFilePath);
        }

        [TestMethod()]
        public void PeakListFileExistsFollowsTheFile() {
            var model = AnalysisFileBeanModelTestHelper.Create();
            model.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, "sample_0.pai");
            Assert.IsFalse(model.PeakListFileExists());

            File.WriteAllBytes(Path.Combine(_folder, "sample_0.pai2"), new byte[0]);
            Assert.IsTrue(model.PeakListFileExists());

            model.File.PeakAreaBeanInformationFilePath = string.Empty;
            Assert.IsFalse(model.PeakListFileExists());
        }

        private static AnalysisFileBeanModel Create(string analysisFilePath) {
            var model = AnalysisFileBeanModelTestHelper.Create();
            model.AnalysisFilePath = analysisFilePath;
            return model;
        }
    }
}
