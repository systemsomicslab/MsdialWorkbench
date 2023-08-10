using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class StandardMsCharacterizationUtilityTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(GetDefaultCharacterizationResultForCeramidesTestData))]
        public void GetDefaultCharacterizationResultForCeramidesTest(ILipid lipid, LipidMsCharacterizationResult characterizationResult, ILipid expected) {
            (var actual, _) = StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForCeramides(lipid, characterizationResult);
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        public static IEnumerable<object[]> GetDefaultCharacterizationResultForCeramidesTestData {
            get {
                var lipid = new Lipid(Enum.LbmClass.SM, 100d, new PositionLevelChains(new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)), new AcylChain(16, new DoubleBond(DoubleBondInfo.Z(16)), new Oxidized(0))));
                var expected1 = new Lipid(Enum.LbmClass.SM, 100d, new TotalChain(34, 2, 2, 1, 0, 1));
                yield return new object[] { lipid, new LipidMsCharacterizationResult { }, expected1, };
                var expected2 = new Lipid(Enum.LbmClass.SM, 100d, new PositionLevelChains(new SphingoChain(18, new DoubleBond(1), Oxidized.CreateFromPosition(1, 3)), new AcylChain(16, new DoubleBond(1), new Oxidized(0))));
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = true, }, expected2, };
                var expected3 = new Lipid(Enum.LbmClass.SM, 100d, new PositionLevelChains(new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)), new AcylChain(16, new DoubleBond(DoubleBondInfo.Create(16)), new Oxidized(0))));
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = true, IsDoubleBondIonsExisted = true, }, expected3, };
            }
        }

        [DataTestMethod()]
        [DynamicData(nameof(RemoveEZForMGTestData))]
        public void RemoveEZForMGTest(ILipid lipid, LipidMsCharacterizationResult characterizationResult, ILipid expected) {
            (var actual, _) = StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForGlycerophospholipid(lipid, characterizationResult);
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        public static IEnumerable<object[]> RemoveEZForMGTestData {
            get {
                var lipid = new Lipid(Enum.LbmClass.MG, 100d, new PositionLevelChains(new AcylChain(16, new DoubleBond(DoubleBondInfo.Z(9)), new Oxidized(0))));
                var expected1 = new Lipid(Enum.LbmClass.MG, 100d, new TotalChain(16, 1, 0, 1, 0, 0));
                var expected2 = new Lipid(Enum.LbmClass.MG, 100d, new PositionLevelChains(new AcylChain(16, new DoubleBond(1), new Oxidized(0))));
                var expected3 = new Lipid(Enum.LbmClass.MG, 100d, new PositionLevelChains(new AcylChain(16, new DoubleBond(DoubleBondInfo.Create(9)), new Oxidized(0))));

                yield return new object[] { lipid, new LipidMsCharacterizationResult {  IsChainIonsExisted = false, IsPositionIonsExisted = false, IsDoubleBondIonsExisted = false, }, expected1, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = false, IsPositionIonsExisted = false, IsDoubleBondIonsExisted = true, }, expected1, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = false, IsPositionIonsExisted = true, IsDoubleBondIonsExisted = false, }, expected1, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = false, IsPositionIonsExisted = true, IsDoubleBondIonsExisted = true, }, expected1, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = true, IsPositionIonsExisted = false, IsDoubleBondIonsExisted = false, }, expected2, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = true, IsPositionIonsExisted = false, IsDoubleBondIonsExisted = true, }, expected3, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = true, IsPositionIonsExisted = true, IsDoubleBondIonsExisted = false, }, expected2, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsChainIonsExisted = true, IsPositionIonsExisted = true, IsDoubleBondIonsExisted = true, }, expected3, };
            }
        }

        [DataTestMethod()]
        [DynamicData(nameof(RemoveEZForCETestData))]
        public void RemoveEZForCETest(ILipid lipid, LipidMsCharacterizationResult characterizationResult, ILipid expected) {
            (var actual, _) = StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForSingleAcylChainLipid(lipid, characterizationResult);
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        public static IEnumerable<object[]> RemoveEZForCETestData {
            get {
                var lipid = new Lipid(Enum.LbmClass.CE, 100d, new PositionLevelChains(new AcylChain(16, new DoubleBond(DoubleBondInfo.Z(9)), new Oxidized(0))));
                var lipid2 = new Lipid(Enum.LbmClass.CE, 100d, new MolecularSpeciesLevelChains(new AcylChain(16, new DoubleBond(DoubleBondInfo.Z(9)), new Oxidized(0))));
                var expected1 = new Lipid(Enum.LbmClass.CE, 100d, new TotalChain(16, 1, 0, 1, 0, 0));
                var expected2 = new Lipid(Enum.LbmClass.CE, 100d, new PositionLevelChains(new AcylChain(16, new DoubleBond(1), new Oxidized(0))));
                var expected3 = new Lipid(Enum.LbmClass.CE, 100d, new PositionLevelChains(new AcylChain(16, new DoubleBond(DoubleBondInfo.Create(9)), new Oxidized(0))));
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsDoubleBondIonsExisted = false, }, expected2, };
                yield return new object[] { lipid, new LipidMsCharacterizationResult { IsDoubleBondIonsExisted = true, }, expected3, };
                yield return new object[] { lipid2, new LipidMsCharacterizationResult { IsDoubleBondIonsExisted = false, }, expected2, };
                yield return new object[] { lipid2, new LipidMsCharacterizationResult { IsDoubleBondIonsExisted = true, }, expected3, };
            }
        }
    }
}