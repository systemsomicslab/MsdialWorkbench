using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Components.Tests
{
    public static class InterfaceChromatogramPeakTestHelper
    {
        public static void AreEqual(this Assert assert, IChromatogramPeak expected, IChromatogramPeak actual, string message = "")
        {
            var errorMessages = new List<string>(0);

            if (expected.ID != actual.ID)
            {
                errorMessages.Add($"{nameof(IChromatogramPeak.ID)}: Expected {expected.ID}, Actual {actual.ID}");
            }

            if (expected.Mass != actual.Mass)
            {
                errorMessages.Add($"{nameof(ISpectrumPeak.Mass)}: Expected {expected.Mass}, Actual {actual.Mass}");
            }

            if (expected.Intensity != actual.Intensity)
            {
                errorMessages.Add($"{nameof(ISpectrumPeak.Intensity)}: Expected {expected.Intensity}, Actual {actual.Intensity}");
            }

            if (!expected.ChromXs.AreEqual(actual.ChromXs))
            {
                errorMessages.Add($"{nameof(IChromatogramPeak.ChromXs)}: Expected {expected.ChromXs}, Actual {actual.ChromXs}");
            }

            if (errorMessages.Count > 0)
            {
                Assert.Fail($"{message} Mismatches found: {string.Join("; ", errorMessages)}");
            }
        }
    }
}
