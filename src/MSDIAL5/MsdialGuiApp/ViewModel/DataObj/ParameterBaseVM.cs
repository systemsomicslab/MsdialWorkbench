using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class ParameterBaseVM : ViewModelBase
    {

        protected readonly ParameterBase innerModel;
        public ParameterBaseVM(ParameterBase innerModel) {
            this.innerModel = innerModel;
        }

        public DateTime ProjectStartDate {
            get => innerModel.ProjectStartDate;
            set {
                if (innerModel.ProjectStartDate == value) return;
                innerModel.ProjectStartDate = value;
                OnPropertyChanged(nameof(ProjectStartDate));
            }
        }
        public DateTime FinalSavedDate {
            get => innerModel.FinalSavedDate;
            set {
                if (innerModel.FinalSavedDate == value) return;
                innerModel.FinalSavedDate = value;
                OnPropertyChanged(nameof(FinalSavedDate));
            }
        }
        public string MsdialVersionNumber {
            get => innerModel.MsdialVersionNumber;
            set {
                if (innerModel.MsdialVersionNumber == value) return;
                innerModel.MsdialVersionNumber = value;
                OnPropertyChanged(nameof(MsdialVersionNumber));
            }
        }


        // Project container
        public string ProjectFolderPath {
            get => innerModel.ProjectFolderPath;
            set {
                if (innerModel.ProjectFolderPath == value) return;
                innerModel.ProjectFolderPath = value;
                OnPropertyChanged(nameof(ProjectFolderPath));
            }
        }
        public string ProjectFilePath {
            get => innerModel.ProjectParam.ProjectFilePath;
            set {
                if (innerModel.ProjectFileName == System.IO.Path.GetFileName(value)) return;
                innerModel.ProjectFileName = System.IO.Path.GetFileName(value);
                OnPropertyChanged(nameof(ProjectFilePath));
            }
        }

        public Dictionary<int, string> FileID_ClassName {
            get => innerModel.FileID_ClassName;
            set {
                if (innerModel.FileID_ClassName == value) return;
                innerModel.FileID_ClassName = value;
                OnPropertyChanged(nameof(FileID_ClassName));
            }
        }

        public Dictionary<int, AnalysisFileType> FileID_AnalysisFileType {
            get => innerModel.FileID_AnalysisFileType;
            set {
                if (innerModel.FileID_AnalysisFileType == value) return;
                innerModel.FileID_AnalysisFileType = value;
                OnPropertyChanged(nameof(FileID_AnalysisFileType));
            }
        }

        public bool IsBoxPlotForAlignmentResult {
            get => innerModel.IsBoxPlotForAlignmentResult;
            set {
                if (innerModel.IsBoxPlotForAlignmentResult == value) return;
                innerModel.IsBoxPlotForAlignmentResult = value;
                OnPropertyChanged(nameof(IsBoxPlotForAlignmentResult));
            }
        }

        public Dictionary<string, int> ClassnameToOrder {
            get => innerModel.ClassnameToOrder;
            set {
                if (innerModel.ClassnameToOrder == value) return;
                innerModel.ClassnameToOrder = value;
                OnPropertyChanged(nameof(ClassnameToOrder));
            }
        }

        public Dictionary<string, List<byte>> ClassnameToColorBytes {
            get => innerModel.ClassnameToColorBytes;
            set {
                if (innerModel.ClassnameToColorBytes == value) return;
                innerModel.ClassnameToColorBytes = value;
                OnPropertyChanged(nameof(ClassnameToColorBytes));
            }
        }


        // Project type

        public MSDataType MSDataType {
            get => innerModel.MSDataType;
            set {
                if (innerModel.MSDataType == value) return;
                innerModel.MSDataType = value;
                OnPropertyChanged(nameof(MSDataType));
            }
        }

        public MSDataType MS2DataType {
            get => innerModel.MS2DataType;
            set {
                if (innerModel.MS2DataType == value) return;
                innerModel.MS2DataType = value;
                OnPropertyChanged(nameof(MS2DataType));
            }
        }

        public IonMode IonMode {
            get => innerModel.IonMode;
            set {
                if (innerModel.IonMode == value) return;
                innerModel.IonMode = value;
                OnPropertyChanged(nameof(IonMode));
            }
        }

        public TargetOmics TargetOmics {
            get => innerModel.TargetOmics;
            set {
                if (innerModel.TargetOmics == value) return;
                innerModel.TargetOmics = value;
                OnPropertyChanged(nameof(TargetOmics));
            }
        }

        public Ionization Ionization {
            get => innerModel.Ionization;
            set {
                if (innerModel.Ionization == value) return;
                innerModel.Ionization = value;
                OnPropertyChanged(nameof(Ionization));
            }
        }

        public MachineCategory MachineCategory {
            get => innerModel.MachineCategory;
            set {
                if (innerModel.MachineCategory == value) return;
                innerModel.MachineCategory = value;
                OnPropertyChanged(nameof(MachineCategory));
            }
        }

        // Project metadata
        public string InstrumentType {
            get => innerModel.InstrumentType;
            set {
                if (innerModel.InstrumentType == value) return;
                innerModel.InstrumentType = value;
                OnPropertyChanged(nameof(InstrumentType));
            }
        }

        public string Instrument {
            get => innerModel.Instrument;
            set {
                if (innerModel.Instrument == value) return;
                innerModel.Instrument = value;
                OnPropertyChanged(nameof(Instrument));
            }
        }

        public string Authors {
            get => innerModel.Authors;
            set {
                if (innerModel.Authors == value) return;
                innerModel.Authors = value;
                OnPropertyChanged(nameof(Authors));
            }
        }

        public string License {
            get => innerModel.License;
            set {
                if (innerModel.License == value) return;
                innerModel.License = value;
                OnPropertyChanged(nameof(License));
            }
        }

        public string Comment {
            get => innerModel.Comment;
            set {
                if (innerModel.Comment == value) return;
                innerModel.Comment = value;
                OnPropertyChanged(nameof(Comment));
            }
        }

        // Annotation

        public string MspFilePath {
            get => innerModel.MspFilePath;
            set {
                if (innerModel.MspFilePath == value) return;
                innerModel.MspFilePath = value;
                OnPropertyChanged(nameof(MspFilePath));
            }
        }

        public string TextDBFilePath {
            get => innerModel.TextDBFilePath;
            set {
                if (innerModel.TextDBFilePath == value) return;
                innerModel.TextDBFilePath = value;
                OnPropertyChanged(nameof(TextDBFilePath));
            }
        }

        public string IsotopeTextDBFilePath {
            get => innerModel.IsotopeTextDBFilePath;
            set {
                if (innerModel.IsotopeTextDBFilePath == value) return;
                innerModel.IsotopeTextDBFilePath = value;
                OnPropertyChanged(nameof(IsotopeTextDBFilePath));
            }
        }

        public string CompoundListInTargetModePath {
            get => innerModel.CompoundListInTargetModePath;
            set {
                if (innerModel.CompoundListInTargetModePath == value) return;
                innerModel.CompoundListInTargetModePath = value;
                OnPropertyChanged(nameof(CompoundListInTargetModePath));
            }
        }

        public string CompoundListForRtCorrectionPath {
            get => innerModel.CompoundListForRtCorrectionPath;
            set {
                if (innerModel.CompoundListForRtCorrectionPath == value) return;
                innerModel.CompoundListForRtCorrectionPath = value;
                OnPropertyChanged(nameof(CompoundListForRtCorrectionPath));
            }
        }

        public List<AdductIon> SearchedAdductIons {
            get => innerModel.SearchedAdductIons;
            set {
                if (innerModel.SearchedAdductIons == value) return;
                innerModel.SearchedAdductIons = value;
                OnPropertyChanged(nameof(SearchedAdductIons));
            }
        }

        // Export

        public ExportSpectraFileFormat ExportSpectraFileFormat {
            get => innerModel.ExportSpectraFileFormat;
            set {
                if (innerModel.ExportSpectraFileFormat == value) return;
                innerModel.ExportSpectraFileFormat = value;
                OnPropertyChanged(nameof(ExportSpectraFileFormat));
            }
        }

        public ExportspectraType ExportSpectraType {
            get => innerModel.ExportSpectraType;
            set {
                if (innerModel.ExportSpectraType == value) return;
                innerModel.ExportSpectraType = value;
                OnPropertyChanged(nameof(ExportSpectraType));
            }
        }

        public string MatExportFolderPath {
            get => innerModel.MatExportFolderPath;
            set {
                if (innerModel.MatExportFolderPath == value) return;
                innerModel.MatExportFolderPath = value;
                OnPropertyChanged(nameof(MatExportFolderPath));
            }
        }

        public string ExportFolderPath {
            get => innerModel.ExportFolderPath;
            set {
                if (innerModel.ExportFolderPath == value) return;
                innerModel.ExportFolderPath = value;
                OnPropertyChanged(nameof(ExportFolderPath));
            }
        }

        public bool IsHeightMatrixExport {
            get => innerModel.IsHeightMatrixExport;
            set {
                if (innerModel.IsHeightMatrixExport == value) return;
                innerModel.IsHeightMatrixExport = value;
                OnPropertyChanged(nameof(IsHeightMatrixExport));
            }
        }

        public bool IsNormalizedMatrixExport {
            get => innerModel.IsNormalizedMatrixExport;
            set {
                if (innerModel.IsNormalizedMatrixExport == value) return;
                innerModel.IsNormalizedMatrixExport = value;
                OnPropertyChanged(nameof(IsNormalizedMatrixExport));
            }
        }

        public bool IsRepresentativeSpectraExport {
            get => innerModel.IsRepresentativeSpectraExport;
            set {
                if (innerModel.IsRepresentativeSpectraExport == value) return;
                innerModel.IsRepresentativeSpectraExport = value;
                OnPropertyChanged(nameof(IsRepresentativeSpectraExport));
            }
        }

        public bool IsPeakIdMatrixExport {
            get => innerModel.IsPeakIdMatrixExport;
            set {
                if (innerModel.IsPeakIdMatrixExport == value) return;
                innerModel.IsPeakIdMatrixExport = value;
                OnPropertyChanged(nameof(IsPeakIdMatrixExport));
            }
        }

        public bool IsRetentionTimeMatrixExport {
            get => innerModel.IsRetentionTimeMatrixExport;
            set {
                if (innerModel.IsRetentionTimeMatrixExport == value) return;
                innerModel.IsRetentionTimeMatrixExport = value;
                OnPropertyChanged(nameof(IsRetentionTimeMatrixExport));
            }
        }

        public bool IsMassMatrixExport {
            get => innerModel.IsMassMatrixExport;
            set {
                if (innerModel.IsMassMatrixExport == value) return;
                innerModel.IsMassMatrixExport = value;
                OnPropertyChanged(nameof(IsMassMatrixExport));
            }
        }

        public bool IsMsmsIncludedMatrixExport {
            get => innerModel.IsMsmsIncludedMatrixExport;
            set {
                if (innerModel.IsMsmsIncludedMatrixExport == value) return;
                innerModel.IsMsmsIncludedMatrixExport = value;
                OnPropertyChanged(nameof(IsMsmsIncludedMatrixExport));
            }
        }

        public bool IsUniqueMsMatrixExport {
            get => innerModel.IsUniqueMsMatrixExport;
            set {
                if (innerModel.IsUniqueMsMatrixExport == value) return;
                innerModel.IsUniqueMsMatrixExport = value;
                OnPropertyChanged(nameof(IsUniqueMsMatrixExport));
            }
        }

        public bool IsPeakAreaMatrixExport {
            get => innerModel.IsPeakAreaMatrixExport;
            set {
                if (innerModel.IsPeakAreaMatrixExport == value) return;
                innerModel.IsPeakAreaMatrixExport = value;
                OnPropertyChanged(nameof(IsPeakAreaMatrixExport));
            }
        }

        public bool IsParameterExport {
            get => innerModel.IsParameterExport;
            set {
                if (innerModel.IsParameterExport == value) return;
                innerModel.IsParameterExport = value;
                OnPropertyChanged(nameof(IsParameterExport));
            }
        }

        public bool IsGnpsExport {
            get => innerModel.IsGnpsExport;
            set {
                if (innerModel.IsGnpsExport == value) return;
                innerModel.IsGnpsExport = value;
                OnPropertyChanged(nameof(IsGnpsExport));
            }
        }

        public bool IsMolecularNetworkingExport {
            get => innerModel.IsMolecularNetworkingExport;
            set {
                if (innerModel.IsMolecularNetworkingExport == value) return;
                innerModel.IsMolecularNetworkingExport = value;
                OnPropertyChanged(nameof(IsMolecularNetworkingExport));
            }
        }

        public bool IsSnMatrixExport {
            get => innerModel.IsSnMatrixExport;
            set {
                if (innerModel.IsSnMatrixExport == value) return;
                innerModel.IsSnMatrixExport = value;
                OnPropertyChanged(nameof(IsSnMatrixExport));
            }
        }

        public bool IsExportedAsMzTabM {
            get => innerModel.IsExportedAsMzTabM;
            set {
                if (innerModel.IsExportedAsMzTabM == value) return;
                innerModel.IsExportedAsMzTabM = value;
                OnPropertyChanged(nameof(IsExportedAsMzTabM));
            }
        }

        // Process parameters

        public ProcessOption ProcessOption {
            get => innerModel.ProcessOption;
            set {
                if (innerModel.ProcessOption == value) return;
                innerModel.ProcessOption = value;
                OnPropertyChanged(nameof(ProcessOption));
            }
        }

        public int NumThreads {
            get => innerModel.NumThreads;
            set {
                var val = Math.Max(1, Math.Min(Environment.ProcessorCount, value));
                if (innerModel.NumThreads == val) return;
                innerModel.NumThreads = val;
                OnPropertyChanged(nameof(NumThreads));
            }
        }

        // feature detection base

        public SmoothingMethod SmoothingMethod {
            get => innerModel.SmoothingMethod;
            set {
                if (innerModel.SmoothingMethod == value) return;
                innerModel.SmoothingMethod = value;
                OnPropertyChanged(nameof(SmoothingMethod));
            }
        }

        public int SmoothingLevel {
            get => innerModel.SmoothingLevel;
            set {
                if (innerModel.SmoothingLevel == value) return;
                innerModel.SmoothingLevel = value;
                OnPropertyChanged(nameof(SmoothingLevel));
            }
        }

        public double MinimumAmplitude {
            get => innerModel.MinimumAmplitude;
            set {
                if (innerModel.MinimumAmplitude == value) return;
                innerModel.MinimumAmplitude = value;
                OnPropertyChanged(nameof(MinimumAmplitude));
            }
        }

        public double MinimumDatapoints {
            get => innerModel.MinimumDatapoints;
            set {
                if (innerModel.MinimumDatapoints == value) return;
                innerModel.MinimumDatapoints = value;
                OnPropertyChanged(nameof(MinimumDatapoints));
            }
        }

        public float MassSliceWidth {
            get => innerModel.MassSliceWidth;
            set {
                if (innerModel.MassSliceWidth == value) return;
                innerModel.MassSliceWidth = value;
                OnPropertyChanged(nameof(MassSliceWidth));
            }
        }

        public float RetentionTimeBegin {
            get => innerModel.RetentionTimeBegin;
            set {
                if (innerModel.RetentionTimeBegin == value) return;
                innerModel.RetentionTimeBegin = value;
                OnPropertyChanged(nameof(RetentionTimeBegin));
            }
        }
        public float RetentionTimeEnd {
            get => innerModel.RetentionTimeEnd;
            set {
                if (innerModel.RetentionTimeEnd == value) return;
                innerModel.RetentionTimeEnd = value;
                OnPropertyChanged(nameof(RetentionTimeEnd));
            }
        }

        public float MassRangeBegin {
            get => innerModel.MassRangeBegin;
            set {
                if (innerModel.MassRangeBegin == value) return;
                innerModel.MassRangeBegin = value;
                OnPropertyChanged(nameof(MassRangeBegin));
            }
        }

        public float MassRangeEnd {
            get => innerModel.MassRangeEnd;
            set {
                if (innerModel.MassRangeEnd == value) return;
                innerModel.MassRangeEnd = value;
                OnPropertyChanged(nameof(MassRangeEnd));
            }
        }

        public float Ms2MassRangeBegin {
            get => innerModel.Ms2MassRangeBegin;
            set {
                if (innerModel.Ms2MassRangeBegin == value) return;
                innerModel.Ms2MassRangeBegin = value;
                OnPropertyChanged(nameof(Ms2MassRangeBegin));
            }
        }

        public float Ms2MassRangeEnd {
            get => innerModel.Ms2MassRangeEnd;
            set {
                if (innerModel.Ms2MassRangeEnd == value) return;
                innerModel.Ms2MassRangeEnd = value;
                OnPropertyChanged(nameof(Ms2MassRangeEnd));
            }
        }

        public float CentroidMs1Tolerance {
            get => innerModel.CentroidMs1Tolerance;
            set {
                if (innerModel.CentroidMs1Tolerance == value) return;
                innerModel.CentroidMs1Tolerance = value;
                OnPropertyChanged(nameof(CentroidMs1Tolerance));
            }
        }

        public float CentroidMs2Tolerance {
            get => innerModel.CentroidMs2Tolerance;
            set {
                if (innerModel.CentroidMs2Tolerance == value) return;
                innerModel.CentroidMs2Tolerance = value;
                OnPropertyChanged(nameof(CentroidMs2Tolerance));
            }
        }

        public int MaxChargeNumber {
            get => innerModel.MaxChargeNumber;
            set {
                if (innerModel.MaxChargeNumber == value) return;
                innerModel.MaxChargeNumber = value;
                OnPropertyChanged(nameof(MaxChargeNumber));
            }
        }

        public bool IsBrClConsideredForIsotopes {
            get => innerModel.IsBrClConsideredForIsotopes;
            set {
                if (innerModel.IsBrClConsideredForIsotopes == value) return;
                innerModel.IsBrClConsideredForIsotopes = value;
                OnPropertyChanged(nameof(IsBrClConsideredForIsotopes));
            }
        }

        public List<MzSearchQuery> ExcludedMassList {
            get => innerModel.ExcludedMassList;
            set {
                if (innerModel.ExcludedMassList == value) return;
                innerModel.ExcludedMassList = value;
                OnPropertyChanged(nameof(ExcludedMassList));
            }
        }

        public int MaxIsotopesDetectedInMs1Spectrum {
            get => innerModel.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum;
            set {
                if (innerModel.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum == value) return;
                innerModel.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum = value;
                OnPropertyChanged(nameof(MaxIsotopesDetectedInMs1Spectrum));
            }
        }

        // alignment base

        public int AlignmentReferenceFileID {
            get => innerModel.AlignmentReferenceFileID;
            set {
                if (innerModel.AlignmentReferenceFileID == value) return;
                innerModel.AlignmentReferenceFileID = value;
                OnPropertyChanged(nameof(AlignmentReferenceFileID));
            }
        }

        public float RetentionTimeAlignmentFactor {
            get => innerModel.RetentionTimeAlignmentFactor;
            set {
                if (innerModel.RetentionTimeAlignmentFactor == value) return;
                innerModel.RetentionTimeAlignmentFactor = value;
                OnPropertyChanged(nameof(RetentionTimeAlignmentFactor));
            }
        }

        public float Ms1AlignmentFactor {
            get => innerModel.Ms1AlignmentFactor;
            set {
                if (innerModel.Ms1AlignmentFactor == value) return;
                innerModel.Ms1AlignmentFactor = value;
                OnPropertyChanged(nameof(Ms1AlignmentFactor));
            }
        }

        public float SpectrumSimilarityAlignmentFactor {
            get => innerModel.SpectrumSimilarityAlignmentFactor;
            set {
                if (innerModel.SpectrumSimilarityAlignmentFactor == value) return;
                innerModel.SpectrumSimilarityAlignmentFactor = value;
                OnPropertyChanged(nameof(SpectrumSimilarityAlignmentFactor));
            }
        }

        public float Ms1AlignmentTolerance {
            get => innerModel.Ms1AlignmentTolerance;
            set {
                if (innerModel.Ms1AlignmentTolerance == value) return;
                innerModel.Ms1AlignmentTolerance = value;
                OnPropertyChanged(nameof(Ms1AlignmentTolerance));
            }
        }

        public float RetentionTimeAlignmentTolerance {
            get => innerModel.RetentionTimeAlignmentTolerance;
            set {
                if (innerModel.RetentionTimeAlignmentTolerance == value) return;
                innerModel.RetentionTimeAlignmentTolerance = value;
                OnPropertyChanged(nameof(RetentionTimeAlignmentTolerance));
            }
        }

        public float SpectrumSimilarityAlignmentTolerance {
            get => innerModel.SpectrumSimilarityAlignmentTolerance;
            set {
                if (innerModel.SpectrumSimilarityAlignmentTolerance == value) return;
                innerModel.SpectrumSimilarityAlignmentTolerance = value;
                OnPropertyChanged(nameof(SpectrumSimilarityAlignmentTolerance));
            }
        }

        public float AlignmentScoreCutOff {
            get => innerModel.AlignmentScoreCutOff;
            set {
                if (innerModel.AlignmentScoreCutOff == value) return;
                innerModel.AlignmentScoreCutOff = value;
                OnPropertyChanged(nameof(AlignmentScoreCutOff));
            }
        }

        public bool TogetherWithAlignment {
            get => innerModel.TogetherWithAlignment;
            set {
                if (innerModel.TogetherWithAlignment == value) return;
                innerModel.TogetherWithAlignment = value;
                OnPropertyChanged(nameof(TogetherWithAlignment));
            }
        }

        // spectral library search

        public LipidQueryBean LipidQueryContainer {
            get => innerModel.LipidQueryContainer;
            set {
                if (innerModel.LipidQueryContainer == value) return;
                innerModel.LipidQueryContainer = value;
                OnPropertyChanged(nameof(LipidQueryContainer));
            }
        }

        public MsRefSearchParameterBase MspSearchParam {
            get => innerModel.MspSearchParam;
            set {
                if (innerModel.MspSearchParam == value) return;
                innerModel.MspSearchParam = value;
                OnPropertyChanged(nameof(MspSearchParam));
            }
        }

        public bool OnlyReportTopHitInMspSearch {
            get => innerModel.OnlyReportTopHitInMspSearch;
            set {
                if (innerModel.OnlyReportTopHitInMspSearch == value) return;
                innerModel.OnlyReportTopHitInMspSearch = value;
                OnPropertyChanged(nameof(OnlyReportTopHitInMspSearch));
            }
        }

        public MsRefSearchParameterBase TextDbSearchParam {
            get => innerModel.TextDbSearchParam;
            set {
                if (innerModel.TextDbSearchParam == value) return;
                innerModel.TextDbSearchParam = value;
                OnPropertyChanged(nameof(TextDbSearchParam));
            }
        }

        public bool OnlyReportTopHitInTextDBSearch {
            get => innerModel.OnlyReportTopHitInTextDBSearch;
            set {
                if (innerModel.OnlyReportTopHitInTextDBSearch == value) return;
                innerModel.OnlyReportTopHitInTextDBSearch = value;
                OnPropertyChanged(nameof(OnlyReportTopHitInTextDBSearch));
            }
        }

        public bool IsIdentificationOnlyPerformedForAlignmentFile {
            get => innerModel.IsIdentificationOnlyPerformedForAlignmentFile;
            set {
                if (innerModel.IsIdentificationOnlyPerformedForAlignmentFile == value) return;
                innerModel.IsIdentificationOnlyPerformedForAlignmentFile = value;
                OnPropertyChanged(nameof(IsIdentificationOnlyPerformedForAlignmentFile));
            }
        }

        public Dictionary<int, RiDictionaryInfo> FileIdRiInfoDictionary {
            get => innerModel.FileIdRiInfoDictionary;
            set {
                if (innerModel.FileIdRiInfoDictionary == value) return;
                innerModel.FileIdRiInfoDictionary = value;
                OnPropertyChanged(nameof(FileIdRiInfoDictionary));
            }
        }

        // deconvolution
        public float SigmaWindowValue {
            get => innerModel.SigmaWindowValue;
            set {
                if (innerModel.SigmaWindowValue == value) return;
                innerModel.SigmaWindowValue = value;
                OnPropertyChanged(nameof(SigmaWindowValue));
            }
        }

        public float AmplitudeCutoff {
            get => innerModel.AmplitudeCutoff;
            set {
                if (innerModel.AmplitudeCutoff == value) return;
                innerModel.AmplitudeCutoff = value;
                OnPropertyChanged(nameof(AmplitudeCutoff));
            }
        }

        public float AveragePeakWidth {
            get => innerModel.AveragePeakWidth;
            set {
                if (innerModel.AveragePeakWidth == value) return;
                innerModel.AveragePeakWidth = value;
                OnPropertyChanged(nameof(AveragePeakWidth));
            }
        }

        public float KeptIsotopeRange {
            get => innerModel.KeptIsotopeRange;
            set {
                if (innerModel.KeptIsotopeRange == value) return;
                innerModel.KeptIsotopeRange = value;
                OnPropertyChanged(nameof(KeptIsotopeRange));
            }
        }

        public bool RemoveAfterPrecursor {
            get => innerModel.RemoveAfterPrecursor;
            set {
                if (innerModel.RemoveAfterPrecursor == value) return;
                innerModel.RemoveAfterPrecursor = value;
                OnPropertyChanged(nameof(RemoveAfterPrecursor));
            }
        }

        public bool KeepOriginalPrecursorIsotopes {
            get => innerModel.KeepOriginalPrecursorIsotopes;
            set {
                if (innerModel.KeepOriginalPrecursorIsotopes == value) return;
                innerModel.KeepOriginalPrecursorIsotopes = value;
                OnPropertyChanged(nameof(KeepOriginalPrecursorIsotopes));
            }
        }

        public AccuracyType AccuracyType {
            get => innerModel.AccuracyType;
            set {
                if (innerModel.AccuracyType == value) return;
                innerModel.AccuracyType = value;
                OnPropertyChanged(nameof(AccuracyType));
            }
        }

        public double TargetCE {
            get => innerModel.TargetCE;
            set {
                if (innerModel.TargetCE == value) return;
                innerModel.TargetCE = value;
                OnPropertyChanged(nameof(TargetCE));
            }
        }

        // proteomics param
        public bool IsDoAndromedaMs2Deconvolution {
            get => innerModel.IsDoAndromedaMs2Deconvolution;
            set {
                if (innerModel.IsDoAndromedaMs2Deconvolution == value) return;
                innerModel.IsDoAndromedaMs2Deconvolution = value;
                OnPropertyChanged(nameof(IsDoAndromedaMs2Deconvolution));
            }
        }

        public float AndromedaDelta {
            get => innerModel.AndromedaDelta;
            set {
                if (innerModel.AndromedaDelta == value) return;
                innerModel.AndromedaDelta = value;
                OnPropertyChanged(nameof(AndromedaDelta));
            }
        }

        public int AndromedaMaxPeaks {
            get => innerModel.AndromedaMaxPeaks;
            set {
                if (innerModel.AndromedaMaxPeaks == value) return;
                innerModel.AndromedaMaxPeaks = value;
                OnPropertyChanged(nameof(AndromedaMaxPeaks));
            }
        }

        public string FastaFilePath {
            get => innerModel.FastaFilePath;
            set {
                if (innerModel.FastaFilePath == value) return;
                innerModel.FastaFilePath = value;
                OnPropertyChanged(nameof(FastaFilePath));
            }
        }

        public List<Modification> VariableModifications {
            get => innerModel.VariableModifications;
            set {
                if (innerModel.VariableModifications == value) return;
                innerModel.VariableModifications = value;
                OnPropertyChanged(nameof(VariableModifications));
            }
        }

        public List<Modification> FixedModifications {
            get => innerModel.FixedModifications;
            set {
                if (innerModel.FixedModifications == value) return;
                innerModel.FixedModifications = value;
                OnPropertyChanged(nameof(FixedModifications));
            }
        }

        public List<Enzyme> EnzymesForDigestion {
            get => innerModel.EnzymesForDigestion;
            set {
                if (innerModel.EnzymesForDigestion == value) return;
                innerModel.EnzymesForDigestion = value;
                OnPropertyChanged(nameof(EnzymesForDigestion));
            }
        }

        public CollisionType CollistionType {
            get => innerModel.CollistionType;
            set {
                if (innerModel.CollistionType == value) return;
                innerModel.CollistionType = value;
                OnPropertyChanged(nameof(CollistionType));
            }
        }

        public float FalseDiscoveryRateForPeptide {
            get => innerModel.FalseDiscoveryRateForPeptide;
            set {
                if (innerModel.FalseDiscoveryRateForPeptide == value) return;
                innerModel.FalseDiscoveryRateForPeptide = value;
                OnPropertyChanged(nameof(FalseDiscoveryRateForPeptide));
            }
        }

        public float FalseDiscoveryRateForProtein {
            get => innerModel.FalseDiscoveryRateForProtein;
            set {
                if (innerModel.FalseDiscoveryRateForProtein == value) return;
                innerModel.FalseDiscoveryRateForProtein = value;
                OnPropertyChanged(nameof(FalseDiscoveryRateForProtein));
            }
        }


        // Post-alignment and filtering

        public float PeakCountFilter {
            get => innerModel.PeakCountFilter;
            set {
                if (innerModel.PeakCountFilter == value) return;
                innerModel.PeakCountFilter = value;
                OnPropertyChanged(nameof(PeakCountFilter));
            }
        }

        public bool IsForceInsertForGapFilling {
            get => innerModel.IsForceInsertForGapFilling;
            set {
                if (innerModel.IsForceInsertForGapFilling == value) return;
                innerModel.IsForceInsertForGapFilling = value;
                OnPropertyChanged(nameof(IsForceInsertForGapFilling));
            }
        }

        public float NPercentDetectedInOneGroup {
            get => innerModel.NPercentDetectedInOneGroup;
            set {
                if (innerModel.NPercentDetectedInOneGroup == value) return;
                innerModel.NPercentDetectedInOneGroup = value;
                OnPropertyChanged(nameof(NPercentDetectedInOneGroup));
            }
        }

        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange {
            get => innerModel.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange;
            set {
                if (innerModel.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange == value) return;
                innerModel.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = value;
                OnPropertyChanged(nameof(IsRemoveFeatureBasedOnBlankPeakHeightFoldChange));
            }
        }

        public float SampleMaxOverBlankAverage {
            get => innerModel.SampleMaxOverBlankAverage;
            set {
                if (innerModel.SampleMaxOverBlankAverage == value) return;
                innerModel.SampleMaxOverBlankAverage = value;
                OnPropertyChanged(nameof(SampleMaxOverBlankAverage));
            }
        }

        public float SampleAverageOverBlankAverage {
            get => innerModel.SampleAverageOverBlankAverage;
            set {
                if (innerModel.SampleAverageOverBlankAverage == value) return;
                innerModel.SampleAverageOverBlankAverage = value;
                OnPropertyChanged(nameof(SampleAverageOverBlankAverage));
            }
        }

        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking {
            get => innerModel.IsKeepRemovableFeaturesAndAssignedTagForChecking;
            set {
                if (innerModel.IsKeepRemovableFeaturesAndAssignedTagForChecking == value) return;
                innerModel.IsKeepRemovableFeaturesAndAssignedTagForChecking = value;
                OnPropertyChanged(nameof(IsKeepRemovableFeaturesAndAssignedTagForChecking));
            }
        }

        public bool IsKeepRefMatchedMetaboliteFeatures {
            get => innerModel.IsKeepRefMatchedMetaboliteFeatures;
            set {
                if (innerModel.IsKeepRefMatchedMetaboliteFeatures == value) return;
                innerModel.IsKeepRefMatchedMetaboliteFeatures = value;
                OnPropertyChanged(nameof(IsKeepRefMatchedMetaboliteFeatures));
            }
        }

        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get => innerModel.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            set {
                if (innerModel.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples == value) return;
                innerModel.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value;
                OnPropertyChanged(nameof(IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples));
            }
        }

        public BlankFiltering BlankFiltering {
            get => innerModel.BlankFiltering;
            set {
                if (innerModel.BlankFiltering == value) return;
                innerModel.BlankFiltering = value;
                OnPropertyChanged(nameof(BlankFiltering));
            }
        }

        public bool IsKeepSuggestedMetaboliteFeatures {
            get => innerModel.IsKeepSuggestedMetaboliteFeatures;
            set {
                if (innerModel.IsKeepSuggestedMetaboliteFeatures == value) return;
                innerModel.IsKeepSuggestedMetaboliteFeatures = value;
                OnPropertyChanged(nameof(IsKeepSuggestedMetaboliteFeatures));
            }
        }

        public float FoldChangeForBlankFiltering {
            get => innerModel.FoldChangeForBlankFiltering;
            set {
                if (innerModel.FoldChangeForBlankFiltering == value) return;
                innerModel.FoldChangeForBlankFiltering = value;
                OnPropertyChanged(nameof(FoldChangeForBlankFiltering));
            }
        }

        // Normalization option

        public bool IsNormalizeNone {
            get => innerModel.IsNormalizeNone;
            set {
                if (innerModel.IsNormalizeNone == value) return;
                innerModel.IsNormalizeNone = value;
                OnPropertyChanged(nameof(IsNormalizeNone));
            }
        }

        public bool IsNormalizeIS {
            get => innerModel.IsNormalizeIS;
            set {
                if (innerModel.IsNormalizeIS == value) return;
                innerModel.IsNormalizeIS = value;
                OnPropertyChanged(nameof(IsNormalizeIS));
            }
        }

        public bool IsNormalizeLowess {
            get => innerModel.IsNormalizeLowess;
            set {
                if (innerModel.IsNormalizeLowess == value) return;
                innerModel.IsNormalizeLowess = value;
                OnPropertyChanged(nameof(IsNormalizeLowess));
            }
        }

        public bool IsNormalizeIsLowess {
            get => innerModel.IsNormalizeIsLowess;
            set {
                if (innerModel.IsNormalizeIsLowess == value) return;
                innerModel.IsNormalizeIsLowess = value;
                OnPropertyChanged(nameof(IsNormalizeIsLowess));
            }
        }

        public bool IsNormalizeTic {
            get => innerModel.IsNormalizeTic;
            set {
                if (innerModel.IsNormalizeTic == value) return;
                innerModel.IsNormalizeTic = value;
                OnPropertyChanged(nameof(IsNormalizeTic));
            }
        }

        public bool IsNormalizeMTic {
            get => innerModel.IsNormalizeMTic;
            set {
                if (innerModel.IsNormalizeMTic == value) return;
                innerModel.IsNormalizeMTic = value;
                OnPropertyChanged(nameof(IsNormalizeMTic));
            }
        }

        public bool IsNormalizeSplash {
            get => innerModel.IsNormalizeSplash;
            set {
                if (innerModel.IsNormalizeSplash == value) return;
                innerModel.IsNormalizeSplash = value;
                OnPropertyChanged(nameof(IsNormalizeSplash));
            }
        }

        public double LowessSpan {
            get => innerModel.LowessSpan;
            set {
                if (innerModel.LowessSpan == value) return;
                innerModel.LowessSpan = value;
                OnPropertyChanged(nameof(LowessSpan));
            }
        }

        public bool IsBlankSubtract {
            get => innerModel.IsBlankSubtract;
            set {
                if (innerModel.IsBlankSubtract == value) return;
                innerModel.IsBlankSubtract = value;
                OnPropertyChanged(nameof(IsBlankSubtract));
            }
        }

        // Statistics

        public TransformMethod Transform {
            get => innerModel.Transform;
            set {
                if (innerModel.Transform == value) return;
                innerModel.Transform = value;
                OnPropertyChanged(nameof(Transform));
            }
        }

        public ScaleMethod Scale {
            get => innerModel.Scale;
            set {
                if (innerModel.Scale == value) return;
                innerModel.Scale = value;
                OnPropertyChanged(nameof(Scale));
            }
        }

        public int MaxComponent {
            get => innerModel.MaxComponent;
            set {
                if (innerModel.MaxComponent == value) return;
                innerModel.MaxComponent = value;
                OnPropertyChanged(nameof(MaxComponent));
            }
        }

        public TransformMethod TransformPls {
            get => innerModel.TransformPls;
            set {
                if (innerModel.TransformPls == value) return;
                innerModel.TransformPls = value;
                OnPropertyChanged(nameof(TransformPls));
            }
        }

        public ScaleMethod ScalePls {
            get => innerModel.ScalePls;
            set {
                if (innerModel.ScalePls == value) return;
                innerModel.ScalePls = value;
                OnPropertyChanged(nameof(ScalePls));
            }
        }

        public bool IsAutoFitPls {
            get => innerModel.IsAutoFitPls;
            set {
                if (innerModel.IsAutoFitPls == value) return;
                innerModel.IsAutoFitPls = value;
                OnPropertyChanged(nameof(IsAutoFitPls));
            }
        }

        public int ComponentPls {
            get => innerModel.ComponentPls;
            set {
                if (innerModel.ComponentPls == value) return;
                innerModel.ComponentPls = value;
                OnPropertyChanged(nameof(ComponentPls));
            }
        }

        public MultivariateAnalysisOption MultivariateAnalysisOption {
            get => innerModel.MultivariateAnalysisOption;
            set {
                if (innerModel.MultivariateAnalysisOption == value) return;
                innerModel.MultivariateAnalysisOption = value;
                OnPropertyChanged(nameof(MultivariateAnalysisOption));
            }
        }

        public bool IsIdentifiedImportedInStatistics {
            get => innerModel.IsIdentifiedImportedInStatistics;
            set {
                if (innerModel.IsIdentifiedImportedInStatistics == value) return;
                innerModel.IsIdentifiedImportedInStatistics = value;
                OnPropertyChanged(nameof(IsIdentifiedImportedInStatistics));
            }
        }

        public bool IsAnnotatedImportedInStatistics {
            get => innerModel.IsAnnotatedImportedInStatistics;
            set {
                if (innerModel.IsAnnotatedImportedInStatistics == value) return;
                innerModel.IsAnnotatedImportedInStatistics = value;
                OnPropertyChanged(nameof(IsAnnotatedImportedInStatistics));
            }
        }

        public bool IsUnknownImportedInStatistics {
            get => innerModel.IsUnknownImportedInStatistics;
            set {
                if (innerModel.IsUnknownImportedInStatistics == value) return;
                innerModel.IsUnknownImportedInStatistics = value;
                OnPropertyChanged(nameof(IsUnknownImportedInStatistics));
            }
        }

        // Mrmprobs export

        public float MpMs1Tolerance {
            get => innerModel.MpMs1Tolerance;
            set {
                if (innerModel.MpMs1Tolerance == value) return;
                innerModel.MpMs1Tolerance = value;
                OnPropertyChanged(nameof(MpMs1Tolerance));
            }
        }

        public float MpMs2Tolerance {
            get => innerModel.MpMs2Tolerance;
            set {
                if (innerModel.MpMs2Tolerance == value) return;
                innerModel.MpMs2Tolerance = value;
                OnPropertyChanged(nameof(MpMs2Tolerance));
            }
        }

        public float MpRtTolerance {
            get => innerModel.MpRtTolerance;
            set {
                if (innerModel.MpRtTolerance == value) return;
                innerModel.MpRtTolerance = value;
                OnPropertyChanged(nameof(MpRtTolerance));
            }
        }

        public int MpTopN {
            get => innerModel.MpTopN;
            set {
                if (innerModel.MpTopN == value) return;
                innerModel.MpTopN = value;
                OnPropertyChanged(nameof(MpTopN));
            }
        }

        public bool MpIsIncludeMsLevel1 {
            get => innerModel.MpIsIncludeMsLevel1;
            set {
                if (innerModel.MpIsIncludeMsLevel1 == value) return;
                innerModel.MpIsIncludeMsLevel1 = value;
                OnPropertyChanged(nameof(MpIsIncludeMsLevel1));
            }
        }

        public bool MpIsUseMs1LevelForQuant {
            get => innerModel.MpIsUseMs1LevelForQuant;
            set {
                if (innerModel.MpIsUseMs1LevelForQuant == value) return;
                innerModel.MpIsUseMs1LevelForQuant = value;
                OnPropertyChanged(nameof(MpIsUseMs1LevelForQuant));
            }
        }

        public bool MpIsFocusedSpotOutput {
            get => innerModel.MpIsFocusedSpotOutput;
            set {
                if (innerModel.MpIsFocusedSpotOutput == value) return;
                innerModel.MpIsFocusedSpotOutput = value;
                OnPropertyChanged(nameof(MpIsFocusedSpotOutput));
            }
        }

        public bool MpIsReferenceBaseOutput {
            get => innerModel.MpIsReferenceBaseOutput;
            set {
                if (innerModel.MpIsReferenceBaseOutput == value) return;
                innerModel.MpIsReferenceBaseOutput = value;
                OnPropertyChanged(nameof(MpIsReferenceBaseOutput));
            }
        }

        public bool MpIsExportOtherCandidates {
            get => innerModel.MpIsExportOtherCandidates;
            set {
                if (innerModel.MpIsExportOtherCandidates == value) return;
                innerModel.MpIsExportOtherCandidates = value;
                OnPropertyChanged(nameof(MpIsExportOtherCandidates));
            }
        }

        public float MpIdentificationScoreCutOff {
            get => innerModel.MpIdentificationScoreCutOff;
            set {
                if (innerModel.MpIdentificationScoreCutOff == value) return;
                innerModel.MpIdentificationScoreCutOff = value;
                OnPropertyChanged(nameof(MpIdentificationScoreCutOff));
            }
        }

        // molecular networking

        public double MnRtTolerance {
            get => innerModel.MnRtTolerance;
            set {
                if (innerModel.MnRtTolerance == value) return;
                innerModel.MnRtTolerance = value;
                OnPropertyChanged(nameof(MnRtTolerance));
            }
        }

        public double MnIonCorrelationSimilarityCutOff {
            get => innerModel.MnIonCorrelationSimilarityCutOff;
            set {
                if (innerModel.MnIonCorrelationSimilarityCutOff == value) return;
                innerModel.MnIonCorrelationSimilarityCutOff = value;
                OnPropertyChanged(nameof(MnIonCorrelationSimilarityCutOff));
            }
        }

        public double MnSpectrumSimilarityCutOff {
            get => innerModel.MnSpectrumSimilarityCutOff;
            set {
                if (innerModel.MnSpectrumSimilarityCutOff == value) return;
                innerModel.MnSpectrumSimilarityCutOff = value;
                OnPropertyChanged(nameof(MnSpectrumSimilarityCutOff));
            }
        }

        public double MnRelativeAbundanceCutOff {
            get => innerModel.MnRelativeAbundanceCutOff;
            set {
                if (innerModel.MnRelativeAbundanceCutOff == value) return;
                innerModel.MnRelativeAbundanceCutOff = value;
                OnPropertyChanged(nameof(MnRelativeAbundanceCutOff));
            }
        }

        public double MnMassTolerance {
            get => innerModel.MnMassTolerance;
            set {
                if (innerModel.MnMassTolerance == value) return;
                innerModel.MnMassTolerance = value;
                OnPropertyChanged(nameof(MnMassTolerance));
            }
        }

        public bool MnIsExportIonCorrelation {
            get => innerModel.MnIsExportIonCorrelation;
            set {
                if (innerModel.MnIsExportIonCorrelation == value) return;
                innerModel.MnIsExportIonCorrelation = value;
                OnPropertyChanged(nameof(MnIsExportIonCorrelation));
            }
        }

        // others

        public RetentionTimeCorrectionCommon RetentionTimeCorrectionCommon {
            get => innerModel.RetentionTimeCorrectionCommon;
            set {
                if (innerModel.RetentionTimeCorrectionCommon == value) return;
                innerModel.RetentionTimeCorrectionCommon = value;
                OnPropertyChanged(nameof(RetentionTimeCorrectionCommon));
            }
        }

        public List<MoleculeMsReference> CompoundListInTargetMode {
            get => innerModel.CompoundListInTargetMode;
            set {
                if (innerModel.CompoundListInTargetMode == value) return;
                innerModel.CompoundListInTargetMode = value;
                OnPropertyChanged(nameof(CompoundListInTargetMode));
            }
        }

        public List<StandardCompound> StandardCompounds {
            get => innerModel.StandardCompounds;
            set {
                if (innerModel.StandardCompounds == value) return;
                innerModel.StandardCompounds = value;
                OnPropertyChanged(nameof(StandardCompounds));
            }
        }

        public bool IsLabPrivate {
            get => innerModel.IsLabPrivate;
            set {
                if (innerModel.IsLabPrivate == value) return;
                innerModel.IsLabPrivate = value;
                OnPropertyChanged(nameof(IsLabPrivate));
            }
        }

        public bool IsLabPrivateVersionTada {
            get => innerModel.IsLabPrivateVersionTada;
            set {
                if (innerModel.IsLabPrivateVersionTada == value) return;
                innerModel.IsLabPrivateVersionTada = value;
                OnPropertyChanged(nameof(IsLabPrivateVersionTada));
            }
        }

        public bool QcAtLeastFilter {
            get => innerModel.QcAtLeastFilter;
            set {
                if (innerModel.QcAtLeastFilter == value) return;
                innerModel.QcAtLeastFilter = value;
                OnPropertyChanged(nameof(QcAtLeastFilter));
            }
        }

        //Tracking of isotope labeles

        public bool TrackingIsotopeLabels {
            get => innerModel.TrackingIsotopeLabels;
            set {
                if (innerModel.TrackingIsotopeLabels == value) return;
                innerModel.TrackingIsotopeLabels = value;
                OnPropertyChanged(nameof(TrackingIsotopeLabels));
            }
        }

        public bool UseTargetFormulaLibrary {
            get => innerModel.UseTargetFormulaLibrary;
            set {
                if (innerModel.UseTargetFormulaLibrary == value) return;
                innerModel.UseTargetFormulaLibrary = value;
                OnPropertyChanged(nameof(UseTargetFormulaLibrary));
            }
        }

        public IsotopeTrackingDictionary IsotopeTrackingDictionary {
            get => innerModel.IsotopeTrackingDictionary;
            set {
                if (innerModel.IsotopeTrackingDictionary == value) return;
                innerModel.IsotopeTrackingDictionary = value;
                OnPropertyChanged(nameof(IsotopeTrackingDictionary));
            }
        }

        public int NonLabeledReferenceID {
            get => innerModel.NonLabeledReferenceID;
            set {
                if (innerModel.NonLabeledReferenceID == value) return;
                innerModel.NonLabeledReferenceID = value;
                OnPropertyChanged(nameof(NonLabeledReferenceID));
            }
        }

        public bool SetFullyLabeledReferenceFile {
            get => innerModel.SetFullyLabeledReferenceFile;
            set {
                if (innerModel.SetFullyLabeledReferenceFile == value) return;
                innerModel.SetFullyLabeledReferenceFile = value;
                OnPropertyChanged(nameof(SetFullyLabeledReferenceFile));
            }
        }

        public int FullyLabeledReferenceID {
            get => innerModel.FullyLabeledReferenceID;
            set {
                if (innerModel.FullyLabeledReferenceID == value) return;
                innerModel.FullyLabeledReferenceID = value;
                OnPropertyChanged(nameof(FullyLabeledReferenceID));
            }
        }

        // corrdec

        public CorrDecParam CorrDecParam {
            get => innerModel.CorrDecParam;
            set {
                if (innerModel.CorrDecParam == value) return;
                innerModel.CorrDecParam = value;
                OnPropertyChanged(nameof(CorrDecParam));
            }
        }
    }
}
