using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    /// <summary>
    /// A project saved before raw data formats were recorded learns each file's format the first time the GUI finds its
    /// raw data, and a file pointed at other raw data is described by those.
    /// </summary>
    [TestClass()]
    public class AnalysisFileBeanModelRawFileFormatTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-analysisfile-format-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void RefreshingFindsAndRecordsTheFormatOfAnOldProjectsFile() {
            var bean = new AnalysisFileBean { AnalysisFilePath = CreateThermoRaw("sample.raw"), };
            var model = new AnalysisFileBeanModel(bean);

            Assert.IsTrue(model.RefreshRawDataAvailability());

            Assert.AreEqual(RawFileFormat.ThermoRaw, bean.RawFileFormat);
        }

        [TestMethod()]
        public void TheFirstCheckOfTheRawDataAlsoRecordsTheFormat() {
            var folder = Path.Combine(_folder, "sample.d");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "analysis.tdf"), new byte[0]);
            var bean = new AnalysisFileBean { AnalysisFilePath = folder, };
            var model = new AnalysisFileBeanModel(bean);

            Assert.IsTrue(model.IsRawDataAvailable);

            Assert.AreEqual(RawFileFormat.BrukerTdf, bean.RawFileFormat);
        }

        [TestMethod()]
        public void AFileWhoseRawDataAreMissingStaysUnknown() {
            var bean = new AnalysisFileBean { AnalysisFilePath = Path.Combine(_folder, "gone.raw"), };
            var model = new AnalysisFileBeanModel(bean);

            Assert.IsFalse(model.RefreshRawDataAvailability());
            Assert.IsFalse(model.IsRawDataAvailable);

            Assert.AreEqual(RawFileFormat.Unknown, bean.RawFileFormat);
        }

        [TestMethod()]
        public void ARecordedFormatIsNotReplacedByARefresh() {
            var bean = new AnalysisFileBean { AnalysisFilePath = CreateThermoRaw("sample.raw"), RawFileFormat = RawFileFormat.WatersRaw, };
            var model = new AnalysisFileBeanModel(bean);

            Assert.IsTrue(model.RefreshRawDataAvailability());

            Assert.AreEqual(RawFileFormat.WatersRaw, bean.RawFileFormat);
        }

        [TestMethod()]
        public void PointingAtOtherRawDataDescribesThem() {
            var bean = new AnalysisFileBean { AnalysisFilePath = CreateThermoRaw("thermo.raw"), RawFileFormat = RawFileFormat.ThermoRaw, };
            var model = new AnalysisFileBeanModel(bean);
            var waters = Path.Combine(_folder, "waters.raw");
            Directory.CreateDirectory(waters);

            model.AnalysisFilePath = waters;
            Assert.AreEqual(RawFileFormat.WatersRaw, bean.RawFileFormat);

            model.AnalysisFilePath = Path.Combine(_folder, "sample.mzML");
            Assert.AreEqual(RawFileFormat.MzML, bean.RawFileFormat);

            // What was recorded for the previous file does not describe one that cannot be told.
            model.AnalysisFilePath = Path.Combine(_folder, "gone.d");
            Assert.AreEqual(RawFileFormat.Unknown, bean.RawFileFormat);
        }

        [TestMethod()]
        public void SettingTheSamePathKeepsTheRecordedFormat() {
            var path = Path.Combine(_folder, "gone.raw");
            var bean = new AnalysisFileBean { AnalysisFilePath = path, RawFileFormat = RawFileFormat.ThermoRaw, };
            var model = new AnalysisFileBeanModel(bean);

            model.AnalysisFilePath = path;

            Assert.AreEqual(RawFileFormat.ThermoRaw, bean.RawFileFormat);
        }

        [TestMethod()]
        public void MarkingTheRawDataMissingKeepsTheRecordedFormat() {
            var bean = new AnalysisFileBean { AnalysisFilePath = Path.Combine(_folder, "gone", "sample.d"), RawFileFormat = RawFileFormat.BrukerTdf, };
            var model = new AnalysisFileBeanModel(bean);

            model.MarkRawDataMissing();
            Assert.AreEqual(RawFileFormat.BrukerTdf, bean.RawFileFormat);

            // The collection marks the samples of a folder that is gone without checking each of them.
            var collection = new AnalysisFileBeanModelCollection(new[] { model });
            CollectionAssert.AreEqual(new[] { model }, collection.RefreshRawDataAvailability().ToArray());
            Assert.AreEqual(RawFileFormat.BrukerTdf, bean.RawFileFormat);
        }

        private string CreateThermoRaw(string name) {
            var path = Path.Combine(_folder, name);
            File.WriteAllBytes(path, new byte[0]);
            return path;
        }
    }
}
