using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.DataObj.Property;

namespace CompMs.Common.DataObj.PeakFeature {
    public class ChromatogramPeakCharacter {
        // ion feature
        public AdductIon AdductType { get; set; } = new AdductIon();
        public AdductIon AdductTypeByAmalgamationProgram { get; set; } = new AdductIon();
        public int Charge { get; set; } = 1;
        public List<LinkedPeakFeature> PeakLinks { get; set; }
        public int IsotopeWeightNumber { get; set; } // mono isotopic ion (M), M+1, M+2, M+3 etc
        public int IsotopeParentPeakID { get; set; } // link to monoisotopic ion
        public int PeakGroupID { get; set; } // at least, isotopes and adduct types from same metabolite are organized

        // peak shape prop
        public float EstimatedNoise { get; set; }
        public float SignalToNoise { get; set; }
        public float PeakPureValue { get; set; }
        public float ShapenessValue { get; set; }
        public float GaussianSimilarityValue { get; set; }
        public float IdealSlopeValue { get; set; }
        public float BasePeakValue { get; set; }
        public float SymmetryValue { get; set; }
    }
}
