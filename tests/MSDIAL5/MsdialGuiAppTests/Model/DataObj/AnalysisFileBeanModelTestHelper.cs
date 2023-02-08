using CompMs.MsdialCore.DataObj.Tests;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    public static class AnalysisFileBeanModelTestHelper
    {
        public static AnalysisFileBeanModel Create() {
            return new AnalysisFileBeanModel(AnalysisFileTestHelper.Create());
        }
    }
}
