using CompMs.App.Msdial.Model.Dims;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Dims.Tests
{
    [TestClass()]
    public class DimsIdentifySettingViewModelTests
    {
        // [TestMethod()]
        // public void DataBaseFocusSyncTest() {
        //     var model = new DimsIdentifySettingModel(new ParameterBase());
        //     var viewmodel = new DimsIdentifySettingViewModel(model);

        //     // DB
        //     model.AddDataBaseZZZ();
        //     model.DataBaseModel = model.DataBaseModels.Last();
        //     model.DataBaseModel.DataBaseID = "1";
        //     model.DataBaseModel.DBSource = DataBaseSource.Msp;
        //     model.AddDataBaseZZZ();
        //     model.DataBaseModel = model.DataBaseModels.Last();
        //     model.DataBaseModel.DataBaseID = "2";
        //     model.DataBaseModel.DBSource = DataBaseSource.Msp;
        //     model.AddDataBaseZZZ();
        //     model.DataBaseModel = model.DataBaseModels.Last();
        //     model.DataBaseModel.DataBaseID = "3";
        //     model.DataBaseModel.DBSource = DataBaseSource.Msp;

        //     Assert.AreEqual(model.DataBaseModel, viewmodel.DataBaseViewModel.Value.Model);

        //     viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels[0];
        //     Assert.AreEqual(model.DataBaseModel, viewmodel.DataBaseViewModel.Value.Model);

        //     model.DataBaseModel = model.DataBaseModels[1];
        //     Assert.AreEqual(model.DataBaseModel, viewmodel.DataBaseViewModel.Value.Model);
        // }

        // [TestMethod()]
        // public void AnnotatorFocusSyncTest() {
        //     var model = new DimsIdentifySettingModel(new ParameterBase());
        //     var viewmodel = new DimsIdentifySettingViewModel(model);

        //     model.AddDataBaseZZZ();
        //     model.DataBaseModel = model.DataBaseModels.Last();
        //     model.DataBaseModel.DataBaseID = "1";
        //     model.DataBaseModel.DBSource = DataBaseSource.Msp;

        //     // Annotator
        //     model.AddAnnotatorZZZ(model.DataBaseModel);
        //     model.AnnotatorModel = model.AnnotatorModels.Last();
        //     model.AnnotatorModel.AnnotatorID = "1";
        //     model.AddAnnotatorZZZ(model.DataBaseModel);
        //     model.AnnotatorModel = model.AnnotatorModels.Last();
        //     model.AnnotatorModel.AnnotatorID = "2";
        //     model.AddAnnotatorZZZ(model.DataBaseModel);
        //     model.AnnotatorModel = model.AnnotatorModels.Last();
        //     model.AnnotatorModel.AnnotatorID = "3";

        //     Assert.AreEqual(model.AnnotatorModel, viewmodel.AnnotatorViewModel.Value.Model);

        //     viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels[0];
        //     Assert.AreEqual(model.AnnotatorModel, viewmodel.AnnotatorViewModel.Value.Model);

        //     model.AnnotatorModel = model.AnnotatorModels[1];
        //     Assert.AreEqual(model.AnnotatorModel, viewmodel.AnnotatorViewModel.Value.Model);
        // }

        [TestMethod()]
        public void DataBaseFocusWhenAnnotatorRemovedTest() {
            var model = new DimsIdentifySettingModel(new ParameterBase());
            var viewmodel = new DimsIdentifySettingViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            model.RemoveAnnotatorZZZ(viewmodel.AnnotatorViewModel.Value.Model);
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
        }

        [TestMethod()]
        public void AnnotatorFocusWhenDataBaseRemovedTest() {
            var model = new DimsIdentifySettingModel(new ParameterBase());
            var viewmodel = new DimsIdentifySettingViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            model.RemoveDataBaseZZZ(viewmodel.DataBaseViewModel.Value.Model);
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
        }

        [TestMethod()]
        public void DataBaseFocusWhenAnnotatorChangedTest() {
            var model = new DimsIdentifySettingModel(new ParameterBase());
            var viewmodel = new DimsIdentifySettingViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels[0];
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
        }

        [TestMethod()]
        public void AnnotatorFocusWhenDataBaseChangedTest() {
            var model = new DimsIdentifySettingModel(new ParameterBase());
            var viewmodel = new DimsIdentifySettingViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels[0];
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
        }
    }
}