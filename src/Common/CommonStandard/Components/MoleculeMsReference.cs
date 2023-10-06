using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using MessagePack;
using System;
using System.Collections.Generic;

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
        public List<SpectrumPeak> Spectrum { get; set; }

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
