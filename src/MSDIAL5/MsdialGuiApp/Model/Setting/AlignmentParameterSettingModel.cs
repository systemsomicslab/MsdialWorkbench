using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAlignmentParameterSettingModel {
        bool ShouldRunAlignment { get; }
        bool TryCommit();
        void LoadParameter(ParameterBase parameter);
    }

    public sealed class AlignmentParameterSettingModel : BindableBase, IAlignmentParameterSettingModel
    {
        public AlignmentParameterSettingModel(ParameterBase parameter, DateTime now, List<AnalysisFileBean> files, AlignmentFileBeanModelCollection alignmentFiles, ProcessOption process) {
            IsReadOnly = (process & ProcessOption.Alignment) == 0;
            _alignmentFiles = alignmentFiles;

            alignmentResultFileName = $"AlignmentResult_{now:yyyy_MM_dd_HH_mm_ss}";
            AnalysisFiles = files.AsReadOnly();
            referenceFile = AnalysisFiles.FirstOrDefault(f => f.AnalysisFileId == parameter.AlignmentReferenceFileID) ?? AnalysisFiles.First();
            EqualityParameterSettings = new ObservableCollection<IPeakEqualityParameterSetting>(PrepareEqualityParameterSettings(parameter));
            PeakCountFilter = parameter.PostProcessBaseParam.PeakCountFilter;
            NPercentDetectedInOneGroup = parameter.PostProcessBaseParam.NPercentDetectedInOneGroup;
            BlankFiltering = parameter.PostProcessBaseParam.BlankFiltering;
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = parameter.PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            FoldChangeForBlankFiltering = parameter.PostProcessBaseParam.FoldChangeForBlankFiltering;
            IsKeepRefMatchedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures;
            IsKeepSuggestedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepSuggestedMetaboliteFeatures;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = parameter.PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking;
            IsForceInsertForGapFilling = parameter.PostProcessBaseParam.IsForceInsertForGapFilling;
            ShouldRunAlignment = AnalysisFiles.Count > 1 && !IsReadOnly;
            this.parameter = parameter;
        }

        private readonly ParameterBase parameter;
        private readonly AlignmentFileBeanModelCollection _alignmentFiles;

        public bool IsReadOnly { get; }

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

        public ObservableCollection<IPeakEqualityParameterSetting> EqualityParameterSettings { get; }

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

        public bool ShouldRunAlignment {
            get => shouldRunAlignment;
            set => SetProperty(ref shouldRunAlignment, value);
        }
        private bool shouldRunAlignment;

        public bool IsCompleted {
            get => isCompleted;
            private set => SetProperty(ref isCompleted, value);
        }
        private bool isCompleted;

        public bool TryCommit() {
            if (!ShouldRunAlignment) {
                parameter.ProcessBaseParam.ProcessOption &= ~ProcessOption.Alignment;
                return true;
            }
            if (IsRemoveFeatureBasedOnBlankPeakHeightFoldChange
                && AnalysisFiles.All(file => file.AnalysisFileType != AnalysisFileType.Blank)) {
                var request = new ErrorMessageBoxRequest()
                {
                    Caption = "Message",
                    Content = "If you use blank sample filter, please set at least one file's type as Blank in file property setting."
                        + " Do you continue this analysis without the filter option?",
                    ButtonType = MessageBoxButton.OKCancel,
                };
                MessageBroker.Default.Publish(request);
                if (request.Result != MessageBoxResult.OK) {
                    return false;
                }
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false;
            }

            parameter.ProcessBaseParam.ProcessOption |= ProcessOption.Alignment;
            var projectFolder = parameter.ProjectParam.ProjectFolderPath;
            _alignmentFiles.Add(new AlignmentFileBean
            {
                FileID = _alignmentFiles.Files.Select(file => ((IFileBean)file).FileID).DefaultIfEmpty(0).Max() + 1,
                FileName = alignmentResultFileName,
                FilePath = Path.Combine(projectFolder, alignmentResultFileName + "." + MsdialDataStorageFormat.arf),
                EicFilePath = Path.Combine(projectFolder, alignmentResultFileName + ".EIC.aef"),
                SpectraFilePath = Path.Combine(projectFolder, alignmentResultFileName + "." + MsdialDataStorageFormat.dcl),
                ProteinAssembledResultFilePath = Path.Combine(projectFolder, alignmentResultFileName + "." + MsdialDataStorageFormat.prf),
            });
            parameter.AlignmentBaseParam.AlignmentReferenceFileID = ReferenceFile.AnalysisFileId;
            var postProcessParameter = parameter.PostProcessBaseParam;
            postProcessParameter.PeakCountFilter = PeakCountFilter;
            postProcessParameter.NPercentDetectedInOneGroup = NPercentDetectedInOneGroup;
            postProcessParameter.BlankFiltering = BlankFiltering;
            postProcessParameter.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            postProcessParameter.FoldChangeForBlankFiltering = FoldChangeForBlankFiltering;
            postProcessParameter.IsKeepRefMatchedMetaboliteFeatures = IsKeepRefMatchedMetaboliteFeatures;
            postProcessParameter.IsKeepSuggestedMetaboliteFeatures = IsKeepSuggestedMetaboliteFeatures;
            postProcessParameter.IsKeepRemovableFeaturesAndAssignedTagForChecking = IsKeepRemovableFeaturesAndAssignedTagForChecking;
            postProcessParameter.IsForceInsertForGapFilling = IsForceInsertForGapFilling;
            foreach (var s in EqualityParameterSettings) {
                s.Commit();
            }
            IsCompleted = true;
            return true;
        }

        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly) {
                return;
            }
            foreach (var s in EqualityParameterSettings) {
                s.Update(parameter);
            }
            PeakCountFilter = parameter.PostProcessBaseParam.PeakCountFilter;
            NPercentDetectedInOneGroup = parameter.PostProcessBaseParam.NPercentDetectedInOneGroup;
            BlankFiltering = parameter.PostProcessBaseParam.BlankFiltering;
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = parameter.PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            FoldChangeForBlankFiltering = parameter.PostProcessBaseParam.FoldChangeForBlankFiltering;
            IsKeepRefMatchedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures;
            IsKeepSuggestedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepSuggestedMetaboliteFeatures;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = parameter.PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking;
            IsForceInsertForGapFilling = parameter.PostProcessBaseParam.IsForceInsertForGapFilling;
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
