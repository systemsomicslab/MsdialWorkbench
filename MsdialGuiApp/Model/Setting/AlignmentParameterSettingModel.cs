using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public class AlignmentParameterSettingModel : BindableBase
    {
        public AlignmentParameterSettingModel(ParameterBase parameter, DateTime now, List<AnalysisFileBean> files) {
            AlignmentResultFileName = $"AlignmentResult_{now:yyyy_MM_dd_hh_mm_ss}";
            AnalysisFiles = files.AsReadOnly();
            ReferenceFile = AnalysisFiles.FirstOrDefault(f => f.AnalysisFileId == parameter.AlignmentReferenceFileID);
            EqualityParameterSettings = PrepareEqualityParameterSettings(parameter);
            PeakCountFilter = parameter.PostProcessBaseParam.PeakCountFilter;
            NPercentDetectedInOneGroup = parameter.PostProcessBaseParam.NPercentDetectedInOneGroup;
            BlankFiltering = parameter.PostProcessBaseParam.BlankFiltering;
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = parameter.PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            FoldChangeForBlankFiltering = parameter.PostProcessBaseParam.FoldChangeForBlankFiltering;
            IsKeepRefMatchedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures;
            IsKeepSuggestedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepSuggestedMetaboliteFeatures;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = parameter.PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking;
            IsForceInsertForGapFilling = parameter.PostProcessBaseParam.IsForceInsertForGapFilling;
            this.parameter = parameter;
        }

        private readonly ParameterBase parameter;

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }
        private string alignmentResultFileName;

        public ReadOnlyCollection<AnalysisFileBean> AnalysisFiles { get; }

        public AnalysisFileBean ReferenceFile {
            get => referenceFile;
            set => SetProperty(ref referenceFile, value);
        }
        private AnalysisFileBean referenceFile;

        public List<IPeakEqualityParameterSetting> EqualityParameterSettings { get; }

        public float PeakCountFilter {
            get => peakCountFilter;
            set => SetProperty(ref peakCountFilter, value);
        }
        private float peakCountFilter;

        public float NPercentDetectedInOneGroup {
            get => nPercentDetectedInOneGroup;
            set => SetProperty(ref nPercentDetectedInOneGroup, value);
        }
        private float nPercentDetectedInOneGroup;

        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange {
            get => isRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            set => SetProperty(ref isRemoveFeatureBasedOnBlankPeakHeightFoldChange, value);
        }
        private bool isRemoveFeatureBasedOnBlankPeakHeightFoldChange;

        public BlankFiltering BlankFiltering {
            get => blankFiltering;
            set => SetProperty(ref blankFiltering, value);
        }
        private BlankFiltering blankFiltering;

        public float FoldChangeForBlankFiltering {
            get => foldChangeForBlankFiltering;
            set => SetProperty(ref foldChangeForBlankFiltering, value);
        }
        private float foldChangeForBlankFiltering;

        public bool IsKeepRefMatchedMetaboliteFeatures {
            get => isKeepRefMatchedMetaboliteFeatures;
            set => SetProperty(ref isKeepRefMatchedMetaboliteFeatures, value);
        }
        private bool isKeepRefMatchedMetaboliteFeatures;

        public bool IsKeepSuggestedMetaboliteFeatures {
            get => isKeepSuggestedMetaboliteFeatures;
            set => SetProperty(ref isKeepSuggestedMetaboliteFeatures, value);
        }
        private bool isKeepSuggestedMetaboliteFeatures;

        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking {
            get => isKeepRemovableFeaturesAndAssignedTagForChecking;
            set => SetProperty(ref isKeepRemovableFeaturesAndAssignedTagForChecking, value);
        }
        private bool isKeepRemovableFeaturesAndAssignedTagForChecking;

        public bool IsForceInsertForGapFilling {
            get => isForceInsertForGapFilling;
            set => SetProperty(ref isForceInsertForGapFilling, value);
        }
        private bool isForceInsertForGapFilling;

        public bool IsCompleted {
            get => isCompleted;
            private set => SetProperty(ref isCompleted, value);
        }
        private bool isCompleted;

        public void Commit() {
            parameter.AlignmentBaseParam.AlignmentReferenceFileID = ReferenceFile.AnalysisFileId;
            parameter.PostProcessBaseParam.PeakCountFilter = PeakCountFilter;
            parameter.PostProcessBaseParam.NPercentDetectedInOneGroup = NPercentDetectedInOneGroup;
            parameter.PostProcessBaseParam.BlankFiltering = BlankFiltering;
            parameter.PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            parameter.PostProcessBaseParam.FoldChangeForBlankFiltering = FoldChangeForBlankFiltering;
            parameter.PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures = IsKeepRefMatchedMetaboliteFeatures;
            parameter.PostProcessBaseParam.IsKeepSuggestedMetaboliteFeatures = IsKeepSuggestedMetaboliteFeatures;
            parameter.PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking = IsKeepRemovableFeaturesAndAssignedTagForChecking;
            parameter.PostProcessBaseParam.IsForceInsertForGapFilling = IsForceInsertForGapFilling;
            EqualityParameterSettings.ForEach(s => s.Commit());
            IsCompleted = true;
        }

        private static List<IPeakEqualityParameterSetting> PrepareEqualityParameterSettings(ParameterBase parameter) {
            switch (parameter) {
                case MsdialLcImMsParameter lcimmsParameter:
                    return new List<IPeakEqualityParameterSetting>
                    {
                        new RetentionTimeEqualityParameterSetting(parameter.AlignmentBaseParam),
                        new DriftTimeEqualityParameterSetting(lcimmsParameter),
                        new Ms1EqualityParameterSetting(parameter.AlignmentBaseParam),
                    };
                case MsdialLcmsParameter _:
                case MsdialGcmsParameter _:
                    return new List<IPeakEqualityParameterSetting>
                    {
                        new RetentionTimeEqualityParameterSetting(parameter.AlignmentBaseParam),
                        new Ms1EqualityParameterSetting(parameter.AlignmentBaseParam),
                    };
                case MsdialImmsParameter immsParameter:
                    return new List<IPeakEqualityParameterSetting>
                    {
                        new DriftTimeEqualityParameterSetting(immsParameter),
                        new Ms1EqualityParameterSetting(parameter.AlignmentBaseParam),
                    };
                case MsdialDimsParameter _:
                    return new List<IPeakEqualityParameterSetting>
                    {
                        new Ms1EqualityParameterSetting(parameter.AlignmentBaseParam),
                    };
                default:
                    return new List<IPeakEqualityParameterSetting>();
            }
        }
    }
}
