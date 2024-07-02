using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests;

[TestClass()]
public class LipidomicsConverterTests
{
    [TestMethod()]
    public void ConvertMsdialLipidnameToLipidMoleculeObjectVS2Test() {
        var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2("PC 16:0_18:2", "PC");
        Assert.AreEqual("Glycerophospholipids", molecule.LipidCategory);
        Assert.AreEqual(LbmClass.PC, molecule.LipidClass);
        Assert.AreEqual("PC", molecule.LipidSubclass);
        Assert.AreEqual("PC 16:0_18:2", molecule.LipidName);
        Assert.AreEqual("16:0", molecule.Sn1AcylChainString);
        Assert.AreEqual(16, molecule.Sn1CarbonCount);
        Assert.AreEqual(0, molecule.Sn1DoubleBondCount);
        Assert.AreEqual(0, molecule.Sn1Oxidizedount);
        Assert.AreEqual("18:2", molecule.Sn2AcylChainString);
        Assert.AreEqual(18, molecule.Sn2CarbonCount);
        Assert.AreEqual(2, molecule.Sn2DoubleBondCount);
        Assert.AreEqual(0, molecule.Sn2Oxidizedount);
        Assert.AreEqual("PC 34:2", molecule.SublevelLipidName);
        Assert.AreEqual("34:2", molecule.TotalChainString);
        Assert.AreEqual(34, molecule.TotalCarbonCount);
        Assert.AreEqual(2, molecule.TotalDoubleBondCount);
        Assert.AreEqual(0, molecule.TotalOxidizedCount);
    }

    [DataTestMethod()]
    [DataRow("FA 18:1", "FA", 0)]
    [DataRow("FA 18:1;O", "FA", 1)]
    [DataRow("FA 18:1;O2", "FA", 2)]
    public void EstimateOxidizeTest(string lipidname, string ontology, int expected) {
        var lipid = new LipidMolecule();
        LipidomicsConverter.SetLipidAcylChainProperties(lipid, lipidname, ontology);
        Assert.AreEqual(expected, lipid.TotalOxidizedCount);
    }
}