using CompMs.Common.Interfaces.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    public static class SpectrumFeatureTestHelper {
        public static void AreEqual(this Assert assert, QuantifiedMSDecResult expected, QuantifiedMSDecResult actual) {
            Assert.AreEqual(expected.QuantMass, actual.QuantMass);
            assert.AreEqual(expected.MSDecResult, actual.MSDecResult);
            //assert.AreEqual(expected.Molecule, actual.Molecule);
            assert.AreEqual(expected.MatchResults, actual.MatchResults);
            //assert.AreEqual(expected.PeakFeature, actual.PeakFeature);
            Assert.AreEqual(expected.MSDecResult.ScanID, actual.MSDecResult.ScanID);
            Assert.AreEqual(expected.MS1RawSpectrumIdTop, actual.MS1RawSpectrumIdTop);
            Assert.AreEqual(expected.MS1RawSpectrumIdLeft, actual.MS1RawSpectrumIdLeft);
            Assert.AreEqual(expected.MS1RawSpectrumIdRight, actual.MS1RawSpectrumIdRight);
            Assert.AreEqual(expected.MSDecResult.IonMode, actual.MSDecResult.IonMode);
            Assert.AreEqual(expected.Comment, actual.Comment);
            //assert.AreEqual(expected.PeakShape, actual.PeakShape);
            //assert.AreEqual(expected.FeatureFilterStatus, actual.FeatureFilterStatus);
        }
    }
}