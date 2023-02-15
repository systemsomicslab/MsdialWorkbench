using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
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
            var scan = new MSDecResult();
            var annotationResults = new[]
            {
                new MsScanMatchResult { Name = "Result1", TotalScore = .8f, IsReferenceMatched = true, },
                new MsScanMatchResult { Name = "Result2", TotalScore = .9f, IsReferenceMatched = false, },
            };
            var molecule = new MoleculeProperty
            {
                Name = annotationResults[0].Name,
            };
            var resultContainer = new MsScanMatchResultContainer();
            resultContainer.AddResults(annotationResults);
            var annotatedMSDecResult = new AnnotatedMSDecResult(scan, resultContainer, molecule, 1);
            var peak = new BaseChromatogramPeakFeature();
            var quantifiedChromatogramPeak = new QuantifiedChromatogramPeak(peak, 100, 50, 150, new ChromatogramPeakShape());
            var feature = new QuantifiedMSDecResult(annotatedMSDecResult, quantifiedChromatogramPeak);

            var memory = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(feature, memory);
            memory.Position = 0;
            var actual = MessagePackDefaultHandler.LoadFromStream<QuantifiedMSDecResult>(memory);
            Assert.That.AreEqual(feature, actual);
        }
    }
}