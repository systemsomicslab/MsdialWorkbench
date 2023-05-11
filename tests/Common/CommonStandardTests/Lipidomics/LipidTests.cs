using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class LipidTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(IncludesTestData), dynamicDataSourceType: DynamicDataSourceType.Property)]
        public void IncludesTest(ILipid lipid1, ILipid lipid2, bool expected) {
            var actual = lipid1.Includes(lipid2);
            Assert.AreEqual(expected, actual);
        }

        [DataTestMethod()]
        [DynamicData(nameof(EqualsTestData), DynamicDataSourceType.Property)]
        public void EqualsTest(ILipid lipid, ILipid other, bool expected) {
            Assert.AreEqual(expected, lipid.Equals(other));
        }

        public static IEnumerable<object[]> IncludesTestData {
            get {
                var parser = FacadeLipidParser.Default;

                ILipid pc360 = parser.Parse("PC 36:0"), pc180_180 = parser.Parse("PC 18:0_18:0"), pc180180 = parser.Parse("PC 18:0/18:0");
                ILipid pc361 = parser.Parse("PC 36:1"), pc180_181 = parser.Parse("PC 18:0_18:1"), pc180181 = parser.Parse("PC 18:0/18:1"), pc181180 = parser.Parse("PC 18:1/18:0");

                yield return new object[] { pc360, pc180_180, true };
                yield return new object[] { pc180_180, pc180180, true };
                yield return new object[] { pc361, pc180_181, true };
                yield return new object[] { pc180_181, pc181180, true };
                yield return new object[] { pc180_181, pc180181, true };

                yield return new object[] { pc360, pc360, true };
                yield return new object[] { pc180_180, pc180_180, true };
                yield return new object[] { pc180180, pc180180, true };

                yield return new object[] { pc180_180, pc360, false };
                yield return new object[] { pc180180, pc360, false };
                yield return new object[] { pc180180, pc180_180, false };

                yield return new object[] { pc360, pc180180, true };

                yield return new object[] { pc361, pc181180, true };
                yield return new object[] { pc361, pc180181, true };
            }
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