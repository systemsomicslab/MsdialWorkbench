using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components {
    public class MoleculeMsReference: IMoleculeMsProperty {

        public MoleculeMsReference() { }
        // set for IMMScanProperty
        public int ID { get; set; }
        public double PrecursorMz { get; set; }
        public Times Times { get; set; }
        public IonMode IonMode { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();

        // set for IMoleculeProperty
        public string MoleculeName { get; set; } = string.Empty;
        public Formula Formula { get; set; } = new Formula();
        public string Ontology { get; set; } = string.Empty;
        public string SMILES { get; set; } = string.Empty;
        public string InChIKey { get; set; } = string.Empty;

        // ion physiochemical information
        public AdductType AdductType { get; set; } = new AdductType();
        public double CollisionCrossSection { get; set; }
        public List<IsotopicPeak> IsotopicPeaks { get; set; } = new List<IsotopicPeak>();
        public double QuantMass { get; set; } // used for GCMS project

        // other additional metadata
        public string Comment { get; set; } = string.Empty;
        public string InstrumentModel { get; set; } = string.Empty;
        public string InstrumentType { get; set; } = string.Empty;
        public string Links { get; set; } = string.Empty; // used to link molecule record to databases. Each database must be separated by semi-colon (;)

        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }
    }
}
