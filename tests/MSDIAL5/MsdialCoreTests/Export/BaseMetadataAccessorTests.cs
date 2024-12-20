using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass()]
    public class BaseMetadataAccessorTests
    {
        private static readonly string[] headers = new string[] {
            "Alignment ID" ,
            // PeakSpotHeader,
            "Metabolite name",
            "Adduct type",
            "Post curation result",
            "Fill %",
            "MS/MS assigned",
            // ReferenceHeader,
            "Formula",
            "Ontology",
            "INCHIKEY",
            "SMILES",
            "Annotation tag (VS1.0)" ,
            // AnnotationMatchInfoHeader,
            "Comment",
            "Manually modified for quantification",
            "Manually modified for annotation" ,
            "Isotope tracking parent ID",
            "Isotope tracking weight number",
            "m/z similarity",
            "Simple dot product",
            "Weighted dot product",
            "Reverse dot product",
            "Matched peaks count",
            "Matched peaks percentage",
            "Total score",
            "S/N average",
            "Spectrum reference file name",
            "MS1 isotopic spectrum",
            "MS/MS spectrum",
        };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new TestMetadataAccessor(null, null);
            CollectionAssert.AreEqual(headers, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            var parameter = new ParameterBase { MachineCategory = Common.Enum.MachineCategory.LCMS };
            var refer = new MockRefer();
            IMetadataAccessor accessor = new TestMetadataAccessor(refer, parameter);
            var spot = new AlignmentSpotProperty
            {
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
                MatchResults = new MsScanMatchResultContainer(),
                SignalToNoiseAve = 12.34f,
                IsotopicPeaks = new List<IsotopicPeak> { new IsotopicPeak { Mass = 701.12345, AbsoluteAbundance = 345, }, new IsotopicPeak { Mass = 702.12345, AbsoluteAbundance = 12, } },
                IonAbundanceUnit = IonAbundanceUnit.nmol_per_mg_tissue,
            };
            spot.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
            var matchResult = new MsScanMatchResult
            {
                Source = SourceType.Manual | SourceType.MspDB,
                SimpleDotProduct = 0.81f,
                WeightedDotProduct = 0.82f,
                ReverseDotProduct = 0.83f,
                MatchedPeaksCount = 10,
                MatchedPeaksPercentage = 0.84f,
                TotalScore = 0.83f,
                AnnotatorID = "Annotation method1",
            };
            spot.MatchResults.AddResults(new List<MsScanMatchResult> { matchResult, });
            spot.PeakCharacter.IsotopeParentPeakID = 200;
            spot.PeakCharacter.IsotopeWeightNumber = 1;
            var msdecResult = new MSDecResult
            {
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 700.12345, Intensity = 999, },
                    new SpectrumPeak { Mass = 600.12345, Intensity = 164, },
                    new SpectrumPeak { Mass = 400.12345, Intensity = 190, },
                    new SpectrumPeak { Mass = 300.12345, Intensity = 587, },
                }
            };

            var dict = accessor.GetContent(spot, msdecResult);

            Assert.AreEqual("100", dict["Alignment ID"]);
            Assert.AreEqual("AAA", dict["Metabolite name"]);
            Assert.AreEqual("[M+H]+", dict["Adduct type"]);
            Assert.AreEqual("null", dict["Post curation result"]);
            Assert.AreEqual("0.67", dict["Fill %"]);
            Assert.AreEqual("True", dict["MS/MS assigned"]);
            Assert.AreEqual("C6H1200O6", dict["Formula"]);
            Assert.AreEqual("BBB", dict["Ontology"]);
            Assert.AreEqual("DDD", dict["INCHIKEY"]);
            Assert.AreEqual("EEE", dict["SMILES"]);
            Assert.AreEqual(DataAccess.GetAnnotationCode(spot.MatchResults.Representative, parameter).ToString(), dict["Annotation tag (VS1.0)"]);
            Assert.AreEqual($"{spot.Comment}; Normalized unit: {spot.IonAbundanceUnit}; Annotation method: {matchResult.AnnotatorID}", dict["Comment"]);
            Assert.AreEqual("True", dict["Manually modified for quantification"]);
            Assert.AreEqual("True", dict["Manually modified for annotation"]);
            Assert.AreEqual("200", dict["Isotope tracking parent ID"]);
            Assert.AreEqual("1", dict["Isotope tracking weight number"]);
            Assert.AreEqual("0.81", dict["Simple dot product"]);
            Assert.AreEqual("0.82", dict["Weighted dot product"]);
            Assert.AreEqual("0.83", dict["Reverse dot product"]);
            Assert.AreEqual("10.00", dict["Matched peaks count"]);
            Assert.AreEqual("0.84", dict["Matched peaks percentage"]);
            Assert.AreEqual("0.83", dict["Total score"]);
            Assert.AreEqual("12.34", dict["S/N average"]);
            Assert.AreEqual("GGG", dict["Spectrum reference file name"]);
            Assert.AreEqual("701.12345:345 702.12345:12", dict["MS1 isotopic spectrum"]);
            Assert.AreEqual("700.12345:999 600.12345:164 400.12345:190 300.12345:587", dict["MS/MS spectrum"]);
        }

        [TestMethod()]
        public void GetContentWhenCommentEmptyTest() {
            var refer = new MockRefer();
            IMetadataAccessor accessor = new TestMetadataAccessor(refer, new ParameterBase { MachineCategory = MachineCategory.LCMS });
            var spot = new AlignmentSpotProperty { Comment = string.Empty, IonAbundanceUnit = IonAbundanceUnit.pmol_per_mg_tissue, };
            var dict = accessor.GetContent(spot, null);
            Assert.AreEqual($"Normalized unit: {spot.IonAbundanceUnit}", dict["Comment"]);
        }

        [TestMethod()]
        public void GetContentWhenLipidCompoundTest() {
            var refer = new MockRefer();
            refer.Reference.CompoundClass = "CCC";
            refer.Reference.Ontology = null;
            IMetadataAccessor accessor = new TestMetadataAccessor(refer, new ParameterBase { MachineCategory = Common.Enum.MachineCategory.LCMS });
            var spot = new AlignmentSpotProperty { };

            var dict = accessor.GetContent(spot, null);

            Assert.AreEqual("CCC", dict["Ontology"]);
        }

        [TestMethod()]
        public void GetContentWhenIsotopesIsNullTest() {
            var parameter = new ParameterBase {};
            IMetadataAccessor accessor = new TestMetadataAccessor(new MockRefer(), parameter);
            var spot = new AlignmentSpotProperty { };

            var dict = accessor.GetContent(spot, null);

            Assert.AreEqual("null", dict["MS1 isotopic spectrum"]);
        }

        [TestMethod()]
        public void GetContentWhenMSDecIsNullTest() {
            var parameter = new ParameterBase {};
            IMetadataAccessor accessor = new TestMetadataAccessor(new MockRefer(), parameter);
            var spot = new AlignmentSpotProperty { };

            var dict = accessor.GetContent(spot, null);

            Assert.AreEqual("null", dict["MS/MS spectrum"]);
        }

        class TestMetadataAccessor : BaseMetadataAccessor
        {
            public TestMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) : base(refer, parameter) {

            }
        }

        class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => "Mock";

            public MoleculeMsReference Reference = new MoleculeMsReference
            {
                Formula = new Formula { FormulaString = "C6H1200O6" },
                Ontology = "BBB",
                InChIKey = "DDD",
                SMILES = "EEE",
            };

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return Reference;
            }
        }
    }
}