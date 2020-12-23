using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AlignmentSpotProperty {

        // IDs to link properties
        [Key(0)]
        public int MasterAlignmentID { get; set; } // sequential IDs parsing all peak features extracted from an MS data
        [Key(1)]
        public int AlignmentID { get; set; } // sequential IDs from the same dimmension e.g. RT vs MZ or IM vs MZ
        [Key(2)]
        public int ParentAlignmentID { get; set; } // for LC-IM-MS/MS. The parent peak ID generating the daughter peak IDs

        // Basic property
        [Key(3)]
        public int RepresentativeFileID { get; set; }
        [Key(4)]
        public ChromXs TimesCenter { get; set; }
        [Key(5)]
        public double MassCenter { get; set; }
        [Key(6)]
        public double QuantMass { get; set; } // gcms
        [Key(7)]
        public int InternalStandardAlignmentID { get; set; } // masteralignmentid is inserted.
        [Key(8)]
        public List<AlignmentChromPeakFeature> AlignedPeakProperties { get; set; } = new List<AlignmentChromPeakFeature>();
        [Key(9)]
        public List<AlignmentSpotProperty> AlignmentDriftSpotFeatures { get; set; } = null;
        [Key(53)]
        public List<IsotopicPeak> IsotopicPeaks { get; set; } = new List<IsotopicPeak>(); // isotopes from representative file id

        // Ion property
        [Key(10)]
        public IonFeatureCharacter PeakCharacter { get; set; } = new IonFeatureCharacter();
        [Key(11)]
        public IonMode IonMode { get; set; }
        [Key(54)]
        public AdductIon AdductType { get; set; } = new AdductIon();

        // Annotation
        // set for IMoleculeProperty (for representative)
        [Key(12)]
        public string Name { get; set; } = string.Empty;
        [Key(13)]
        public Formula Formula { get; set; } = new Formula();
        [Key(14)]
        public string Ontology { get; set; } = string.Empty;
        [Key(15)]
        public string SMILES { get; set; } = string.Empty;
        [Key(16)]
        public string InChIKey { get; set; } = string.Empty;

        // ion physiochemical information
        [Key(17)]
        public double CollisionCrossSection { get; set; }

        // molecule annotation results
        // IDs to link properties
        //[Key(18)]
        //public int MspID { get; set; } // representative msp id
        [Key(19)]
        public Dictionary<int, List<int>> MSRawID2MspIDs { get; set; } = new Dictionary<int, List<int>>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS). ID list having the metabolite candidates exceeding the threshold
        //[Key(20)]
        //public int TextDbID { get; set; }// representative text id
        [Key(21)]
        public List<int> TextDbIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (optional)
        [Key(22)]
        public int IsotopeTrackTextDbID { get; set; }// representative text id
        [Key(23)]
        public Dictionary<int, MsScanMatchResult> MSRawID2MspBasedMatchResult { get; set; } = new Dictionary<int, MsScanMatchResult>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS).
        [Key(24)]
        public MsScanMatchResult TextDbBasedMatchResult { get; set; }
        [Key(25)]
        public string Comment { get; set; } = string.Empty;
        [Key(26)]
        public string AnnotationCode { get; set; } = string.Empty;
        [Key(27)]
        public string AnnotationCodeCorrDec { get; set; } = string.Empty;

        [IgnoreMember]
        public MsScanMatchResult MspBasedMatchResult { // get result having max score
            get {
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return null;
                else {
                    return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value)).Value;
                }
            }
        }

        [IgnoreMember]
        public int TextDbID {
            get {
                if (TextDbBasedMatchResult != null) return TextDbBasedMatchResult.LibraryID;
                else return -1;
            }
        }

        [IgnoreMember]
        public int MspID {
            get {
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return -1;
                else {
                    return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value.LibraryID)).LibraryID;
                }
            }
        }

        [IgnoreMember]
        public bool IsReferenceMatched {
            get {
                if (TextDbID >= 0) return true;
                if (MspID >= 0 && MSRawID2MspBasedMatchResult.Values.Any(n => n.IsSpectrumMatch)) return true;
                return false;
            }
        }

        [IgnoreMember]
        public bool IsAnnotationSuggested {
            get {
                return MspID >= 0 && !MSRawID2MspBasedMatchResult.Values.Any(n => n.IsSpectrumMatch);
            }
        }

        [IgnoreMember]
        public bool IsUnknown {
            get {
                return MspID < 0 && TextDbID < 0;
            }
        }


        [Key(28)]
        public List<int> CorrDecLibraryIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (for AIF project)

        [Key(29)]
        public float AnovaPvalue;
        [Key(30)]
        public float FoldChange;

        [Key(31)]
        public float HeightAverage { get; set; }
        [Key(32)]
        public float HeightMin { get; set; }
        [Key(33)]
        public float HeightMax { get; set; }
        [Key(34)]
        public float PeakWidthAverage { get; set; }

        [Key(35)]
        public float SignalToNoiseAve { get; set; }
        [Key(36)]
        public float SignalToNoiseMax { get; set; }
        [Key(37)]
        public float SignalToNoiseMin { get; set; }
        [Key(38)]
        public float EstimatedNoiseAve { get; set; }
        [Key(39)]
        public float EstimatedNoiseMax { get; set; }
        [Key(40)]
        public float EstimatedNoiseMin { get; set; }

        [Key(41)]
        public ChromXs TimesMin { get; set; }
        [Key(42)]
        public ChromXs TimesMax { get; set; }

        [Key(43)]
        public double MassMin { get; set; }
        [Key(44)]
        public double MassMax { get; set; }


        // others
        [Key(45)]
        public FeatureFilterStatus FeatureFilterStatus { get; set; }
        [Key(46)]
        public bool IsManuallyModifiedForQuant { get; set; }
        [Key(47)]
        public IonAbundanceUnit IonAbundanceUnit { get; set; }
        [Key(48)]
        public bool IsManuallyModifiedForAnnotation { get; set; }
        [Key(49)]
        public float FillParcentage { get; set; }
        [Key(50)]
        public float RelativeAmplitudeValue { get; set; }
        [Key(51)]
        public float MonoIsotopicPercentage { get; set; }
        [Key(55)]
        public bool IsMsmsAssigned { get; set; }

        [Key(52)]
        public List<AlignmentSpotVariableCorrelation> AlignmentSpotVariableCorrelations { get; set; } = new List<AlignmentSpotVariableCorrelation>();
    }

    [MessagePackObject]
    public class AlignmentSpotVariableCorrelation {
        [Key(0)]
        public int CorrelateAlignmentID { get; set; }
        [Key(1)]
        public float CorrelationScore { get; set; }
    }
}
