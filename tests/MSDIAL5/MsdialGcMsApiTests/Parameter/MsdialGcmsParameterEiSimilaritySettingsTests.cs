using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialGcMsApi.Parameter.Tests
{
    [TestClass]
    public class MsdialGcmsParameterEiSimilaritySettingsTests
    {
        [TestMethod]
        public void ANewGcmsParameterStartsAtTheGcmsThreshold() {
            var parameter = new MsdialGcmsParameter(isLabUseOnly: false);

            // MS-DIAL 4's cutoff of 70 on the total score, and the GC-MS dialog's 70 %.
            Assert.AreEqual(.7f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.5f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
        }
    }
}
