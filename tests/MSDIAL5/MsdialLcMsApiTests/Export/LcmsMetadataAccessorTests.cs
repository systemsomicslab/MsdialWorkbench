using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

using System.Linq;
namespace CompMs.MsdialLcMsApi.Export.Tests
{
    [TestClass()]
    public class LcmsMetadataAccessorTests
    {
        private static readonly string[] lcHeaders = new string[] {
            "Alignment ID",
            "Average Rt(min)", "Average Mz",
            "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
            "Reference RT", "Reference m/z",
            "Formula", "Ontology", "INCHIKEY", "SMILES", "Annotation tag (VS1.0)",
            "RT matched", "m/z matched", "MS/MS matched",
            "Comment", "Manually modified for quantification", "Manually modified for annotation",
            "Isotope tracking parent ID",  "Isotope tracking weight number", "RT similarity", "m/z similarity", 
            "Simple dot product", "Weighted dot product", "Reverse dot product",
            "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
            "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum",
            "Measured terms",
            "Evidence source",
            "Candidates found",
            "Candidates above threshold",
            "Candidates reference matched" };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new LcmsMetadataAccessor(null, null, trimSpectrumToExcelLimit: false);
            CollectionAssert.AreEqual(lcHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            IMetadataAccessor accessor = new LcmsMetadataAccessor(new MockRefer(), new MsdialCore.Parameter.ParameterBase { MachineCategory = Common.Enum.MachineCategory.LCMS }, trimSpectrumToExcelLimit: false);
            var spot = new AlignmentSpotProperty
            {
                TimesCenter = new ChromXs(3d, ChromXType.RT, ChromXUnit.Min),
                MassCenter = 700d,
                MatchResults = new MsScanMatchResultContainer(),

                // to avoid errors
                MasterAlignmentID = 100,
                Name = "AAA",
                FillParcentage = 0.67f,
                RepresentativeFileID = 1,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { FileID = 0, },
                    new AlignmentChromPeakFeature { FileID = 1, FileName = "GGG", MS2RawSpectrumID2CE = new Dictionary<int, double> { { 1, 0d }, }, },
                    new AlignmentChromPeakFeature { FileID = 2, MS2RawSpectrumID2CE = new Dictionary<int, double> { { 2, 0d }, }, },
                },
                Comment = "FFF",
                IsManuallyModifiedForQuant = true,
                SignalToNoiseAve = 12.34f,
            };
            spot.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
            spot.MatchResults.AddResults(
                new List<MsScanMatchResult> {
                    new MsScanMatchResult {
                        Priority = 1,
                        IsRtMatch = true,
                        IsPrecursorMzMatch = true,
                        IsSpectrumMatch = true,
                        RtSimilarity = 0.92f,
                    }
                });
            var dict = accessor.GetContent(spot, null);
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
            };
            foreach (var header in accessor.GetHeaders()) {
                Assert.IsTrue(dict.ContainsKey(header),
                    $"header \"{header}\" has no content key, so exporting would throw");
            }
            foreach (var key in dict.Keys) {
                Assert.IsTrue(declaredDrops.Contains(key) || accessor.GetHeaders().Contains(key),
                    $"content key \"{key}\" has no header, so its value never reaches the file");
            }


            Assert.AreEqual("3.000", dict["Average Rt(min)"]);
            Assert.AreEqual("700.00000", dict["Average Mz"]);
            Assert.AreEqual("3.100", dict["Reference RT"]);
            Assert.AreEqual("700.00100", dict["Reference m/z"]);
            Assert.AreEqual("True", dict["RT matched"]);
            Assert.AreEqual("True", dict["m/z matched"]);
            Assert.AreEqual("True", dict["MS/MS matched"]);
            Assert.AreEqual("0.92", dict["RT similarity"]);
        }
    }

    class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public string Key => "Mock";

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return new MoleculeMsReference
            {
                PrecursorMz = 700.00100d,
                ChromXs = new ChromXs(3.100d, ChromXType.RT, ChromXUnit.Min),
            };
        }
    }
}