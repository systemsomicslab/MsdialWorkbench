using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.DataObj {
    public class MgfRecord
    {
        #region properties
        public string Title { get; set; } = string.Empty;
        public float Pepmass { get; set; }
        public float Rt { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();
        public AdductIon Adduct { get; set; } = AdductIon.Default;
        public int Charge { get; set; } = 1;
        public int Mslevel { get; set; }
        public string Source_instrument { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string Seq { get; set; } = string.Empty;
        public IonMode IonMode { get; set; }
        public string Organism { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Pi { get; set; } = string.Empty;
        public string Datacollector { get; set; } = string.Empty;
        public string Smiles { get; set; } = string.Empty;
        public string Inchi { get; set; } = string.Empty;
        public string Inchiaux { get; set; } = string.Empty;
        public string Pubmed { get; set; } = string.Empty;
        public string Submituser { get; set; } = string.Empty;
        public string LibraryQuality { get; set; } = string.Empty;
        public string SpectrumID { get; set; } = string.Empty;
        public int Scan { get; set; }
        #endregion
    }
}
