using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Components.Tests
{
    public static class ChromatogramPeakTestHelper
    {
        public static void AreEqual(this Assert assert, ChromatogramPeak expected, ChromatogramPeak actual, string message = "")
        {
            var errorMessages = new List<string>(0);
            if (expected.ID != actual.ID)
            {
                errorMessages.Add($"{nameof(ChromatogramPeak.ID)}: Expected {expected.ID}, Actual {actual.ID}");
            }

            if (expected.Mass != actual.Mass)
            {
                errorMessages.Add($"{nameof(ChromatogramPeak.Mass)}: Expected {expected.Mass}, Actual {actual.Mass}");
            }

            if (expected.Intensity != actual.Intensity)
            {
                errorMessages.Add($"{nameof(ChromatogramPeak.Intensity)}: Expected {expected.Intensity}, Actual {actual.Intensity}");
            }

            if (!expected.ChromXs.AreEqual(actual.ChromXs))
            {
                errorMessages.Add($"{nameof(ChromatogramPeak.ChromXs)}: Expected {expected.ChromXs}, Actual {actual.ChromXs}");
            }
            if (errorMessages.Count > 0)
            {
                Assert.Fail($"{message} Mismatches found: {string.Join("; ", errorMessages)}");
            }
        }
    }
}
