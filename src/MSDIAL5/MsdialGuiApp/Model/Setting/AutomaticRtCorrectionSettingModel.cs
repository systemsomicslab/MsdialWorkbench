using CompMs.App.Msdial.Model.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    /// <summary>
    /// The settings of the automatic alignment RT correction (LC-MS only), as the alignment settings
    /// page shows them: every value <see cref="AutomaticAlignmentRetentionTimeCorrectionParameter"/>
    /// holds, read from the project and written back only when the person runs alignment.
    /// </summary>
    /// <remarks>
    /// The values are checked by the library code the Console checks them with
    /// (<see cref="AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(AutomaticAlignmentRetentionTimeCorrectionParameter, double, bool)"/>):
    /// the numeric ranges, the strict seven-column anchor library in UserOnly and Hybrid mode, and
    /// the exclusion of the user-defined RT correction.
    /// </remarks>
    public sealed class AutomaticRtCorrectionSettingModel : BindableBase
    {
        private readonly ParameterBase _parameter;
        private readonly IMessageBroker _broker;

        public AutomaticRtCorrectionSettingModel(ParameterBase parameter, IReadOnlyList<AnalysisFileBean> files, bool isReadOnly, IMessageBroker? broker = null) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _broker = broker ?? MessageBroker.Default;
            IsAvailable = parameter is MsdialLcmsParameter;
            IsReadOnly = isReadOnly;
            var references = new List<AutomaticRtCorrectionReferenceChoice> { AutomaticRtCorrectionReferenceChoice.Automatic };
            references.AddRange((files ?? Array.Empty<AnalysisFileBean>()).Select(file => new AutomaticRtCorrectionReferenceChoice(file.AnalysisFileId, file.AnalysisFileName)));
            ReferenceChoices = references.AsReadOnly();
            _reference = ReferenceChoices[0];
            LoadParameter(parameter);
        }

        /// <summary>True for an LC-MS project, the only kind whose alignment applies the correction.</summary>
        public bool IsAvailable { get; }

        public bool IsReadOnly { get; }

        public ReadOnlyCollection<AutomaticRtCorrectionReferenceChoice> ReferenceChoices { get; }

        public bool Execute {
            get => _execute;
            set => SetProperty(ref _execute, value);
        }
        private bool _execute;

        public AutomaticRtCorrectionAnchorMode AnchorMode {
            get => _anchorMode;
            set => SetProperty(ref _anchorMode, value);
        }
        private AutomaticRtCorrectionAnchorMode _anchorMode;

        public string AnchorLibraryFilePath {
            get => _anchorLibraryFilePath;
            set => SetProperty(ref _anchorLibraryFilePath, value ?? string.Empty);
        }
        private string _anchorLibraryFilePath = string.Empty;

        public RetentionTimeCorrectionPeakSelectionMode AnchorPeakSelectionMode {
            get => _anchorPeakSelectionMode;
            set => SetProperty(ref _anchorPeakSelectionMode, value);
        }
        private RetentionTimeCorrectionPeakSelectionMode _anchorPeakSelectionMode;

        public float AnchorPeakSelectionRtWeight {
            get => _anchorPeakSelectionRtWeight;
            set => SetProperty(ref _anchorPeakSelectionRtWeight, value);
        }
        private float _anchorPeakSelectionRtWeight;

        public AutomaticRtCorrectionReferenceChoice Reference {
            get => _reference;
            set => SetProperty(ref _reference, value ?? AutomaticRtCorrectionReferenceChoice.Automatic);
        }
        private AutomaticRtCorrectionReferenceChoice _reference;

        public float LocalSupportRtWindow {
            get => _localSupportRtWindow;
            set => SetProperty(ref _localSupportRtWindow, value);
        }
        private float _localSupportRtWindow;

        public int MinimumAnchorCount {
            get => _minimumAnchorCount;
            set => SetProperty(ref _minimumAnchorCount, value);
        }
        private int _minimumAnchorCount;

        public int MaximumAnchorCount {
            get => _maximumAnchorCount;
            set => SetProperty(ref _maximumAnchorCount, value);
        }
        private int _maximumAnchorCount;

        public float RtBinWidth {
            get => _rtBinWidth;
            set => SetProperty(ref _rtBinWidth, value);
        }
        private float _rtBinWidth;

        public float MatchRtTolerance {
            get => _matchRtTolerance;
            set => SetProperty(ref _matchRtTolerance, value);
        }
        private float _matchRtTolerance;

        public float MinimumSampleCoverage {
            get => _minimumSampleCoverage;
            set => SetProperty(ref _minimumSampleCoverage, value);
        }
        private float _minimumSampleCoverage;

        public float IntensityQuantile {
            get => _intensityQuantile;
            set => SetProperty(ref _intensityQuantile, value);
        }
        private float _intensityQuantile;

        public float MaximumPeakWidthQuantile {
            get => _maximumPeakWidthQuantile;
            set => SetProperty(ref _maximumPeakWidthQuantile, value);
        }
        private float _maximumPeakWidthQuantile;

        public float MinimumSignalToNoise {
            get => _minimumSignalToNoise;
            set => SetProperty(ref _minimumSignalToNoise, value);
        }
        private float _minimumSignalToNoise;

        public float MinimumGaussianSimilarity {
            get => _minimumGaussianSimilarity;
            set => SetProperty(ref _minimumGaussianSimilarity, value);
        }
        private float _minimumGaussianSimilarity;

        public float MinimumIdealSlope {
            get => _minimumIdealSlope;
            set => SetProperty(ref _minimumIdealSlope, value);
        }
        private float _minimumIdealSlope;

        public float OutlierMadThreshold {
            get => _outlierMadThreshold;
            set => SetProperty(ref _outlierMadThreshold, value);
        }
        private float _outlierMadThreshold;

        public float ReferenceCentralityWeight {
            get => _referenceCentralityWeight;
            set => SetProperty(ref _referenceCentralityWeight, value);
        }
        private float _referenceCentralityWeight;

        public bool InterpolateBlankByAnalyticalOrder {
            get => _interpolateBlankByAnalyticalOrder;
            set => SetProperty(ref _interpolateBlankByAnalyticalOrder, value);
        }
        private bool _interpolateBlankByAnalyticalOrder;

        /// <summary>Reads every value from <paramref name="parameter"/>; nothing is written.</summary>
        public void LoadParameter(ParameterBase parameter) {
            var source = parameter.AlignmentBaseParam.AutomaticRtCorrection ?? new AutomaticAlignmentRetentionTimeCorrectionParameter();
            Execute = source.Execute;
            AnchorMode = source.AnchorMode;
            AnchorLibraryFilePath = source.AnchorLibraryFilePath;
            AnchorPeakSelectionMode = source.AnchorPeakSelectionMode;
            AnchorPeakSelectionRtWeight = source.AnchorPeakSelectionRtWeight;
            Reference = ReferenceChoices.FirstOrDefault(choice => choice.FileId == source.ReferenceFileId && source.ReferenceFileId >= 0)
                ?? AutomaticRtCorrectionReferenceChoice.Automatic;
            LocalSupportRtWindow = source.LocalSupportRtWindow;
            MinimumAnchorCount = source.MinimumAnchorCount;
            MaximumAnchorCount = source.MaximumAnchorCount;
            RtBinWidth = source.RtBinWidth;
            MatchRtTolerance = source.MatchRtTolerance;
            MinimumSampleCoverage = source.MinimumSampleCoverage;
            IntensityQuantile = source.IntensityQuantile;
            MaximumPeakWidthQuantile = source.MaximumPeakWidthQuantile;
            MinimumSignalToNoise = source.MinimumSignalToNoise;
            MinimumGaussianSimilarity = source.MinimumGaussianSimilarity;
            MinimumIdealSlope = source.MinimumIdealSlope;
            OutlierMadThreshold = source.OutlierMadThreshold;
            ReferenceCentralityWeight = source.ReferenceCentralityWeight;
            InterpolateBlankByAnalyticalOrder = source.InterpolateBlankByAnalyticalOrder;
        }

        /// <summary>The values as the parameter the correction reads.</summary>
        public AutomaticAlignmentRetentionTimeCorrectionParameter ToParameter() {
            return new AutomaticAlignmentRetentionTimeCorrectionParameter
            {
                Execute = Execute,
                AnchorMode = AnchorMode,
                AnchorLibraryFilePath = AnchorLibraryFilePath?.Trim() ?? string.Empty,
                AnchorPeakSelectionMode = AnchorPeakSelectionMode,
                AnchorPeakSelectionRtWeight = AnchorPeakSelectionRtWeight,
                ReferenceFileId = Reference.FileId,
                LocalSupportRtWindow = LocalSupportRtWindow,
                MinimumAnchorCount = MinimumAnchorCount,
                MaximumAnchorCount = MaximumAnchorCount,
                RtBinWidth = RtBinWidth,
                MatchRtTolerance = MatchRtTolerance,
                MinimumSampleCoverage = MinimumSampleCoverage,
                IntensityQuantile = IntensityQuantile,
                MaximumPeakWidthQuantile = MaximumPeakWidthQuantile,
                MinimumSignalToNoise = MinimumSignalToNoise,
                MinimumGaussianSimilarity = MinimumGaussianSimilarity,
                MinimumIdealSlope = MinimumIdealSlope,
                OutlierMadThreshold = OutlierMadThreshold,
                ReferenceCentralityWeight = ReferenceCentralityWeight,
                InterpolateBlankByAnalyticalOrder = InterpolateBlankByAnalyticalOrder,
            };
        }

        /// <summary>
        /// Why the values cannot be used, or null when they can. Checked as the Console checks them;
        /// nothing is checked while the correction is off.
        /// </summary>
        public string? Validate(double ms1AlignmentTolerance) {
            if (!IsAvailable) {
                return null;
            }
            return AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(
                ToParameter(),
                ms1AlignmentTolerance,
                _parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection);
        }

        /// <summary>Writes every value into the project's parameter.</summary>
        public void Commit() {
            if (!IsAvailable || IsReadOnly) {
                return;
            }
            var target = _parameter.AlignmentBaseParam.AutomaticRtCorrection;
            if (target is null) {
                target = new AutomaticAlignmentRetentionTimeCorrectionParameter();
                _parameter.AlignmentBaseParam.AutomaticRtCorrection = target;
            }
            var values = ToParameter();
            target.Execute = values.Execute;
            target.AnchorMode = values.AnchorMode;
            target.AnchorLibraryFilePath = values.AnchorLibraryFilePath;
            target.AnchorPeakSelectionMode = values.AnchorPeakSelectionMode;
            target.AnchorPeakSelectionRtWeight = values.AnchorPeakSelectionRtWeight;
            target.ReferenceFileId = values.ReferenceFileId;
            target.LocalSupportRtWindow = values.LocalSupportRtWindow;
            target.MinimumAnchorCount = values.MinimumAnchorCount;
            target.MaximumAnchorCount = values.MaximumAnchorCount;
            target.RtBinWidth = values.RtBinWidth;
            target.MatchRtTolerance = values.MatchRtTolerance;
            target.MinimumSampleCoverage = values.MinimumSampleCoverage;
            target.IntensityQuantile = values.IntensityQuantile;
            target.MaximumPeakWidthQuantile = values.MaximumPeakWidthQuantile;
            target.MinimumSignalToNoise = values.MinimumSignalToNoise;
            target.MinimumGaussianSimilarity = values.MinimumGaussianSimilarity;
            target.MinimumIdealSlope = values.MinimumIdealSlope;
            target.OutlierMadThreshold = values.OutlierMadThreshold;
            target.ReferenceCentralityWeight = values.ReferenceCentralityWeight;
            target.InterpolateBlankByAnalyticalOrder = values.InterpolateBlankByAnalyticalOrder;
        }

        /// <summary>Asks for the anchor library file.</summary>
        public void BrowseAnchorLibrary() {
            var request = new OpenFileRequest(path => AnchorLibraryFilePath = path)
            {
                Title = "Automatic RT correction anchor library",
                Filter = "Anchor library (*.txt;*.tsv)|*.txt;*.tsv|All files (*.*)|*.*",
            };
            _broker.Publish(request);
        }
    }

    /// <summary>A choice of the correction's reference file: automatic, or one file.</summary>
    public sealed class AutomaticRtCorrectionReferenceChoice
    {
        public AutomaticRtCorrectionReferenceChoice(int fileId, string label) {
            FileId = fileId;
            Label = label;
        }

        public static AutomaticRtCorrectionReferenceChoice Automatic { get; } = new AutomaticRtCorrectionReferenceChoice(-1, "Automatic (chosen by the correction)");

        public int FileId { get; }
        public string Label { get; }

        public override string ToString() => Label;
    }
}
