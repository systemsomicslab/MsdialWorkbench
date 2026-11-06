using CompMs.App.Msdial.Model.DataObj;
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
    /// By default only reference-matched spots (an accepted MSP or LBM match, not a suggestion) with at least one MS/MS
    /// peak are exported; suggested annotations and unknowns are opt-in. CountExportFiles and Export select spots with
    /// the same rule. The records are built by <see cref="MassBankRecordHandler"/> from the project metadata, and the
    /// export reports what it wrote and what it skipped, with the reasons.
    /// </remarks>
    internal sealed class AlignmentResultMassBankRecordExportModel : BindableBase, IAlignmentResultExportModel
    {
        internal const string SkippedSuggested = "suggested annotations (exported only with \"Include suggested annotations\")";
        internal const string SkippedUnknown = "unknowns (exported only with \"Include unknowns\")";
        internal const string SkippedNoSpectrum = "without a deconvoluted MS/MS spectrum";
        internal const string SkippedNoPeaks = "without MS/MS peaks";

        private readonly AlignmentPeakSpotSupplyer _supplyer;
        private readonly ProjectBaseParameter _parameter;
        private readonly StudyContextModel _studyContextModel;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMessageBroker _broker;

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

        int IAlignmentResultExportModel.CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            if (!IsSelected) {
                return 0;
            }
            return SelectSpots(alignmentFile).Count(selection => selection.Scan is not null);
        }

        void IAlignmentResultExportModel.Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            if (!IsSelected) {
                return;
            }
            // A new handler per export, so that DATE and the accession date are the time of this export.
            var handler = CreateHandler();
            var problems = handler.ValidateMetadata();
            if (problems.Count > 0) {
                _broker.Publish(MassBankExportSummary.MetadataProblems(problems));
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
                var result = handler.BuildRecord(selection.Spot, selection.Spot, selection.Scan, selection.Spot, selection.Level);
                if (result.IsSuccess) {
                    File.WriteAllText(Path.Combine(exportDirectory, result.FileName), result.Text, encoding);
                    summary.AddWritten(result);
                    notification.Invoke($"Exporting {result.Accession}");
                }
                else {
                    summary.AddSkipped($"refused: {result.SkipReason}");
                    notification.Invoke($"Skipped {result.Accession}: {result.SkipReason}");
                }
            }
            _broker.Publish(summary.ToMessage(exportDirectory));
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
                if (level == MassBankAnnotationLevel.Suggested && !IncludeSuggested) {
                    yield return new SpotSelection(spot, level, null, SkippedSuggested);
                    continue;
                }
                if (level == MassBankAnnotationLevel.Unknown && !IncludeUnknowns) {
                    yield return new SpotSelection(spot, level, null, SkippedUnknown);
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

        /// <summary>
        /// How far the annotation of <paramref name="spot"/> goes, from its match results.
        /// </summary>
        /// <remarks>
        /// The match flags decide first. A spot set to Unsettled by hand, or renamed "Putative:" because another spot
        /// kept the same reference, still carries the reference-matched flag, so the name tells those from an accepted
        /// match. A named spot that is neither matched nor suggested is not taken as an identification.
        /// </remarks>
        internal static MassBankAnnotationLevel Classify(AlignmentSpotProperty spot, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (spot.IsReferenceMatched(evaluator)) {
                return MassBankRecordHandler.IsSuggestionName(spot.Name) ? MassBankAnnotationLevel.Suggested : MassBankAnnotationLevel.ReferenceMatched;
            }
            if (spot.IsAnnotationSuggested(evaluator)) {
                return MassBankAnnotationLevel.Suggested;
            }
            if (spot.IsUnknown || string.IsNullOrWhiteSpace(spot.Name)) {
                return MassBankAnnotationLevel.Unknown;
            }
            return MassBankAnnotationLevel.Suggested;
        }
    }
}
