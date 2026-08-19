using CompMs.App.MsdialConsole.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class ConfigParserTests
{
    [TestMethod]
    public void ReadCommonParameter_UpdatesActiveBlankFilteringFoldChange()
    {
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "sample max / blank average", "7");

        Assert.IsTrue(result);
        Assert.AreEqual(7f, parameter.SampleMaxOverBlankAverage);
        Assert.AreEqual(7f, parameter.FoldChangeForBlankFiltering);
    }
}
