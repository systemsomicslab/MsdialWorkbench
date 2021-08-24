using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.App.Msdial.Model.Imms.Tests
{
    [TestClass()]
    public class ImmsAnalysisParameterSetModelTests
    {
        [TestMethod()]
        public void ClosingMethodTest() {
            var parameter = new MsdialImmsParameter();
            var files = new[]
            {
                new AnalysisFileBean { },
                new AnalysisFileBean { },
            };
            var model = new ImmsAnalysisParameterSetModel(parameter, files);

            model.Parameter.MaxChargeNumber = 0;
            model.Parameter.QcAtLeastFilter = true;
            model.Parameter.TogetherWithAlignment = true;

            model.ClosingMethod();

            Assert.IsFalse(parameter.QcAtLeastFilter);
            Assert.IsTrue(parameter.MaxChargeNumber >= 2);
        }

        [TestMethod()]
        public void ExistsFileID2CoefficientsTest() {
            var parameter = new MsdialImmsParameter
            {
                FileID2CcsCoefficients = null,
            };
            var files = new[]
            {
                new AnalysisFileBean { AnalysisFileId = 0, },
                new AnalysisFileBean { AnalysisFileId = 1, },
                new AnalysisFileBean { AnalysisFileId = 3, },
            };

            var model = new ImmsAnalysisParameterSetModel(parameter, files);
            model.ClosingMethod();

            CollectionAssert.AreEquivalent(new[] { 0, 1, 3 }, parameter.FileID2CcsCoefficients.Keys);
        }
    }
}