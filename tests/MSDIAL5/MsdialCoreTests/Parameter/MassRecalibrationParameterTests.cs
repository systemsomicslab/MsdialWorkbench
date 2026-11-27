using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.MassCalibration;
using CompMs.MsdialCore.DataObj;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Parameter.Tests
{
    [TestClass]
    public class MassRecalibrationParameterTests
    {
        [TestMethod]
        public void ByDefaultNothingIsRecalibrated() {
            var parameter = new ParameterBase().MassRecalibrationParam;

            Assert.IsFalse(parameter.IsEnabled);
            Assert.AreEqual(AnnotationCalibrantCollector.DefaultSearchPpm, parameter.SearchPpm);
            Assert.AreEqual(MassCalibrationDecision.DefaultMinimumCorrectionPpm, parameter.MinimumCorrectionPpm);
            Assert.IsNull(parameter.Problem());
        }

        [TestMethod]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        public void AProjectWrittenBeforeTheSettingExistedReadsAsOff() {
            var bytes = File.ReadAllBytes(Path.Combine("Resources", "Export", "Dataset_2025_07_31_12_31_11.mddata"));

            var storage = LZ4MessagePackSerializer.Deserialize<MsdialDataStorage>(bytes);

            Assert.IsNotNull(storage.ParameterBase);
            var parameter = storage.ParameterBase.MassRecalibrationParam;
            Assert.IsNotNull(parameter);
            Assert.IsFalse(parameter.IsEnabled);
            Assert.AreEqual(AnnotationCalibrantCollector.DefaultSearchPpm, parameter.SearchPpm);
        }

        [TestMethod]
        public void TheSettingsSurviveSavingAndLoadingTheProjectParameter() {
            var parameter = new ParameterBase();
            parameter.MassRecalibrationParam = new MassRecalibrationParameter { IsEnabled = true, SearchPpm = 60d, MinimumCorrectionPpm = 2.5d };

            var read = MessagePackSerializer.Deserialize<ParameterBase>(MessagePackSerializer.Serialize(parameter));

            Assert.IsTrue(read.MassRecalibrationParam.IsEnabled);
            Assert.AreEqual(60d, read.MassRecalibrationParam.SearchPpm);
            Assert.AreEqual(2.5d, read.MassRecalibrationParam.MinimumCorrectionPpm);
        }

        [TestMethod]
        public void AWindowOrMinimumThatCannotRunIsNamed() {
            StringAssert.Contains(new MassRecalibrationParameter { SearchPpm = 0d }.Problem(), "search window");
            StringAssert.Contains(new MassRecalibrationParameter { SearchPpm = double.NaN }.Problem(), "search window");
            StringAssert.Contains(new MassRecalibrationParameter { MinimumCorrectionPpm = -1d }.Problem(), "minimum correction");
            Assert.IsNull(new MassRecalibrationParameter { MinimumCorrectionPpm = 0d }.Problem(), "zero applies every modelled error that is significant");
        }

        [TestMethod]
        public void TheParameterTextUsesTheMethodFileKeys() {
            var lines = new MassRecalibrationParameter { IsEnabled = true, SearchPpm = 60d, MinimumCorrectionPpm = 2.5d }.ParametersAsText();

            CollectionAssert.AreEqual(new[]
            {
                "Mass recalibration: True",
                "Mass recalibration search window (ppm): 60",
                "Mass recalibration minimum correction (ppm): 2.5",
            }, lines.ToArray());
        }

        [TestMethod]
        public void ARecalibrationThatIsNotAskedForIsNotBuilt() {
            var process = MassRecalibrationProcess.FromParameter(new MassRecalibrationParameter(), DataBaseStorage.CreateEmpty(), TargetOmics.Lipidomics, out var refusal);

            Assert.IsNull(process);
            Assert.IsNull(refusal);
        }

        [TestMethod]
        public void ARecalibrationWithoutALibraryIsRefused() {
            var process = MassRecalibrationProcess.FromParameter(new MassRecalibrationParameter { IsEnabled = true }, DataBaseStorage.CreateEmpty(), TargetOmics.Lipidomics, out var refusal);

            Assert.IsNull(process);
            StringAssert.Contains(refusal, "needs an MSP or LBM library");
        }

        [TestMethod]
        public void ARecalibrationWithAWindowThatCannotRunIsRefused() {
            var process = MassRecalibrationProcess.FromParameter(new MassRecalibrationParameter { IsEnabled = true, SearchPpm = -5d }, DataBaseStorage.CreateEmpty(), TargetOmics.Lipidomics, out var refusal);

            Assert.IsNull(process);
            StringAssert.Contains(refusal, "search window");
        }
    }
}
