using Accord.Statistics.Testing;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Normalize;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AlignmentSpotProperty : IMSIonProperty, IMoleculeProperty, IChromatogramPeak, IAnnotatedObject, INormalizationTarget {

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
        public List<AlignmentChromPeakFeature> AlignedPeakProperties {
            get => AlignedPeakPropertiesTask.Result;
            set => AlignedPeakPropertiesTask = Task.FromResult(value);
        }
        [IgnoreMember]
        public Task<List<AlignmentChromPeakFeature>> AlignedPeakPropertiesTask { get; set; } = Task.FromResult(new List<AlignmentChromPeakFeature>());
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
        public AdductIon AdductType { get; [Obsolete("Use SetAdductType")] set; } = AdductIon.Default;

        public void SetAdductType(AdductIon adduct) {
            AdductType = adduct;
            PeakCharacter.AdductType = adduct;
            PeakCharacter.Charge = adduct.ChargeNumber;
        }

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
        [Key(60)]
        public string Protein { get; set; } = string.Empty;
        [Key(61)]
        public int ProteinGroupID { get; set; } = -1;

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
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) {
                    return null;
                }
                else {
                    return MSRawID2MspBasedMatchResult.Values.Argmax(n => n.TotalScore);
                }
            }
        }

        [IgnoreMember]
        public int TextDbID {
            get {
                if (TextDbBasedMatchResult != null) {
                    return TextDbBasedMatchResult.LibraryID;
                }
                else {
                    return -1;
                }
            }
        }

        [IgnoreMember]
        public int MspID {
            get {
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) {
                    return -1;
                }
                else {
                    return MSRawID2MspBasedMatchResult.Values.Argmax(n => n.TotalScore).LibraryID;
                }
            }
        }

        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (MatchResults.IsManuallyModifiedRepresentative) {
                return !MatchResults.IsUnknown;
            }
            if (TextDbBasedMatchResult != null) {
                return true;
            }
            if (MspBasedMatchResult != null && MspBasedMatchResult.IsSpectrumMatch) {
                return true;
            }
            return MatchResults.IsReferenceMatched(evaluator);
        }

        public bool IsAnnotationSuggested(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (MatchResults.IsManuallyModifiedRepresentative) {
                return false;
            }
            else if (TextDbBasedMatchResult != null) {
                return false;
            }
            else if (MspBasedMatchResult != null && MspBasedMatchResult.IsSpectrumMatch) {
                return false;
            }
            else if (MspBasedMatchResult != null && MspBasedMatchResult.IsPrecursorMzMatch) {
                return true;
            }
            return MatchResults.IsAnnotationSuggested(evaluator);
        }

        [IgnoreMember]
        public bool IsUnknown {
            get {
                if (MatchResults.IsManuallyModifiedRepresentative && MatchResults.IsUnknown) {
                    return true;
                }
                if (MatchResults.IsUnknown && (TextDbBasedMatchResult == null || MspBasedMatchResult == null)) {
                    return true;
                }
                return false;
            }
        }

        [Key(56)]
        public MsScanMatchResultContainer MatchResults {
            get => matchResults ?? (matchResults = new MsScanMatchResultContainer());
            set => matchResults = value;
        }
        private MsScanMatchResultContainer matchResults;

        [Key(28)]
        public List<int> CorrDecLibraryIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (for AIF project)

        [Key(29)]
        public float AnovaPvalue;
        [Key(30)]
        public float FoldChange;

        public void CalculateAnovaPvalue(IReadOnlyDictionary<int, string> id2class) {
            var targets = AlignedPeakProperties
                .Where(peak => id2class.ContainsKey(peak.FileID))
                .GroupBy(peak => id2class[peak.FileID])
                .ToArray();
            if (targets.Length <= 1 || targets.All(group => group.Count() == 1)) {
                AnovaPvalue = 0f;
                return;
            }
            var inputs = targets.Select(group => group.Select(peak => peak.PeakHeightTop).ToArray()).ToArray();
            var oneWayAnova = new OneWayAnova(inputs);
            AnovaPvalue = (float)oneWayAnova.FTest.PValue;
        }

        public void CalculateFoldChange(IReadOnlyDictionary<int, string> id2class) {
            var aves = AlignedPeakProperties
                .Where(peak => id2class.ContainsKey(peak.FileID))
                .GroupBy(peak => id2class[peak.FileID])
                .Select(group => group.Average(peak => peak.PeakHeightTop))
                .ToArray();
            if (!aves.Any()) {
                FoldChange = 0f;
                return;
            }
            FoldChange = (float)(aves.Max() / Math.Max(1, aves.Min()));
        }

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
        public FeatureFilterStatus FeatureFilterStatus { get; set; } = new FeatureFilterStatus();
        [Key(46)]
        public bool IsManuallyModifiedForQuant { get; set; }
        [Key(47)]
        public IonAbundanceUnit IonAbundanceUnit { get; set; }
        [IgnoreMember]
        public bool IsManuallyModifiedForAnnotation {
            get {
                if (MatchResults?.Representative is MsScanMatchResult result) {
                    return (result.Source & SourceType.Manual) != SourceType.None;
                }
                return false;
            }
        }
        [Key(49)]
        public float FillParcentage { get; set; }
        [Key(50)]
        public float RelativeAmplitudeValue { get; set; }
        [Key(51)]
        public float MonoIsotopicPercentage { get; set; }
        [IgnoreMember]
        public bool IsMsmsAssigned => AlignedPeakProperties.Any(peak => peak.IsMsmsAssigned);

        [Key(52)]
        public List<AlignmentSpotVariableCorrelation> AlignmentSpotVariableCorrelations { get; set; } = new List<AlignmentSpotVariableCorrelation>();

        // Post curation result
        [IgnoreMember]
        public bool IsFilteredByPostCurator { get => IsBlankFilteredByPostCurator || IsBlankGhostFilteredByPostCurator || IsMzFilteredByPostCurator; }
        [Key(58)]
        public bool IsBlankFilteredByPostCurator { get; set; } = false;
        [Key(65)]
        public bool IsRmdFilteredByPostCurator { get; set; } = false;
        [Key(64)]
        public bool IsRsdFilteredByPostCurator { get; set; } = false;
        [Key(63)]
        public bool IsBlankGhostFilteredByPostCurator { get; set; } = false;
        [Key(62)]
        public bool IsMzFilteredByPostCurator { get; set; } = false;
        public bool IsMultiLayeredData() {
            if (AlignmentDriftSpotFeatures.IsEmptyOrNull()) return false;
            return true;
        }
        [Key(59)]
        public int MSDecResultIdUsed { get; set; } = -1;

        public int GetMSDecResultID() {
            if (MSDecResultIdUsed == -1) {
                if (IsMultiLayeredData())
                    return MasterAlignmentID;
                else
                    return AlignmentID;
            }
            else {
                return MSDecResultIdUsed;
            }
        }

        [IgnoreMember]
        public PeakSpotTagCollection TagCollection { get; } = new PeakSpotTagCollection();

        public AlignmentSpotProperty Clone(ref int masterId, int alignmentId) {
            var driftSpots = new List<AlignmentSpotProperty>(AlignmentDriftSpotFeatures.Count);
            foreach (var driftSpot in AlignmentDriftSpotFeatures) {
                driftSpots.Add(driftSpot.Clone(ref masterId, driftSpot.AlignmentID));
            }
            var spot = new AlignmentSpotProperty
            {
                MasterAlignmentID = masterId++,
                AlignmentID = alignmentId,
                ParentAlignmentID = ParentAlignmentID,
                RepresentativeFileID = RepresentativeFileID,
                TimesCenter = new ChromXs(TimesCenter.RT, TimesCenter.RI, TimesCenter.Drift, TimesCenter.Mz, TimesCenter.MainType),
                MassCenter = MassCenter,
                QuantMass = QuantMass,
                InternalStandardAlignmentID = InternalStandardAlignmentID,
                AlignedPeakProperties = AlignedPeakPropertiesTask.Result.Select(peak => peak.Clone()).ToList(),
                AlignmentDriftSpotFeatures = driftSpots,
                IsotopicPeaks = IsotopicPeaks?.Select(p => new IsotopicPeak(p)).ToList(),
                PeakCharacter = new IonFeatureCharacter(PeakCharacter),
                IonMode = IonMode,
                AdductType = AdductType,
                Name = Name,
                Formula = Formula,
                Ontology = Ontology,
                SMILES = SMILES,
                InChIKey = InChIKey,
                Protein = Protein,
                ProteinGroupID = ProteinGroupID,
                CollisionCrossSection = CollisionCrossSection,
                MSRawID2MspIDs = MSRawID2MspIDs?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList()),
                TextDbIDs = TextDbIDs?.ToList(),
                IsotopeTrackTextDbID = IsotopeTrackTextDbID,
                MSRawID2MspBasedMatchResult = MSRawID2MspBasedMatchResult?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                TextDbBasedMatchResult = TextDbBasedMatchResult,
                Comment = Comment,
                AnnotationCode = AnnotationCode,
                AnnotationCodeCorrDec = AnnotationCodeCorrDec,
                matchResults = new MsScanMatchResultContainer(matchResults),
                CorrDecLibraryIDs = CorrDecLibraryIDs?.ToList(),
                AnovaPvalue = AnovaPvalue,
                FoldChange = FoldChange,
                HeightAverage = HeightAverage,
                HeightMin = HeightMin,
                HeightMax = HeightMax,
                PeakWidthAverage = PeakWidthAverage,
                SignalToNoiseAve = SignalToNoiseAve,
                SignalToNoiseMax = SignalToNoiseMax,
                SignalToNoiseMin = SignalToNoiseMin,
                EstimatedNoiseAve = EstimatedNoiseAve,
                EstimatedNoiseMax = EstimatedNoiseMax,
                EstimatedNoiseMin = EstimatedNoiseMin,
                TimesMin = new ChromXs(TimesMin.RT, TimesMin.RI, TimesMin.Drift, TimesMin.Mz, TimesMin.MainType),
                TimesMax = new ChromXs(TimesMax.RT, TimesMax.RI, TimesMax.Drift, TimesMax.Mz, TimesMax.MainType),
                MassMin = MassMin,
                MassMax = MassMax,
                FeatureFilterStatus = new FeatureFilterStatus(FeatureFilterStatus),
                IsManuallyModifiedForQuant = IsManuallyModifiedForQuant,
                IonAbundanceUnit = IonAbundanceUnit,
                FillParcentage = FillParcentage,
                RelativeAmplitudeValue = RelativeAmplitudeValue,
                MonoIsotopicPercentage = MonoIsotopicPercentage,
                AlignmentSpotVariableCorrelations = AlignmentSpotVariableCorrelations?.Select(c => new AlignmentSpotVariableCorrelation(c)).ToList(),
                IsBlankFilteredByPostCurator = IsBlankFilteredByPostCurator,
                IsRmdFilteredByPostCurator = IsRmdFilteredByPostCurator,
                IsRsdFilteredByPostCurator = IsRsdFilteredByPostCurator,
                IsBlankGhostFilteredByPostCurator = IsBlankGhostFilteredByPostCurator,
                IsMzFilteredByPostCurator = IsMzFilteredByPostCurator,
                MSDecResultIdUsed = MSDecResultIdUsed,
            };
            spot.SetAdductType(AdductType);
            return spot;
        }

        // IMSProperty
        ChromXs IMSProperty.ChromXs {
            get => TimesCenter;
            set => TimesCenter = value;
        }
        IonMode IMSProperty.IonMode {
            get => IonMode;
            set => IonMode = value;
        }
        double IMSProperty.PrecursorMz {
            get => MassCenter;
            set => MassCenter = value;
        }

        // ISpectrumPeak
        double ISpectrumPeak.Intensity {
            get => HeightAverage;
            set => HeightAverage = (float)value;
        }

        double ISpectrumPeak.Mass {
            get => MassCenter;
            set => MassCenter = value;
        }

        // IChromatogramPeak
        int IChromatogramPeak.ID {
            get => MasterAlignmentID;
        }

        ChromXs IChromatogramPeak.ChromXs {
            get => TimesCenter;
            set => TimesCenter = value;
        }

        // INormalizationTarget
        int INormalizationTarget.Id => MasterAlignmentID;

        int INormalizationTarget.InternalStandardId {
            get => InternalStandardAlignmentID;
            set => InternalStandardAlignmentID = value;
        }

        IReadOnlyList<INormalizationTarget> INormalizationTarget.Children => AlignmentDriftSpotFeatures;

        IReadOnlyList<INormalizableValue> INormalizationTarget.Values => AlignedPeakProperties;

        MoleculeMsReference INormalizationTarget.RetriveReference(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return refer.Refer(MatchResults.Representative);
        }
    }

    [MessagePackObject]
    public class AlignmentSpotVariableCorrelation {
        [SerializationConstructor]
        public AlignmentSpotVariableCorrelation() {
            
        }

        public AlignmentSpotVariableCorrelation(AlignmentSpotVariableCorrelation source) {
            CorrelateAlignmentID = source.CorrelateAlignmentID;
            CorrelationScore = source.CorrelationScore;
        }

        [Key(0)]
        public int CorrelateAlignmentID { get; set; }
        [Key(1)]
        public float CorrelationScore { get; set; }
    }
}
