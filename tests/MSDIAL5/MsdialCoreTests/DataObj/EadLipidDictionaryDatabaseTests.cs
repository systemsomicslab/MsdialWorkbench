using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class EadLipidDictionaryDatabaseTests
    {
        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(GetGeneratesDifferentAdductTests), DynamicDataSourceType.Method)]
        public void GeneratesDifferentAdductTests(ILipid lipid1, AdductIon adduct1, ILipid lipid2, AdductIon adduct2, ILipid seed) {
            var db = new EadLipidDictionaryDatabase("stubId", Common.DataObj.Result.DataBaseSource.EieioLipid);
            var stubReference1 = new MoleculeMsReference { AdductType = adduct1, };
            var lipids1 = db.Generates(new[] { lipid1, }, seed, adduct1, stubReference1);
            var stubReference2 = new MoleculeMsReference { AdductType = adduct2, };
            var lipids2 = db.Generates(new[] { lipid2, }, seed, adduct2, stubReference2);
            Assert.AreEqual(1, lipids2.Count);
            Assert.AreEqual(lipid2.Name, lipids2[0].Name);
            Assert.AreEqual(adduct2.ToString(), lipids2[0].AdductType.ToString());
        }

        public static IEnumerable<object[]> GetGeneratesDifferentAdductTests() {
            var pc = new Lipid(LbmClass.PC, 100d, new TotalChain(2, 0, 0, 2, 0, 0));
            yield return new object[] { pc, AdductIon.GetAdductIon("[M+H]+"), pc, AdductIon.GetAdductIon("[M+H]+"), pc };
            var pc2 = new Lipid(LbmClass.PC, 100d, new MolecularSpeciesLevelChains(new AcylChain(1, new DoubleBond(0), new Oxidized(0)), new AcylChain(1, new DoubleBond(0), new Oxidized(0))));
            yield return new object[] { pc, AdductIon.GetAdductIon("[M+H]+"), pc2, AdductIon.GetAdductIon("[M+Na]+"), pc };
            yield return new object[] { pc, AdductIon.GetAdductIon("[M+H]+"), pc, AdductIon.GetAdductIon("[M+Na]+"), pc };
            yield return new object[] { pc, AdductIon.GetAdductIon("[M+Na]+"), pc, AdductIon.GetAdductIon("[M+H]+"), pc };
        }
    }
}
