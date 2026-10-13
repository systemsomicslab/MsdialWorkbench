using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    [TestClass()]
    public class MobilitySettingModelTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-mobilitysetting-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void TheCalibrationOfAFileWithoutRawDataIsReadOnceTheFileIsBack() {
            var parameter = new MsdialImmsParameter();
            var files = new List<AnalysisFileBean> { Sample(0, "a", raw: false), Sample(1, "b"), };
            var model = new MobilitySettingModel(parameter, files, ProcessOption.All);

            CollectionAssert.AreEquivalent(new[] { 1 }, parameter.FileID2CcsCoefficients.Keys.ToArray(), "Nothing is stored for a file that was not read.");

            File.WriteAllBytes(files[0].AnalysisFilePath, new byte[0]);
            model.ReadCalibrationOfRestoredFiles();

            var row = model.CalibrationInfoCollection[0];
            row.AgilentBeta = 0.5;
            Assert.AreEqual(0.5, parameter.FileID2CcsCoefficients[0].AgilentBeta, "The row shown for the file edits the coefficients processing reads.");
        }

        [TestMethod()]
        public void TheSettingsViewReadsTheCalibrationOfARestoredFileBeforeShowingIt() {
            var parameter = new MsdialImmsParameter();
            var files = new List<AnalysisFileBean> { Sample(0, "a", raw: false), };
            var model = new MobilitySettingModel(parameter, files, ProcessOption.All);
            File.WriteAllBytes(files[0].AnalysisFilePath, new byte[0]);

            using var viewModel = new MobilitySettingViewModel(model, Observable.Return(true));

            Assert.IsTrue(parameter.FileID2CcsCoefficients.ContainsKey(0));
            viewModel.CalibrationInfoCollection[0].WatersT0 = 1.5;
            Assert.AreEqual(1.5, parameter.FileID2CcsCoefficients[0].WatersT0);
        }

        [TestMethod()]
        public void CommittingStoresTheCalibrationOfARestoredFile() {
            var parameter = new MsdialImmsParameter();
            var files = new List<AnalysisFileBean> { Sample(0, "a", raw: false), };
            var model = new MobilitySettingModel(parameter, files, ProcessOption.All);
            File.WriteAllBytes(files[0].AnalysisFilePath, new byte[0]);

            model.IsAllCalibrantDataImported = true;
            Assert.IsTrue(model.TryCommit());

            Assert.IsTrue(parameter.FileID2CcsCoefficients.ContainsKey(0));
        }

        [TestMethod()]
        public void SettingsBuiltAfterTheFileIsBackShareTheStoredCalibration() {
            var parameter = new MsdialImmsParameter();
            var files = new List<AnalysisFileBean> { Sample(0, "a", raw: false), };
            var first = new MobilitySettingModel(parameter, files, ProcessOption.All);
            File.WriteAllBytes(files[0].AnalysisFilePath, new byte[0]);
            var second = new MobilitySettingModel(parameter, files, ProcessOption.Alignment);

            first.ReadCalibrationOfRestoredFiles();
            first.CalibrationInfoCollection[0].AgilentTFix = 2.5;

            Assert.AreEqual(2.5, second.CalibrationInfoCollection[0].AgilentTFix);
        }

        private AnalysisFileBean Sample(int id, string name, bool raw = true) {
            var file = new AnalysisFileBean
            {
                AnalysisFileId = id,
                AnalysisFileName = name,
                AnalysisFilePath = Path.Combine(_folder, name + ".mzML"),
            };
            if (raw) {
                File.WriteAllBytes(file.AnalysisFilePath, new byte[0]);
            }
            return file;
        }
    }
}
