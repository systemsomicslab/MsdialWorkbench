using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    [TestClass()]
    public class MassRecalibrationSettingModelTests
    {
        [TestMethod()]
        public void ShowsTheProjectsSettings() {
            var parameter = new MsdialLcmsParameter();
            parameter.MassRecalibrationParam = new MassRecalibrationParameter { IsEnabled = true, SearchPpm = 60d, MinimumCorrectionPpm = 2.5d };

            var model = new MassRecalibrationSettingModel(parameter, ProcessOption.All);

            Assert.IsTrue(model.Recalibrate);
            Assert.AreEqual(60d, model.SearchPpm);
            Assert.AreEqual(2.5d, model.MinimumCorrectionPpm);
            Assert.IsFalse(model.IsReadOnly);
        }

        [TestMethod()]
        public void ANewProjectIsNotRecalibrated() {
            var model = new MassRecalibrationSettingModel(new MsdialLcmsParameter(), ProcessOption.All);

            Assert.IsFalse(model.Recalibrate);
            Assert.AreEqual(100d, model.SearchPpm);
            Assert.AreEqual(1.5d, model.MinimumCorrectionPpm);
        }

        [TestMethod()]
        public void CommitWritesTheSettingsIntoTheProjectsParameter() {
            var parameter = new MsdialLcmsParameter();
            var model = new MassRecalibrationSettingModel(parameter, ProcessOption.All)
            {
                Recalibrate = true,
                SearchPpm = 40d,
                MinimumCorrectionPpm = 0d,
            };

            Assert.IsTrue(model.TryCommit());

            Assert.IsTrue(parameter.MassRecalibrationParam.IsEnabled);
            Assert.AreEqual(40d, parameter.MassRecalibrationParam.SearchPpm);
            Assert.AreEqual(0d, parameter.MassRecalibrationParam.MinimumCorrectionPpm);
        }

        [TestMethod()]
        public void SettingsThatCannotRunAreNotCommitted() {
            var parameter = new MsdialLcmsParameter();
            var model = new MassRecalibrationSettingModel(parameter, ProcessOption.All)
            {
                Recalibrate = true,
                SearchPpm = 0d,
            };

            Assert.IsFalse(model.TryCommit());

            StringAssert.Contains(model.Problem(), "search window");
            Assert.IsFalse(parameter.MassRecalibrationParam.IsEnabled, "the project keeps what it had");
            Assert.AreEqual(100d, parameter.MassRecalibrationParam.SearchPpm);
        }

        [TestMethod()]
        public void WithoutPeakPickingTheSettingsAreReadOnlyAndLeftAlone() {
            // The recalibration is decided where the peaks are picked; a run from identification or alignment keeps the
            // files' recalibrations and the settings they were made with.
            var parameter = new MsdialLcmsParameter();
            parameter.MassRecalibrationParam = new MassRecalibrationParameter { IsEnabled = true, SearchPpm = 60d };
            var model = new MassRecalibrationSettingModel(parameter, ProcessOption.IdentificationPlusAlignment)
            {
                Recalibrate = false,
            };

            Assert.IsTrue(model.IsReadOnly);
            Assert.IsTrue(model.TryCommit(), "it does not stop the run");
            Assert.IsTrue(parameter.MassRecalibrationParam.IsEnabled);
            Assert.AreEqual(60d, parameter.MassRecalibrationParam.SearchPpm);
        }

        [TestMethod()]
        public void ALoadedParameterFileIsShown() {
            var model = new MassRecalibrationSettingModel(new MsdialLcmsParameter(), ProcessOption.All);
            var loaded = new MsdialLcmsParameter();
            loaded.MassRecalibrationParam = new MassRecalibrationParameter { IsEnabled = true, SearchPpm = 30d, MinimumCorrectionPpm = 3d };

            model.LoadParameter(loaded);

            Assert.IsTrue(model.Recalibrate);
            Assert.AreEqual(30d, model.SearchPpm);
            Assert.AreEqual(3d, model.MinimumCorrectionPpm);
        }
    }
}
