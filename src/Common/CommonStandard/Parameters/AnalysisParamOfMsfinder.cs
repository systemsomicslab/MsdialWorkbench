using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Parameter {
    
    public enum FseaNonsignificantDef { OntologySpace, ReverseSpectrum, LowAbundantIons }
    /// <summary>
    /// This is the storage of analysis parameters used in MS-FINDER program.
    /// </summary>
    public class AnalysisParamOfMsfinder
    {
        public AnalysisParamOfMsfinder()
        {
            IsLewisAndSeniorCheck = true;
            MassTolType = MassToleranceType.Da;
            Mass1Tolerance = 0.001;
            Mass2Tolerance = 0.01;
            IsotopicAbundanceTolerance = 20;
            CoverRange = CoverRange.CommonRange;
            IsElementProbabilityCheck = true;
            IsOcheck = true;
            IsNcheck = true;
            IsPcheck = true;
            IsScheck = true;
            IsFcheck = false;
            IsClCheck = false;
            IsBrCheck = false;
            IsIcheck = false;
            IsSiCheck = false;
            IsNitrogenRule = true;

            CanExcuteMS2AdductSearch = false;
            FormulaScoreCutOff = 70;
            FormulaMaximumReportNumber = 100;

            CcsToleranceForStructureElucidation = 10.0;
            IsUsePredictedCcsForStructureElucidation = false;
            IsUseCcsInchikeyAdductLibrary = false;
            CcsAdductInChIKeyDictionaryFilepath = string.Empty;
            IsUseExperimentalCcsForSpectralSearching = true;
            CcsToleranceForSpectralSearching = 10.0;
            IsUseCcsForFilteringCandidates = true;

            IsNeutralLossCheck = true;

            TreeDepth = 2;
            RelativeAbundanceCutOff = 1;

            StructureScoreCutOff = 60;
            StructureMaximumReportNumber = 100;

            IsAllProcess = false;
            IsFormulaFinder = false;
            IsStructureFinder = false;
            TryTopNmolecularFormulaSearch = 5;

            DatabaseQuery = new DatabaseQuery()
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

            IsUserDefinedDB = false;
            UserDefinedDbFilePath = string.Empty;
            
            IsPubChemAllTime = false;
            IsPubChemNeverUse = true;
            IsPubChemOnlyUseForNecessary = false;

            IsMinesOnlyUseForNecessary = true;
            IsMinesNeverUse = false;
            IsMinesAllTime = false;
            IsUseEiFragmentDB = false;

            CLabelMass = 0;
            HLabelMass = 0;
            NLabelMass = 0;
            PLabelMass = 0;
            OLabelMass = 0;
            SLabelMass = 0;
            FLabelMass = 0;
            ClLabelMass = 0;
            BrLabelMass = 0;
            ILabelMass = 0;
            SiLabelMass = 0;

            IsTmsMeoxDerivative = false;
            MinimumMeoxCount = 0;
            MinimumTmsCount = 1;

            IsRunSpectralDbSearch = false;
            IsRunInSilicoFragmenterSearch = true;
            IsPrecursorOrientedSearch = true;

            IsUseInternalExperimentalSpectralDb = true;
            IsUseInSilicoSpectralDbForLipids = false;
            IsUseUserDefinedSpectralDb = false;

            MassRangeMax = 2000;
            MassRangeMin = 0;
            RetentionType = RetentionType.RT;

            IsUsePredictedRtForStructureElucidation = false;
            IsUseRtInchikeyLibrary = true;
            IsUseXlogpPrediction = false;
            RtInChIKeyDictionaryFilepath = string.Empty;
            RtSmilesDictionaryFilepath = string.Empty;
            Coeff_RtPrediction = -1;
            Intercept_RtPrediction = -1;
            RtToleranceForStructureElucidation = 2.5; //min
            RtPredictionSummaryReport = string.Empty;
            IsUseRtForFilteringCandidates = false;

            IsUseExperimentalRtForSpectralSearching = false;
            RtToleranceForSpectralSearching = 0.5; //min

            // fsea parameter
            FseaRelativeAbundanceCutOff = 5.0; // %
            FseanonsignificantDef = FseaNonsignificantDef.OntologySpace;
            FseaPvalueCutOff = 1.0; // %

            // msfinder molecular networking
            IsMmnLocalCytoscape = true;
            IsMmnMsdialOutput = false;
            IsMmnFormulaBioreaction = false;
            IsMmnRetentionRestrictionUsed = false;
            IsMmnOntologySimilarityUsed = true;
            IsMmnSelectedFileCentricProcess = true;

            MmnMassTolerance = 0.025;
            MmnRelativeCutoff = 1.0;
            MmnMassSimilarityCutOff = 75.0;
            MmnRtTolerance = 100;
            MmnRtToleranceForReaction = 0.5;
            MmnOntologySimilarityCutOff = 90.0;
            MmnOutputFolderPath = string.Empty;

            FormulaPredictionTimeOut = -1.0; // means no timeout if number is existed, the unit is [min]
            StructurePredictionTimeOut = -1.0; // means no timeout 

            StructureScoreCutOff = 0;
            ScoreCutOffForSpectralMatch = 80;
        }

        #region Properties
        // basic
        public MassToleranceType MassTolType { get; set; }
        public double Mass1Tolerance { get; set; }
        public double Mass2Tolerance { get; set; }
        public double IsotopicAbundanceTolerance { get; set; }
        public double MassRangeMin { get; set; }
        public double MassRangeMax { get; set; }
        public double RelativeAbundanceCutOff { get; set; }
        public SolventType SolventType { get; set; }
        public RetentionType RetentionType { get; set; }

        // formula generator
        public CoverRange CoverRange { get; set; }
        public bool IsLewisAndSeniorCheck { get; set; }
        public bool IsElementProbabilityCheck { get; set; }
        public bool IsOcheck { get; set; }
        public bool IsNcheck { get; set; }
        public bool IsPcheck { get; set; }
        public bool IsScheck { get; set; }
        public bool IsFcheck { get; set; }
        public bool IsClCheck { get; set; }
        public bool IsBrCheck { get; set; }
        public bool IsIcheck { get; set; }
        public bool IsSiCheck { get; set; }
        public bool IsNitrogenRule { get; set; }
        public double FormulaScoreCutOff { get; set; }
        public bool CanExcuteMS1AdductSearch { get; set; } = false;
        public bool CanExcuteMS2AdductSearch { get; set; }
        public int FormulaMaximumReportNumber { get; set; }
        public bool IsNeutralLossCheck { get; set; }
        
        // structure finder
        public int TreeDepth { get; set; }
        public double StructureScoreCutOff { get; set; }
        public int StructureMaximumReportNumber { get; set; }
        public bool IsUserDefinedDB { get; set; }
        public string UserDefinedDbFilePath { get; set; }
        public bool IsAllProcess { get; set; }
        public bool IsUseEiFragmentDB { get; set; }
        public int TryTopNmolecularFormulaSearch { get; set; }

        //batch job
        public bool IsFormulaFinder { get; set; }
        public bool IsStructureFinder { get; set; }
        public DatabaseQuery DatabaseQuery { get; set; }
        public bool IsPubChemNeverUse { get; set; }
        public bool IsPubChemOnlyUseForNecessary { get; set; }
        public bool IsPubChemAllTime { get; set; }
        public bool IsMinesNeverUse { get; set; }
        public bool IsMinesOnlyUseForNecessary { get; set; }
        public bool IsMinesAllTime { get; set; }


        //labeled compound info
        public double CLabelMass { get; set; }
        public double HLabelMass { get; set; }
        public double NLabelMass { get; set; }
        public double OLabelMass { get; set; }
        public double PLabelMass { get; set; }
        public double SLabelMass { get; set; }
        public double FLabelMass { get; set; }
        public double ClLabelMass { get; set; }
        public double BrLabelMass { get; set; }
        public double ILabelMass { get; set; }
        public double SiLabelMass { get; set; }

        //TMS-derivative compound
        public bool IsTmsMeoxDerivative { get; set; }
        public int MinimumTmsCount { get; set; }
        public int MinimumMeoxCount { get; set; }

        //Spectral database search
        public bool IsRunSpectralDbSearch { get; set; }
        public bool IsRunInSilicoFragmenterSearch { get; set; }
        public bool IsPrecursorOrientedSearch { get; set; }
        public bool IsUseInternalExperimentalSpectralDb { get; set; }
        public bool IsUseInSilicoSpectralDbForLipids { get; set; }
        public bool IsUseUserDefinedSpectralDb { get; set; }
        public string UserDefinedSpectralDbFilePath { get; set; }
        public LipidQueryBean LipidQueryBean { get; set; }
        public double ScoreCutOffForSpectralMatch { get; set; }

        //retention time setting for structure elucidation
        public bool IsUsePredictedRtForStructureElucidation { get; set; }
        public string RtSmilesDictionaryFilepath { get; set; }
        public double Coeff_RtPrediction { get; set; }
        public double Intercept_RtPrediction { get; set; }
        public double RtToleranceForStructureElucidation { get; set; }
        public bool IsUseRtInchikeyLibrary { get; set; }
        public bool IsUseXlogpPrediction { get; set; }
        public string RtInChIKeyDictionaryFilepath { get; set; }
        public bool IsUseRtForFilteringCandidates { get; set; }

        //retention time setting for spectral searching
        public bool IsUseExperimentalRtForSpectralSearching { get; set; }
        public double RtToleranceForSpectralSearching { get; set; }
        public string RtPredictionSummaryReport { get; set; }

        // FSEA parameter
        public double FseaRelativeAbundanceCutOff { get; set; }
        public FseaNonsignificantDef FseanonsignificantDef { get; set; }
        public double FseaPvalueCutOff { get; set; }

        //msfinder molecular networking
        public bool IsMmnLocalCytoscape { get; set; } // The node and edge files are created if this is true. else, the json file including node and edge is created for cytoscape.js
        public bool IsMmnMsdialOutput { get; set; } // the IDs in Comment field are used when this property is true.  else, the title field is used for the ID.
        public bool IsMmnFormulaBioreaction { get; set; }
        public bool IsMmnRetentionRestrictionUsed { get; set; }
        public bool IsMmnOntologySimilarityUsed { get; set; }
        public double MmnMassTolerance { get; set; } // general parameters
        public double MmnRelativeCutoff { get; set; } // general parameters
        public double MmnMassSimilarityCutOff { get; set; } // MS/MS network cut off %
        public double MmnRtTolerance { get; set; }
        public double MmnOntologySimilarityCutOff { get; set; } // ontology network cut off %
        public string MmnOutputFolderPath { get; set; }
        public double MmnRtToleranceForReaction { get; set; } // formula bioreaction parameter
        public bool IsMmnSelectedFileCentricProcess { get; set; }

        //timeout
        public double FormulaPredictionTimeOut { get; set; }
        public double StructurePredictionTimeOut { get; set; }

        // pos/neg adduct ion list
        public List<AdductIon> MS1PositiveAdductIonList { get; set; } = new List<AdductIon>();
        public List<AdductIon> MS2PositiveAdductIonList { get; set; } = new List<AdductIon>();
        public List<AdductIon> MS1NegativeAdductIonList { get; set; } = new List<AdductIon>();
        public List<AdductIon> MS2NegativeAdductIonList { get; set; } = new List<AdductIon>();

        //collision cross section
        public double CcsToleranceForStructureElucidation { get; set; }
        public bool IsUsePredictedCcsForStructureElucidation { get; set; }
        public bool IsUseCcsInchikeyAdductLibrary { get; set; }
        public string CcsAdductInChIKeyDictionaryFilepath { get; set; }
        public bool IsUseExperimentalCcsForSpectralSearching { get; set; }
        public double CcsToleranceForSpectralSearching { get; set; }
        public bool IsUseCcsForFilteringCandidates { get; set; }
       
        #endregion
    }
}
