using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public sealed class LipidConverterBuilderTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(LipidConverterBuilderTestData), DynamicDataSourceType.Property)]
        public void LipidConverterBuilderTest(IVisitor<ILipid, ILipid> visitor, ILipid lipid, ILipid expected) {
            var actual = lipid.Accept(visitor, IdentityDecomposer<ILipid, ILipid>.Instance);
            Assert.IsTrue(expected.Equals(actual));
        }

        public static IEnumerable<object[]> LipidConverterBuilderTestData {
            get {
                AcylChain acyl181 = new AcylChain(18, new DoubleBond(1), new Oxidized(0)), acyl1819 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0)), acyl180 = new AcylChain(18, new DoubleBond(0), new Oxidized(0)); 
                TotalChain acyl361 = new TotalChain(36, 1, 0, 2, 0, 0);
                MolecularSpeciesLevelChains acyl180_181 = new MolecularSpeciesLevelChains(acyl180, acyl181), acyl180_1819 = new MolecularSpeciesLevelChains(acyl180, acyl1819);
                PositionLevelChains acyl180181 = new PositionLevelChains(acyl180, acyl181), acyl1801819 = new PositionLevelChains(acyl180, acyl1819);
                var builder = new LipidConverterBuilder();
                ((ILipidomicsVisitorBuilder)builder).SetAcylDoubleBond(DoubleBondIndeterminateState.AllPositions);
                ((ILipidomicsVisitorBuilder)builder).SetAcylOxidized(OxidizedIndeterminateState.AllPositions);
                ((ILipidomicsVisitorBuilder)builder).SetAlkylDoubleBond(DoubleBondIndeterminateState.AllPositions.Exclude(1));
                ((ILipidomicsVisitorBuilder)builder).SetAlkylOxidized(OxidizedIndeterminateState.AllPositions);
                ((ILipidomicsVisitorBuilder)builder).SetSphingoDoubleBond(DoubleBondIndeterminateState.AllPositions);
                ((ILipidomicsVisitorBuilder)builder).SetSphingoOxidized(OxidizedIndeterminateState.AllPositions.Exclude(1));
                ((ILipidomicsVisitorBuilder)builder).SetChainsState(ChainsIndeterminateState.SpeciesLevel);
                var visitor = builder.Create();
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_1819.Mass, acyl180_1819), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180181.Mass, acyl180181), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl1801819.Mass, acyl1801819), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                ((ILipidomicsVisitorBuilder)builder).SetChainsState(ChainsIndeterminateState.MolecularSpeciesLevel);
                visitor = builder.Create();
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_1819.Mass, acyl180_1819), new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180181.Mass, acyl180181), new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl1801819.Mass, acyl1801819), new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), };
                ((ILipidomicsVisitorBuilder)builder).SetChainsState(ChainsIndeterminateState.PositionLevel);
                visitor = builder.Create();
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), new Lipid(Enum.LbmClass.PC, acyl361.Mass, acyl361), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_1819.Mass, acyl180_1819), new Lipid(Enum.LbmClass.PC, acyl180_181.Mass, acyl180_181), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180181.Mass, acyl180181), new Lipid(Enum.LbmClass.PC, acyl180181.Mass, acyl180181), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl1801819.Mass, acyl1801819), new Lipid(Enum.LbmClass.PC, acyl180181.Mass, acyl180181), };
                ((ILipidomicsVisitorBuilder)builder).SetAcylDoubleBond(DoubleBondIndeterminateState.Identity);
                visitor = builder.Create();
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl180_1819.Mass, acyl180_1819), new Lipid(Enum.LbmClass.PC, acyl180_1819.Mass, acyl180_1819), };
                yield return new object[] { visitor, new Lipid(Enum.LbmClass.PC, acyl1801819.Mass, acyl1801819), new Lipid(Enum.LbmClass.PC, acyl1801819.Mass, acyl1801819), };
            }
        }
    }
}