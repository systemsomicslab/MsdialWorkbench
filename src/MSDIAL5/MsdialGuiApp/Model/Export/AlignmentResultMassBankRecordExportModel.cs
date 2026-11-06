using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// Export &gt; Alignment result export &gt; MassBank: one MassBank record per alignment spot.
    /// </summary>
    /// <remarks>
    /// Spots are chosen by <see cref="MassBankExportPolicy"/>: by default only reference-matched spots with at least one
    /// MS/MS peak; suggested annotations and unknowns are opt-in. CountExportFiles and Export select spots with the same
    /// rule. The records are built by <see cref="MassBankRecordHandler"/> from the project metadata, and the export
    /// reports what it wrote and what it skipped, with the reasons, once the whole alignment export has finished. The
    /// report also counts the MassBank records already in the export folder that this export did not write: every export
    /// takes accessions of its own, so a re-export adds a second set beside the first instead of replacing it.
    /// </remarks>
    internal sealed class AlignmentResultMassBankRecordExportModel : BindableBase, IAlignmentResultExportModel, IAlignmentResultExportSession
    {
        internal const string SkippedSuggested = MassBankExportPolicy.SkippedSuggested;
        internal const string SkippedUnknown = MassBankExportPolicy.SkippedUnknown;
        internal const string SkippedNoSpectrum = MassBankExportPolicy.SkippedNoSpectrum;
        internal const string SkippedNoPeaks = MassBankExportPolicy.SkippedNoPeaks;

        private readonly AlignmentPeakSpotSupplyer _supplyer;
        private readonly ProjectBaseParameter _parameter;
        private readonly StudyContextModel _studyContextModel;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMessageBroker _broker;

        // Within an export session the report is kept until EndExport; outside one it is published at once.
        private bool _inSession;
        private MessageRequest? _pendingReport;

        public AlignmentResultMassBankRecordExportModel(AlignmentPeakSpotSupplyer supplyer, ProjectBaseParameter parameter, StudyContextModel studyContextModel, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMessageBroker? broker) {
            _supplyer = supplyer ?? throw new ArgumentNullException(nameof(supplyer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _studyContextModel = studyContextModel ?? throw new ArgumentNullException(nameof(studyContextModel));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _broker = broker ?? MessageBroker.Default;
        }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        /// <summary>The contributor ID MassBank assigned, the second block of every accession.</summary>
        public string ContributorID {
            get => _contributorID;
            set {
                if (SetProperty(ref _contributorID, value ?? string.Empty)) {
                    OnPropertyChanged(nameof(IsContributorIdValid));
                }
            }
        }
        private string _contributorID = string.Empty;

        /// <summary>True when <see cref="ContributorID"/> is a MassBank contributor ID; the export is refused otherwise.</summary>
        public bool IsContributorIdValid => MassBankVocabulary.IsValidContributorId(ContributorID);

        /// <summary>Also export suggestions (low score, no MS2, Unsettled, Putative), marked as such in COMMENT.</summary>
        public bool IncludeSuggested {
            get => _includeSuggested;
            set => SetProperty(ref _includeSuggested, value);
        }
        private bool _includeSuggested = false;

        /// <summary>Also export unannotated spots, as "Unknown" with every structure field N/A.</summary>
        public bool IncludeUnknowns {
            get => _includeUnknowns;
            set => SetProperty(ref _includeUnknowns, value);
        }
        private bool _includeUnknowns = false;

        /// <summary>
        /// What in the project metadata keeps the records from passing the MassBank Validator; empty when nothing does.
        /// </summary>
        public IReadOnlyList<string> ValidateProjectMetadata() {
            return CreateHandler().ValidateProjectMetadata();
        }

        void IAlignmentResultExportSession.BeginExport() {
            _inSession = true;
            _pendingReport = null;
        }

        MessageRequest? IAlignmentResultExportSession.EndExport() {
            var report = _pendingReport;
            _inSession = false;
            _pendingReport = null;
            return report;
        }

        int IAlignmentResultExportModel.CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            // Export writes nothing while the metadata is invalid, so nothing is counted either.
            if (!IsSelected || CreateHandler().ValidateMetadata().Count > 0) {
                return 0;
            }
            return SelectSpots(alignmentFile).Count(selection => selection.Scan is not null);
        }

        void IAlignmentResultExportModel.Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            if (!IsSelected) {
                return;
            }
            // A new handler per export, so that DATE and the time in every accession are the time of this export: two
            // alignment results, or the positive and the negative project of a study, exported on one day do not share
            // accessions.
            var handler = CreateHandler();
            var problems = handler.ValidateMetadata();
            if (problems.Count > 0) {
                Report(MassBankExportSummary.MetadataProblems(problems));
                return;
            }

            var summary = new MassBankExportSummary();
            var encoding = new UTF8Encoding(false);
            foreach (var selection in SelectSpots(alignmentFile)) {
                if (selection.Scan is null) {
                    summary.AddSkipped(selection.SkipReason ?? string.Empty);
                    continue;
                }
                // Built in memory first: a spot that cannot become a valid record leaves no file behind.
                // CH$NAME takes the spot's chain verdict, as the name in every other export of the alignment does.
                var result = handler.BuildRecord(selection.Spot, selection.Spot, selection.Scan, selection.Spot, selection.Level, lipidChainsResolved: selection.Spot.AreLipidChainsResolved());
                if (result.IsSuccess) {
                    var path = Path.Combine(exportDirectory, result.FileName);
                    File.WriteAllText(path, result.Text, encoding);
                    summary.AddWritten(result, path);
                    notification.Invoke($"Exporting {result.Accession}");
                }
                else {
                    summary.AddSkipped($"refused: {result.SkipReason}");
                    notification.Invoke($"Skipped {result.Accession}: {result.SkipReason}");
                }
            }
            // The records of an earlier export into this folder stay beside the new ones under accessions of their own.
            summary.CountOtherRecords(exportDirectory, handler.Identifier);
            Report(summary.ToMessage(exportDirectory));
        }

        private void Report(MessageRequest message) {
            if (_inSession) {
                _pendingReport = message;
            }
            else {
                _broker.Publish(message);
            }
        }

        private MassBankRecordHandler CreateHandler() {
            return new MassBankRecordHandler(_parameter.IonMode, _studyContextModel.InstrumentType ?? string.Empty, MassBankSplash.Compute)
            {
                Authors = _studyContextModel.Authors ?? string.Empty,
                License = _studyContextModel.License ?? string.Empty,
                Instrument = _studyContextModel.Instrument ?? string.Empty,
                CollisionEnergy = _studyContextModel.CollisionEnergy ?? string.Empty,
                Comment = _studyContextModel.Comment ?? string.Empty,
                ContributorIdentifier = ContributorID,
                MsdialVersion = MsdialBuildIdentity.FullIdentity,
            };
        }

        private sealed class SpotSelection
        {
            public SpotSelection(AlignmentSpotProperty spot, MassBankAnnotationLevel level, MSDecResult? scan, string? skipReason) {
                Spot = spot;
                Level = level;
                Scan = scan;
                SkipReason = skipReason;
            }

            public AlignmentSpotProperty Spot { get; }
            public MassBankAnnotationLevel Level { get; }
            /// <summary>The spectrum of a spot to export; null for a spot that is skipped.</summary>
            public MSDecResult? Scan { get; }
            public string? SkipReason { get; }
        }

        /// <summary>
        /// Every supplied spot, with its spectrum when it is to be exported or the reason it is not. Lazy, so that the
        /// spectra are not all held at once.
        /// </summary>
        private IEnumerable<SpotSelection> SelectSpots(AlignmentFileBeanModel alignmentFile) {
            foreach (var spot in _supplyer.Supply(alignmentFile, default)) {
                var level = Classify(spot, _evaluator);
                var exclusion = MassBankExportPolicy.Exclusion(level, IncludeSuggested, IncludeUnknowns);
                if (exclusion is not null) {
                    yield return new SpotSelection(spot, level, null, exclusion);
                    continue;
                }
                var scan = alignmentFile.LoadMSDecResultByIndexAsync(spot.MasterAlignmentID).Result;
                if (scan is null) {
                    yield return new SpotSelection(spot, level, null, SkippedNoSpectrum);
                    continue;
                }
                if (MassBankRecordHandler.CountExportablePeaks(scan) == 0) {
                    yield return new SpotSelection(spot, level, null, SkippedNoPeaks);
                    continue;
                }
                yield return new SpotSelection(spot, level, scan, null);
            }
        }

        /// <summary>How far the annotation of <paramref name="spot"/> goes; see <see cref="MassBankExportPolicy"/>.</summary>
        internal static MassBankAnnotationLevel Classify(AlignmentSpotProperty spot, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return MassBankExportPolicy.Classify(spot, evaluator);
        }
    }
}
