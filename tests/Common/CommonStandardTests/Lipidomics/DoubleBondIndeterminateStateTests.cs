using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class DoubleBondIndeterminateStateTests
    {
        [TestMethod()]
        [DynamicData(nameof(TestDatas), DynamicDataSourceType.Property)]
        public void IndeterminateTest(DoubleBondInfo[] infos, DoubleBondIndeterminateState state, DoubleBondInfo[] expected) {
            var actual = state.Indeterminate(infos);
            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++) {
                Assert.That.AreEqual(expected[i], actual.ElementAt(i));
            }
        }


        public static IEnumerable<object[]> TestDatas {
            get {
                var infos = new DoubleBondInfo[] { DoubleBondInfo.Z(9), DoubleBondInfo.Create(12), DoubleBondInfo.Create(15), };
                var no_db = new DoubleBondInfo[0];
                var only_9 = new DoubleBondInfo[] { DoubleBondInfo.Z(9), };
                var no_isomer = new DoubleBondInfo[] { DoubleBondInfo.Create(9), DoubleBondInfo.Create(12), DoubleBondInfo.Create(15), };
                yield return new object[] { infos, DoubleBondIndeterminateState.AllPositions, no_db, };
                yield return new object[] { infos, DoubleBondIndeterminateState.AllCisTransIsomers, no_isomer, };
                yield return new object[] { infos, DoubleBondIndeterminateState.Positions(9, 12, 15), no_db, };
                yield return new object[] { infos, DoubleBondIndeterminateState.Positions(12, 15), only_9, };
                yield return new object[] { infos, DoubleBondIndeterminateState.Positions(13), infos, };
                yield return new object[] { infos, DoubleBondIndeterminateState.PositionsExclude(9), only_9, };
                yield return new object[] { infos, DoubleBondIndeterminateState.AllPositions.Exclude(9), only_9, };
                yield return new object[] { infos, DoubleBondIndeterminateState.CisTransIsomer(9), no_isomer, };
                yield return new object[] { infos, DoubleBondIndeterminateState.CisTransIsomer(10), infos, };
                yield return new object[] { infos, DoubleBondIndeterminateState.CisTransIsomerExclude(9), infos, };
                yield return new object[] { infos, DoubleBondIndeterminateState.AllCisTransIsomers.Exclude(9), infos, };
                yield return new object[] { infos, DoubleBondIndeterminateState.CisTransIsomerExclude(10), no_isomer, };
            }
        }
    }
}