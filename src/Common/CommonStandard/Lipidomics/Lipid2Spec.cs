using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Lipidomics {
    public sealed class Lipid2Spec
    {
        private Lipid2Spec() { }
        public static List<SpectrumPeak> Convert2SpecPeaks(string lipidname, string lipidclass, AdductIon adduct, ILipidSpectrumGenerator generator) {
            var lipidmolecule = FacadeLipidParser.Default.Parse(lipidname);
            if (!generator.CanGenerate(lipidmolecule, adduct)) return null;
            var msref = lipidmolecule.GenerateSpectrum(generator, adduct);
            return msref.Spectrum;
        }
    }
}
