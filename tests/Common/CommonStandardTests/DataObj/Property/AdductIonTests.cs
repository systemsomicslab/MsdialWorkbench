using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.DataObj.Property.Tests
{
    [TestClass()]
    public class AdductIonTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(GetAdductIonTestData))]
        public void GetAdductIonTest(string adductName) {
            var adductIon = AdductIon.GetAdductIon(adductName);
            var newAdduct = AdductIon.GetAdductIon(adductIon.AdductIonName);
            Assert.AreEqual(adductIon, newAdduct);
        }

        public static IEnumerable<object[]> GetAdductIonTestData {
            get {
                yield return new object[] { "[M+H]+", };
                yield return new object[] { "[M-H]-", };
                yield return new object[] { "[M+2H]2+", };
                yield return new object[] { "[M+Na]+", };
            }
        }
    }
}