using CompMs.Common.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class ChromatogramRangeTests
    {
        [TestMethod]
        [DynamicData(nameof(ExtendWithTestData))]
        public void ExtendWithTest(ChromatogramRange range, double value, ChromatogramRange expected) {
            var actual = range.ExtendWith(value);
            ChromatogramRangeTestHelper.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> ExtendWithTestData {
            get {
                yield return new object[]
                {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    10d,
                    ChromatogramRangeTestHelper.Create(90d, 210d),
                };
                yield return new object[]
                {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    0d,
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                };
                yield return new object[]
                {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    -10d,
                    ChromatogramRangeTestHelper.Create(110d, 190d),
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(ExtendRelativeTestData))]
        public void ExtendRelative(ChromatogramRange range, double rate, ChromatogramRange expected) {
            var actual = range.ExtendRelative(rate);
            ChromatogramRangeTestHelper.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> ExtendRelativeTestData {
            get {
                yield return new object[]
                {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    0.1d,
                    ChromatogramRangeTestHelper.Create(90d, 210d),
                };
                yield return new object[]
                {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    0d,
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                };
                yield return new object[]
                {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    -0.1d,
                    ChromatogramRangeTestHelper.Create(110d, 190d),
                };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(RestrictByTestData))]
        public void RestrictByTest(ChromatogramRange range, double limitLow, double limitHigh, ChromatogramRange expected) {
            var actual = range.RestrictBy(limitLow, limitHigh);
            ChromatogramRangeTestHelper.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> RestrictByTestData {
            get {
                yield return new object[] {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    0d, 300d,
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                };
                yield return new object[] {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    150d, 300d,
                    ChromatogramRangeTestHelper.Create(150d, 200d),
                };
                yield return new object[] {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    0d, 150d,
                    ChromatogramRangeTestHelper.Create(100d, 150d),
                };
                yield return new object[] {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    120d, 180d,
                    ChromatogramRangeTestHelper.Create(120d, 180d),
                };
                yield return new object[] {
                    ChromatogramRangeTestHelper.Create(100d, 200d),
                    300d, 400d,
                    ChromatogramRangeTestHelper.Create(200d, 200d),
                };
            }
        }
    }

    public static class ChromatogramRangeTestHelper {
        public static void AreEqual(ChromatogramRange expected, ChromatogramRange actual) {
            Assert.AreEqual(expected.Begin, actual.Begin);
            Assert.AreEqual(expected.End, actual.End);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Unit, actual.Unit);
        }

        public static void AreEqual(this Assert assert, ChromatogramRange expected, ChromatogramRange actual) {
            AreEqual(expected, actual);
        }

        public static ChromatogramRange Create(double begin, double end) {
            return new ChromatogramRange(begin, end, ChromXType.RT, ChromXUnit.Min);
        }
    }
}