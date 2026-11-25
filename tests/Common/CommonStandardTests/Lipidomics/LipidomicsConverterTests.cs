using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
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

    // Names a user can register by mistake. Each one used to throw inside the converter;
    // it must come back as an unvalidated molecule so the reference is scored by spectrum only.
    [DataTestMethod()]
    [DataRow("Cholesterol d7", "ST")] // mono chain token without ':'
    [DataRow("CE 18.1", "CE")]
    [DataRow("PC 34", "PC")]
    [DataRow("Cer 18:1;2O/16", "Cer_NS")] // second of two chains without ':'
    [DataRow("TG 16:0_18:1_18", "TG")] // third of three
    [DataRow("CL 16:0_18:1_18:1_18", "CL")] // fourth of four
    [DataRow("Q1", "CoQ")] // CoQ name shorter than "CoQ"
    [DataRow(null, "PC")]
    public void ConvertMsdialLipidnameToLipidMoleculeObjectVS2_MisregisteredReference_IsNotValidated(string name, string compoundClass) {
        var reference = new MoleculeMsReference
        {
            Name = name,
            CompoundClass = compoundClass,
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
            IonMode = IonMode.Positive,
        };
        var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(reference);
        Assert.IsFalse(molecule.IsValidatedFormat);
        Assert.IsNull(molecule.LipidName);
    }

    [DataTestMethod()]
    [DataRow("Cholesterol d7", "ST")]
    [DataRow("Cer 18:1;2O/16", "Cer_NS")]
    [DataRow("Q1", "CoQ")]
    [DataRow(null, "PC")]
    public void ConvertMsdialLipidnameToLipidMoleculeObjectVS2_MisregisteredName_IsNotValidated(string name, string ontology) {
        var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(name, ontology);
        Assert.IsFalse(molecule.IsValidatedFormat);
        Assert.AreEqual(0, molecule.TotalCarbonCount);
    }

    // The MSP parser leaves Formula null for FORMULA: "", "Unknown" or "null". The name still parses.
    [TestMethod()]
    public void ConvertMsdialLipidnameToLipidMoleculeObjectVS2_NullFormula_KeepsLipidName() {
        var reference = new MoleculeMsReference
        {
            Name = "PC 16:0_18:1",
            CompoundClass = "PC",
            Formula = null,
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
            IonMode = IonMode.Positive,
        };
        var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(reference);
        Assert.IsTrue(molecule.IsValidatedFormat);
        Assert.AreEqual("PC 16:0_18:1", molecule.LipidName);
        Assert.AreEqual(string.Empty, molecule.Formula);
    }
}