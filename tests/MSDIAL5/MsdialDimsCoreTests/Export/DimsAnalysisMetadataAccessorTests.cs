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

namespace CompMs.MsdialDimsCore.Export.Tests
{
    [TestClass()]
    public class DimsAnalysisMetadataAccessorTests
    {
        private static readonly string[] diHeaders = new[] {
            "Peak ID",
            "Name",
            "Scan",
            "m/z left",
            "m/z",
            "m/z right",
            "Height",
            "Area",
            "Model masses",
            "Adduct",
            "Isotope",
            "Comment",
            "Reference m/z",
            "Formula",
            "Ontology",
            "InChIKey",
            "SMILES",
            "Annotation tag (VS1.0)",
            "m/z matched",
            "MS/MS matched",
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
            var accessor = new DimsAnalysisMetadataAccessor(null, null, default);
            CollectionAssert.AreEqual(diHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            var parameter = new ParameterBase { CentroidMs1Tolerance = 0.01f, MS2DataType = MSDataType.Centroid, };
            var stubFile = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA, };
            var refer = new MockRefer();
            var accessor = new DimsAnalysisMetadataAccessor(refer, parameter);
            var basePeak = new BaseChromatogramPeakFeature
            {
                ChromXsLeft = new ChromXs(699.99951, ChromXType.Mz, ChromXUnit.Mz),
                ChromXsRight = new ChromXs(700.00051, ChromXType.Mz, ChromXUnit.Mz),
                Mass = 700.00001,
            };
            var feature = new ChromatogramPeakFeature(basePeak)
            {
                MatchResults = new MsScanMatchResultContainer(),
            };
            feature.MatchResults.AddResults(
                new List<MsScanMatchResult> {
                    new MsScanMatchResult {
                        Priority = 1,
                        IsPrecursorMzMatch = true,
                        AcurateMassSimilarity = 0.921f,
                    },
                });
            var msdec = new MSDecResult();
            var provider = new MockDataProvider();
            var content = accessor.GetContent(feature, msdec, provider, stubFile, new());

            Assert.AreEqual("699.99951", content["m/z left"]);
            Assert.AreEqual("700.00001", content["m/z"]);
            Assert.AreEqual("700.00051", content["m/z right"]);
            Assert.AreEqual("True", content["m/z matched"]);
            Assert.AreEqual("0.92", content["m/z similarity"]);
            Assert.AreEqual("700.00021", content["Reference m/z"]);
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
                };
            }
        }
    }
}