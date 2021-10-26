using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Lcms.Tests
{
    [TestClass()]
    public class LcmsAnalysisParameterSetModelTests
    {
        [TestMethod()]
        public void ClosingMethodTest() {
            var parameter = new MsdialLcmsParameter();
            var files = new[]
            {
                new AnalysisFileBean { },
                new AnalysisFileBean { },
            };
            var model = new LcmsAnalysisParameterSetModel(parameter, files, null);

            model.Parameter.MaxChargeNumber = 0;
            model.Parameter.QcAtLeastFilter = true;
            model.Parameter.TogetherWithAlignment = true;

            model.ClosingMethod();

            Assert.IsFalse(parameter.QcAtLeastFilter);
            Assert.IsTrue(parameter.MaxChargeNumber >= 2);
        }
    }
}