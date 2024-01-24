using CompMs.Common.Components.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Interfaces.Tests
{
    public static class InterfaceMSScanPropertyTestHelper
    {
        public static void AreEqual(this Assert assert, IMSScanProperty expected, IMSScanProperty actual) {
            assert.AreEqual(expected.ChromXs, actual.ChromXs);
            Assert.AreEqual(expected.IonMode, actual.IonMode);
            Assert.AreEqual(expected.PrecursorMz, actual.PrecursorMz);
            Assert.AreEqual(expected.ScanID, actual.ScanID);
            Assert.AreEqual(expected.Spectrum.Count, actual.Spectrum.Count);
            for (int i = 0; i < expected.Spectrum.Count; i++) {
                Assert.AreEqual(expected.Spectrum[i].Mass, actual.Spectrum[i].Mass);
                Assert.AreEqual(expected.Spectrum[i].Intensity, actual.Spectrum[i].Intensity);
                Assert.AreEqual(expected.Spectrum[i].Comment, actual.Spectrum[i].Comment);
                Assert.AreEqual(expected.Spectrum[i].PeakQuality, actual.Spectrum[i].PeakQuality);
                Assert.AreEqual(expected.Spectrum[i].PeakID, actual.Spectrum[i].PeakID);
                Assert.AreEqual(expected.Spectrum[i].SpectrumComment, actual.Spectrum[i].SpectrumComment);
                Assert.AreEqual(expected.Spectrum[i].IsAbsolutelyRequiredFragmentForAnnotation, actual.Spectrum[i].IsAbsolutelyRequiredFragmentForAnnotation);
            }
        }
    }
}
