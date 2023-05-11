using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class LipidTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(EqualsTestData), DynamicDataSourceType.Property)]
        public void EqualsTest(ILipid lipid, ILipid other, bool expected) {
            Assert.AreEqual(expected, lipid.Equals(other));
        }

        public static IEnumerable<object[]> EqualsTestData {
            get {
                var parser = FacadeLipidParser.Default;
                ILipid lipid1 = parser.Parse("PC 34:1"),
                    lipid2 = parser.Parse("PC 34:1"),
                    lipid3 = parser.Parse("PC 16:0_18:1");
                yield return new object[] { lipid1, lipid1, true, };
                yield return new object[] { lipid1, lipid2, true, };
                yield return new object[] { lipid1, lipid3, false, };
            }
        }
    }
}