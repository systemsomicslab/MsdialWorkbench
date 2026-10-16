using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
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

using System.Linq;
namespace CompMs.MsdialLcImMsApi.Export.Tests
{
    [TestClass()]
    public class LcimmsAnalysisMetadataAccessorTests
    {
        private static readonly string[] lcimmsHeaders = new string[] {
            "Peak ID",
            "Name",
            "Scan",
            "RT left(min)",
            "RT (min)",
            "RT right (min)",
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
            "Reference RT",
            "Reference CCS",
            "Reference m/z",
            "Formula",
            "Ontology",
            "InChIKey",
            "SMILES",
            "Annotation tag (VS1.0)",
            "RT matched",
            "CCS matched",
            "m/z matched",
            "MS/MS matched",
            "RT similarity",
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
            "MSMS spectrum",
            "Measured terms",
            "Evidence source",
            "Candidates found",
            "Candidates above threshold",
            "Candidates reference matched" };

        [TestMethod()]
        public void GetHeadersTest()
        {
            var accessor = new LcimmsAnalysisMetadataAccessor(null, null, ExportspectraType.deconvoluted);
            CollectionAssert.AreEqual(lcimmsHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest()
        {
            var accessor = new LcimmsAnalysisMetadataAccessor(new MockRefer(), new ParameterBase { MachineCategory = MachineCategory.LCIMMS }, ExportspectraType.deconvoluted);
            var feature = new ChromatogramPeakFeature
            {
                PeakFeature = new BaseChromatogramPeakFeature
                {
                    ChromXsLeft = new ChromXs(new RetentionTime(1.1, ChromXUnit.Min)) { Drift = new DriftTime(4.0, ChromXUnit.Msec) },
                    ChromXsTop = new ChromXs(new RetentionTime(1.2, ChromXUnit.Min)) { Drift = new DriftTime(4.1, ChromXUnit.Msec) },
                    ChromXsRight = new ChromXs(new RetentionTime(1.3, ChromXUnit.Min)) { Drift = new DriftTime(4.2, ChromXUnit.Msec) },
                    PeakHeightTop = 1000,
                    PeakAreaAboveZero = 2000,
                },
                CollisionCrossSection = 120.0,
                PrecursorMz = 500.12345,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Comment = "Test comment",
                MatchResults = new MsScanMatchResultContainer(),
            };
            feature.MatchResults.AddResults(
                new List<MsScanMatchResult> {
                    new MsScanMatchResult {
                        Priority = 1,
                        IsPrecursorMzMatch = true,
                        IsRtMatch = true,
                        IsCcsMatch = true,
                        AcurateMassSimilarity = 0.99f,
                        RtSimilarity = 0.95f,
                        CcsSimilarity = 0.98f,
                    },
                });
            var msdec = new MSDecResult();
            var analysisFile = new AnalysisFileBean();

            var dict = accessor.GetContent(feature, msdec, new MockDataProvider(), analysisFile, new());
            // HEADER/CONTENT PARITY. Every header must have a content key, or an exporter throws
            // KeyNotFoundException the moment it indexes the content by header name; and every content
            // key must have a header, or its value is computed and then silently dropped on the floor.
            // Nothing checked either direction before, which is how "Enhanced dot product" and
            // "Spectrum entropy" came to be computed on every analysis row and written to no file in
            // any mode, for as long as they have existed.
            //
            // A drop has to be declared here, with a reason. That is the whole mechanism: it does not
            // forbid dropping a key, it forbids dropping one by accident.
            var declaredDrops = new HashSet<string>
            {
            // PRE-EXISTING AND UNINTENDED, declared here rather than fixed. BaseAnalysisMetadataAccessor
            // computes both of these for every row and lists them in its own header array, but this
            // mode replaces that array and omits them, so they are computed and thrown away on every
            // analysis export in this mode. Fixing it changes the published column set of four modes,
            // which is a compatibility decision that does not belong in the commit that adds the
            // evidence columns. Declared so that the next accidental drop cannot hide among them.
            "Enhanced dot product",
            "Spectrum entropy",
            };
            foreach (var header in accessor.GetHeaders()) {
                Assert.IsTrue(dict.ContainsKey(header),
                    $"header \"{header}\" has no content key, so exporting would throw");
            }
            foreach (var key in dict.Keys) {
                Assert.IsTrue(declaredDrops.Contains(key) || accessor.GetHeaders().Contains(key),
                    $"content key \"{key}\" has no header, so its value never reaches the file");
            }


            Assert.AreEqual("1.100", dict["RT left(min)"]);
            Assert.AreEqual("1.200", dict["RT (min)"]);
            Assert.AreEqual("1.300", dict["RT right (min)"]);
            Assert.AreEqual("4.000", dict["Mobility left"]);
            Assert.AreEqual("4.100", dict["Mobility"]);
            Assert.AreEqual("4.200", dict["Mobility right"]);
            Assert.AreEqual("120.000", dict["CCS"]);
            Assert.AreEqual("500.12345", dict["Precursor m/z"]);
            Assert.AreEqual("1.250", dict["Reference RT"]);
            Assert.AreEqual("122.000", dict["Reference CCS"]);
            Assert.AreEqual("500.12340", dict["Reference m/z"]);
            Assert.AreEqual("True", dict["RT matched"]);
            Assert.AreEqual("True", dict["CCS matched"]);
            Assert.AreEqual("True", dict["m/z matched"]);
            Assert.AreEqual("0.95", dict["RT similarity"]);
            Assert.AreEqual("0.98", dict["CCS similarity"]);
            Assert.AreEqual("0.99", dict["m/z similarity"]);
        }
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
        public string Key => "Mock";

        public MoleculeMsReference Refer(MsScanMatchResult result)
        {
            return new MoleculeMsReference
            {
                PrecursorMz = 500.12340,
                ChromXs = new ChromXs(new RetentionTime(1.25, ChromXUnit.Min)),
                CollisionCrossSection = 122.0
            };
        }
    }
}

