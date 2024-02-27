using CompMs.Common.Components;
using CompMs.Common.Components.Tests;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces.Tests;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.MSDec.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    public static class AnnotatedMSDecResultTestHelper {
        public static AnnotatedMSDecResult CreateSample() {
            var scan = new MSDecResult { ScanID = 1, };
            var annotationResults = new[]
            {
                new MsScanMatchResult { Name = "Result1", TotalScore = .8f, IsReferenceMatched = true, },
                new MsScanMatchResult { Name = "Result2", TotalScore = .9f, IsReferenceMatched = false, },
            };
            var molecule = new MoleculeMsReference
            {
                ScanID = 1,
                Name = annotationResults[0].Name,
            };
            var resultContainer = new MsScanMatchResultContainer();
            resultContainer.AddResults(annotationResults);
            return new AnnotatedMSDecResult(scan, resultContainer, molecule);
        }

        public static void AreEqual(this Assert assert, AnnotatedMSDecResult expected, AnnotatedMSDecResult actual) {
            assert.AreEqual(expected.MSDecResult, actual.MSDecResult);
            assert.AreEqual(expected.MatchResults, actual.MatchResults);
            assert.AreEqual(expected.Molecule, actual.Molecule);
            Assert.AreEqual(expected.QuantMass, actual.QuantMass);
        }
    }

    public static class QuantifiedChromatogramPeakTestHelper {
        public static QuantifiedChromatogramPeak CreateSample() {
            return new QuantifiedChromatogramPeak(
                new ChromatogramPeakFeature { MasterPeakID = 1, Mass = 100d, },
                new ChromatogramPeakShape { AmplitudeOrderValue = 1, AmplitudeScoreValue = 1, },
                100, 50, 150);
        }

        public static QuantifiedChromatogramPeak WithMs1RawSpectrumIdTop(this QuantifiedChromatogramPeak peak, int id) {
            return new QuantifiedChromatogramPeak(peak.PeakFeature, peak.PeakShape, id, peak.MS1RawSpectrumIdLeft, peak.MS1RawSpectrumIdRight);
        }

        public static void AreEqual(this Assert assert, QuantifiedChromatogramPeak expected, QuantifiedChromatogramPeak actual) {
            assert.AreEqual(expected.PeakFeature, actual.PeakFeature);
            assert.AreEqual(expected.PeakShape, actual.PeakShape);
            Assert.AreEqual(expected.MS1RawSpectrumIdTop, actual.MS1RawSpectrumIdTop);
            Assert.AreEqual(expected.MS1RawSpectrumIdLeft, actual.MS1RawSpectrumIdLeft);
            Assert.AreEqual(expected.MS1RawSpectrumIdRight, actual.MS1RawSpectrumIdRight);
        }
    }

    public static class SpectrumFeatureTestHelper {
        public static SpectrumFeature CreateSample() {
            return new SpectrumFeature(
                AnnotatedMSDecResultTestHelper.CreateSample(),
                QuantifiedChromatogramPeakTestHelper.CreateSample(),
                FeatureFilterStatusTestHelper.CreateSample());
        }

        public static void AreEqual(this Assert assert, SpectrumFeature expected, SpectrumFeature actual) {
            assert.AreEqual(expected.AnnotatedMSDecResult, actual.AnnotatedMSDecResult);
            assert.AreEqual(expected.QuantifiedChromatogramPeak, actual.QuantifiedChromatogramPeak);
            Assert.AreEqual(expected.Comment, actual.Comment);
            assert.AreEqual(expected.FeatureFilterStatus, actual.FeatureFilterStatus);
        }
    }
}