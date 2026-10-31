using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.Common.Components {
    [MessagePackObject]
    public class MoleculeMsReference : IMoleculeMsProperty {
        public MoleculeMsReference() {
            ChromXs = new ChromXs();
            Spectrum = new List<SpectrumPeak>();
            Formula = new Formula();
            AdductType = AdductIon.Default;
        }

        [SerializationConstructor]
        [Obsolete("This constructor is for MessagePack only, don't use.")]
        public MoleculeMsReference(
            int scanID, double precursorMz, ChromXs chromXs, IonMode ionMode, List<SpectrumPeak> spectrum,
            string name, Formula formula, string ontology, string sMILES, string inChIKey,
            AdductIon adductType) {
            ScanID = scanID;
            PrecursorMz = precursorMz;
            ChromXs = chromXs;
            IonMode = ionMode;
            Spectrum = spectrum;
            Name = name;
            Formula = formula;
            Ontology = ontology;
            SMILES = sMILES;
            InChIKey = inChIKey;
            AdductType = adductType;
        }

        // set for IMMScanProperty
        [Key(0)]
        public int ScanID { get; set; }
        [Key(1)]
        public double PrecursorMz { get; set; }
        [Key(2)]
        public ChromXs ChromXs { get; set; }
        [Key(3)]
        public IonMode IonMode { get; set; }
        [Key(4)]
        public List<SpectrumPeak> Spectrum {
            get {
                var current = Volatile.Read(ref _spectrum);
                if (current != null) { return current; }
                if (Volatile.Read(ref _compactSpectrum) is { } values) {
                    var expanded = SpectrumPeakData.FromCompact(values).ToMutableList();
                    current = Interlocked.CompareExchange(ref _spectrum, expanded, null) ?? expanded;
                    Volatile.Write(ref _compactSpectrum, null);
                    return current;
                }
                return Volatile.Read(ref _spectrum);
            }
            set { _spectrum = value; _compactSpectrum = null; }
        }
        private List<SpectrumPeak> _spectrum;
        private Array _compactSpectrum;

        [IgnoreMember]
        public bool IsSpectrumCompact => _compactSpectrum != null;

        public SpectrumPeakData GetSpectrumPeakData() => _compactSpectrum is { } values
            ? SpectrumPeakData.FromCompact(values) : new SpectrumPeakData(_spectrum);

        /// <summary>Call only while importing an owned library; existing public lists remain mutable.</summary>
        public bool TryCompactSpectrum() {
            if (_compactSpectrum != null) { return true; }
            if (_spectrum == null || _spectrum.Count == 0) { return false; }
            var packable = true;
            foreach (var peak in _spectrum) {
                // Preserve annotated/derived peaks verbatim rather than dropping uncommon metadata.
                if (peak == null || peak.GetType() != typeof(SpectrumPeak) || peak.Comment != null
                    || BitConverter.DoubleToInt64Bits(peak.Resolution) != 0 || peak.Charge != 0 || peak.IsotopeFrag
                    || peak.PeakQuality != 0 || peak.PeakID != 0 || peak.IsotopeParentPeakID != -1 || peak.IsotopeWeightNumber != -1
                    || peak.IsMatched || peak.SpectrumComment != 0 || peak.IsAbsolutelyRequiredFragmentForAnnotation
                    || BitConverter.DoubleToInt64Bits(peak.FragmentationScore) != 0) {
                    return false;
                }
                if (packable) { packable = FitsPackedValue(peak.Mass) && FitsPackedValue(peak.Intensity); }
            }
            if (packable) {
                // Two exact unsigned 16-bit integers share one word; fractional and special values keep doubles.
                var values = new uint[_spectrum.Count];
                // Signed conversions avoid expensive floating-to-unsigned helpers on .NET Framework.
                for (var i = 0; i < values.Length; i++) { values[i] = ((uint)(int)_spectrum[i].Mass << 16) | (uint)(int)_spectrum[i].Intensity; }
                _compactSpectrum = values;
            }
            else {
                var values = new SpectrumPeakValue[_spectrum.Count];
                for (var i = 0; i < values.Length; i++) { values[i] = new SpectrumPeakValue(_spectrum[i].Mass, _spectrum[i].Intensity); }
                _compactSpectrum = values;
            }
            _spectrum = null;
            return true;
        }

        private static bool FitsPackedValue(double value) => value >= 0 && value <= ushort.MaxValue && (int)value == value
            && (value != 0 || BitConverter.DoubleToInt64Bits(value) == 0);

        public MoleculeMsReference CreateSerializationCopy() {
            if (_compactSpectrum == null) { return this; }
            var copy = (MoleculeMsReference)MemberwiseClone();
            copy.Spectrum = GetSpectrumPeakData().ToMutableList();
            return copy;
        }

        /// <summary>Copies metadata for a serializer that writes the spectrum separately.</summary>
        public MoleculeMsReference CreateSerializationMetadataCopy() {
            var copy = (MoleculeMsReference)MemberwiseClone();
            copy.Spectrum = null;
            return copy;
        }

        // set for IMoleculeProperty
        [Key(5)]
        public string Name { get; set; } = string.Empty;
        [Key(6)]
        public Formula Formula { get; set; }
        [Key(7)]
        public string Ontology { get; set; } = string.Empty;
        [Key(8)]
        public string SMILES { get; set; } = string.Empty;
        [Key(9)]
        public string InChIKey { get; set; } = string.Empty;

        // ion physiochemical information
        [Key(10)]
        public AdductIon AdductType { get; set; }

        public void SetAdductType(AdductIon adduct) {
            AdductType = adduct;
        }

        [Key(11)]
        public double CollisionCrossSection { get; set; }
        [Key(12)]
        public List<IsotopicPeak> IsotopicPeaks { get; set; } = new List<IsotopicPeak>();
        [Key(13)]
        public double QuantMass { get; set; } // used for GCMS project

        // other additional metadata
        [Key(14)]
        public string CompoundClass { get; set; } // lipidomics
        [Key(15)]
        public string Comment { get; set; } = string.Empty;
        [Key(16)]
        public string InstrumentModel { get; set; } = string.Empty;
        [Key(17)]
        public string InstrumentType { get; set; } = string.Empty;
        [Key(18)]
        public string Links { get; set; } = string.Empty; // used to link molecule record to databases. Each database must be separated by semi-colon (;)
        [Key(19)]
        public float CollisionEnergy { get; set; }
        [Key(20)]
        public int DatabaseID { get; set; } // used for binbase, fastaDB etc
        [Key(27)]
        public string DatabaseUniqueIdentifier { get; set; } // used for binbase, fastaDB etc
        [Key(21)]
        public int Charge { get; set; }
        [Key(22)]
        public int MsLevel { get; set; }
        [Key(23)]
        public float RetentionTimeTolerance { get; set; } = 0.05F; // used for text library searching
        [Key(24)]
        public float MassTolerance { get; set; } = 0.05F; // used for text library searching
        [Key(25)]
        public float MinimumPeakHeight { get; set; } = 1000F; // used for text library searching
        [Key(26)]
        public bool IsTargetMolecule { get; set; } = true; // used for text library searching
        [Key(28)]
        public string FragmentationCondition { get; set; }

        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }

        [IgnoreMember]
        public string OntologyOrCompoundClass => string.IsNullOrEmpty(Ontology) ? CompoundClass : Ontology;
    }
}
