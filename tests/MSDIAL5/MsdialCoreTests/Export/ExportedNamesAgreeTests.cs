using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export.Tests
{
    /// <summary>
    /// The four files a run produces show the same compound name for the same peak.
    /// </summary>
    /// <remarks>
    /// The join between the peak table, the alignment table, mzTab-M and the exported spectra is the
    /// peak ID, and that contract is not in question here. What is, is the field a person looks at
    /// first: a row that reads "low score: Quercetin" in one file and "Quercetin" in another invites
    /// the conclusion that they are different rows, and the reader has no reason to suspect that one
    /// exporter strips a prefix and another does not.
    ///
    /// Until now mzTab-M alone canonicalised the name; the tables and the exported spectra published
    /// spot.Name raw, prefix and all. AnnotationName.Canonical is now the single rule and these are
    /// the tests that hold the four together. What the prefix said travels as its own field --
    /// "Evidence source" in the tables, opt_global_evidence_source in mzTab-M, EVIDENCE= in a
    /// spectrum's COMMENT -- where it says more than the prefix could.
    /// </remarks>
    [TestClass()]
    public class ExportedNamesAgreeTests
    {
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task APrefixedNameReadsTheSameInTheTableInMztabAndInTheSpectrum() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            // Paired by position, which is how MztabFormatExporterCore pairs them.
            var msdec = msdecs[container.AlignmentSpotProperties.IndexOf(spot)];
            spot.Name = AnnotationName.AsLowScore("Quercetin");

            var accessor = (IMetadataAccessor)new StubMetadataAccessor(storage.DataBaseMapper, storage.Parameter);
            var fromTable = accessor.GetContent(spot, msdec)["Metabolite name"];
            var fromSpectrum = NameFromExportedSpectrum(spot, msdec, storage);

            Assert.AreEqual("Quercetin", fromTable, "the alignment table");
            Assert.AreEqual("Quercetin", fromSpectrum, "the exported spectrum");
        }

        /// <summary>
        /// A lipid is reported at the same structural level everywhere, and the level comes from the
        /// match record rather than from the punctuation.
        /// </summary>
        /// <remarks>
        /// "PC 34:1|PC 16:0_18:1" means "chains resolved" when MsScanMatching wrote it and
        /// "chains unsupported" when MsReferenceScorer did. Reading the text cannot tell them apart;
        /// IsLipidChainsMatch can. If one exporter guessed and another asked, the two files would
        /// disagree about what was measured, which is worse than either answer.
        /// </remarks>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task AnUnresolvedLipidIsReportedAtClassLevelInBothTheTableAndTheSpectrum() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            // Paired by position, which is how MztabFormatExporterCore pairs them.
            var msdec = msdecs[container.AlignmentSpotProperties.IndexOf(spot)];
            spot.Name = "PC 34:1|PC 16:0_18:1";
            spot.MatchResults.Representative.IsLipidChainsMatch = false;

            var accessor = (IMetadataAccessor)new StubMetadataAccessor(storage.DataBaseMapper, storage.Parameter);

            Assert.AreEqual("PC 34:1", accessor.GetContent(spot, msdec)["Metabolite name"]);
            Assert.AreEqual("PC 34:1", NameFromExportedSpectrum(spot, msdec, storage));
        }

        /// <summary>
        /// An exported spectrum carries the evidence record beside the peak ID it is joined by.
        /// </summary>
        /// <remarks>
        /// This is what the handoff needed. A spectrum leaves MS-DIAL for MS-FINDER, ICEBERG or
        /// SIRIUS and comes back as a new annotation; without this, what MS-DIAL had already
        /// established about that spectrum stayed behind in a table, and the downstream tool could
        /// not see whether the spectrum arrived already identified by a reference match or entirely
        /// unexplained.
        /// </remarks>
        [TestMethod()]
        public void AnExportedSpectrumCarriesTheEvidenceBesideThePeakId() {
            var spot = Spot("Quercetin", AnnotationEvidenceSource.ReferenceSpectrum,
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime);

            var comment = FieldFromExportedSpectrum(spot, "COMMENT");

            var fields = comment.Split('|');
            CollectionAssert.Contains(fields, "PEAKID=" + spot.MasterAlignmentID);
            CollectionAssert.Contains(fields, "EVIDENCE=ReferenceSpectrum");
            CollectionAssert.Contains(fields, "TERMS=Spectrum,AccurateMass,RetentionTime");
        }

        /// <summary>
        /// THE TERM LIST USES A SEPARATOR THAT THE COMMENT FIELD HAS NOT ALREADY TAKEN.
        /// </summary>
        /// <remarks>
        /// COMMENT is a '|'-delimited list of key=value fields, and the tables and mzTab-M join
        /// measured terms with '|'. Reusing it here would turn one value into several fields, and a
        /// reader splitting on '|' would lose every term after the first without any error --
        /// exactly the kind of silent loss this whole record exists to end.
        /// </remarks>
        [TestMethod()]
        public void TheTermListDoesNotCollideWithTheCommentFieldSeparator() {
            var spot = Spot("Quercetin", AnnotationEvidenceSource.ReferenceSpectrum,
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime);

            var comment = FieldFromExportedSpectrum(spot, "COMMENT");
            var terms = comment.Split('|').Single(f => f.StartsWith("TERMS="));

            Assert.AreEqual(3, terms.Substring("TERMS=".Length).Split(',').Length,
                "all three terms survive the field split");
            // The tab-delimited formats have no such collision and keep the '|' form.
            Assert.AreEqual("Spectrum|AccurateMass|RetentionTime",
                AnnotationEvidenceFormat.Terms(spot.MatchResults.Representative));
        }

        /// <summary>
        /// A peak with nothing recorded says so, rather than omitting the fields.
        /// </summary>
        /// <remarks>
        /// A stable field set is what lets a reader parse the comment at all; a field that appears
        /// only sometimes is a field that gets missed.
        /// </remarks>
        [TestMethod()]
        public void AnUnannotatedSpectrumStillCarriesTheFieldsAndSaysTheyAreEmpty() {
            var spot = Spot("Unknown", AnnotationEvidenceSource.Unspecified, MeasuredTerms.None);

            var fields = FieldFromExportedSpectrum(spot, "COMMENT").Split('|');

            CollectionAssert.Contains(fields, "EVIDENCE=null");
            CollectionAssert.Contains(fields, "TERMS=null");
        }

        private static string NameFromExportedSpectrum(AlignmentSpotProperty spot, IMSScanProperty msdec, IMsdialDataStorage<ParameterBase> storage) {
            using var stream = new MemoryStream();
            SpectraExport.SaveSpectraTableAsNistFormat(stream, spot, msdec.Spectrum, storage.DataBaseMapper, storage.Parameter);
            return FieldOf(stream, "NAME");
        }

        private static string FieldFromExportedSpectrum(AlignmentSpotProperty spot, string field) {
            using var stream = new MemoryStream();
            SpectraExport.SaveSpectraTableAsNistFormat(
                stream, spot,
                new List<SpectrumPeak> { new SpectrumPeak { Mass = 100d, Intensity = 50d, }, },
                new DataBaseMapper(), new ParameterBase());
            return FieldOf(stream, field);
        }

        private static string FieldOf(MemoryStream stream, string field) {
            var text = System.Text.Encoding.ASCII.GetString(stream.ToArray());
            var line = text.Split('\n').Select(l => l.TrimEnd('\r')).First(l => l.StartsWith(field + ": "));
            return line.Substring(field.Length + 2);
        }

        private static AlignmentSpotProperty Spot(string name, AnnotationEvidenceSource evidence, MeasuredTerms terms) {
            var spot = new AlignmentSpotProperty
            {
                MasterAlignmentID = 42,
                Name = name,
                Comment = string.Empty,
                MassCenter = 250.15,
                TimesCenter = new ChromXs(new RetentionTime(5.5)),
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, },
            };
            spot.MatchResults.AddResult(new MsScanMatchResult
            {
                Name = name,
                Source = SourceType.MspDB,
                AnnotatorID = "MspDB",
                EvidenceSource = evidence,
                MeasuredTerms = terms,
                IsReferenceMatched = evidence == AnnotationEvidenceSource.ReferenceSpectrum,
            });
            return spot;
        }

        private static async Task<(IMsdialDataStorage<ParameterBase>, AlignmentResultContainer, List<MSDec.MSDecResult>)> LoadExportFixtureAsync() {
            IMsdialDataStorage<ParameterBase> storage;
            using (var streamManager = new DirectoryTreeStreamManager("./Resources/Export")) {
                storage = await MsdialDataStorage.Serializer.LoadAsync(streamManager, "Dataset_2025_07_31_12_31_11.mddata", "", "");
                storage.FixDatasetFolder("./Resources/Export");
            }
            var alignmentFile = storage.AlignmentFiles.Last();
            var container = AlignmentResultContainer.Load(alignmentFile);
            var loader = new MSDec.MSDecLoader(alignmentFile.SpectraFilePath, []);
            return (storage, container, loader.LoadMSDecResults());
        }
    }
}
