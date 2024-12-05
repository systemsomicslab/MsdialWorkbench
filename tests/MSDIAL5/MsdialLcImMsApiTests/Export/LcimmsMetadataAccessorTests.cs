using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialLcImMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Export.Tests
{
    [TestClass()]
    public class LcimmsMetadataAccessorTests
    {
        private static readonly string[] lcimHeaders = new string[] {
            "Alignment ID", "Average Rt(min)", "Average Mz","Average mobility", "Average CCS",
            "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
            "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", 
            "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
            "Comment", "Manually modified for quantification", "Manually modified for annotation",
            "Isotope tracking parent ID",  "Isotope tracking weight number", "RT similarity", "CCS similarity",
            "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
            "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
            "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum" };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new LcimmsMetadataAccessor(null, null, trimSpectrumToExcelLimit: false);
            CollectionAssert.AreEqual(lcimHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            IMetadataAccessor accessor = new LcimmsMetadataAccessor(new FakeRefer(), new MsdialLcImMsParameter { MachineCategory = MachineCategory.LCIMMS }, trimSpectrumToExcelLimit: false);
            var spot = new AlignmentSpotProperty
            {
                TimesCenter = new ChromXs(20.001, ChromXType.Drift, ChromXUnit.Msec)
                {
                    RT = new RetentionTime(3d, ChromXUnit.Min),
                },
                MassCenter = 700d,
                CollisionCrossSection = 5.002,
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
                        IsPrecursorMzMatch = true,
                        IsSpectrumMatch = true,
                        IsCcsMatch = true,
                        CcsSimilarity = 0.81f,
                        IsRtMatch = true,
                        RtSimilarity = 0.92f,
                    }
                });

            var dict = accessor.GetContent(spot, null);

            Assert.AreEqual("3.000", dict["Average Rt(min)"]);
            Assert.AreEqual("3.100", dict["Reference RT"]);
            Assert.AreEqual("True", dict["RT matched"]);
            Assert.AreEqual("0.92", dict["RT similarity"]);

            Assert.AreEqual("700.00000", dict["Average Mz"]);
            Assert.AreEqual("20.001", dict["Average mobility"]);
            Assert.AreEqual("5.002", dict["Average CCS"]);
            Assert.AreEqual("6.001", dict["Reference CCS"]);
            Assert.AreEqual("700.00100", dict["Reference m/z"]);
            Assert.AreEqual("True", dict["CCS matched"]);
            Assert.AreEqual("True", dict["m/z matched"]);
            Assert.AreEqual("True", dict["MS/MS matched"]);
            Assert.AreEqual("0.81", dict["CCS similarity"]);
        }

        private sealed class FakeRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => "Mock";

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference
                {
                    PrecursorMz = 700.00100d,
                    ChromXs = new ChromXs(20.003d, ChromXType.Drift, ChromXUnit.Msec)
                    {
                        RT = new RetentionTime(3.1d, ChromXUnit.Min),
                    },
                    CollisionCrossSection = 6.001,
                };
            }
        }
    }
}
