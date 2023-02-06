using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics {
    public class LipoqualityAnnotation
    {
        public float Mz { get; set; }
        public float Rt { get; set; }
        public float AveragedIntensity { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LipidClass { get; set; } = string.Empty;
        public string LipidSuperClass { get; set; } = string.Empty;
        public string TotalChain { get; set; } = string.Empty;
        public string Sn1AcylChain { get; set; } = string.Empty;
        public string Sn2AcylChain { get; set; } = string.Empty;
        public string Sn3AcylChain { get; set; } = string.Empty;
        public string Sn4AcylChain { get; set; } = string.Empty;
        public AdductIon Adduct { get; set; }
        public IonMode IonMode { get; set; }
        public string Smiles { get; set; } = string.Empty;
        public string Inchikey { get; set; } = string.Empty;
        public float StandardDeviation { get; set; }
        public int SpotID { get; set; }
        public string Formula { get; set; } = string.Empty;
        public List<double> Intensities { get; set; } = new List<double>();
    }
}
