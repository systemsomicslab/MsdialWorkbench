using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    public static class ChromXsTestHelper {
        public static void AreEqual(this Assert assert, ChromXs expected, ChromXs actual, string message="") {
            assert.AreEqual(expected.RT, actual.RT, $"{message} {nameof(ChromXs.RT)}: Expected {expected.RT}, Actual {actual.RT}");
            assert.AreEqual(expected.RI, actual.RI, $"{message} {nameof(ChromXs.RI)}: Expected {expected.RI}, Actual {actual.RI}");
            assert.AreEqual(expected.Drift, actual.Drift, $"{message} {nameof(ChromXs.Drift)}: Expected {expected.Drift}, Actual {actual.Drift}");
            assert.AreEqual(expected.Mz, actual.Mz, $"{message} {nameof(ChromXs.Mz)}: Expected {expected.Mz}, Actual {actual.Mz}");
            Assert.AreEqual(expected.MainType, actual.MainType, $"{message} {nameof(ChromXs.MainType)}: Expected {expected.MainType}, Actual {actual.MainType}");
        }

        public static bool AreEqual(this ChromXs expected, ChromXs actual)
        {
            return expected.RT.AreEqual(actual.RT) &&
                expected.RI.AreEqual(actual.RI) &&
                expected.Drift.AreEqual(actual.Drift) &&
                expected.Mz.AreEqual(actual.Mz) &&
                expected.MainType == actual.MainType;
        }
    }
}
