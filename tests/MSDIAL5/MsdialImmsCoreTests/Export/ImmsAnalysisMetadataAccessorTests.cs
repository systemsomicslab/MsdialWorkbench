using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsdialCoreTestHelper.DataProvider;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Export.Tests
{
    [TestClass()]
    public class ImmsAnalysisMetadataAccessorTests
    {
        private static readonly string[] imHeaders = new[] {
            "Peak ID",
            "Name",
            "Scan",
            "Mobility left",
            "Mobility",
            "Mobility right",
            "CCS",
            "Precursor m/z",
            "Height",
            "Area",
            "Model masses",
            "Adduct",
            "Isotope",
            "Comment",
            "Reference CCS",
            "Reference m/z",
            "Formula",
            "Ontology",
            "InChIKey",
            "SMILES",
            "Annotation tag (VS1.0)",
            "CCS matched",
            "m/z matched",
            "MS/MS matched",
            "CCS similarity",
            "m/z similarity",
            "Simple dot product",
            "Weighted dot product",
            "Reverse dot product",
            "Matched peaks count",
            "Matched peaks percentage",
            "Total score",
            "S/N",
            "MS1 isotopes",
            "MSMS spectrum" };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new ImmsAnalysisMetadataAccessor(null, null, default);
            CollectionAssert.AreEqual(imHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            var parameter = new ParameterBase { CentroidMs1Tolerance = 0.01f, MS2DataType = MSDataType.Centroid, };
            var stubFile = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA, };
            var refer = new MockRefer();
            var accessor = new ImmsAnalysisMetadataAccessor(refer, parameter);
            var basePeak = new BaseChromatogramPeakFeature
            {
                ChromXsLeft = new ChromXs(100.991, ChromXType.Drift, ChromXUnit.Msec),
                ChromXsTop = new ChromXs(101.001, ChromXType.Drift, ChromXUnit.Msec),
                ChromXsRight = new ChromXs(101.011, ChromXType.Drift, ChromXUnit.Msec),
                Mass = 700.00001,
            };
            var feature = new ChromatogramPeakFeature(basePeak)
            {
                CollisionCrossSection = 12.001f,
                MatchResults = new MsScanMatchResultContainer(),
            };
            feature.MatchResults.AddResults(
                new List<MsScanMatchResult> {
                    new MsScanMatchResult {
                        Priority = 1,
                        IsPrecursorMzMatch = true,
                        IsCcsMatch = true,
                        AcurateMassSimilarity = 0.811f,
                        CcsSimilarity = 0.679f,
                    },
                });
            var msdec = new MSDecResult();
            var provider = new StubDataProvider();
            var content = accessor.GetContent(feature, msdec, provider, stubFile, new());

            Assert.AreEqual("100.991", content["Mobility left"]);
            Assert.AreEqual("101.001", content["Mobility"]);
            Assert.AreEqual("101.011", content["Mobility right"]);
            Assert.AreEqual("12.001", content["CCS"]);
            Assert.AreEqual("700.00001", content["Precursor m/z"]);
            Assert.AreEqual("12.006", content["Reference CCS"]);
            Assert.AreEqual("700.00021", content["Reference m/z"]);
            Assert.AreEqual("True", content["CCS matched"]);
            Assert.AreEqual("True", content["m/z matched"]);
            Assert.AreEqual("0.68", content["CCS similarity"]);
            Assert.AreEqual("0.81", content["m/z similarity"]);
        }

        class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => throw new System.NotImplementedException();

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference
                {
                    PrecursorMz = 700.00021,
                    CollisionCrossSection = 12.0061,
                };
            }
        }
    }
}