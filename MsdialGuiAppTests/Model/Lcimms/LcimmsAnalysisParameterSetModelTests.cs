using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Lcimms.Tests
{
    [TestClass()]
    public class LcimmsAnalysisParameterSetModelTests
    {
        [TestMethod()]
        public void ClosingMethodTest() {
            var parameter = new MsdialLcImMsParameter();
            var files = new[]
            {
                new AnalysisFileBean { },
                new AnalysisFileBean { },
            };
            var model = new LcimmsAnalysisParameterSetModel(parameter, files);

            model.Parameter.MaxChargeNumber = 0;
            model.Parameter.QcAtLeastFilter = true;
            model.Parameter.TogetherWithAlignment = true;

            model.ClosingMethod();

            Assert.IsFalse(parameter.QcAtLeastFilter);
            Assert.IsTrue(parameter.MaxChargeNumber >= 2);
        }

        [TestMethod()]
        public void ExistsFileID2CoefficientsTest() {
            var parameter = new MsdialLcImMsParameter
            {
                FileID2CcsCoefficients = null,
            };
            var files = new[]
            {
                new AnalysisFileBean { AnalysisFileId = 0, },
                new AnalysisFileBean { AnalysisFileId = 1, },
                new AnalysisFileBean { AnalysisFileId = 3, },
            };

            var model = new LcimmsAnalysisParameterSetModel(parameter, files);
            model.ClosingMethod();

            CollectionAssert.AreEquivalent(new[] { 0, 1, 3 }, parameter.FileID2CcsCoefficients.Keys);
        }
    }
}