using CompMs.Common.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.ViewModel.DataObj.Tests
{
    [TestClass()]
    public class MsRefSearchParameterBaseViewModelTests
    {
        [TestMethod()]
        public void MsRefSearchParameterBaseViewModelTest() {
            var parameter = new MsRefSearchParameterBase();
            parameter.MassRangeBegin = 10;
            using (var vm = new MsRefSearchParameterBaseViewModel(parameter)) {
                vm.MassRangeBegin.Value = "50";
                Assert.AreEqual(50, parameter.MassRangeBegin);
                vm.MassRangeBegin.Value = "Invalid";
            }
            Assert.AreEqual(50, parameter.MassRangeBegin);
        }
    }
}