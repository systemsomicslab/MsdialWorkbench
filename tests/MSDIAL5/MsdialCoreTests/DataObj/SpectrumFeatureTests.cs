using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class SpectrumFeatureTests
    {
        [TestMethod()]
        public void SerializationAndDeserializationTest() {
            var scan = new MSScanProperty();
            var annotationResults = new[]
            {
                new MsScanMatchResult { Name = "Result1", TotalScore = .8f, IsReferenceMatched = true, },
                new MsScanMatchResult { Name = "Result2", TotalScore = .9f, IsReferenceMatched = false, },
            };
            var molecule = new MoleculeProperty
            {
                Name = annotationResults[0].Name,
            };
            var feature = new SpectrumFeature(1, scan, molecule);
            feature.MatchResults.AddResults(annotationResults);

            var memory = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(feature, memory);
            memory.Position = 0;
            var actual = MessagePackDefaultHandler.LoadFromStream<SpectrumFeature>(memory);
            Assert.That.AreEqual(feature, actual);
        }
    }
}