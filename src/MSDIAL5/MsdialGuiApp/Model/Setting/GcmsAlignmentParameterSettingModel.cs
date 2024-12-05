using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsAlignmentParameterSettingModel : BindableBase, IAlignmentParameterSettingModel {
        private readonly MsdialGcmsParameter _parameter;
        private readonly AlignmentFileBeanModelCollection _alignmentFiles;
        private readonly IMessageBroker _broker;

        public GcmsAlignmentParameterSettingModel(MsdialGcmsParameter parameter, DateTime now, AnalysisFileBeanModelCollection files, AlignmentFileBeanModelCollection alignmentFiles, ProcessOption processOption, IMessageBroker broker) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _alignmentFiles = alignmentFiles ?? throw new ArgumentNullException(nameof(alignmentFiles));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            IsReadOnly = (processOption & ProcessOption.Alignment) == 0;

            AlignmentResultFileName = $"AlignmentResult_{now:yyyy_MM_dd_HH_mm_ss}";
            AnalysisFiles = files.AnalysisFiles;
            _referenceFile = files.FindByID(parameter.AlignmentBaseParam.AlignmentReferenceFileID);
            RtEqualityParameter = new RetentionTimeEqualityParameterSetting(parameter.AlignmentBaseParam);
            RiEqualityParameter = new RetentionIndexEqualityParameterSetting(parameter);
            EiEqualityParameter = new Ms1EqualityParameterSetting(parameter.AlignmentBaseParam);
            PeakCountFilter = parameter.PostProcessBaseParam.PeakCountFilter;
            NPercentDetectedInOneGroup = parameter.PostProcessBaseParam.NPercentDetectedInOneGroup;
            BlankFiltering = parameter.PostProcessBaseParam.BlankFiltering;
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = parameter.PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            FoldChangeForBlankFiltering = parameter.PostProcessBaseParam.FoldChangeForBlankFiltering;
            IsKeepRefMatchedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = parameter.PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking;
            IsForceInsertForGapFilling = parameter.PostProcessBaseParam.IsForceInsertForGapFilling;
            ShouldRunAlignment = files.Count > 1 && !IsReadOnly;

            IndexType = parameter.AlignmentIndexType;
            IsIdentificationOnlyPerformedForAlignmentFile = parameter.RefSpecMatchBaseParam.IsIdentificationOnlyPerformedForAlignmentFile;
            IsRepresentativeQuantMassBasedOnBasePeakMz = parameter.IsRepresentativeQuantMassBasedOnBasePeakMz;
        }

        public bool IsReadOnly { get; }

        public string AlignmentResultFileName {
            get => _alignmentResultFileName;
            set => SetProperty(ref _alignmentResultFileName, value);
        }
        private string _alignmentResultFileName = string.Empty;

        public ReadOnlyCollection<AnalysisFileBeanModel> AnalysisFiles { get; }

        public AnalysisFileBeanModel ReferenceFile {
            get => _referenceFile;
            set => SetProperty(ref _referenceFile, value);
        }
        private AnalysisFileBeanModel _referenceFile;

        public AlignmentIndexType IndexType {
            get => _indexType;
            set => SetProperty(ref _indexType, value);
        }
        private AlignmentIndexType _indexType;

        public RetentionTimeEqualityParameterSetting RtEqualityParameter { get; }
        public RetentionIndexEqualityParameterSetting RiEqualityParameter { get; }
        public Ms1EqualityParameterSetting EiEqualityParameter { get; }

        public float EiSimilarityTolerance {
            get => _eiSimilarityTolerance;
            set => SetProperty(ref _eiSimilarityTolerance, value);
        }
        private float _eiSimilarityTolerance;

        public float EiSimilarityFactor {
            get => _eiSimilarityFactor;
            set => SetProperty(ref _eiSimilarityFactor, value);
        }
        private float _eiSimilarityFactor;

        public bool IsIdentificationOnlyPerformedForAlignmentFile {
            get => _isIdentificationOnlyPerformedForAlignmentFile;
            set => SetProperty(ref _isIdentificationOnlyPerformedForAlignmentFile, value);
        }
        private bool _isIdentificationOnlyPerformedForAlignmentFile;

        public bool IsForceInsertForGapFilling {
            get => _isForceInsertForGapFilling;
            set => SetProperty(ref _isForceInsertForGapFilling, value);
        }
        private bool _isForceInsertForGapFilling;

        public bool IsRepresentativeQuantMassBasedOnBasePeakMz {
            get => _isRepresentativeQuantMassBasedOnBasePeakMz;
            set => SetProperty(ref _isRepresentativeQuantMassBasedOnBasePeakMz, value);
        }
        private bool _isRepresentativeQuantMassBasedOnBasePeakMz;

        public bool ShouldRunAlignment {
            get => _shouldRunAlignment;
            set => SetProperty(ref _shouldRunAlignment, value);
        }
        private bool _shouldRunAlignment;

        public float PeakCountFilter {
            get => _peakCountFilter;
            set => SetProperty(ref _peakCountFilter, value);
        }
        private float _peakCountFilter;

        public float NPercentDetectedInOneGroup {
            get => _nPercentDetectedInOneGroup;
            set => SetProperty(ref _nPercentDetectedInOneGroup, value);
        }
        private float _nPercentDetectedInOneGroup;

        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange {
            get => _isRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            set => SetProperty(ref _isRemoveFeatureBasedOnBlankPeakHeightFoldChange, value);
        }
        private bool _isRemoveFeatureBasedOnBlankPeakHeightFoldChange;

        public BlankFiltering BlankFiltering {
            get => _blankFiltering;
            set => SetProperty(ref _blankFiltering, value);
        }
        private BlankFiltering _blankFiltering;

        public float FoldChangeForBlankFiltering {
            get => _foldChangeForBlankFiltering;
            set => SetProperty(ref _foldChangeForBlankFiltering, value);
        }
        private float _foldChangeForBlankFiltering;

        public bool IsKeepRefMatchedMetaboliteFeatures {
            get => _isKeepRefMatchedMetaboliteFeatures;
            set => SetProperty(ref _isKeepRefMatchedMetaboliteFeatures, value);
        }
        private bool _isKeepRefMatchedMetaboliteFeatures;

        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking {
            get => _isKeepRemovableFeaturesAndAssignedTagForChecking;
            set => SetProperty(ref _isKeepRemovableFeaturesAndAssignedTagForChecking, value);
        }
        private bool _isKeepRemovableFeaturesAndAssignedTagForChecking;

        public bool IsCompleted {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
        private bool _isCompleted;

        public bool TryCommit() {
            if (!ShouldRunAlignment) {
                _parameter.ProcessBaseParam.ProcessOption &= ~ProcessOption.Alignment;
                return true;
            }
            if (IsRemoveFeatureBasedOnBlankPeakHeightFoldChange && AnalysisFiles.All(file => file.AnalysisFileType != AnalysisFileType.Blank)) {
                var request = new ErrorMessageBoxRequest()
                {
                    Caption = "Message",
                    Content = "If you use blank sample filter, please set at least one file's type as Blank in file property setting."
                        + " Do you continue this analysis without the filter option?",
                    ButtonType = MessageBoxButton.OKCancel,
                };
                _broker.Publish(request);
                if (request.Result != MessageBoxResult.OK) {
                    return false;
                }
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false;
            }

            _parameter.ProcessBaseParam.ProcessOption |= ProcessOption.Alignment;
            var projectFolder = _parameter.ProjectParam.ProjectFolderPath;

            _alignmentFiles.Add(new AlignmentFileBean
            {
                FileID = _alignmentFiles.Files.Select(file => ((IFileBean)file).FileID).DefaultIfEmpty(0).Max() + 1,
                FileName = AlignmentResultFileName,
                FilePath = Path.Combine(projectFolder, AlignmentResultFileName + "." + MsdialDataStorageFormat.arf),
                EicFilePath = Path.Combine(projectFolder, AlignmentResultFileName + ".EIC.aef"),
                SpectraFilePath = Path.Combine(projectFolder, AlignmentResultFileName + "." + MsdialDataStorageFormat.dcl),
                ProteinAssembledResultFilePath = Path.Combine(projectFolder, AlignmentResultFileName + "." + MsdialDataStorageFormat.prf),
            });
            _parameter.AlignmentBaseParam.AlignmentReferenceFileID = ReferenceFile.AnalysisFileId;
            var postProcessParameter = _parameter.PostProcessBaseParam;
            postProcessParameter.PeakCountFilter = PeakCountFilter;
            postProcessParameter.NPercentDetectedInOneGroup = NPercentDetectedInOneGroup;
            postProcessParameter.BlankFiltering = BlankFiltering;
            postProcessParameter.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            postProcessParameter.FoldChangeForBlankFiltering = FoldChangeForBlankFiltering;
            postProcessParameter.IsKeepRefMatchedMetaboliteFeatures = IsKeepRefMatchedMetaboliteFeatures;
            postProcessParameter.IsKeepRemovableFeaturesAndAssignedTagForChecking = IsKeepRemovableFeaturesAndAssignedTagForChecking;
            postProcessParameter.IsForceInsertForGapFilling = IsForceInsertForGapFilling;
            RtEqualityParameter.Commit();
            RiEqualityParameter.Commit();
            EiEqualityParameter.Commit();
            _parameter.AlignmentIndexType = IndexType;
            _parameter.RefSpecMatchBaseParam.IsIdentificationOnlyPerformedForAlignmentFile = IsIdentificationOnlyPerformedForAlignmentFile;
            _parameter.IsRepresentativeQuantMassBasedOnBasePeakMz = IsRepresentativeQuantMassBasedOnBasePeakMz;

            IsCompleted = true;
            return true;
        }

        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly) {
                return;
            }
            RtEqualityParameter.Update(parameter);
            RiEqualityParameter.Update(parameter);
            EiEqualityParameter.Update(parameter);

            PeakCountFilter = parameter.PostProcessBaseParam.PeakCountFilter;
            NPercentDetectedInOneGroup = parameter.PostProcessBaseParam.NPercentDetectedInOneGroup;
            BlankFiltering = parameter.PostProcessBaseParam.BlankFiltering;
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = parameter.PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            FoldChangeForBlankFiltering = parameter.PostProcessBaseParam.FoldChangeForBlankFiltering;
            IsKeepRefMatchedMetaboliteFeatures = parameter.PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = parameter.PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking;
            IsForceInsertForGapFilling = parameter.PostProcessBaseParam.IsForceInsertForGapFilling;
            IsIdentificationOnlyPerformedForAlignmentFile = parameter.RefSpecMatchBaseParam.IsIdentificationOnlyPerformedForAlignmentFile;

            if (parameter is MsdialGcmsParameter gcms) {
                IndexType = gcms.AlignmentIndexType;
                IsRepresentativeQuantMassBasedOnBasePeakMz = gcms.IsRepresentativeQuantMassBasedOnBasePeakMz;
            }
        }
    }
}
