using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    [TestClass()]
    public class DataCollectionSettingModelTests
    {
        [TestMethod()]
        public void ImagingDims_HasNoMs1SelectionMethod() {
            var parameter = new MsdialDimsParameter(isImaging: true, isLabUseOnly: false);

            var model = new DataCollectionSettingModel(parameter, new PeakPickBaseParameterModel(parameter.PeakPickBaseParam), new AnalysisFileBean[0], ProcessOption.All);

            Assert.IsNull(model.DimsProviderFactoryParameter);
        }

        [TestMethod()]
        public void ImagingDims_CommitLeavesTheStoredMs1SelectionMethodAlone() {
            var stored = new DimsAccumulateDataProviderFactoryParameter(0, 100, 0.01);
            var parameter = new MsdialDimsParameter(isImaging: true, isLabUseOnly: false) { ProviderFactoryParameter = stored };
            var model = new DataCollectionSettingModel(parameter, new PeakPickBaseParameterModel(parameter.PeakPickBaseParam), new AnalysisFileBean[0], ProcessOption.All);

            Assert.IsTrue(model.TryCommit());

            Assert.AreSame(stored, parameter.ProviderFactoryParameter);
        }

        [TestMethod()]
        public void Dims_StillHasAnMs1SelectionMethod() {
            var parameter = new MsdialDimsParameter(isImaging: false, isLabUseOnly: false);

            var model = new DataCollectionSettingModel(parameter, new PeakPickBaseParameterModel(parameter.PeakPickBaseParam), new AnalysisFileBean[0], ProcessOption.All);

            Assert.IsNotNull(model.DimsProviderFactoryParameter);
            Assert.IsTrue(model.TryCommit());
            Assert.IsInstanceOfType(parameter.ProviderFactoryParameter, typeof(DimsTicDataProviderFactoryParameter));
        }
    }
}
