using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// Export &gt; Peak list result &gt; MassBank record: one MassBank record per peak of each selected sample.
    /// </summary>
    /// <remarks>
    /// Every peak with at least one MS/MS peak is exported; an unannotated peak as "Unknown" with every structure
    /// field N/A, and a suggestion with "COMMENT: MS-DIAL annotation level: suggested". The records are built by
    /// <see cref="MassBankRecordHandler"/> from the project metadata, with one handler per export so that DATE is the
    /// time of the export.
    /// </remarks>
    internal sealed class MsdialAnalysisMassBankRecordExportModel : BindableBase, IMsdialAnalysisExport, IMsdialAnalysisExportSession
    {
        private const string SkippedNoSpectrum = "without a deconvoluted MS/MS spectrum";

        private readonly ProjectBaseParameter _parameter;
        private readonly StudyContextModel _studyContextModel;
        private readonly IMessageBroker _broker;

        private MassBankRecordHandler? _sessionHandler;
        private MassBankExportSummary? _sessionSummary;
        private bool _sessionRefused;

        public MsdialAnalysisMassBankRecordExportModel(ProjectBaseParameter parameter, StudyContextModel studyContextModel, IMessageBroker? broker) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _studyContextModel = studyContextModel ?? throw new ArgumentNullException(nameof(studyContextModel));
            _broker = broker ?? MessageBroker.Default;
        }

        public string Label { get; } = "MassBank record";

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

        public bool ShouldExport {
            get => _shouldExport;
            set => SetProperty(ref _shouldExport, value);
        }
        private bool _shouldExport = false;

        /// <summary>
        /// What in the project metadata keeps the records from passing the MassBank Validator; empty when nothing does.
        /// </summary>
        public IReadOnlyList<string> ValidateProjectMetadata() {
            return CreateHandler().ValidateProjectMetadata();
        }

        void IMsdialAnalysisExportSession.BeginExport() {
            _sessionHandler = null;
            _sessionSummary = null;
            _sessionRefused = false;
            if (!ShouldExport) {
                return;
            }
            _sessionHandler = CreateHandler();
            _sessionSummary = new MassBankExportSummary();
            var problems = _sessionHandler.ValidateMetadata();
            if (problems.Count > 0) {
                _sessionRefused = true;
                _broker.Publish(MassBankExportSummary.MetadataProblems(problems));
            }
        }

        MessageRequest? IMsdialAnalysisExportSession.EndExport(string destinationFolder) {
            var summary = _sessionRefused ? null : _sessionSummary;
            _sessionHandler = null;
            _sessionSummary = null;
            _sessionRefused = false;
            return summary?.ToMessage(destinationFolder);
        }

        void IMsdialAnalysisExport.Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            if (!ShouldExport || _sessionRefused) {
                return;
            }
            var handler = _sessionHandler;
            var summary = _sessionSummary;
            if (handler is null || summary is null) {
                // Called outside an export session: check and report for this file alone.
                handler = CreateHandler();
                var problems = handler.ValidateMetadata();
                if (problems.Count > 0) {
                    _broker.Publish(MassBankExportSummary.MetadataProblems(problems));
                    return;
                }
                summary = new MassBankExportSummary();
            }

            var features = ChromatogramPeakFeatureCollection.LoadAsync(fileBeanModel.PeakAreaBeanInformationFilePath).Result;
            features = features.Flatten();
            var loader = fileBeanModel.MSDecLoader;
            var folder = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fileBeanModel.AnalysisFileName));
            var encoding = new UTF8Encoding(false);

            foreach (var feature in features.Items) {
                var msdec = loader.LoadMSDecResult(feature.GetMSDecResultID());
                if (msdec is null) {
                    summary.AddSkipped(SkippedNoSpectrum);
                    continue;
                }
                // Built in memory first: a peak that cannot become a valid record leaves no file behind.
                var result = handler.BuildRecord(feature, feature, msdec, feature, Classify(feature));
                if (!result.IsSuccess) {
                    summary.AddSkipped(result.SkipReason == "no MS/MS peaks" ? "without MS/MS peaks" : $"refused: {result.SkipReason}");
                    continue;
                }
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, result.FileName), result.Text, encoding);
                summary.AddWritten(result);
            }

            if (_sessionSummary is null) {
                _broker.Publish(summary.ToMessage(folder));
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

        // The peak list has no match evaluator here, so the level is read from what annotation left on the peak.
        private static MassBankAnnotationLevel Classify(ChromatogramPeakFeature feature) {
            if (feature.IsUnknown || string.IsNullOrWhiteSpace(feature.Name)) {
                return MassBankAnnotationLevel.Unknown;
            }
            return MassBankRecordHandler.IsSuggestionName(feature.Name) ? MassBankAnnotationLevel.Suggested : MassBankAnnotationLevel.Unspecified;
        }
    }
}
