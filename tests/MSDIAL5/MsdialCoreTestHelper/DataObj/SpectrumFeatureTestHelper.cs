using CompMs.Common.Components.Tests;
using CompMs.Common.Interfaces.Tests;
using CompMs.MsdialCore.MSDec.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    public static class AnnotatedMSDecResultTestHelper {
        public static void AreEqual(this Assert assert, AnnotatedMSDecResult expected, AnnotatedMSDecResult actual) {
            assert.AreEqual(expected.MSDecResult, actual.MSDecResult);
            assert.AreEqual(expected.MatchResults, actual.MatchResults);
            assert.AreEqual(expected.Molecule, actual.Molecule);
            Assert.AreEqual(expected.QuantMass, actual.QuantMass);
        }
    }

    public static class QuantifiedChromatogramPeakTestHelper {
        public static void AreEqual(this Assert assert, QuantifiedChromatogramPeak expected, QuantifiedChromatogramPeak actual) {
            assert.AreEqual(expected.PeakFeature, actual.PeakFeature);
            assert.AreEqual(expected.PeakShape, actual.PeakShape);
            Assert.AreEqual(expected.MS1RawSpectrumIdTop, actual.MS1RawSpectrumIdTop);
            Assert.AreEqual(expected.MS1RawSpectrumIdLeft, actual.MS1RawSpectrumIdLeft);
            Assert.AreEqual(expected.MS1RawSpectrumIdRight, actual.MS1RawSpectrumIdRight);
        }
    }

    public static class SpectrumFeatureTestHelper {
        public static void AreEqual(this Assert assert, SpectrumFeature expected, SpectrumFeature actual) {
            assert.AreEqual(expected.AnnotatedMSDecResult, actual.AnnotatedMSDecResult);
            assert.AreEqual(expected.QuantifiedChromatogramPeak, actual.QuantifiedChromatogramPeak);
            Assert.AreEqual(expected.Comment, actual.Comment);
            assert.AreEqual(expected.FeatureFilterStatus, actual.FeatureFilterStatus);
        }
    }
}