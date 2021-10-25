using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Dims.Tests
{
    [TestClass()]
    public class DimsIdentifySettingModelTests
    {
        [TestMethod()]
        public void DataBaseNotChangedWhenSelectedAnnotatorNullTest() {
            var model = new DimsIdentifySettingModel(new ParameterBase());

            model.AddDataBase();
            model.DataBaseModel = model.DataBaseModels[0];
            model.DataBaseModel.DataBaseID = "1";
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotator();

            model.AddDataBase();
            model.DataBaseModel = model.DataBaseModels[1];
            model.DataBaseModel.DataBaseID = "2";
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotator();

            model.AnnotatorModel = model.AnnotatorModels[1];
            model.RemoveAnnotator();

            model.DataBaseModel = model.DataBaseModels[1];
            Assert.IsNull(model.AnnotatorModel);
            Assert.AreEqual(model.DataBaseModels[1], model.DataBaseModel);
        }
    }
}