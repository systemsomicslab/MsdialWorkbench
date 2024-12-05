using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
            "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum" };

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