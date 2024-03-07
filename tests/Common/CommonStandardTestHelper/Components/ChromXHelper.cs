using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components.Tests
{
    public static class ChromXHelper
    {
        public static void AreEqual(this Assert assert, IChromX expected, IChromX actual, string message = "")
        {
            var errorMessages = new List<string>(0);

            if (expected.Value != actual.Value)
            {
                errorMessages.Add($"Value: Expected {expected.Value}, Actual {actual.Value}");
            }

            if (expected.Type != actual.Type)
            {
                errorMessages.Add($"Type: Expected {expected.Type}, Actual {actual.Type}");
            }

            if (expected.Unit != actual.Unit)
            {
                errorMessages.Add($"Unit: Expected {expected.Unit}, Actual {actual.Unit}");
            }

            if (errorMessages.Any())
            {
                Assert.Fail($"{message} Mismatches: {string.Join("; ", errorMessages)}");
            }
        }

        public static bool AreEqual(this IChromX expected, IChromX actual)
        {
            return expected.Value == actual.Value &&
                expected.Type == actual.Type &&
                expected.Unit == actual.Unit;
        }
    }
}
