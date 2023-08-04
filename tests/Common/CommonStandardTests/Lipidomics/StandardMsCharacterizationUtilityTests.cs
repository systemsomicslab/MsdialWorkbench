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
    }
}