using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Normalize;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class AlignmentChromPeakFeature : IChromatogramPeak, IChromatogramPeakFeature, IMSIonProperty, IAnnotatedObject, INormalizableValue {

        public AlignmentChromPeakFeature() {
            Formula = new Formula();
            PeakCharacter = new IonFeatureCharacter();
            PeakShape = new ChromatogramPeakShape();
        }

        [SerializationConstructor]
        [Obsolete("This constructor is for MessagePack only, don't use.")]
        public AlignmentChromPeakFeature(int fileID, string fileName, int masterPeakID, int peakID, int parentPeakID) {
            FileID = fileID;
            FileName = fileName;
            MasterPeakID = masterPeakID;
            PeakID = peakID;
            ParentPeakID = parentPeakID;
        }

        // ID metadata
        [Key(0)]
        public int FileID { get; set; }
        [Key(1)]
        public string FileName { get; set; } = string.Empty;
        [Key(2)]
        public int MasterPeakID { get; set; } // sequential IDs parsing all peak features extracted from an MS data
        [Key(3)]
        public int PeakID { get; set; } // sequential IDs from the same dimmension e.g. RT vs MZ or IM vs MZ
        [Key(4)]
        public int ParentPeakID { get; set; } // for LC-IM-MS/MS. The parent peak ID generating the daughter peak IDs
        [Key(6)]
        public long SeekPointToDCLFile { get; set; } // deconvoluted spectrum is stored in dcl file, and this is the seek pointer
        [Key(7)]
        public int MS1RawSpectrumID { get; set; }
        [Key(8)]
        public int MS1RawSpectrumIDatAccumulatedMS1 { get; set; } // used for LC-IM-MS/MS
        [Key(9)]
        public int MS2RawSpectrumID { get; set; } // representative ID
        [Key(10)]
        public Dictionary<int, double> MS2RawSpectrumID2CE { get; set; }

        [IgnoreMember]
        public bool IsMsmsAssigned => MS2RawSpectrumID2CE?.Any() ?? false;

        // basic property of IChromatogramPeakFeature
        [Key(11)]
        public int ChromScanIdLeft { get; set; }
        [Key(12)]
        public int ChromScanIdTop { get; set; }
        [Key(13)]
        public int ChromScanIdRight { get; set; }
        [Key(38)]
        public int MS1RawSpectrumIdTop { get; set; }
        [Key(39)]
        public int MS1RawSpectrumIdLeft { get; set; }
        [Key(40)]
        public int MS1RawSpectrumIdRight { get; set; }
        [Key(41)]
        public int MS1AccumulatedMs1RawSpectrumIdTop { get; set; } // used for LC-IM-MS/MS
        [Key(42)]
        public int MS1AccumulatedMs1RawSpectrumIdLeft { get; set; } // used for LC-IM-MS/MS
        [Key(43)]
        public int MS1AccumulatedMs1RawSpectrumIdRight { get; set; } // used for LC-IM-MS/MS

        [Key(14)]
        public ChromXs ChromXsLeft { get; set; }
        [Key(15)]
        public ChromXs ChromXsTop { get; set; }
        [Key(16)]
        public ChromXs ChromXsRight { get; set; }
        [Key(17)]
        public double PeakHeightLeft { get; set; }
        [Key(18)]
        public double PeakHeightTop { get; set; }
        [Key(19)]
        public double PeakHeightRight { get; set; }
        [Key(20)]
        public double PeakAreaAboveZero { get; set; }
        [Key(21)]
        public double PeakAreaAboveBaseline { get; set; }
        [Key(22)]
        public double Mass { get; set; } // for quant mass in gcms

        public double PeakWidth() {
            return ChromXsRight.Value - ChromXsLeft.Value;
        }

        public double PeakWidth(ChromXType type) {
            switch (type) {
                case ChromXType.RT: return ChromXsRight.RT.Value - ChromXsLeft.RT.Value;
                case ChromXType.RI: return ChromXsRight.RI.Value - ChromXsLeft.RI.Value;
                case ChromXType.Drift: return ChromXsRight.Drift.Value - ChromXsLeft.Drift.Value;
                default: return ChromXsRight.Value - ChromXsLeft.Value;
            }
        }

        // set for IMMScanProperty
        [Key(23)]
        public IonMode IonMode { get; set; }

        // set for IMoleculeProperty (for representative)
        [Key(24)]
        public string Name { get; set; } = string.Empty;
        [Key(25)]
        public Formula Formula { get; set; }
        [Key(26)]
        public string Ontology { get; set; } = string.Empty;
        [Key(27)]
        public string SMILES { get; set; } = string.Empty;
        [Key(28)]
        public string InChIKey { get; set; } = string.Empty;
        [Key(50)]
        public string Protein { get; set; } = string.Empty;
        [Key(51)]
        public int ProteinGroupID { get; set; } = -1;

        // ion physiochemical information
        [Key(29)]
        public double CollisionCrossSection { get; set; }

        // molecule annotation results
        //[Key(30)]
        //public int MspID { get; set; } // representative msp id
        [Key(31)]
        public Dictionary<int, List<int>> MSRawID2MspIDs { get; set; } = new Dictionary<int, List<int>>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS). ID list having the metabolite candidates exceeding the threshold
        //[Key(32)]
        //public int TextDbID { get; set; }// representative text id
        [Key(33)]
        public List<int> TextDbIDs { get; set; } = new List<int>(); // ID list having the metabolite candidates exceeding the threshold (optional)
        [Key(34)]
        public Dictionary<int, MsScanMatchResult> MSRawID2MspBasedMatchResult { get; set; } = new Dictionary<int, MsScanMatchResult>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS).
        [Key(35)]
        public MsScanMatchResult TextDbBasedMatchResult { get; set; }

        public MsScanMatchResult MspBasedMatchResult() { // get result having max score
            if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return null;
            else {
                return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value)).Value;
            }
        }

        public int TextDbID() {
            if (TextDbBasedMatchResult != null) return TextDbBasedMatchResult.LibraryID;
            else return -1;
        }

        public int MspID() {
            if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return -1;
            else {
                return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value.LibraryID)).LibraryID;
            }
        }

        public bool IsReferenceMatched() {
            if (MatchResults.IsManuallyModifiedRepresentative) {
                return !MatchResults.IsUnknown;
            }
            if (TextDbBasedMatchResult != null) {
                return true;
            }
            if (MspBasedMatchResult() != null && MspBasedMatchResult().IsSpectrumMatch) {
                return true;
            }
            return false;
        }

        public bool IsAnnotationSuggested() {
            if (MatchResults.IsManuallyModifiedRepresentative) {
                return false;
            }
            else if (TextDbBasedMatchResult != null) {
                return false;
            }
            else if (MspBasedMatchResult() != null && MspBasedMatchResult().IsSpectrumMatch) {
                return false;
            }
            else if (MspBasedMatchResult() != null && MspBasedMatchResult().IsPrecursorMzMatch) {
                return true;
            }
            return false;
        }

        public bool IsUnknown() {
            if (MatchResults.IsManuallyModifiedRepresentative && MatchResults.IsUnknown) {
                return true;
            }
            if (MatchResults.IsUnknown && (TextDbBasedMatchResult == null || MspBasedMatchResult() == null)) {
                return true;
            }
            return false;
        }

        [Key(47)]
        public MsScanMatchResultContainer MatchResults {
            get => matchResults ?? (matchResults = new MsScanMatchResultContainer());
            set => matchResults = value;
        }
        private MsScanMatchResultContainer matchResults;

        // peak characters
        [Key(36)]
        public IonFeatureCharacter PeakCharacter { get; set; }
        [Key(37)]
        public ChromatogramPeakShape PeakShape { get; set; }
        [Key(44)]
        public double NormalizedPeakHeight { get; set; }
        [Key(45)]
        public double NormalizedPeakAreaAboveZero { get; set; }
        [Key(46)]
        public double NormalizedPeakAreaAboveBaseline { get; set; }

        // IMSProperty members
        ChromXs IMSProperty.ChromXs {
            get => ChromXsTop;
            set => ChromXsTop = value;
        }
        IonMode IMSProperty.IonMode {
            get => IonMode;
            set => IonMode = value;
        }
        double IMSProperty.PrecursorMz {
            get => Mass;
            set => Mass = value;
        }

        // IIonProperty members
        AdductIon IIonProperty.AdductType {
            get => PeakCharacter.AdductType;
        }

        void IIonProperty.SetAdductType(AdductIon adduct) {
            PeakCharacter.AdductType = adduct;
            PeakCharacter.Charge = adduct.ChargeNumber;
        }

        [Key(49)]
        public int MSDecResultIdUsed { get; set; } = -1;
        public int GetMSDecResultID() {
            if (MSDecResultIdUsed == -1) {
                return MasterPeakID;
            }
            return MSDecResultIdUsed;
        }

        [Key(52)]
        public bool IsManuallyModifiedForQuant { get; set; }

        // INormalizableValue
        double INormalizableValue.PeakHeight => PeakHeightTop;

        // IChromatogramPeak members
        int IChromatogramPeak.ID => MasterPeakID;

        ChromXs IChromatogramPeak.ChromXs {
            get => ChromXsTop;
            set => ChromXsTop = value;
        }
        double ISpectrumPeak.Intensity {
            get => PeakHeightTop;
            set => PeakHeightTop = value;
        }

        public AlignmentChromPeakFeature Clone() {
            return new AlignmentChromPeakFeature
            {
                FileID = FileID,
                FileName = FileName,
                MasterPeakID = MasterPeakID,
                PeakID = PeakID,
                ParentPeakID = ParentPeakID,
                SeekPointToDCLFile = SeekPointToDCLFile,
                MS1RawSpectrumID = MS1RawSpectrumID,
                MS1RawSpectrumIDatAccumulatedMS1 = MS1RawSpectrumIDatAccumulatedMS1,
                MS2RawSpectrumID = MS2RawSpectrumID,
                MS2RawSpectrumID2CE = MS2RawSpectrumID2CE is null ? null : new Dictionary<int, double>(MS2RawSpectrumID2CE),
                ChromScanIdLeft = ChromScanIdLeft,
                ChromScanIdTop = ChromScanIdTop,
                ChromScanIdRight = ChromScanIdRight,
                MS1RawSpectrumIdTop = MS1RawSpectrumIdTop,
                MS1RawSpectrumIdLeft = MS1RawSpectrumIdLeft,
                MS1RawSpectrumIdRight = MS1RawSpectrumIdRight,
                MS1AccumulatedMs1RawSpectrumIdTop = MS1AccumulatedMs1RawSpectrumIdTop,
                MS1AccumulatedMs1RawSpectrumIdLeft = MS1AccumulatedMs1RawSpectrumIdLeft,
                MS1AccumulatedMs1RawSpectrumIdRight = MS1AccumulatedMs1RawSpectrumIdRight,
                ChromXsLeft = new ChromXs(ChromXsLeft.RT, ChromXsLeft.RI, ChromXsLeft.Drift, ChromXsLeft.Mz, ChromXsLeft.MainType),
                ChromXsTop = new ChromXs(ChromXsTop.RT, ChromXsTop.RI, ChromXsTop.Drift, ChromXsTop.Mz, ChromXsTop.MainType),
                ChromXsRight = new ChromXs(ChromXsRight.RT, ChromXsRight.RI, ChromXsRight.Drift, ChromXsRight.Mz, ChromXsRight.MainType),
                PeakHeightLeft = PeakHeightLeft,
                PeakHeightTop = PeakHeightTop,
                PeakHeightRight = PeakHeightRight,
                PeakAreaAboveZero = PeakAreaAboveZero,
                PeakAreaAboveBaseline = PeakAreaAboveBaseline,
                Mass = Mass,
                IonMode = IonMode,
                Name = Name,
                Formula = Formula,
                Ontology = Ontology,
                SMILES = SMILES,
                InChIKey = InChIKey,
                Protein = Protein,
                ProteinGroupID = ProteinGroupID,
                CollisionCrossSection = CollisionCrossSection,
                MSRawID2MspIDs = MSRawID2MspIDs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList()),
                TextDbIDs = TextDbIDs.ToList(),
                MSRawID2MspBasedMatchResult = MSRawID2MspBasedMatchResult is null ? null : new Dictionary<int, MsScanMatchResult>(MSRawID2MspBasedMatchResult),
                TextDbBasedMatchResult = TextDbBasedMatchResult,
                matchResults = new MsScanMatchResultContainer(MatchResults),
                PeakCharacter = new IonFeatureCharacter(PeakCharacter),
                PeakShape = new ChromatogramPeakShape(PeakShape),
                NormalizedPeakHeight = NormalizedPeakHeight,
                NormalizedPeakAreaAboveZero = NormalizedPeakAreaAboveZero,
                NormalizedPeakAreaAboveBaseline = NormalizedPeakAreaAboveBaseline,
                MSDecResultIdUsed = MSDecResultIdUsed,
                IsManuallyModifiedForQuant = IsManuallyModifiedForQuant,
            };
        }
    }
}
