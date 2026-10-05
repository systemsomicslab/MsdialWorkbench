using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class GeneratedLipidLibraryTests
{
    [DataTestMethod]
    [DataRow(CollisionType.EIEIO, DataBaseSource.EieioLipid, "EadLipidDB")]
    [DataRow(CollisionType.OAD, DataBaseSource.OadLipid, "OadLipidDB")]
    [DataRow(CollisionType.EID, DataBaseSource.EidLipid, "EidLipidDB")]
    public void AddTo_AddsTheLibraryTheCollisionTypeCallsFor(CollisionType collisionType, DataBaseSource expectedSource, string expectedId)
    {
        var parameter = CreateParameter(TargetOmics.Lipidomics, collisionType);
        var storage = DataBaseStorage.CreateEmpty();

        var db = GeneratedLipidLibrary.AddTo(storage, parameter, new GeneratedLipidAnnotatorSetting(), new DataBaseMapper());

        Assert.IsNotNull(db);
        var item = storage.EadLipidomicsDatabases.Single();
        Assert.AreEqual(expectedSource, item.DataBase.Source);
        Assert.AreEqual(expectedId, item.DataBase.Id);
        Assert.AreEqual(expectedId, item.Pairs.Single().AnnotatorID);
    }

    [DataTestMethod]
    [DataRow(TargetOmics.Metabolomics, CollisionType.EIEIO)]
    [DataRow(TargetOmics.Lipidomics, CollisionType.CID)]
    public void AddTo_AddsNothingWhenTheProjectDoesNotCallForIt(TargetOmics targetOmics, CollisionType collisionType)
    {
        var parameter = CreateParameter(targetOmics, collisionType);
        var storage = DataBaseStorage.CreateEmpty();

        var explicitlyOn = new GeneratedLipidAnnotatorSetting { Enabled = true };
        Assert.IsNull(GeneratedLipidLibrary.AddTo(storage, parameter, new GeneratedLipidAnnotatorSetting(), new DataBaseMapper()));
        Assert.IsNull(GeneratedLipidLibrary.AddTo(storage, parameter, explicitlyOn, new DataBaseMapper()));
        Assert.AreEqual(0, storage.EadLipidomicsDatabases.Count);
    }

    [TestMethod]
    public void AddTo_AddsNothingWhenSwitchedOff()
    {
        var parameter = CreateParameter(TargetOmics.Lipidomics, CollisionType.EIEIO);
        var storage = DataBaseStorage.CreateEmpty();

        var db = GeneratedLipidLibrary.AddTo(storage, parameter, new GeneratedLipidAnnotatorSetting { Enabled = false }, new DataBaseMapper());

        Assert.IsNull(db);
        Assert.AreEqual(0, storage.EadLipidomicsDatabases.Count);
    }

    [TestMethod]
    public void AddTo_RanksAboveEveryMoleculeAnnotatorByDefault()
    {
        var parameter = CreateParameter(TargetOmics.Lipidomics, CollisionType.EIEIO);
        var storage = DataBaseStorage.CreateEmpty();
        AddLbm(storage, parameter, priority: 4);

        GeneratedLipidLibrary.AddTo(storage, parameter, new GeneratedLipidAnnotatorSetting(), new DataBaseMapper());

        Assert.AreEqual(5, GeneratedFactory(storage).Priority);
    }

    [TestMethod]
    public void AddTo_UsesTheConfiguredPriorityAndSearchParameter()
    {
        var parameter = CreateParameter(TargetOmics.Lipidomics, CollisionType.EIEIO);
        var storage = DataBaseStorage.CreateEmpty();
        AddLbm(storage, parameter, priority: 4);
        var setting = new GeneratedLipidAnnotatorSetting { Priority = 2 };
        setting.SearchParameter.Ms2Tolerance = 0.05f;

        GeneratedLipidLibrary.AddTo(storage, parameter, setting, new DataBaseMapper());

        var factory = GeneratedFactory(storage);
        Assert.AreEqual(2, factory.Priority);
        Assert.AreEqual(0.05f, factory.PrepareParameter().Ms2Tolerance);
    }

    [TestMethod]
    public void AddTo_TheAnnotatorRestoredFromTheProjectKeepsItsSetting()
    {
        var parameter = CreateParameter(TargetOmics.Lipidomics, CollisionType.EIEIO);
        var storage = DataBaseStorage.CreateEmpty();
        var setting = new GeneratedLipidAnnotatorSetting { Priority = 3 };
        setting.SearchParameter.Ms2Tolerance = 0.05f;
        GeneratedLipidLibrary.AddTo(storage, parameter, setting, new DataBaseMapper());

        // Rebuild the query factory the way opening a saved project does: from the saved key alone.
        var item = storage.EadLipidomicsDatabases.Single();
        var key = ((EadLipidAnnotatorParameterPair)item.Pairs.Single()).SerializableAnnotatorKey;
        var factoryVisitor = new CompMs.MsdialLcMsApi.Parser.LcmsAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam, parameter.ProteomicsParam, new DataBaseMapper());
        IAnnotationQueryFactory<MsScanMatchResult> factory = key.Accept(factoryVisitor, new CompMs.MsdialLcMsApi.Parser.LcmsLoadAnnotatorVisitor(parameter), item.DataBase);

        Assert.AreEqual(3, factory.Priority);
        Assert.AreEqual(0.05f, factory.PrepareParameter().Ms2Tolerance);
    }

    private static MsdialLcmsParameter CreateParameter(TargetOmics targetOmics, CollisionType collisionType)
    {
        return new MsdialLcmsParameter {
            TargetOmics = targetOmics,
            CollistionType = collisionType,
        };
    }

    private static void AddLbm(DataBaseStorage storage, MsdialLcmsParameter parameter, int priority)
    {
        var reference = new MoleculeMsReference { ScanID = 0, Name = "PC 34:1", PrecursorMz = 760.585 };
        var lbmDB = new MoleculeDataBase(new[] { reference }, "LbmDB", DataBaseSource.Lbm, SourceType.MspDB, string.Empty);
        var annotator = new LcmsMspAnnotator(lbmDB, parameter.LbmSearchParam, TargetOmics.Lipidomics, "LbmDB", priority);
        storage.AddMoleculeDataBase(lbmDB, [
            new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, parameter.LbmSearchParam, ignoreIsotopicPeak: true)),
        ]);
    }

    private static IAnnotationQueryFactory<MsScanMatchResult> GeneratedFactory(DataBaseStorage storage)
    {
        return storage.EadLipidomicsDatabases.Single().Pairs.Single().AnnotationQueryFactory;
    }
}
