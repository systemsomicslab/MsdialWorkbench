using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.DataObj.Property;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class IonFeatureCharacter {
        // ion feature
        [Key(0)]
        public AdductIon AdductType { get; set; } = new AdductIon();
        [Key(1)]
        public AdductIon AdductTypeByAmalgamationProgram { get; set; } = new AdductIon();
        [Key(2)]
        public int Charge { get; set; } = 1;
        [Key(3)]
        public List<LinkedPeakFeature> PeakLinks { get; set; }
        [Key(4)]
        public int IsotopeWeightNumber { get; set; } = -1; // mono isotopic ion (M), M+1, M+2, M+3 etc
        [Key(5)]
        public int IsotopeParentPeakID { get; set; } // link to monoisotopic ion
        [Key(6)]
        public int PeakGroupID { get; set; } // at least, isotopes and adduct types from same metabolite are organized
    }
}
