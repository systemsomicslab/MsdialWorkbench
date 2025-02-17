using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj
{
    public class MspRecord
    {
        public MspRecord()
        {
            Id = -1;
            BinId = -1;
            PeakNumber = -1;
            Name = string.Empty;
            Formula = new Formula();
            CompoundClass = string.Empty;
            Smiles = string.Empty;
            Ontology = string.Empty;
            InchiKey = string.Empty;
            IonMode = IonMode.Positive;
            PrecursorMz = -1;
            RetentionTime = -1;
            DriftTime = -1;
            CollisionCrossSection = -1;
            Links = string.Empty;
            Intensity = -1;
            AdductIon = AdductIon.Default;
            IsotopeRatioList = new List<float>();
            Spectrum = new List<SpectrumPeak>();
            Instrument = string.Empty;
            InstrumentType = string.Empty;
            CollisionEnergy = string.Empty;
            QuantMass = -1;
        }
        public int Id { get; set; }
        public int BinId { get; set; }
        public string CompoundClass { get; set; }
        public string Name { get; set; }
        public float RetentionTime { get; set; }
        public IonMode IonMode { get; set; }
        public string Smiles { get; set; }
        public string InchiKey { get; set; }
        public float PrecursorMz { get; set; }
        public List<float> IsotopeRatioList { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; }
        public string Links { get; set; }
        public AdductIon AdductIon { get; set; }
        public float RetentionIndex { get; set; }
        public int PeakNumber { get; set; }
        public string Comment { get; set; }
        public string Ontology { get; set; }
        public float Intensity { get; set; }
        public string Instrument { get; set; }
        public string InstrumentType { get; set; }
        public Formula Formula { get; set; }
        public string CollisionEnergy { get; set; }
        public float QuantMass { get; set; }
        public float DriftTime { get; set; }
        public float CollisionCrossSection { get; set; }
    }
}
