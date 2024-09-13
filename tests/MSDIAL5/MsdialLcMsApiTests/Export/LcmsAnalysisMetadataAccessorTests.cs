using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Export.Tests
{
    [TestClass()]
    public class LcmsAnalysisMetadataAccessorTests
    {
        private static readonly string[] lcHeaders = new[] {
            "Peak ID", "Name", "Scan", "RT left(min)", "RT (min)", "RT right (min)",
            "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
            "Reference RT", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
            "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
            "RT similarity", "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
            "Matched peaks count", "Matched peaks percentage", "Total score",
            "S/N", "MS1 isotopes", "MSMS spectrum" };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new LcmsAnalysisMetadataAccessor(null, null, default);
            CollectionAssert.AreEqual(lcHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            var parameter = new ParameterBase { CentroidMs1Tolerance = 0.01f, MS2DataType = MSDataType.Centroid, };
            var stubFile = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA, };
            var refer = new MockRefer();
            var accessor = new LcmsAnalysisMetadataAccessor(refer, parameter);
            var basePeak = new BaseChromatogramPeakFeature
            {
                ChromXsLeft = new ChromXs(100.991, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(101.001, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(101.011, ChromXType.RT, ChromXUnit.Min),
                Mass = 700.00001,
            };
            var feature = new ChromatogramPeakFeature(basePeak)
            {
                MatchResults = new MsScanMatchResultContainer()
            };
            feature.MatchResults.AddResults(
                new List<MsScanMatchResult> {
                    new MsScanMatchResult {
                        Priority = 1,
                        IsPrecursorMzMatch = true,
                        IsRtMatch = true,
                        AcurateMassSimilarity = 0.811f,
                        RtSimilarity = 0.679f,
                    },
                });
            var msdec = new MSDecResult();
            var provider = new MockDataProvider();
            var content = accessor.GetContent(feature, msdec, provider, stubFile, new());

            Assert.AreEqual("100.991", content["RT left(min)"]);
            Assert.AreEqual("101.001", content["RT (min)"]);
            Assert.AreEqual("101.011", content["RT right (min)"]);
            Assert.AreEqual("700.00001", content["Precursor m/z"]);
            Assert.AreEqual("102.001", content["Reference RT"]);
            Assert.AreEqual("700.00021", content["Reference m/z"]);
            Assert.AreEqual("True", content["RT matched"]);
            Assert.AreEqual("True", content["m/z matched"]);
            Assert.AreEqual("0.68", content["RT similarity"]);
            Assert.AreEqual("0.81", content["m/z similarity"]);
        }

        class MockDataProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                return new List<RawSpectrum> { }.AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                return Task.FromResult(LoadMs1Spectrums());
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                throw new System.NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new System.NotImplementedException();
            }
        }

        class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => throw new System.NotImplementedException();

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference
                {
                    PrecursorMz = 700.00021,
                    ChromXs = new ChromXs(102.001, ChromXType.RT, ChromXUnit.Min),
                };
            }
        }
    }
}