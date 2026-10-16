using CompMs.MsdialCore.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    [TestClass()]
    public class DatasetFileSettingModelTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-datasetfilesetting-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        /// <summary>
        /// The files are registered while they are present, so their formats are recorded then, for mzTab-M to name
        /// after the raw data are gone.
        /// </summary>
        [TestMethod()]
        public void SetFilesRecordsTheFormatOfEachFile() {
            var thermo = Path.Combine(_folder, "a_thermo.raw");
            File.WriteAllBytes(thermo, new byte[0]);
            var waters = Path.Combine(_folder, "b_waters.raw");
            Directory.CreateDirectory(waters);
            var agilent = Path.Combine(_folder, "c_agilent.d");
            Directory.CreateDirectory(Path.Combine(agilent, "AcqData"));
            var bruker = Path.Combine(_folder, "d_bruker.d");
            Directory.CreateDirectory(bruker);
            File.WriteAllBytes(Path.Combine(bruker, "analysis.baf"), new byte[0]);
            var mzml = Path.Combine(_folder, "e_sample.mzML");
            File.WriteAllBytes(mzml, new byte[0]);
            var model = new DatasetFileSettingModel(new DateTime(2026, 10, 16));

            model.SetFiles(new[] { mzml, bruker, agilent, waters, thermo, });

            CollectionAssert.AreEqual(
                new[] { RawFileFormat.ThermoRaw, RawFileFormat.WatersRaw, RawFileFormat.AgilentMassHunterD, RawFileFormat.BrukerBaf, RawFileFormat.MzML, },
                model.FileModels.AnalysisFiles.Select(file => file.File.RawFileFormat).ToArray());
        }
    }
}
