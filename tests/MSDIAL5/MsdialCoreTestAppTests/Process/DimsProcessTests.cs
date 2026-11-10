using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class DimsProcessTests
{
    [TestMethod]
    public void CreateDataBaseStorage_SavesTheLbmSearchParameterForTheLbmAnnotator()
    {
        var parameter = CreateParameterWithDistinctMspAndLbmSettings();
        var lbmDB = CreateDataBase("LbmDB", DataBaseSource.Lbm);

        var storage = DimsProcess.CreateDataBaseStorage(parameter, mspDB: null, lbmDB, txtDB: null);

        var key = (StandardRestorationKey)SingleAnnotatorKey(storage);
        AssertIsLbmSetting(key.Parameter);
    }

    [TestMethod]
    public void CreateDataBaseStorage_LbmAnnotatorRestoredFromTheProjectStillUsesTheLbmSearchParameter()
    {
        var parameter = CreateParameterWithDistinctMspAndLbmSettings();
        var lbmDB = CreateDataBase("LbmDB", DataBaseSource.Lbm);

        var storage = DimsProcess.CreateDataBaseStorage(parameter, mspDB: null, lbmDB, txtDB: null);

        // Rebuild the query factory the way opening a saved project does: from the saved key alone.
        var key = SingleAnnotatorKey(storage);
        var factoryVisitor = new DimsAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam, parameter.ProteomicsParam, new DataBaseMapper());
        IAnnotationQueryFactory<MsScanMatchResult> factory = key.Accept(factoryVisitor, new DimsLoadAnnotatorVisitor(parameter), lbmDB);
        AssertIsLbmSetting(factory.PrepareParameter());
    }

    [TestMethod]
    public void CreateDataBaseStorage_KeepsTheMspSearchParameterForTheMspAnnotator()
    {
        var parameter = CreateParameterWithDistinctMspAndLbmSettings();
        var mspDB = CreateDataBase("MspDB", DataBaseSource.Msp);

        var storage = DimsProcess.CreateDataBaseStorage(parameter, mspDB, lbmDB: null, txtDB: null);

        var key = (StandardRestorationKey)SingleAnnotatorKey(storage);
        Assert.AreEqual(parameter.MspSearchParam.Ms2Tolerance, key.Parameter.Ms2Tolerance);
        Assert.AreEqual(parameter.MspSearchParam.TotalScoreCutoff, key.Parameter.TotalScoreCutoff);
    }

    private static MsdialDimsParameter CreateParameterWithDistinctMspAndLbmSettings()
    {
        var parameter = new MsdialDimsParameter();
        parameter.MspSearchParam.Ms1Tolerance = 0.01f;
        parameter.MspSearchParam.Ms2Tolerance = 0.025f;
        parameter.MspSearchParam.TotalScoreCutoff = 0.8f;
        parameter.LbmSearchParam.Ms1Tolerance = 0.005f;
        parameter.LbmSearchParam.Ms2Tolerance = 0.05f;
        parameter.LbmSearchParam.TotalScoreCutoff = 0.3f;
        return parameter;
    }

    private static void AssertIsLbmSetting(CompMs.Common.Parameter.MsRefSearchParameterBase actual)
    {
        Assert.AreEqual(0.005f, actual.Ms1Tolerance);
        Assert.AreEqual(0.05f, actual.Ms2Tolerance);
        Assert.AreEqual(0.3f, actual.TotalScoreCutoff);
    }

    private static IAnnotationQueryFactoryGenerationKey<MoleculeDataBase> SingleAnnotatorKey(DataBaseStorage storage)
    {
        return ((MetabolomicsAnnotatorParameterPair)storage.MetabolomicsDataBases.Single().Pairs.Single()).SerializableAnnotatorKey;
    }

    private static MoleculeDataBase CreateDataBase(string id, DataBaseSource source)
    {
        var reference = new MoleculeMsReference { ScanID = 0, Name = "PC 34:1", PrecursorMz = 760.585 };
        return new MoleculeDataBase(new[] { reference }, id, source, SourceType.MspDB, string.Empty);
    }
}
