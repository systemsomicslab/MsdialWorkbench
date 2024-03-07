using Accord.Diagnostics;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Utility;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class ChromatogramPeakFeature : IChromatogramPeakFeature, IChromatogramPeak, IMoleculeMsProperty, IMSIonProperty, IAnnotatedObject
    {
        public ChromatogramPeakFeature() {
            PeakFeature = new BaseChromatogramPeakFeature();
        }

        public ChromatogramPeakFeature(IChromatogramPeakFeature peakFeature) {
            PeakFeature = peakFeature;
        }

        [IgnoreMember]
        public IChromatogramPeakFeature PeakFeature { get; set; }
        // basic property of IChromatogramPeakFeature
        // The following IChromatogramPeakFeature property is written only for serialize and deserialize by MessagePack. Do not use them.
        // Use the PeakFeature property instead of them.
        [Key(0)]
        [Obsolete("Use PeakFeature property instead.")]
        public int ChromScanIdLeft { get => PeakFeature.ChromScanIdLeft; set => PeakFeature.ChromScanIdLeft = value; }
        [Key(1)]
        [Obsolete("Use PeakFeature property instead.")]
        public int ChromScanIdTop { get => PeakFeature.ChromScanIdTop; set => PeakFeature.ChromScanIdTop = value; }
        [Key(2)]
        [Obsolete("Use PeakFeature property instead.")]
        public int ChromScanIdRight { get => PeakFeature.ChromScanIdRight; set => PeakFeature.ChromScanIdRight = value; }
        [Key(3)]
        [Obsolete("Use PeakFeature property instead.")]
        public ChromXs ChromXsLeft { get => PeakFeature.ChromXsLeft; set => PeakFeature.ChromXsLeft = value; }
        [Key(4)]
        [Obsolete("Use PeakFeature property instead.")]
        public ChromXs ChromXsTop { get => PeakFeature.ChromXsTop; set => PeakFeature.ChromXsTop = value; }
        [Key(5)]
        [Obsolete("Use PeakFeature property instead.")]
        public ChromXs ChromXsRight { get => PeakFeature.ChromXsRight; set => PeakFeature.ChromXsRight = value; }
        [Key(6)]
        [Obsolete("Use PeakFeature property instead.")]
        public double PeakHeightLeft { get => PeakFeature.PeakHeightLeft; set => PeakFeature.PeakHeightLeft = value; }
        [Key(7)]
        [Obsolete("Use PeakFeature property instead.")]
        public double PeakHeightTop { get => PeakFeature.PeakHeightTop; set => PeakFeature.PeakHeightTop = value; }
        [Key(8)]
        [Obsolete("Use PeakFeature property instead.")]
        public double PeakHeightRight { get => PeakFeature.PeakHeightRight; set => PeakFeature.PeakHeightRight = value; }
        [Key(9)]
        [Obsolete("Use PeakFeature property instead.")]
        public double PeakAreaAboveZero { get => PeakFeature.PeakAreaAboveZero; set => PeakFeature.PeakAreaAboveZero = value; }
        [Key(10)]
        [Obsolete("Use PeakFeature property instead.")]
        public double PeakAreaAboveBaseline { get => PeakFeature.PeakAreaAboveBaseline; set => PeakFeature.PeakAreaAboveBaseline = value; }
        [Key(43)]
        [Obsolete("Use PeakFeature property instead.")]
        public double Mass { get => PeakFeature.Mass; set => PeakFeature.Mass = value; }
        public double PeakWidth(ChromXType type) {
            switch (type) {
                case ChromXType.RT: return ChromXsRight.RT.Value - ChromXsLeft.RT.Value;
                case ChromXType.RI: return ChromXsRight.RI.Value - ChromXsLeft.RI.Value;
                case ChromXType.Drift: return ChromXsRight.Drift.Value - ChromXsLeft.Drift.Value;
                default: return ChromXsRight.Value - ChromXsLeft.Value;
            }
        }
        public double PeakWidth() {
            return ChromXsRight.Value - ChromXsLeft.Value;
        }

        // basic ID metadata
        [Key(11)]
        public int MasterPeakID { get; set; } // sequential IDs parsing all peak features extracted from an MS data
        [Key(12)]
        public int PeakID { get; set; } // sequential IDs from the same dimmension e.g. RT vs MZ or IM vs MZ
        [Key(13)]
        public int ParentPeakID { get; set; } // for LC-IM-MS/MS. The parent peak ID generating the daughter peak IDs
        [Key(15)]
        public long SeekPointToDCLFile { get; set; } // deconvoluted spectrum is stored in dcl file, and this is the seek pointer

        public int GetMSDecResultID() {
            if (MSDecResultIdUsed == -1) {
                if (IsMultiLayeredData())
                    return MasterPeakID;
                else
                    return PeakID;
            }
            else {
                return MSDecResultIdUsed;
            }
        }

        // link to raw data
        [Key(16)]
        public int MS1RawSpectrumIdTop { get; set; }
        [Key(44)]
        public int MS1RawSpectrumIdLeft { get; set; }
        [Key(45)]
        public int MS1RawSpectrumIdRight { get; set; }
        [Key(17)]
        public int MS1AccumulatedMs1RawSpectrumIdTop { get; set; } // used for LC-IM-MS/MS
        [Key(46)]
        public int MS1AccumulatedMs1RawSpectrumIdLeft { get; set; } // used for LC-IM-MS/MS
        [Key(47)]
        public int MS1AccumulatedMs1RawSpectrumIdRight { get; set; } // used for LC-IM-MS/MS
        [Key(18)]
        public int MS2RawSpectrumID { get; set; } = -1; // representative ID
        [Key(19)]
        public Dictionary<int, double> MS2RawSpectrumID2CE { get; set; } = new Dictionary<int, double>();

        // set for IMMScanProperty
        [Key(20)]
        public int ScanID { get; set; } // same as MS1RawSpectrumID
        [IgnoreMember]
        public double PrecursorMz { get => PeakFeature.Mass; set => PeakFeature.Mass = value; } // in LC-MS/MS same as Mass
        [Key(22)]
        public IonMode IonMode { get; set; }
        [IgnoreMember]
        public ChromXs ChromXs { get => PeakFeature.ChromXsTop; set => PeakFeature.ChromXsTop = value; } // same as ChromXsTop
        [Key(24)]
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();
        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }

        // set for IMoleculeProperty (for representative)
        [Key(25)]
        public string Name { get; set; } = string.Empty;
        [Key(26)]
        public Formula Formula { get; set; } = new Formula();
        [Key(27)]
        public string Ontology { get; set; } = string.Empty;
        [Key(28)]
        public string SMILES { get; set; } = string.Empty;
        [Key(29)]
        public string InChIKey { get; set; } = string.Empty;
        [Key(52)]
        public string Protein { get; set; } = string.Empty;
        [Key(53)]
        public int ProteinGroupID { get; set; } = -1;

        public bool IsValidInChIKey() {
            return !string.IsNullOrWhiteSpace(InChIKey) && InChIKey.Length == 27;
        }

        // ion physiochemical information
        [Key(30)]
        public AdductIon AdductType { get; set; } // representative
        public void SetAdductType(AdductIon adductIon) {
            AdductType = adductIon;
            if (PeakCharacter is null) PeakCharacter = new IonFeatureCharacter();
            PeakCharacter.AdductType = adductIon;
            PeakCharacter.Charge = adductIon.ChargeNumber;
        }
        [IgnoreMember]
        public bool IsAdductTypeFormatted
            => AdductType != null && AdductType.FormatCheck && AdductType.HasAdduct;

        [Key(31)]
        public double CollisionCrossSection { get; set; }

        // molecule annotation results
        // IDs to link properties
        //[Key(32)]
        //public int MspID { get; set; } = -1; // representative msp id
        //[Key(48)]
        //public int MspIDWhenOrdered { get; set; } = -1; // representative msp id
        [Key(33)]
        public Dictionary<int, List<int>> MSRawID2MspIDs { get; set; } = new Dictionary<int, List<int>>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS). ID list having the metabolite candidates exceeding the threshold
        [Key(36)]
        public Dictionary<int, MsScanMatchResult> MSRawID2MspBasedMatchResult { get; set; } = new Dictionary<int, MsScanMatchResult>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS).

        //[Key(34)]
        //public int TextDbID { get; set; }// representative text id
        //[Key(49)]
        //public int TextDbIDWhenOrdered { get; set; }// representative text id
        [Key(35)]
        public List<int> TextDbIDs { get; set; } = new List<int>(); // ID list having the metabolite candidates exceeding the threshold (optional)
        [Key(37)]
        public MsScanMatchResult TextDbBasedMatchResult { get; set; } = null;

        [IgnoreMember]
        public MsScanMatchResult MspBasedMatchResult { // get result having max score
            get {
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) {
                    return null;
                }
                else {
                    return MSRawID2MspBasedMatchResult.Argmax(n => n.Value.TotalScore).Value;
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
        public int TextDbIDWhenOrdered {
            get {
                if (TextDbBasedMatchResult != null) {
                    return TextDbBasedMatchResult.LibraryIDWhenOrdered;
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

        [IgnoreMember]
        public int MspIDWhenOrdered {
            get {
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) {
                    return -1;
                }
                else {
                    return MSRawID2MspBasedMatchResult.Values.Argmax(n => n.TotalScore).LibraryIDWhenOrdered;
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

        [Key(49)]
        public MsScanMatchResultContainer MatchResults {
            get => matchResults ?? (matchResults = new MsScanMatchResultContainer());
            set => matchResults = value;
        }

        [Key(50)]
        private MsScanMatchResultContainer matchResults;

        [IgnoreMember]
        public bool IsManuallyModifiedForAnnotation {
            get {
                if (MatchResults?.Representative is MsScanMatchResult result) {
                    return (result.Source & SourceType.Manual) != SourceType.None;
                }
                return false;
            }
        }

        [IgnoreMember]
        public bool IsMsmsContained => MSDecResultIdUsed >= 0 || MS2RawSpectrumID >= 0;

        [Key(38)]
        public string Comment { get; set; } = string.Empty;

        // peak characters
        [Key(39)]
        public IonFeatureCharacter PeakCharacter { get; set; } = new IonFeatureCharacter();
        [Key(40)]
        public ChromatogramPeakShape PeakShape { get; set; } = new ChromatogramPeakShape();

        // others
        [Key(41)]
        public FeatureFilterStatus FeatureFilterStatus { get; set; } = new FeatureFilterStatus();
        [Key(42)]
        public List<ChromatogramPeakFeature> DriftChromFeatures { get; set; } = null;

        public bool IsMultiLayeredData() {
            return !DriftChromFeatures.IsEmptyOrNull();
        }

        public bool AllDriftFeaturesAreAnnotated(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return IsMultiLayeredData() && DriftChromFeatures.All(n => n.IsReferenceMatched(evaluator));
        }

        [Key(51)]
        public int MSDecResultIdUsed { get; set; } = -1;

        [IgnoreMember]
        public PeakSpotTagCollection TagCollection { get; } = new PeakSpotTagCollection();

        public static ChromatogramPeakFeature FromPeakDetectionResult(PeakDetectionResult peakDetectionResult, Chromatogram chromatogram, double mz) {
            if (peakDetectionResult == null) {
                return null;
            }

            var basePeak = new BaseChromatogramPeakFeature
            {
                ChromScanIdLeft = peakDetectionResult.ScanNumAtLeftPeakEdge,
                ChromScanIdTop = peakDetectionResult.ScanNumAtPeakTop,
                ChromScanIdRight = peakDetectionResult.ScanNumAtRightPeakEdge,
                ChromXsLeft = chromatogram.PeakChromXs(peakDetectionResult.ChromXAxisAtLeftPeakEdge, mz),
                ChromXsTop = chromatogram.PeakChromXs(peakDetectionResult.ChromXAxisAtPeakTop, mz),
                ChromXsRight = chromatogram.PeakChromXs(peakDetectionResult.ChromXAxisAtRightPeakEdge, mz),
                PeakHeightLeft = peakDetectionResult.IntensityAtLeftPeakEdge,
                PeakHeightTop = peakDetectionResult.IntensityAtPeakTop,
                PeakHeightRight = peakDetectionResult.IntensityAtRightPeakEdge,
                PeakAreaAboveZero = peakDetectionResult.AreaAboveZero,
                PeakAreaAboveBaseline = peakDetectionResult.AreaAboveBaseline,
                Mass = mz,
            };

            var peaks = chromatogram.AsPeakArray();
            return new ChromatogramPeakFeature(basePeak)
            {
                MasterPeakID = peakDetectionResult.PeakID,
                PeakID = peakDetectionResult.PeakID,

                //assign the scan number of MS1 and MS/MS for precursor ion's peaks
                MS1RawSpectrumIdTop = peaks[peakDetectionResult.ScanNumAtPeakTop].ID,
                MS1RawSpectrumIdLeft = peaks[peakDetectionResult.ScanNumAtLeftPeakEdge].ID,
                MS1RawSpectrumIdRight = peaks[peakDetectionResult.ScanNumAtRightPeakEdge].ID,

                PeakShape = new ChromatogramPeakShape()
                {
                    SignalToNoise = peakDetectionResult.SignalToNoise,
                    EstimatedNoise = peakDetectionResult.EstimatedNoise,
                    BasePeakValue = peakDetectionResult.BasePeakValue,
                    GaussianSimilarityValue = peakDetectionResult.GaussianSimilarityValue,
                    IdealSlopeValue = peakDetectionResult.IdealSlopeValue,
                    PeakPureValue = peakDetectionResult.PeakPureValue,
                    ShapenessValue = peakDetectionResult.ShapnessValue,
                    SymmetryValue = peakDetectionResult.SymmetryValue,
                    AmplitudeOrderValue = peakDetectionResult.AmplitudeOrderValue,
                    AmplitudeScoreValue = peakDetectionResult.AmplitudeScoreValue
                }
            };
        }

        public static ChromatogramPeakFeature FromPeakDetectionResult(PeakDetectionResult peakDetectionResult, ExtractedIonChromatogram chromatogram, double mz, IonMode ionMode) {
            if (peakDetectionResult == null) {
                return null;
            }

            var basePeak = new BaseChromatogramPeakFeature
            {
                ChromScanIdLeft = peakDetectionResult.ScanNumAtLeftPeakEdge,
                ChromScanIdTop = peakDetectionResult.ScanNumAtPeakTop,
                ChromScanIdRight = peakDetectionResult.ScanNumAtRightPeakEdge,
                ChromXsLeft = chromatogram.PeakChromXs(peakDetectionResult.ChromXAxisAtLeftPeakEdge, mz),
                ChromXsTop = chromatogram.PeakChromXs(peakDetectionResult.ChromXAxisAtPeakTop, mz),
                ChromXsRight = chromatogram.PeakChromXs(peakDetectionResult.ChromXAxisAtRightPeakEdge, mz),
                PeakHeightLeft = peakDetectionResult.IntensityAtLeftPeakEdge,
                PeakHeightTop = peakDetectionResult.IntensityAtPeakTop,
                PeakHeightRight = peakDetectionResult.IntensityAtRightPeakEdge,
                PeakAreaAboveZero = peakDetectionResult.AreaAboveZero,
                PeakAreaAboveBaseline = peakDetectionResult.AreaAboveBaseline,
                Mass = mz,
            };

            return new ChromatogramPeakFeature(basePeak)
            {
                MasterPeakID = peakDetectionResult.PeakID,
                PeakID = peakDetectionResult.PeakID,
                IonMode = ionMode,

                //assign the scan number of MS1 and MS/MS for precursor ion's peaks
                MS1RawSpectrumIdTop = chromatogram.Id(peakDetectionResult.ScanNumAtPeakTop),
                MS1RawSpectrumIdLeft = chromatogram.Id(peakDetectionResult.ScanNumAtLeftPeakEdge),
                MS1RawSpectrumIdRight = chromatogram.Id(peakDetectionResult.ScanNumAtRightPeakEdge),

                PeakShape = new ChromatogramPeakShape()
                {
                    SignalToNoise = peakDetectionResult.SignalToNoise,
                    EstimatedNoise = peakDetectionResult.EstimatedNoise,
                    BasePeakValue = peakDetectionResult.BasePeakValue,
                    GaussianSimilarityValue = peakDetectionResult.GaussianSimilarityValue,
                    IdealSlopeValue = peakDetectionResult.IdealSlopeValue,
                    PeakPureValue = peakDetectionResult.PeakPureValue,
                    ShapenessValue = peakDetectionResult.ShapnessValue,
                    SymmetryValue = peakDetectionResult.SymmetryValue,
                    AmplitudeOrderValue = peakDetectionResult.AmplitudeOrderValue,
                    AmplitudeScoreValue = peakDetectionResult.AmplitudeScoreValue
                }
            };
        }

        public void SetPeakProperties(PeakOfChromatogram peakOfChromatogram) {
            var peakTop = peakOfChromatogram.GetTop();
            var peakLeft = peakOfChromatogram.GetLeft();
            var peakRight = peakOfChromatogram.GetRight();
            PeakFeature.ChromXsLeft = peakLeft.ChromXs;
            PeakFeature.ChromXsTop = peakTop.ChromXs;
            PeakFeature.ChromXsRight = peakRight.ChromXs;
            PeakFeature.PeakHeightLeft = peakLeft.Intensity;
            PeakFeature.PeakHeightTop = peakTop.Intensity;
            PeakFeature.PeakHeightRight = peakRight.Intensity;
            PeakFeature.ChromScanIdLeft = peakLeft.ID;
            PeakFeature.ChromScanIdTop = peakTop.ID;
            PeakFeature.ChromScanIdRight = peakRight.ID;
            MS1RawSpectrumIdTop = PeakFeature.ChromScanIdTop;
            MS1RawSpectrumIdLeft = PeakFeature.ChromScanIdLeft;
            MS1RawSpectrumIdRight = PeakFeature.ChromScanIdRight;

            PeakFeature.PeakAreaAboveZero = peakOfChromatogram.CalculateArea();
            PeakFeature.PeakAreaAboveBaseline = PeakFeature.PeakAreaAboveZero - peakOfChromatogram.CalculateBaseLineArea();
            PeakShape.SignalToNoise = (float)(peakOfChromatogram.CalculatePeakAmplitude() / PeakShape.EstimatedNoise);
        }

        public void SetMatchResultProperty(MoleculeMsReference reference, MsScanMatchResult result, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (result.IsUnknown) {
                return;
            }
            if (evaluator.IsReferenceMatched(result)) {
                DataAccess.SetMoleculeMsProperty(this, reference, result);
            }
            else if (evaluator.IsAnnotationSuggested(result)) {
                DataAccess.SetMoleculeMsPropertyAsSuggested(this, reference, result);
            }
        }

        public void SetMatchResultProperty(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var representative = MatchResults.Representative;
            SetMatchResultProperty(refer.Refer(representative), representative, evaluator);
        }

        public void ClearMatchResultProperty() {
            DataAccess.ClearMoleculePropertyInfomation(this);
            MatchResults.ClearResults();
        }

        public void SetMs2SpectrumId(PeakMs2Spectra spectra) {
            if (spectra.IsEmpty) {
                return;
            }
            var indexToEnergy = spectra.FindOriginalIndexToCollisionEnergyAtPeakTop(MS1RawSpectrumIdTop);
            foreach (var kvp in indexToEnergy) {
                MS2RawSpectrumID2CE[kvp.Key] = kvp.Value;
            }
            MS2RawSpectrumID = spectra.FindOriginalIndexPeakTop(MS1RawSpectrumIdTop);
        }

        // ISpectrumPeak
        double ISpectrumPeak.Intensity {
            get => PeakFeature.PeakHeightTop;
            set => PeakFeature.PeakHeightTop = value;
        }

        double ISpectrumPeak.Mass {
            get => PrecursorMz;
            set => PrecursorMz = value;
        }

        // IChromatogramPeak
        int IChromatogramPeak.ID {
            get => MasterPeakID;
        }

        ChromXs IChromatogramPeak.ChromXs {
            get => PeakFeature.ChromXsTop;
            set => PeakFeature.ChromXsTop = value;
        }
    }

    [MessagePackObject]
    public class LinkedPeakFeature {
        [Key(0)]
        public int LinkedPeakID { get; set; }
        [Key(1)]
        public PeakLinkFeatureEnum Character { get; set; }
    }
}
