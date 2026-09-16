using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components.Tests;

public static class SpectrumPeakTestHelper
{
    public static void AreEqual(this Assert assert, SpectrumPeak expected, SpectrumPeak actual) {
        Assert.AreEqual(expected.Mass, actual.Mass);
        Assert.AreEqual(expected.Intensity, actual.Intensity);
        Assert.AreEqual(expected.Comment, actual.Comment);
        Assert.AreEqual(expected.PeakQuality, actual.PeakQuality);
        Assert.AreEqual(expected.PeakID, actual.PeakID);
        Assert.AreEqual(expected.SpectrumComment, actual.SpectrumComment);
        Assert.AreEqual(expected.IsAbsolutelyRequiredFragmentForAnnotation, actual.IsAbsolutelyRequiredFragmentForAnnotation);
    }

    public static string ToStringForTest(this IEnumerable<SpectrumPeak> peaks) {
        return string.Join("\n", [
            "[",
            .. peaks.Select(p => $"  m/z: {p.Mass}, Intensity: {p.Intensity}"),
            "]",
        ]);
    }

    public static void ShowForTest(this IEnumerable<SpectrumPeak> peaks) {
        System.Console.WriteLine(peaks.ToStringForTest());
    }
}
