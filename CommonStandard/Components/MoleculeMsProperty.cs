using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components {
    public class MoleculeMsProperty: IMoleculeMsProperty {

        public MoleculeMsProperty() { }
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

        // additional metadata
        public AdductType AdductType { get; set; } = new AdductType();
        public double CollisionCrossSection { get; set; }

        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }
    }
}
