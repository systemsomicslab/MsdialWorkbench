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
            StringAssert.Contains(summary.Content, "1 MassBank record was written");
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
            var unsettled = records.Single(r => r.Contains("CH$NAME: Unsettled: L-Pyroglutamic acid"));
            CollectionAssert.Contains(unsettled, "COMMENT: MS-DIAL annotation level: suggested");
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
        public void AlignmentViewModel_BlocksExportUntilTheContributorIdAndMetadataAreValid() {
            var (model, _) = CreateAlignmentExport(new MessageBroker());
            using (var vm = new AlignmentResultMassBankRecordExportViewModel(model)) {
                Assert.IsTrue(vm.CanExport.Value, "not selected, nothing to block");
                Assert.IsFalse(vm.HasProjectMetadataProblems);

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
                vm.IsSelected.Value = true;
                vm.ContributorID.Value = Contributor;
                Assert.IsTrue(vm.HasProjectMetadataProblems);
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
        public void SingleFile_WritesEveryPeakWithMsMsAndReportsOnce() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var file = SaveSingleFile();
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), broker)
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
            CollectionAssert.Contains(lines, "CH$NAME: Unknown");
            CollectionAssert.Contains(lines, "CH$SMILES: N/A");
            CollectionAssert.Contains(lines, "PK$NUM_PEAK: 2");
            Assert.IsNotNull(report);
            StringAssert.Contains(report!.Content, "1 MassBank record was written");
            StringAssert.Contains(report.Content, "1  without MS/MS peaks");
        }

        [TestMethod()]
        public void SingleFile_IsRefusedWithoutAContributorId() {
            var broker = new MessageBroker();
            var messages = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(messages.Add);
            var file = SaveSingleFile();
            var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), broker) { ShouldExport = true, };
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
            var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new MessageBroker());
            using (var vm = new MsdialAnalysisMassBankRecordExportViewModel(model)) {
                Assert.IsTrue(vm.CanExport.Value);
                vm.ShouldExport.Value = true;
                Assert.IsFalse(vm.CanExport.Value);
                vm.ContributorID.Value = Contributor;
                Assert.IsTrue(vm.CanExport.Value);
            }
        }

        // One sample whose peak list has an unannotated peak with two MS/MS peaks and one with none.
        private AnalysisFileBeanModel SaveSingleFile() {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileName = "sample";
            file.AnalysisFilePath = Path.Combine(_folder, "sample.mzML");
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, "sample.pai");
            file.File.DeconvolutionFilePath = Path.Combine(_folder, "sample.dcl");
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
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakListFilePath, features);
            var withPeaks = new MSDecResult { ScanID = 0, PrecursorMz = 300.1, RawSpectrumID = 13, };
            withPeaks.Spectrum.Add(new SpectrumPeak(100.05, 1000d));
            withPeaks.Spectrum.Add(new SpectrumPeak(150.07, 500d));
            var withoutPeaks = new MSDecResult { ScanID = 1, PrecursorMz = 300.1, RawSpectrumID = 13, };
            MsdecResultsWriter.Write(file.File.DeconvolutionFilePath, new List<MSDecResult> { withPeaks, withoutPeaks, });
            return file;
        }
    }
}
