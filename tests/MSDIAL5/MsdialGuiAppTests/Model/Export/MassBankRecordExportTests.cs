using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export.Tests
{
    /// <summary>
    /// Export &gt; Alignment result export &gt; MassBank and Export &gt; Peak list result &gt; MassBank record, over a
    /// project saved to disk: which spots become records, what the records say, and what the person is told.
    /// </summary>
    [TestClass()]
    public class MassBankRecordExportTests
    {
        private const string Contributor = "MSDIAL_TEST";

        private string _folder = string.Empty;
        private string _output = string.Empty;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-massbank-export-{Guid.NewGuid():N}");
            _output = Path.Combine(_folder, "export");
            Directory.CreateDirectory(_output);
        }

        [TestCleanup()]
        public void Cleanup() {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            try {
                Directory.Delete(_folder, recursive: true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                // A loader that the export does not dispose keeps its file open until the finalizer runs.
            }
        }

        private static StudyContextModel ValidStudyContext(ProjectBaseParameter parameter) {
            return new StudyContextModel(parameter)
            {
                InstrumentType = "LC-ESI-QTOF",
                Instrument = "TripleTOF 6600, SCIEX",
                Authors = "MS-DIAL test",
                License = "CC BY",
                CollisionEnergy = "35 +/- 15 V",
                Comment = "MassBank export test",
            };
        }

        // ------------------------------------------------------------ alignment

        [TestMethod()]
        public void Alignment_ByDefaultExportsOnlyReferenceMatchedSpotsWithPeaks() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var (model, file) = CreateAlignmentExport(broker);
            model.IsSelected = true;
            model.ContributorID = Contributor;
            var export = (IAlignmentResultExportModel)model;

            Assert.AreEqual(1, export.CountExportFiles(file));
            var notified = new List<string>();
            export.Export(file, _output, notified.Add);

            var records = Directory.GetFiles(_output, "*.txt");
            Assert.AreEqual(1, records.Length);
            Assert.AreEqual(1, notified.Count, "one notification per counted file keeps the progress consistent");
            var lines = File.ReadAllLines(records[0]);
            CollectionAssert.Contains(lines, "CH$NAME: L-Pyroglutamic acid");
            CollectionAssert.Contains(lines, "AUTHORS: MS-DIAL test");
            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: COLLISION_ENERGY 35 +/- 15 V");
            CollectionAssert.Contains(lines, "COMMENT: MassBank export test");
            Assert.IsTrue(lines.Any(l => l.StartsWith("MS$DATA_PROCESSING: WHOLE MS-DIAL ")));
            StringAssert.StartsWith(Path.GetFileName(records[0]), $"MSBNK-{Contributor}-MSDIAL");

            var summary = messages.Single();
            Assert.AreEqual(MessageSeverity.Information, summary.Severity);
            StringAssert.Contains(summary.Content, $"1 MassBank record was written to {_output}.");
            StringAssert.Contains(summary.Content, "2  suggested annotations");
            StringAssert.Contains(summary.Content, "1  unknowns");
            StringAssert.Contains(summary.Content, "1  without MS/MS peaks");
        }

        [TestMethod()]
        public void Alignment_IncludesSuggestionsAndUnknownsWhenAsked() {
            var broker = new MessageBroker();
            var (model, file) = CreateAlignmentExport(broker);
            model.IsSelected = true;
            model.ContributorID = Contributor;
            model.IncludeSuggested = true;
            model.IncludeUnknowns = true;
            var export = (IAlignmentResultExportModel)model;

            Assert.AreEqual(4, export.CountExportFiles(file));
            export.Export(file, _output, _ => { });

            var records = Directory.GetFiles(_output, "*.txt").Select(File.ReadAllLines).ToList();
            Assert.AreEqual(4, records.Count);
            var unknown = records.Single(r => r.Contains("CH$NAME: Unknown"));
            CollectionAssert.Contains(unknown, "CH$SMILES: N/A");
            CollectionAssert.Contains(unknown, "CH$IUPAC: N/A");
            Assert.AreEqual(2, records.Count(r => r.Contains("COMMENT: MS-DIAL annotation level: suggested")));
            var unsettled = records.Single(r => r.Contains("COMMENT: Original name: Unsettled: L-Pyroglutamic acid"));
            CollectionAssert.Contains(unsettled, "CH$NAME: L-Pyroglutamic acid", "CH$NAME is the compound name, without MS-DIAL's level prefix.");
            CollectionAssert.Contains(unsettled, "COMMENT: MS-DIAL annotation level: suggested");
            var unknownComments = unknown.Where(l => l.StartsWith("COMMENT: ")).ToArray();
            Assert.AreEqual("COMMENT: MassBank export test", unknownComments[0], "the project comment comes first");
            CollectionAssert.Contains(unknownComments, "COMMENT: CH$EXACT_MASS was calculated from PRECURSOR_M/Z and the adduct [M+H]+; it is not a reference value");
        }

        [TestMethod()]
        public void Alignment_ReportsOnlyWhenTheWholeExportHasEnded() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var (model, file) = CreateAlignmentExport(broker);
            model.IsSelected = true;
            model.ContributorID = Contributor;
            var session = (IAlignmentResultExportSession)model;

            session.BeginExport();
            ((IAlignmentResultExportModel)model).Export(file, _output, _ => { });

            Assert.AreEqual(0, messages.Count, "a message box in the middle would hold up the export groups after MassBank");
            var report = session.EndExport();
            Assert.IsNotNull(report);
            StringAssert.Contains(report!.Content, "1 MassBank record was written");
            Assert.IsNull(session.EndExport(), "the report is given once");
            Assert.AreEqual(1, Directory.GetFiles(_output, "*.txt").Length);
        }

        [TestMethod()]
        public void Alignment_CountsNothingWhileTheMetadataIsInvalid() {
            var (model, file) = CreateAlignmentExport(new MessageBroker(), context => context.Authors = string.Empty);
            model.IsSelected = true;
            model.ContributorID = Contributor;
            Assert.AreEqual(0, ((IAlignmentResultExportModel)model).CountExportFiles(file));

            var (valid, validFile) = CreateAlignmentExport(new MessageBroker());
            valid.IsSelected = true;
            Assert.AreEqual(0, ((IAlignmentResultExportModel)valid).CountExportFiles(validFile), "no contributor ID");
        }

        [TestMethod()]
        public void Alignment_IsRefusedWhileTheMetadataIsInvalid() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var (model, file) = CreateAlignmentExport(broker, context => context.InstrumentType = "QTOF");
            model.IsSelected = true;
            model.ContributorID = "Xxx";

            ((IAlignmentResultExportModel)model).Export(file, _output, _ => { });

            Assert.AreEqual(0, Directory.GetFiles(_output).Length);
            var error = messages.Single();
            Assert.AreEqual(MessageSeverity.Error, error.Severity);
            StringAssert.Contains(error.Content, "Instrument type \"QTOF\"");
            StringAssert.Contains(error.Content, "Contributor ID \"Xxx\"");
            Assert.IsFalse(model.IsContributorIdValid);
            Assert.AreEqual(1, model.ValidateProjectMetadata().Count);
        }

        [TestMethod()]
        public void Alignment_ClassifiesByTheMatchFlagsAndTheSuggestionNames() {
            var evaluator = new FlagEvaluator();
            var spots = Spots();
            Assert.AreEqual(MassBankAnnotationLevel.ReferenceMatched, AlignmentResultMassBankRecordExportModel.Classify(spots[0], evaluator));
            Assert.AreEqual(MassBankAnnotationLevel.Suggested, AlignmentResultMassBankRecordExportModel.Classify(spots[1], evaluator));
            Assert.AreEqual(MassBankAnnotationLevel.Suggested, AlignmentResultMassBankRecordExportModel.Classify(spots[2], evaluator));
            Assert.AreEqual(MassBankAnnotationLevel.Unknown, AlignmentResultMassBankRecordExportModel.Classify(spots[3], evaluator));

            var putative = spots[0];
            putative.Name = "Putative: " + putative.Name;
            Assert.AreEqual(MassBankAnnotationLevel.Suggested, AlignmentResultMassBankRecordExportModel.Classify(putative, evaluator));
        }

        [TestMethod()]
        public void Alignment_ClassifiesWithMsDialsOwnEvaluator() {
            // The evaluator the method models build from the project's databases, not a stand-in.
            var evaluator = new FacadeMatchResultEvaluator();
            evaluator.Add("MspDB", new MsScanMatchResultEvaluator(new MsRefSearchParameterBase()));
            evaluator.Add("TextDB", new MsScanMatchResultEvaluator(new MsRefSearchParameterBase()));
            AlignmentSpotProperty Spot(MsScanMatchResult result, bool textDb) {
                var spot = new AlignmentSpotProperty { Name = result.Name, };
                spot.MatchResults.AddResult(result);
                if (textDb) {
                    spot.TextDbBasedMatchResult = result;
                    spot.MatchResults.AddTextDbResult(result);
                }
                return spot;
            }

            var msp = Spot(new MsScanMatchResult { Name = "Glycine", AnnotatorID = "MspDB", Source = SourceType.MspDB, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, TotalScore = 0.9f, }, textDb: false);
            Assert.AreEqual(MassBankAnnotationLevel.ReferenceMatched, AlignmentResultMassBankRecordExportModel.Classify(msp, evaluator));

            var suggestion = Spot(new MsScanMatchResult { Name = "low score: Glycine", AnnotatorID = "MspDB", Source = SourceType.MspDB, IsAnnotationSuggested = true, IsPrecursorMzMatch = true, TotalScore = 0.4f, }, textDb: false);
            Assert.AreEqual(MassBankAnnotationLevel.Suggested, AlignmentResultMassBankRecordExportModel.Classify(suggestion, evaluator));

            // A text library match (m/z and RT) is reference-matched in MS-DIAL's own sense.
            var text = Spot(new MsScanMatchResult { Name = "Glycine", AnnotatorID = "TextDB", Source = SourceType.TextDB, IsPrecursorMzMatch = true, IsRtMatch = true, TotalScore = 0.8f, }, textDb: true);
            Assert.AreEqual(MassBankAnnotationLevel.ReferenceMatched, AlignmentResultMassBankRecordExportModel.Classify(text, evaluator));

            Assert.AreEqual(MassBankAnnotationLevel.Unknown, AlignmentResultMassBankRecordExportModel.Classify(new AlignmentSpotProperty(), evaluator));
        }

        [TestMethod()]
        public void AlignmentViewModel_BlocksExportUntilTheContributorIdAndMetadataAreValid() {
            var (model, _) = CreateAlignmentExport(new MessageBroker());
            using (var vm = new AlignmentResultMassBankRecordExportViewModel(model)) {
                Assert.IsTrue(vm.CanExport.Value, "not selected, nothing to block");
                Assert.IsFalse(vm.HasProjectMetadataProblems);
                Assert.IsFalse(vm.ContributorID.HasErrors, "an option that is not chosen shows no error");

                vm.IsSelected.Value = true;
                Assert.IsFalse(vm.CanExport.Value, "the contributor ID is empty");
                Assert.IsTrue(vm.ContributorID.HasErrors);

                vm.ContributorID.Value = "riken-ims";
                Assert.IsFalse(vm.CanExport.Value);

                vm.ContributorID.Value = Contributor;
                Assert.IsTrue(vm.CanExport.Value);
                Assert.AreEqual(Contributor, model.ContributorID);
                Assert.IsTrue(model.IsContributorIdValid);
            }

            var (invalid, _) = CreateAlignmentExport(new MessageBroker(), context => context.License = "CC-BY");
            using (var vm = new AlignmentResultMassBankRecordExportViewModel(invalid)) {
                Assert.IsFalse(vm.ShowProjectMetadataProblems.Value, "the problems show only while MassBank is chosen");
                vm.IsSelected.Value = true;
                vm.ContributorID.Value = Contributor;
                Assert.IsTrue(vm.HasProjectMetadataProblems);
                Assert.IsTrue(vm.ShowProjectMetadataProblems.Value);
                StringAssert.Contains(vm.ProjectMetadataProblems, "License \"CC-BY\"");
                Assert.IsFalse(vm.CanExport.Value);
            }
        }

        // Four spots: reference matched; low-score suggestion; set to Unsettled by hand; unknown. A fifth, reference
        // matched, has no MS/MS peaks.
        private static List<AlignmentSpotProperty> Spots() {
            AlignmentSpotProperty Spot(int id, string name, MsScanMatchResult? result) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = id,
                    AlignmentID = id,
                    MassCenter = 130.0499021411,
                    TimesCenter = new ChromXs(0.6358123779, ChromXType.RT, ChromXUnit.Min),
                    IonMode = IonMode.Positive,
                    Name = name,
                };
                spot.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
                if (result is not null) {
                    spot.Formula = FormulaStringParcer.OrganicElementsReader("C5H7NO3");
                    spot.Ontology = "Alpha amino acids and derivatives";
                    spot.SMILES = "C1CC(=O)N[C@@H]1C(=O)O";
                    spot.InChIKey = "ODHCTXKNWHHXJC-UHFFFAOYSA-N";
                    spot.MatchResults.AddResult(result);
                }
                return spot;
            }
            return new List<AlignmentSpotProperty>
            {
                Spot(0, "L-Pyroglutamic acid", new MsScanMatchResult { Name = "L-Pyroglutamic acid", Source = SourceType.MspDB, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, TotalScore = 0.9f, }),
                Spot(1, "low score: L-Pyroglutamic acid", new MsScanMatchResult { Name = "L-Pyroglutamic acid", Source = SourceType.MspDB, IsAnnotationSuggested = true, IsPrecursorMzMatch = true, TotalScore = 0.5f, }),
                Spot(2, "Unsettled: L-Pyroglutamic acid", new MsScanMatchResult { Name = "L-Pyroglutamic acid", Source = SourceType.MspDB | SourceType.Manual, IsReferenceMatched = true, TotalScore = 0.7f, }),
                Spot(3, string.Empty, null),
                Spot(4, "L-Pyroglutamic acid", new MsScanMatchResult { Name = "L-Pyroglutamic acid", Source = SourceType.MspDB, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, TotalScore = 0.9f, }),
            };
        }

        private (AlignmentResultMassBankRecordExportModel Model, AlignmentFileBeanModel File) CreateAlignmentExport(IMessageBroker broker, Action<StudyContextModel>? change = null) {
            var alignmentFile = new AlignmentFileBean
            {
                FileID = 0,
                FileName = "alignment",
                FilePath = Path.Combine(_folder, "alignment.arf2"),
                EicFilePath = Path.Combine(_folder, "alignment.EIC.aef"),
                SpectraFilePath = Path.Combine(_folder, "alignment.dcl"),
            };
            if (!File.Exists(alignmentFile.FilePath)) {
                new AlignmentResultContainer { AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(Spots()), }.Save(alignmentFile);
                var spectra = Enumerable.Range(0, 5).Select(id => {
                    var msdec = new MSDecResult { ScanID = id, PrecursorMz = 130.0499021411, };
                    if (id != 4) {
                        msdec.Spectrum.Add(new SpectrumPeak(56.0467913204, 124.3710022));
                        msdec.Spectrum.Add(new SpectrumPeak(84.0458377361, 1638.2399902));
                        msdec.Spectrum.Add(new SpectrumPeak(130.0487118721, 378.5499878));
                    }
                    return msdec;
                }).ToList();
                MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, spectra);
            }
            var file = new AlignmentFileBeanModel(alignmentFile, new List<AnalysisFileBean>());
            _disposables.Add(file);

            var evaluator = new FlagEvaluator();
            var filter = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All)
                .CreateFilter(new PeakFilterModel(DisplayFilter.All), evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            var supplyer = new AlignmentPeakSpotSupplyer(new ReactivePropertySlim<IAlignmentModel?>(null), filter);
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var context = ValidStudyContext(parameter);
            change?.Invoke(context);
            return (new AlignmentResultMassBankRecordExportModel(supplyer, parameter, context, evaluator, broker), file);
        }

        private sealed class FlagEvaluator : IMatchResultEvaluator<MsScanMatchResult>
        {
            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) => results.First();
            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) => results.ToList();
            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) => results.Where(r => r.IsReferenceMatched).ToList();
            public bool IsReferenceMatched(MsScanMatchResult result) => result.IsReferenceMatched;
            public bool IsAnnotationSuggested(MsScanMatchResult result) => result.IsAnnotationSuggested;
        }

        // ------------------------------------------------------------ single file

        [TestMethod()]
        public void SingleFile_ByDefaultWritesOnlyReferenceMatchedPeaksAndReportsOnce() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var file = SaveSingleFile();
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), broker)
            {
                ShouldExport = true,
                ContributorID = Contributor,
            };
            var session = (IMsdialAnalysisExportSession)model;

            session.BeginExport();
            ((IMsdialAnalysisExport)model).Export(_output, file);
            var report = session.EndExport(_output);

            Assert.AreEqual(0, messages.Count, "the report comes once, at the end");
            var records = Directory.GetFiles(Path.Combine(_output, "sample"), "*.txt");
            Assert.AreEqual(1, records.Length);
            var lines = File.ReadAllLines(records[0]);
            CollectionAssert.Contains(lines, "CH$NAME: L-Pyroglutamic acid");
            CollectionAssert.Contains(lines, "CH$LINK: INCHIKEY ODHCTXKNWHHXJC-VKHMYHEASA-N");
            Assert.IsNotNull(report);
            StringAssert.Contains(report!.Content, $"1 MassBank record was written to per-sample folders under {_output}.", "the records are in a folder per sample, not in the destination folder itself");
            StringAssert.Contains(report.Content, "3 peaks were not exported");
            StringAssert.Contains(report.Content, "2  unknowns");
            StringAssert.Contains(report.Content, "1  suggested annotations");
        }

        [TestMethod()]
        public void SingleFile_IncludesSuggestionsAndUnknownsWhenAsked() {
            var file = SaveSingleFile();
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), new MessageBroker())
            {
                ShouldExport = true,
                ContributorID = Contributor,
                IncludeSuggested = true,
                IncludeUnknowns = true,
            };
            var session = (IMsdialAnalysisExportSession)model;

            session.BeginExport();
            ((IMsdialAnalysisExport)model).Export(_output, file);
            var report = session.EndExport(_output);

            var records = Directory.GetFiles(Path.Combine(_output, "sample"), "*.txt").Select(File.ReadAllLines).ToList();
            Assert.AreEqual(3, records.Count);
            var unknown = records.Single(r => r.Contains("CH$NAME: Unknown"));
            CollectionAssert.Contains(unknown, "CH$SMILES: N/A");
            CollectionAssert.Contains(unknown, "PK$NUM_PEAK: 2");
            var suggested = records.Single(r => r.Contains("COMMENT: MS-DIAL annotation level: suggested"));
            CollectionAssert.Contains(suggested, "CH$NAME: L-Pyroglutamic acid");
            CollectionAssert.Contains(suggested, "COMMENT: Original name: low score: L-Pyroglutamic acid");
            StringAssert.Contains(report!.Content, "1  without MS/MS peaks");
        }

        [TestMethod()]
        public void SingleFile_SamplesOfOneExportNeverShareAnAccession() {
            // Three samples whose peaks share their IDs (0 to 3), exported together as Export > Peak list result does.
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var files = new[] { SaveSingleFile("sample_a", 0), SaveSingleFile("sample_b", 1), SaveSingleFile("sample_c", 2), };
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), broker)
            {
                ShouldExport = true,
                ContributorID = Contributor,
                IncludeSuggested = true,
                IncludeUnknowns = true,
            };
            var collection = new AnalysisFileBeanModelCollection(files);
            _disposables.Add(collection);
            var export = new AnalysisResultExportModel(collection, _output, broker, model);
            export.Selects(files);

            export.Export();

            var records = files.SelectMany(file => Directory.GetFiles(Path.Combine(_output, file.AnalysisFileName), "*.txt")).ToList();
            Assert.AreEqual(9, records.Count, "three records per sample, each in the sample's own folder");
            var accessions = records.Select(record => File.ReadLines(record).First()).ToList();
            CollectionAssert.AllItemsAreUnique(accessions, "the Validator rejects the whole set when two records share an accession");
            for (var i = 0; i < records.Count; i++) {
                Assert.AreEqual("ACCESSION: " + Path.GetFileNameWithoutExtension(records[i]), accessions[i], "the file is named after its accession");
            }
            foreach (var file in files) {
                var folder = Directory.GetFiles(Path.Combine(_output, file.AnalysisFileName), "*.txt").Select(Path.GetFileNameWithoutExtension).ToList();
                Assert.IsTrue(folder.All(accession => accession.Contains($"F{file.AnalysisFileId:D4}P")), string.Join(", ", folder));
            }
            StringAssert.Contains(messages.Single(m => m.Caption == "MassBank export").Content, $"9 MassBank records were written to per-sample folders under {_output}.");
        }

        [TestMethod()]
        public void SingleFile_ReportsWhatWasWrittenWhenAnotherExportFails() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var files = new[] { SaveSingleFile("sample_a", 0), SaveSingleFile("sample_b", 1), SaveSingleFile("sample_c", 2), };
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), broker)
            {
                ShouldExport = true,
                ContributorID = Contributor,
            };
            var collection = new AnalysisFileBeanModelCollection(files);
            _disposables.Add(collection);
            // MassBank, then an export that fails on the second sample, after MassBank has written that sample's record.
            var export = new AnalysisResultExportModel(collection, _output, broker, model, new FailingExport("sample_b"));
            export.Selects(files);

            export.Export();

            Assert.AreEqual(1, Directory.GetFiles(Path.Combine(_output, "sample_a"), "*.txt").Length);
            Assert.AreEqual(1, Directory.GetFiles(Path.Combine(_output, "sample_b"), "*.txt").Length);
            Assert.IsFalse(Directory.Exists(Path.Combine(_output, "sample_c")), "the export stops at the failure");
            CollectionAssert.AreEqual(new[] { "Export failed", "MassBank export", }, messages.Select(m => m.Caption).ToArray());
            StringAssert.Contains(messages[1].Content, "2 MassBank records were written");
            Assert.IsNull(((IMsdialAnalysisExportSession)model).EndExport(_output), "the session was ended, so nothing of it lingers");
        }

        private sealed class FailingExport : IMsdialAnalysisExport
        {
            private readonly string _failOn;

            public FailingExport(string failOn) {
                _failOn = failOn;
            }

            public void Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
                if (fileBeanModel.AnalysisFileName == _failOn) {
                    throw new IOException($"{fileBeanModel.AnalysisFileName} could not be written.");
                }
            }
        }

        [TestMethod()]
        public void SingleFile_NamesALipidAtTheLevelItsMatchSupports() {
            // CH$NAME takes the chain verdict of the peak's match result, as the other exports' names do.
            var file = SaveLipidFile();
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), new MessageBroker())
            {
                ShouldExport = true,
                ContributorID = Contributor,
                IncludeSuggested = true,
            };
            var session = (IMsdialAnalysisExportSession)model;

            session.BeginExport();
            ((IMsdialAnalysisExport)model).Export(_output, file);
            session.EndExport(_output);

            string Field(string[] lines, string tag) => lines.Single(l => l.StartsWith(tag + ": ")).Substring(tag.Length + 2);
            var records = Directory.GetFiles(Path.Combine(_output, "lipids"), "*.txt").Select(File.ReadAllLines).ToDictionary(lines => Field(lines, "CH$COMPOUND_CLASS"));
            Assert.AreEqual(3, records.Count);
            Assert.AreEqual("PC 16:0_18:1", Field(records["chains resolved"], "CH$NAME"));
            Assert.AreEqual("PC 34:1", Field(records["class only"], "CH$NAME"));
            Assert.AreEqual("PC 34:1", Field(records["suggested"], "CH$NAME"));
            Assert.AreEqual("PC 34:1; LC-ESI-QTOF; MS2", Field(records["suggested"], "RECORD_TITLE"));
            CollectionAssert.Contains(records["suggested"], "COMMENT: Original name: low score: PC 34:1|PC 16:0_18:1");
            CollectionAssert.Contains(records["class only"], "COMMENT: Original name: PC 34:1|PC 16:0_18:1");
        }

        // A sample with three lipid peaks, all named "PC 34:1|PC 16:0_18:1": a reference match whose chains were resolved,
        // one whose chains were not, and a low-score suggestion. CH$COMPOUND_CLASS tells the two matches apart.
        private AnalysisFileBeanModel SaveLipidFile() {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileId = 5;
            file.AnalysisFileName = "lipids";
            file.AnalysisFilePath = Path.Combine(_folder, "lipids.mzML");
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, "lipids.pai");
            file.File.DeconvolutionFilePath = Path.Combine(_folder, "lipids.dcl");
            var features = new[] {
                (0, "PC 34:1|PC 16:0_18:1", "chains resolved", new MsScanMatchResult { Name = "PC 34:1|PC 16:0_18:1", Source = SourceType.MspDB, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, IsLipidClassMatch = true, IsLipidChainsMatch = true, TotalScore = 0.9f, }),
                (1, "PC 34:1|PC 16:0_18:1", "class only", new MsScanMatchResult { Name = "PC 34:1|PC 16:0_18:1", Source = SourceType.MspDB, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, IsLipidClassMatch = true, IsLipidChainsMatch = false, TotalScore = 0.8f, }),
                (2, "low score: PC 34:1|PC 16:0_18:1", "suggested", new MsScanMatchResult { Name = "PC 34:1|PC 16:0_18:1", Source = SourceType.MspDB, IsAnnotationSuggested = true, IsPrecursorMzMatch = true, TotalScore = 0.4f, }),
            }.Select(entry => {
                var (id, name, ontology, result) = entry;
                var feature = new ChromatogramPeakFeature
                {
                    PeakID = id,
                    MasterPeakID = id,
                    MS1RawSpectrumIdTop = 12,
                    MS2RawSpectrumID = 13,
                    PrecursorMz = 760.5851,
                    ChromXs = new ChromXs(12.345, ChromXType.RT, ChromXUnit.Min),
                    IonMode = IonMode.Positive,
                    Name = name,
                    Ontology = ontology,
                    Formula = FormulaStringParcer.OrganicElementsReader("C42H82NO8P"),
                };
                feature.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
                feature.MatchResults.AddResult(result);
                return feature;
            }).ToList();
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakListFilePath, features);
            MsdecResultsWriter.Write(file.File.DeconvolutionFilePath, Enumerable.Range(0, 3).Select(id => {
                var msdec = new MSDecResult { ScanID = id, PrecursorMz = 760.5851, RawSpectrumID = 13, };
                msdec.Spectrum.Add(new SpectrumPeak(184.0733214, 10000.2));
                msdec.Spectrum.Add(new SpectrumPeak(760.5850813, 1200.9));
                return msdec;
            }).ToList());
            return file;
        }

        [TestMethod()]
        public void SingleFile_IsRefusedWithoutAContributorId() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var file = SaveSingleFile();
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), broker) { ShouldExport = true, };
            var session = (IMsdialAnalysisExportSession)model;

            session.BeginExport();
            ((IMsdialAnalysisExport)model).Export(_output, file);
            var report = session.EndExport(_output);

            Assert.IsNull(report);
            Assert.IsFalse(Directory.Exists(Path.Combine(_output, "sample")));
            StringAssert.Contains(messages.Single().Content, "Contributor ID is empty");
        }

        [TestMethod()]
        public void SingleFileViewModel_BlocksExportUntilTheContributorIdIsValid() {
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), new MessageBroker());
            using (var vm = new MsdialAnalysisMassBankRecordExportViewModel(model)) {
                Assert.IsTrue(vm.CanExport.Value);
                Assert.IsFalse(vm.ContributorID.HasErrors, "an option that is not chosen shows no error");
                vm.ShouldExport.Value = true;
                Assert.IsFalse(vm.CanExport.Value);
                Assert.IsTrue(vm.ContributorID.HasErrors);
                vm.ContributorID.Value = Contributor;
                Assert.IsTrue(vm.CanExport.Value);
                vm.IncludeUnknowns.Value = true;
                Assert.IsTrue(model.IncludeUnknowns);
            }

            // Default project metadata is empty: the problems show only once MassBank is chosen.
            var bare = new MsdialAnalysisMassBankRecordExportModel(parameter, new StudyContextModel(parameter), new FlagEvaluator(), new MessageBroker());
            using (var vm = new MsdialAnalysisMassBankRecordExportViewModel(bare)) {
                Assert.IsTrue(vm.HasProjectMetadataProblems);
                Assert.IsFalse(vm.ShowProjectMetadataProblems.Value);
                vm.ShouldExport.Value = true;
                Assert.IsTrue(vm.ShowProjectMetadataProblems.Value);
            }
        }

        // One sample whose peak list has an unannotated peak with two MS/MS peaks, an unannotated one with none, a
        // reference-matched peak and a low-score suggestion, both with MS/MS peaks. Its peak IDs are 0 to 3, as those of
        // every sample saved here: MS-DIAL numbers the peaks of each file from 0.
        private AnalysisFileBeanModel SaveSingleFile(string sampleName = "sample", int fileId = 0) {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileId = fileId;
            file.AnalysisFileName = sampleName;
            file.AnalysisFilePath = Path.Combine(_folder, sampleName + ".mzML");
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, sampleName + ".pai");
            file.File.DeconvolutionFilePath = Path.Combine(_folder, sampleName + ".dcl");
            var features = Enumerable.Range(0, 2).Select(id => {
                var feature = new ChromatogramPeakFeature
                {
                    PeakID = id,
                    MasterPeakID = id,
                    MS1RawSpectrumIdTop = 12,
                    MS2RawSpectrumID = 13,
                    PrecursorMz = 300.1,
                    ChromXs = new ChromXs(5.0, ChromXType.RT, ChromXUnit.Min),
                    IonMode = IonMode.Positive,
                };
                feature.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
                return feature;
            }).ToList();
            foreach (var (id, name, result) in new[] {
                (2, "L-Pyroglutamic acid", new MsScanMatchResult { Name = "L-Pyroglutamic acid", Source = SourceType.MspDB, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, TotalScore = 0.9f, }),
                (3, "low score: L-Pyroglutamic acid", new MsScanMatchResult { Name = "L-Pyroglutamic acid", Source = SourceType.MspDB, IsAnnotationSuggested = true, IsPrecursorMzMatch = true, TotalScore = 0.5f, }),
            }) {
                var feature = new ChromatogramPeakFeature
                {
                    PeakID = id,
                    MasterPeakID = id,
                    MS1RawSpectrumIdTop = 12,
                    MS2RawSpectrumID = 13,
                    PrecursorMz = 130.0499021411,
                    ChromXs = new ChromXs(0.6358123779, ChromXType.RT, ChromXUnit.Min),
                    IonMode = IonMode.Positive,
                    Name = name,
                    Formula = FormulaStringParcer.OrganicElementsReader("C5H7NO3"),
                    SMILES = "C1CC(=O)N[C@@H]1C(=O)O",
                    InChIKey = "ODHCTXKNWHHXJC-VKHMYHEASA-N",
                };
                feature.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
                feature.MatchResults.AddResult(result);
                features.Add(feature);
            }
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakListFilePath, features);
            MSDecResult WithPeaks(int id) {
                var msdec = new MSDecResult { ScanID = id, PrecursorMz = 300.1, RawSpectrumID = 13, };
                msdec.Spectrum.Add(new SpectrumPeak(100.05, 1000d));
                msdec.Spectrum.Add(new SpectrumPeak(150.07, 500d));
                return msdec;
            }
            var withoutPeaks = new MSDecResult { ScanID = 1, PrecursorMz = 300.1, RawSpectrumID = 13, };
            MsdecResultsWriter.Write(file.File.DeconvolutionFilePath, new List<MSDecResult> { WithPeaks(0), withoutPeaks, WithPeaks(2), WithPeaks(3), });
            return file;
        }
    }
}
