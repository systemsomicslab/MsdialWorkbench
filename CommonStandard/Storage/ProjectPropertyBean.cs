using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public enum TargetOmics { Metablomics, Lipidomics }
    public enum Ionization { ESI, EI }
    public enum ExportSpectraFileFormat { mgf, msp, txt, mat, ms }
    public enum ExportspectraType { profile, centroid, deconvoluted }

    [DataContract]
    [MessagePackObject]
    public class ProjectPropertyBean
    {
        [DataMember]
        private DateTime projectDate;
        [DataMember]
        private DateTime finalSavedDate;

        [DataMember]
        private string projectFolderPath;
        [DataMember]
        private string projectFilePath;
        [DataMember]
        private string experimentFilePath;
        [DataMember]
        private string libraryFilePath;
        [DataMember]
        private string postIdentificationLibraryFilePath;
        [DataMember]
        private string msAnnotationTagsFolderPath;
        [DataMember]
        private string targetFormulaLibraryFilePath;

        private SeparationType separationType;
        [DataMember]
        private MethodType methodType;
        [DataMember]
        private DataType dataType;
        [DataMember]
        private DataType dataTypeMS2;
        [DataMember]
        private IonMode ionMode;
        [DataMember]
        private TargetOmics targetOmics;
        [DataMember]
        private Ionization ionization;
        [DataMember]
        private bool checkAIF;
        [DataMember]
        private List<int> ms2LevelIdList;
        private bool isBoxPlotForAlignmentResult;

        [DataMember]
        private string instrumentType;
        [DataMember]
        private string instrument;
        [DataMember]
        private string authors;
        [DataMember]
        private string license;
        [DataMember]
        private string collisionEnergy;
        [DataMember]
        private List<float> collisionEnergyList;
        [DataMember]
        private string comment;

        [DataMember]
        private Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean;
        [DataMember]
        private Dictionary<int, string> fileID_ClassName;
        [DataMember]
        private Dictionary<int, int> fileID_RdamID;
        [DataMember]
        private Dictionary<int, AnalysisFileType> fileID_AnalysisFileType;
        [DataMember]
        private bool isLabPrivateVersion;
        [DataMember]
        private bool isLabPrivateVersionTada;
        [DataMember]
        private string compoundListInTargetModePath;

        private Dictionary<string, int> classnameToOrder;
        private Dictionary<string, List<byte>> classnameToColorBytes;  // [0] R [1] G [2] B [3] A

        // alignment result export
        private string exportFolderPath;
        private bool rawDatamatrix;
        private bool normalizedDatamatrix;
        private bool representativeSpectra;
        private bool sampleAxisDeconvolution;
        private bool peakIdMatrix;
        private bool retentionTimeMatrix;
        private bool mzMatrix;
        private bool msmsIncludedMatrix;
        private bool deconvolutedPeakAreaDataMatrix;
        private bool uniqueMs;
        private bool peakareaMatrix;
        private bool isFilteringOptionForIsotopeLabeledTracking;
        private bool parameter;
        private bool gnpsExport;
        private bool molecularNetworkingExport;
        private bool blankFilter;
        private bool isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
        private bool snMatrixExport;
        private bool isExportedAsMzTabM;

        private float massToleranceForMs2Export;
        private ExportSpectraFileFormat exportSpectraFileFormat;
        private ExportspectraType exportSpectraType;

        public ProjectPropertyBean()
        {
            this.projectDate = DateTime.Now;

            this.targetOmics = TargetOmics.Metablomics;
            this.methodType = MethodType.ddMSMS; 
            this.dataType = DataType.Profile;
            this.dataTypeMS2 = DataType.Profile;
            this.ionMode = IonMode.Positive;
            this.ionization = Ionization.ESI;
            this.separationType = SeparationType.Chromatography;
            this.experimentID_AnalystExperimentInformationBean = new Dictionary<int, AnalystExperimentInformationBean>();
            this.fileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>();
            this.fileID_ClassName = new Dictionary<int, string>();
            this.fileID_RdamID = new Dictionary<int, int>();
            this.ms2LevelIdList = new List<int>();
            this.collisionEnergyList = new List<float>();

            this.isLabPrivateVersion = false;
            this.isLabPrivateVersionTada = false;

            this.isBoxPlotForAlignmentResult = false;
            this.rawDatamatrix = true;
            this.exportSpectraFileFormat = ExportSpectraFileFormat.msp;
            this.exportSpectraType = ExportspectraType.deconvoluted;
            this.massToleranceForMs2Export = 0.05F;

            this.classnameToColorBytes = new Dictionary<string, List<byte>>();
            this.classnameToOrder = new Dictionary<string, int>();
        }

        [Key(0)]
        public DateTime ProjectDate
        {
            get { return projectDate; }
            set { projectDate = value; }
        }

        [Key(1)]
        public DateTime FinalSavedDate
        {
            get { return finalSavedDate; }
            set { finalSavedDate = value; }
        }

        [Key(2)]
        public Dictionary<int, AnalystExperimentInformationBean> ExperimentID_AnalystExperimentInformationBean
        {
            get { return experimentID_AnalystExperimentInformationBean; }
            set { experimentID_AnalystExperimentInformationBean = value; }
        }

        [Key(3)]
        public Dictionary<int, string> FileID_ClassName
        {
            get { return fileID_ClassName; }
            set { fileID_ClassName = value; }
        }

        [Key(4)]
        public Dictionary<int, int> FileID_RdamID
        {
            get { return fileID_RdamID; }
            set { fileID_RdamID = value; }
        }

        [Key(5)]
        public Dictionary<int, AnalysisFileType> FileID_AnalysisFileType
        {
            get { return fileID_AnalysisFileType; }
            set { fileID_AnalysisFileType = value; }
        }

        [Key(6)]
        public string ProjectFolderPath
        {
            get { return projectFolderPath; }
            set { projectFolderPath = value; }
        }

        [Key(7)]
        public string ProjectFilePath
        {
            get { return projectFilePath; }
            set { projectFilePath = value; }
        }

        [Key(8)]
        public string ExperimentFilePath
        {
            get { return experimentFilePath; }
            set { experimentFilePath = value; }
        }

        [Key(9)]
        public string LibraryFilePath
        {
            get { return libraryFilePath; }
            set { libraryFilePath = value; }
        }

        [Key(10)]
        public string PostIdentificationLibraryFilePath
        {
            get { return postIdentificationLibraryFilePath; }
            set { postIdentificationLibraryFilePath = value; }
        }

        [Key(11)]
        public string MsAnnotationTagsFolderPath
        {
            get { return msAnnotationTagsFolderPath; }
            set { msAnnotationTagsFolderPath = value; }
        }

        [Key(12)]
        public string TargetFormulaLibraryFilePath
        {
            get { return targetFormulaLibraryFilePath; }
            set { targetFormulaLibraryFilePath = value; }
        }

        [Key(13)]
        public string InstrumentType
        {
            get { return instrumentType; }
            set { instrumentType = value; }
        }

        [Key(14)]
        public string Instrument
        {
            get { return instrument; }
            set { instrument = value; }
        }

        [Key(15)]
        public string Authors
        {
            get { return authors; }
            set { authors = value; }
        }

        [Key(16)]
        public string License
        {
            get { return license; }
            set { license = value; }
        }

        [Key(17)]
        public string CollisionEnergy
        {
            get { return collisionEnergy; }
            set { collisionEnergy = value; }
        }

        [Key(18)]
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        [Key(19)]
        public MethodType MethodType
        {
            get { return methodType; }
            set { methodType = value; }
        }

        [Key(20)]
        public DataType DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        [Key(21)]
        public DataType DataTypeMS2
        {
            get { return dataTypeMS2; }
            set { dataTypeMS2 = value; }
        }

        [Key(22)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(23)]
        public TargetOmics TargetOmics
        {
            get { return targetOmics; }
            set { targetOmics = value; }
        }

        [Key(24)]
        public Ionization Ionization
        {
            get { return ionization; }
            set { ionization = value; }
        }

        [Key(25)]
        public bool CheckAIF {
            get { return checkAIF; }
            set { checkAIF = value; }
        }

        [Key(26)]
        public List<int> Ms2LevelIdList {
            get { return ms2LevelIdList; }
            set { ms2LevelIdList = value; }
        }

        [Key(27)]
        public List<float> CollisionEnergyList {
            get { return collisionEnergyList; }
            set { collisionEnergyList = value; }
        }

        [Key(28)]
        public bool IsLabPrivateVersion {
            get {
                return isLabPrivateVersion;
            }

            set {
                isLabPrivateVersion = value;
            }
        }

        [Key(29)]
        public bool IsLabPrivateVersionTada {
            get {
                return isLabPrivateVersionTada;
            }

            set {
                isLabPrivateVersionTada = value;
            }
        }

        [Key(30)]
        public string CompoundListInTargetModePath {
            get {
                return compoundListInTargetModePath;
            }

            set {
                compoundListInTargetModePath = value;
            }
        }

        [Key(31)]
        public bool IsBoxPlotForAlignmentResult {
            get {
                return isBoxPlotForAlignmentResult;
            }

            set {
                isBoxPlotForAlignmentResult = value;
            }
        }

        [Key(32)]
        public Dictionary<string, int> ClassnameToOrder {
            get {
                return classnameToOrder;
            }

            set {
                classnameToOrder = value;
            }
        }

        [Key(33)]
        public Dictionary<string, List<byte>> ClassnameToColorBytes {
            get {
                return classnameToColorBytes;
            }

            set {
                classnameToColorBytes = value;
            }
        }

        [Key(34)]
        public SeparationType SeparationType {
            get {
                return separationType;
            }
            set {
                separationType = value;
            }
        }

        [Key(35)]
        public string ExportFolderPath {
            get {
                return exportFolderPath;
            }

            set {
                exportFolderPath = value;
            }
        }

        [Key(36)]
        public bool RawDatamatrix {
            get {
                return rawDatamatrix;
            }

            set {
                rawDatamatrix = value;
            }
        }

        [Key(37)]
        public bool NormalizedDatamatrix {
            get {
                return normalizedDatamatrix;
            }

            set {
                normalizedDatamatrix = value;
            }
        }

        [Key(38)]
        public bool RepresentativeSpectra {
            get {
                return representativeSpectra;
            }

            set {
                representativeSpectra = value;
            }
        }

        [Key(39)]
        public bool SampleAxisDeconvolution {
            get {
                return sampleAxisDeconvolution;
            }

            set {
                sampleAxisDeconvolution = value;
            }
        }

        [Key(40)]
        public bool PeakIdMatrix {
            get {
                return peakIdMatrix;
            }

            set {
                peakIdMatrix = value;
            }
        }

        [Key(41)]
        public bool RetentionTimeMatrix {
            get {
                return retentionTimeMatrix;
            }

            set {
                retentionTimeMatrix = value;
            }
        }

        [Key(42)]
        public bool MzMatrix {
            get {
                return mzMatrix;
            }

            set {
                mzMatrix = value;
            }
        }

        [Key(43)]
        public bool MsmsIncludedMatrix {
            get {
                return msmsIncludedMatrix;
            }

            set {
                msmsIncludedMatrix = value;
            }
        }

        [Key(44)]
        public bool DeconvolutedPeakAreaDataMatrix {
            get {
                return deconvolutedPeakAreaDataMatrix;
            }

            set {
                deconvolutedPeakAreaDataMatrix = value;
            }
        }

        [Key(45)]
        public bool UniqueMs {
            get {
                return uniqueMs;
            }

            set {
                uniqueMs = value;
            }
        }

        [Key(46)]
        public bool PeakareaMatrix {
            get {
                return peakareaMatrix;
            }

            set {
                peakareaMatrix = value;
            }
        }

        [Key(47)]
        public bool IsFilteringOptionForIsotopeLabeledTracking {
            get {
                return isFilteringOptionForIsotopeLabeledTracking;
            }

            set {
                isFilteringOptionForIsotopeLabeledTracking = value;
            }
        }

        [Key(48)]
        public bool Parameter {
            get {
                return parameter;
            }

            set {
                parameter = value;
            }
        }

        [Key(49)]
        public bool GnpsExport {
            get {
                return gnpsExport;
            }

            set {
                gnpsExport = value;
            }
        }

        [Key(50)]
        public bool MolecularNetworkingExport {
            get {
                return molecularNetworkingExport;
            }

            set {
                molecularNetworkingExport = value;
            }
        }

        [Key(51)]
        public bool BlankFilter {
            get {
                return blankFilter;
            }

            set {
                blankFilter = value;
            }
        }

        [Key(52)]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get {
                return isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            }

            set {
                isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value;
            }
        }

        [Key(53)]
        public bool SnMatrixExport {
            get {
                return snMatrixExport;
            }

            set {
                snMatrixExport = value;
            }
        }

        [Key(54)]
        public float MassToleranceForMs2Export {
            get {
                return massToleranceForMs2Export;
            }

            set {
                massToleranceForMs2Export = value;
            }
        }

        [Key(55)]
        public ExportSpectraFileFormat ExportSpectraFileFormat {
            get {
                return exportSpectraFileFormat;
            }

            set {
                exportSpectraFileFormat = value;
            }
        }

        [Key(56)]
        public ExportspectraType ExportSpectraType {
            get {
                return exportSpectraType;
            }

            set {
                exportSpectraType = value;
            }
        }

        [Key(57)]
        public bool IsExportedAsMzTabM {
            get {
                return isExportedAsMzTabM;
            }

            set {
                isExportedAsMzTabM = value;
            }
        }
    }
}
