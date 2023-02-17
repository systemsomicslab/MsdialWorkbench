using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class SpectrumFeatureTests
    {
        [TestMethod()]
        public void SerializationAndDeserializationTest() {
            var scan = new MSDecResult { ScanID = 1, };
            var annotationResults = new[]
            {
                new MsScanMatchResult { Name = "Result1", TotalScore = .8f, IsReferenceMatched = true, },
                new MsScanMatchResult { Name = "Result2", TotalScore = .9f, IsReferenceMatched = false, },
            };
            var molecule = new MoleculeMsReference
            {
                Name = annotationResults[0].Name,
            };
            var resultContainer = new MsScanMatchResultContainer();
            resultContainer.AddResults(annotationResults);
            var annotatedMSDecResult = new AnnotatedMSDecResult(scan, resultContainer, molecule);
            var peak = new BaseChromatogramPeakFeature { Mass = 100d, ChromScanIdLeft = 100, ChromScanIdTop = 150, ChromScanIdRight = 200, };
            var quantifiedChromatogramPeak = new QuantifiedChromatogramPeak(peak, new ChromatogramPeakShape(), 100, 50, 150);
            var feature = new SpectrumFeature(annotatedMSDecResult, quantifiedChromatogramPeak);

            var memory = new MemoryStream();
            feature.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = SpectrumFeature.Load(memory);
            Assert.That.AreEqual(feature, actual);
        }
    }
}