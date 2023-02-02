using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public enum MassToleranceType { Da, Ppm }
    public enum FseaNonsignificantDef { OntologySpace, ReverseSpectrum, LowAbundantIons }

    /// <summary>
    /// This is the storage of analysis parameters used in MS-FINDER program.
    /// </summary>
    public class AnalysisParamOfMsfinder
    {
        private bool isLewisAndSeniorCheck;
        private double mass1Tolerance;
        private double mass2Tolerance;
        private MassToleranceType massTolType;

        private double isotopicAbundanceTolerance;

        private CoverRange coverRange;
        private bool isElementProbabilityCheck;
        private bool isOcheck;
        private bool isNcheck;
        private bool isPcheck;
        private bool isScheck;
        private bool isFcheck;
        private bool isClCheck;
        private bool isBrCheck;
        private bool isIcheck;

        private bool isSiCheck;
        private bool isNitrogenRule;

        private bool canExcuteMS2AdductSearch;
        private double formulaScoreCutOff;
        private int formulaMaximumReportNumber;

        private bool isNeutralLossCheck;

        private int treeDepth;
        private double relativeAbundanceCutOff;

        private double structureScoreCutOff;
        private int structureMaximumReportNumber;

        private DatabaseQuery databaseQuery;
        private bool isPubChemNeverUse;
        private bool isPubChemOnlyUseForNecessary;
        private bool isPubChemAllTime;

        private bool isUserDefinedDB;
        private string userDefinedDbFilePath;

        private bool isMinesNeverUse;
        private bool isMinesOnlyUseForNecessary;
        private bool isMinesAllTime;

        private bool isUseEiFragmentDB;

        //batch job
        private bool isAllProcess;
        private bool isFormulaFinder;
        private bool isStructureFinder;
        private int tryTopNmolecularFormulaSearch;

        //labeled compound info
        private double cLabelMass;
        private double hLabelMass;
        private double nLabelMass;
        private double oLabelMass;
        private double pLabelMass;
        private double sLabelMass;
        private double fLabelMass;
        private double clLabelMass;
        private double brLabelMass;
        private double iLabelMass;
        private double siLabelMass;

        //TMS-derivative compound
        private bool isTmsMeoxDerivative;
        private int minimumTmsCount;
        private int minimumMeoxCount;

        //Spectral database search
        private bool isRunSpectralDbSearch;
        private bool isRunInSilicoFragmenterSearch;
        private bool isPrecursorOrientedSearch;
        private bool isUseInternalExperimentalSpectralDb;
        private bool isUseInSilicoSpectralDbForLipids;
        private bool isUseUserDefinedSpectralDb;
        private string userDefinedSpectralDbFilePath;
        private SolventType solventType;
        private double massRangeMin;
        private double massRangeMax;
        private LipidQueryBean lipidQueryBean;

        //retention time setting for structure elucidation
        private bool isUsePredictedRtForStructureElucidation;
        private bool isUseRtInchikeyLibrary;
        private bool isUseXlogpPrediction;
        private string rtInChIKeyDictionaryFilepath;
        private string rtSmilesDictionaryFilepath;
        private double coeff_RtPrediction;
        private double intercept_RtPrediction;
        private double rtToleranceForStructureElucidation;
        private string rtPredictionSummaryReport;
        private bool isUseRtForFilteringCandidates;
       
        //retention time setting for spectral searching
        private bool isUseExperimentalRtForSpectralSearching;
        private RetentionType retentionType;
        private double rtToleranceForSpectralSearching;

        //collision cross section
        private double ccsToleranceForStructureElucidation;
        private double ccsToleranceForSpectralSearching;
        private bool isUsePredictedCcsForStructureElucidation;
        private bool isUseCcsInchikeyAdductLibrary;
        private bool isUseExperimentalCcsForSpectralSearching;
        private bool isUseCcsForFilteringCandidates;
        private string ccsAdductInChIKeyDictionaryFilepath;

        //fsea parameter
        private double fseaRelativeAbundanceCutOff;// %
        private FseaNonsignificantDef fseanonsignificantDef;
        private double fseaPvalueCutOff;

        //msfinder molecular networking
        private bool isMmnLocalCytoscape; // The node and edge files are created if this is true. else, the json file including node and edge is created for cytoscape.js
        private bool isMmnMsdialOutput; // the IDs in Comment field are used when this property is true.  else, the title field is used for the ID.
        private bool isMmnFormulaBioreaction;
        private bool isMmnRetentionRestrictionUsed;
        private bool isMmnOntologySimilarityUsed;
        private bool isMmnSelectedFileCentricProcess;
        private double mmnMassTolerance; // general parameters
        private double mmnRelativeCutoff; // general parameters
        private double mmnMassSimilarityCutOff; // MS/MS network cut off %
        private double mmnRtTolerance;
        private double mmnRtToleranceForReaction; // formula bioreaction parameter
        private double mmnOntologySimilarityCutOff; // ontology network cut off %
        private string mmnOutputFolderPath;

        //timeout
        private double formulaPredictionTimeOut;
        private double structurePredictionTimeOut;

        //cut off
        private double scoreCutOffForSpectralMatch;

        public AnalysisParamOfMsfinder()
        {
            isLewisAndSeniorCheck = true;
            massTolType = MassToleranceType.Da;
            mass1Tolerance = 0.001;
            mass2Tolerance = 0.01;
            isotopicAbundanceTolerance = 20;
            coverRange = CoverRange.CommonRange;
            isElementProbabilityCheck = true;
            isOcheck = true;
            isNcheck = true;
            isPcheck = true;
            isScheck = true;
            isFcheck = false;
            isClCheck = false;
            isBrCheck = false;
            isIcheck = false;
            isSiCheck = false;
            isNitrogenRule = true;

            canExcuteMS2AdductSearch = false;
            formulaScoreCutOff = 70;
            formulaMaximumReportNumber = 100;

            ccsToleranceForStructureElucidation = 10.0;
            isUsePredictedCcsForStructureElucidation = false;
            isUseCcsInchikeyAdductLibrary = false;
            ccsAdductInChIKeyDictionaryFilepath = string.Empty;
            isUseExperimentalCcsForSpectralSearching = true;
            ccsToleranceForSpectralSearching = 10.0;
            isUseCcsForFilteringCandidates = true;

            isNeutralLossCheck = true;

            treeDepth = 2;
            relativeAbundanceCutOff = 1;

            structureScoreCutOff = 60;
            structureMaximumReportNumber = 100;

            isAllProcess = false;
            isFormulaFinder = true;
            isStructureFinder = false;
            tryTopNmolecularFormulaSearch = 5;

            databaseQuery = new DatabaseQuery()
            {
                Chebi = true,
                Ymdb = true,
                Unpd = true,
                Smpdb = true,
                Pubchem = true,
                Hmdb = true,
                Plantcyc = true,
                Knapsack = true,
                Bmdb = true,
                Drugbank = true,
                Ecmdb = true,
                Foodb = true,
                T3db = true,
                Stoff = true,
                Nanpdb = true,
                Blexp = true,
                Npa = true,
                Coconut = true
            };

            isUserDefinedDB = false;
            userDefinedDbFilePath = string.Empty;
            
            isPubChemAllTime = false;
            isPubChemNeverUse = true;
            isPubChemOnlyUseForNecessary = false;

            isMinesOnlyUseForNecessary = true;
            isMinesNeverUse = false;
            isMinesAllTime = false;
            isUseEiFragmentDB = false;

            cLabelMass = 0;
            hLabelMass = 0;
            nLabelMass = 0;
            pLabelMass = 0;
            oLabelMass = 0;
            sLabelMass = 0;
            fLabelMass = 0;
            clLabelMass = 0;
            brLabelMass = 0;
            iLabelMass = 0;
            siLabelMass = 0;

            isTmsMeoxDerivative = false;
            minimumMeoxCount = 0;
            minimumTmsCount = 1;

            isRunSpectralDbSearch = false;
            isRunInSilicoFragmenterSearch = true;
            isPrecursorOrientedSearch = true;

            isUseInternalExperimentalSpectralDb = true;
            isUseInSilicoSpectralDbForLipids = false;
            isUseUserDefinedSpectralDb = false;

            massRangeMax = 2000;
            massRangeMin = 0;
            retentionType = OsakaUniv.RetentionType.RT;

            isUsePredictedRtForStructureElucidation = false;
            isUseRtInchikeyLibrary = true;
            isUseXlogpPrediction = false;
            rtInChIKeyDictionaryFilepath = string.Empty;
            rtSmilesDictionaryFilepath = string.Empty;
            coeff_RtPrediction = -1;
            intercept_RtPrediction = -1;
            rtToleranceForStructureElucidation = 2.5; //min
            rtPredictionSummaryReport = string.Empty;
            isUseRtForFilteringCandidates = false;

            isUseExperimentalRtForSpectralSearching = false;
            rtToleranceForSpectralSearching = 0.5; //min

            // fsea parameter
            fseaRelativeAbundanceCutOff = 5.0; // %
            fseanonsignificantDef = FseaNonsignificantDef.OntologySpace;
            fseaPvalueCutOff = 1.0; // %

            // msfinder molecular networking
            isMmnLocalCytoscape = true;
            isMmnMsdialOutput = false;
            isMmnFormulaBioreaction = false;
            isMmnRetentionRestrictionUsed = false;
            isMmnOntologySimilarityUsed = true;
            IsMmnSelectedFileCentricProcess = true;

            mmnMassTolerance = 0.025;
            mmnRelativeCutoff = 1.0;
            mmnMassSimilarityCutOff = 75.0;
            mmnRtTolerance = 100;
            mmnRtToleranceForReaction = 0.5;
            mmnOntologySimilarityCutOff = 90.0;
            mmnOutputFolderPath = string.Empty;

            formulaPredictionTimeOut = -1.0; // means no timeout if number is existed, the unit is [min]
            structurePredictionTimeOut = -1.0; // means no timeout 

            structureScoreCutOff = 0;
            scoreCutOffForSpectralMatch = 80;
        }

        #region Properties
        public bool IsLewisAndSeniorCheck
        {
            get { return isLewisAndSeniorCheck; }
            set { isLewisAndSeniorCheck = value; }
        }

        public MassToleranceType MassTolType
        {
            get { return massTolType; }
            set { massTolType = value; }
        }

        public double Mass1Tolerance
        {
            get { return mass1Tolerance; }
            set { mass1Tolerance = value; }
        }

        public double Mass2Tolerance
        {
            get { return mass2Tolerance; }
            set { mass2Tolerance = value; }
        }

        public double IsotopicAbundanceTolerance
        {
            get { return isotopicAbundanceTolerance; }
            set { isotopicAbundanceTolerance = value; }
        }

        public CoverRange CoverRange
        {
            get { return coverRange; }
            set { coverRange = value; }
        }

        public bool IsElementProbabilityCheck
        {
            get { return isElementProbabilityCheck; }
            set { isElementProbabilityCheck = value; }
        }

        public bool IsOcheck
        {
            get { return isOcheck; }
            set { isOcheck = value; }
        }

        public bool IsNcheck
        {
            get { return isNcheck; }
            set { isNcheck = value; }
        }

        public bool IsPcheck
        {
            get { return isPcheck; }
            set { isPcheck = value; }
        }

        public bool IsScheck
        {
            get { return isScheck; }
            set { isScheck = value; }
        }

        public bool IsFcheck
        {
            get { return isFcheck; }
            set { isFcheck = value; }
        }

        public bool IsClCheck
        {
            get { return isClCheck; }
            set { isClCheck = value; }
        }

        public bool IsBrCheck
        {
            get { return isBrCheck; }
            set { isBrCheck = value; }
        }

        public double MassRangeMin
        {
            get { return massRangeMin; }
            set { massRangeMin = value; }
        }

        public double MassRangeMax
        {
            get { return massRangeMax; }
            set { massRangeMax = value; }
        }

        public bool IsIcheck
        {
            get { return isIcheck; }
            set { isIcheck = value; }
        }

        public bool IsSiCheck
        {
            get { return isSiCheck; }
            set { isSiCheck = value; }
        }

        public bool IsNitrogenRule
        {
            get { return isNitrogenRule; }
            set { isNitrogenRule = value; }
        }

        public double FormulaScoreCutOff
        {
            get { return formulaScoreCutOff; }
            set { formulaScoreCutOff = value; }
        }
        public bool CanExcuteMS1AdductSearch { get; set; } = false;

        public bool CanExcuteMS2AdductSearch {
            get { return canExcuteMS2AdductSearch; }
            set { canExcuteMS2AdductSearch = value; }
        }

        public int FormulaMaximumReportNumber
        {
            get { return formulaMaximumReportNumber; }
            set { formulaMaximumReportNumber = value; }
        }

        public bool IsNeutralLossCheck
        {
            get { return isNeutralLossCheck; }
            set { isNeutralLossCheck = value; }
        }

        public int TreeDepth
        {
            get { return treeDepth; }
            set { treeDepth = value; }
        }

        public double RelativeAbundanceCutOff
        {
            get { return relativeAbundanceCutOff; }
            set { relativeAbundanceCutOff = value; }
        }

        public double StructureScoreCutOff
        {
            get { return structureScoreCutOff; }
            set { structureScoreCutOff = value; }
        }

        public int StructureMaximumReportNumber
        {
            get { return structureMaximumReportNumber; }
            set { structureMaximumReportNumber = value; }
        }

        public bool IsUserDefinedDB
        {
            get { return isUserDefinedDB; }
            set { isUserDefinedDB = value; }
        }

        public string UserDefinedDbFilePath
        {
            get { return userDefinedDbFilePath; }
            set { userDefinedDbFilePath = value; }
        }

        public bool IsAllProcess
        {
            get { return isAllProcess; }
            set { isAllProcess = value; }
        }

        public bool IsUseEiFragmentDB
        {
            get { return isUseEiFragmentDB; }
            set { isUseEiFragmentDB = value; }
        }

        public bool IsFormulaFinder
        {
            get { return isFormulaFinder; }
            set { isFormulaFinder = value; }
        }

        public bool IsStructureFinder
        {
            get { return isStructureFinder; }
            set { isStructureFinder = value; }
        }

        public int TryTopNmolecularFormulaSearch
        {
            get { return tryTopNmolecularFormulaSearch; }
            set { tryTopNmolecularFormulaSearch = value; }
        }

        public DatabaseQuery DatabaseQuery
        {
            get { return databaseQuery; }
            set { databaseQuery = value; }
        }

        public bool IsPubChemNeverUse
        {
            get { return isPubChemNeverUse; }
            set { isPubChemNeverUse = value; }
        }

        public bool IsPubChemOnlyUseForNecessary
        {
            get { return isPubChemOnlyUseForNecessary; }
            set { isPubChemOnlyUseForNecessary = value; }
        }

        public bool IsPubChemAllTime
        {
            get { return isPubChemAllTime; }
            set { isPubChemAllTime = value; }
        }

        public bool IsMinesNeverUse
        {
            get { return isMinesNeverUse; }
            set { isMinesNeverUse = value; }
        }

        public bool IsMinesOnlyUseForNecessary
        {
            get { return isMinesOnlyUseForNecessary; }
            set { isMinesOnlyUseForNecessary = value; }
        }

        public bool IsMinesAllTime
        {
            get { return isMinesAllTime; }
            set { isMinesAllTime = value; }
        }

        public double CLabelMass
        {
            get { return cLabelMass; }
            set { cLabelMass = value; }
        }

        public double HLabelMass
        {
            get { return hLabelMass; }
            set { hLabelMass = value; }
        }

        public double NLabelMass
        {
            get { return nLabelMass; }
            set { nLabelMass = value; }
        }

        public double OLabelMass
        {
            get { return oLabelMass; }
            set { oLabelMass = value; }
        }

        public double PLabelMass
        {
            get { return pLabelMass; }
            set { pLabelMass = value; }
        }

        public double SLabelMass
        {
            get { return sLabelMass; }
            set { sLabelMass = value; }
        }

        public double FLabelMass
        {
            get { return fLabelMass; }
            set { fLabelMass = value; }
        }

        public double ClLabelMass
        {
            get { return clLabelMass; }
            set { clLabelMass = value; }
        }

        public double BrLabelMass
        {
            get { return brLabelMass; }
            set { brLabelMass = value; }
        }

        public double ILabelMass
        {
            get { return iLabelMass; }
            set { iLabelMass = value; }
        }

        public double SiLabelMass
        {
            get { return siLabelMass; }
            set { siLabelMass = value; }
        }

        public bool IsTmsMeoxDerivative {
            get { return isTmsMeoxDerivative; }
            set { isTmsMeoxDerivative = value; }
        }

        public int MinimumTmsCount {
            get { return minimumTmsCount; }
            set { minimumTmsCount = value; }
        }

        public int MinimumMeoxCount {
            get { return minimumMeoxCount; }
            set { minimumMeoxCount = value; }
        }

        public bool IsRunSpectralDbSearch
        {
            get { return isRunSpectralDbSearch; }
            set { isRunSpectralDbSearch = value; }
        }

        public bool IsRunInSilicoFragmenterSearch
        {
            get { return isRunInSilicoFragmenterSearch; }
            set { isRunInSilicoFragmenterSearch = value; }
        }

        public bool IsPrecursorOrientedSearch
        {
            get { return isPrecursorOrientedSearch; }
            set { isPrecursorOrientedSearch = value; }
        }

        public bool IsUseInternalExperimentalSpectralDb
        {
            get { return isUseInternalExperimentalSpectralDb; }
            set { isUseInternalExperimentalSpectralDb = value; }
        }

        public bool IsUseInSilicoSpectralDbForLipids
        {
            get { return isUseInSilicoSpectralDbForLipids; }
            set { isUseInSilicoSpectralDbForLipids = value; }
        }

        public bool IsUseUserDefinedSpectralDb
        {
            get { return isUseUserDefinedSpectralDb; }
            set { isUseUserDefinedSpectralDb = value; }
        }

        public string UserDefinedSpectralDbFilePath
        {
            get { return userDefinedSpectralDbFilePath; }
            set { userDefinedSpectralDbFilePath = value; }
        }

        public SolventType SolventType
        {
            get { return solventType; }
            set { solventType = value; }
        }

        public RetentionType RetentionType
        {
            get { return retentionType; }
            set { retentionType = value; }
        }

        public bool IsUsePredictedRtForStructureElucidation {
            get {
                return isUsePredictedRtForStructureElucidation;
            }

            set {
                isUsePredictedRtForStructureElucidation = value;
            }
        }

        public string RtSmilesDictionaryFilepath {
            get {
                return rtSmilesDictionaryFilepath;
            }

            set {
                rtSmilesDictionaryFilepath = value;
            }
        }

        public double Coeff_RtPrediction {
            get {
                return coeff_RtPrediction;
            }

            set {
                coeff_RtPrediction = value;
            }
        }

        public double Intercept_RtPrediction {
            get {
                return intercept_RtPrediction;
            }

            set {
                intercept_RtPrediction = value;
            }
        }

        public double RtToleranceForStructureElucidation {
            get {
                return rtToleranceForStructureElucidation;
            }

            set {
                rtToleranceForStructureElucidation = value;
            }
        }

        public bool IsUseExperimentalRtForSpectralSearching {
            get {
                return isUseExperimentalRtForSpectralSearching;
            }

            set {
                isUseExperimentalRtForSpectralSearching = value;
            }
        }

        public double RtToleranceForSpectralSearching {
            get {
                return rtToleranceForSpectralSearching;
            }

            set {
                rtToleranceForSpectralSearching = value;
            }
        }

        public string RtPredictionSummaryReport {
            get {
                return rtPredictionSummaryReport;
            }

            set {
                rtPredictionSummaryReport = value;
            }
        }

        public double FseaRelativeAbundanceCutOff {
            get {
                return fseaRelativeAbundanceCutOff;
            }

            set {
                fseaRelativeAbundanceCutOff = value;
            }
        }

        public FseaNonsignificantDef FseanonsignificantDef {
            get {
                return fseanonsignificantDef;
            }

            set {
                fseanonsignificantDef = value;
            }
        }

        public double FseaPvalueCutOff {
            get {
                return fseaPvalueCutOff;
            }

            set {
                fseaPvalueCutOff = value;
            }
        }

        public bool IsMmnLocalCytoscape {
            get {
                return isMmnLocalCytoscape;
            }

            set {
                isMmnLocalCytoscape = value;
            }
        }

        public bool IsMmnMsdialOutput {
            get {
                return isMmnMsdialOutput;
            }

            set {
                isMmnMsdialOutput = value;
            }
        }

        public bool IsMmnFormulaBioreaction {
            get {
                return isMmnFormulaBioreaction;
            }

            set {
                isMmnFormulaBioreaction = value;
            }
        }

        public bool IsMmnRetentionRestrictionUsed {
            get {
                return isMmnRetentionRestrictionUsed;
            }

            set {
                isMmnRetentionRestrictionUsed = value;
            }
        }

        public bool IsMmnOntologySimilarityUsed {
            get {
                return isMmnOntologySimilarityUsed;
            }

            set {
                isMmnOntologySimilarityUsed = value;
            }
        }

        public double MmnMassTolerance {
            get {
                return mmnMassTolerance;
            }

            set {
                mmnMassTolerance = value;
            }
        }

        public double MmnRelativeCutoff {
            get {
                return mmnRelativeCutoff;
            }

            set {
                mmnRelativeCutoff = value;
            }
        }

        public double MmnMassSimilarityCutOff {
            get {
                return mmnMassSimilarityCutOff;
            }

            set {
                mmnMassSimilarityCutOff = value;
            }
        }

        public double MmnRtTolerance {
            get {
                return mmnRtTolerance;
            }

            set {
                mmnRtTolerance = value;
            }
        }

        public double MmnOntologySimilarityCutOff {
            get {
                return mmnOntologySimilarityCutOff;
            }

            set {
                mmnOntologySimilarityCutOff = value;
            }
        }

        public string MmnOutputFolderPath {
            get {
                return mmnOutputFolderPath;
            }

            set {
                mmnOutputFolderPath = value;
            }
        }

        public double MmnRtToleranceForReaction {
            get {
                return mmnRtToleranceForReaction;
            }

            set {
                mmnRtToleranceForReaction = value;
            }
        }

        public bool IsMmnSelectedFileCentricProcess {
            get {
                return isMmnSelectedFileCentricProcess;
            }

            set {
                isMmnSelectedFileCentricProcess = value;
            }
        }

        public bool IsUseRtInchikeyLibrary {
            get {
                return isUseRtInchikeyLibrary;
            }

            set {
                isUseRtInchikeyLibrary = value;
            }
        }

        public bool IsUseXlogpPrediction {
            get {
                return isUseXlogpPrediction;
            }

            set {
                isUseXlogpPrediction = value;
            }
        }

        public string RtInChIKeyDictionaryFilepath {
            get {
                return rtInChIKeyDictionaryFilepath;
            }

            set {
                rtInChIKeyDictionaryFilepath = value;
            }
        }

        public double FormulaPredictionTimeOut {
            get {
                return formulaPredictionTimeOut;
            }

            set {
                formulaPredictionTimeOut = value;
            }
        }

        public double StructurePredictionTimeOut {
            get {
                return structurePredictionTimeOut;
            }

            set {
                structurePredictionTimeOut = value;
            }
        }

        public bool IsUseRtForFilteringCandidates {
            get {
                return isUseRtForFilteringCandidates;
            }

            set {
                isUseRtForFilteringCandidates = value;
            }
        }

        public List<AdductIon> MS1PositiveAdductIonList { get; set; } = new List<AdductIon>();
        public List<AdductIon> MS2PositiveAdductIonList { get; set; } = new List<AdductIon>();
        public List<AdductIon> MS1NegativeAdductIonList { get; set; } = new List<AdductIon>();
        public List<AdductIon> MS2NegativeAdductIonList { get; set; } = new List<AdductIon>();

        public double CcsToleranceForStructureElucidation {
            get {
                return ccsToleranceForStructureElucidation;
            }

            set {
                ccsToleranceForStructureElucidation = value;
            }
        }

        public LipidQueryBean LipidQueryBean {
            get {
                return lipidQueryBean;
            }

            set {
                lipidQueryBean = value;
            }
        }

        public bool IsUsePredictedCcsForStructureElucidation {
            get {
                return isUsePredictedCcsForStructureElucidation;
            }

            set {
                isUsePredictedCcsForStructureElucidation = value;
            }
        }

        public bool IsUseCcsInchikeyAdductLibrary {
            get {
                return isUseCcsInchikeyAdductLibrary;
            }

            set {
                isUseCcsInchikeyAdductLibrary = value;
            }
        }

        public string CcsAdductInChIKeyDictionaryFilepath {
            get {
                return ccsAdductInChIKeyDictionaryFilepath;
            }

            set {
                ccsAdductInChIKeyDictionaryFilepath = value;
            }
        }

        public bool IsUseExperimentalCcsForSpectralSearching {
            get {
                return isUseExperimentalCcsForSpectralSearching;
            }

            set {
                isUseExperimentalCcsForSpectralSearching = value;
            }
        }

        public double CcsToleranceForSpectralSearching {
            get {
                return ccsToleranceForSpectralSearching;
            }

            set {
                ccsToleranceForSpectralSearching = value;
            }
        }

        public bool IsUseCcsForFilteringCandidates {
            get {
                return isUseCcsForFilteringCandidates;
            }

            set {
                isUseCcsForFilteringCandidates = value;
            }
        }

        public double ScoreCutOffForSpectralMatch {
            get {
                return scoreCutOffForSpectralMatch;
            }

            set {
                scoreCutOffForSpectralMatch = value;
            }
        }

        #endregion
    }
}
