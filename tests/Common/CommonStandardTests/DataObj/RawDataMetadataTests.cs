using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonStandardTests.DataObj {
    [TestClass]
    public sealed class RawDataMetadataTests {
        [TestMethod]
        public void RawMeasurement_InitializesMetadataContract() {
            var measurement = new RawMeasurement();

            Assert.IsNotNull(measurement.Metadata);
            Assert.AreEqual("msdial.raw-metadata.v1", measurement.Metadata.SchemaVersion);
            Assert.IsNotNull(measurement.Metadata.Samples);
            Assert.IsNotNull(measurement.Metadata.Experiments);
        }

        [DataTestMethod]
        [DataRow(-1d, 0d)]
        [DataRow(0.75d, 0.75d)]
        [DataRow(2d, 1d)]
        public void MetadataValue_NormalizesConfidence(double input, double expected) {
            var value = new MetadataValue<string>("value", MetadataSource.VendorHeader, input, "header");

            Assert.AreEqual(expected, value.Confidence, 0.0001d);
            Assert.AreEqual(MetadataSource.VendorHeader, value.Source);
            Assert.AreEqual("header", value.Evidence);
        }
    }
}
