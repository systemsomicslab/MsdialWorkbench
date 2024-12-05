using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Export.Tests
{
    [TestClass()]
    public class ImmsMetadataAccessorTests
    {
        private static readonly string[] imHeaders = new string[] {
            "Alignment ID",
            "Average Mz", "Average mobility", "Average CCS",
            "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
            "Reference CCS", "Reference m/z",
            "Formula", "Ontology", "INCHIKEY", "SMILES", "Annotation tag (VS1.0)",
            "CCS matched", "m/z matched", "MS/MS matched",
            "Comment", "Manually modified for quantification", "Manually modified for annotation",
            "Isotope tracking parent ID",  "Isotope tracking weight number",
            "CCS similarity", "m/z similarity", 
            "Simple dot product", "Weighted dot product", "Reverse dot product",
            "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
            "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum" };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new ImmsMetadataAccessor(null, null, trimSpectrumToExcelLimit: false);
            CollectionAssert.AreEqual(imHeaders, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            IMetadataAccessor accessor = new ImmsMetadataAccessor(new MockRefer(), new MsdialCore.Parameter.ParameterBase { MachineCategory = MachineCategory.IMMS }, trimSpectrumToExcelLimit: false);
            var spot = new AlignmentSpotProperty
            {
                TimesCenter = new ChromXs(20.001, ChromXType.Drift, ChromXUnit.Msec),
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
                    }
                });

            var dict = accessor.GetContent(spot, null);

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

        class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => "Mock";

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference
                {
                    PrecursorMz = 700.00100d,
                    ChromXs = new ChromXs(20.003d, ChromXType.Drift, ChromXUnit.Msec),
                    CollisionCrossSection = 6.001,
                };
            }
        }
    }
}