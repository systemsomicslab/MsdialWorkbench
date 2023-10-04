using CompMs.Common.DataObj.Property;
using MessagePack;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class IonFeatureCharacter {
        public IonFeatureCharacter() {
            AdductType = AdductIon.Default;
            AdductTypeByAmalgamationProgram = AdductIon.Default;
            PeakLinks = new List<LinkedPeakFeature>();
        }

        [SerializationConstructor]
        public IonFeatureCharacter(
            AdductIon adductType, AdductIon adductTypeByAmalgamationProgram, int charge, List<LinkedPeakFeature> peakLinks, int isotopeWeightNumber,
            int isotopeParentPeakID, int peakGroupID, bool isLinked, int adductParent) {

            AdductType = adductType;
            AdductTypeByAmalgamationProgram = adductTypeByAmalgamationProgram;
            Charge = charge;
            PeakLinks = peakLinks;
            IsotopeWeightNumber = isotopeWeightNumber;
            IsotopeParentPeakID = isotopeParentPeakID;
            PeakGroupID = peakGroupID;
            IsLinked = isLinked;
            AdductParent = adductParent;
        }

        public IonFeatureCharacter(IonFeatureCharacter source) {
            AdductType = source.AdductType;
            AdductTypeByAmalgamationProgram = source.AdductTypeByAmalgamationProgram;
            Charge = source.Charge;
            PeakLinks = source.PeakLinks.ToList();
            IsotopeWeightNumber = source.IsotopeWeightNumber;
            IsotopeParentPeakID = source.IsotopeParentPeakID;
            PeakGroupID = source.PeakGroupID;
            IsLinked = source.IsLinked;
            AdductParent = source.AdductParent;
        }

        // ion feature
        [Key(0)]
        public AdductIon AdductType { get; set; }
        [Key(1)]
        public AdductIon AdductTypeByAmalgamationProgram { get; set; }
        [Key(2)]
        public int Charge { get; set; } = 1;
        [Key(3)]
        public List<LinkedPeakFeature> PeakLinks { get; set; }
        [Key(4)]
        public int IsotopeWeightNumber { get; set; } = -1; // mono isotopic ion (M), M+1, M+2, M+3 etc
        [Key(5)]
        public int IsotopeParentPeakID { get; set; } = -1; // link to monoisotopic ion
        [Key(6)]
        public int PeakGroupID { get; set; } = -1; // at least, isotopes and adduct types from same metabolite are organized
        [Key(7)]
        public bool IsLinked { get; set; } = false;
        [Key(8)]
        public int AdductParent { get; set; } = -1; // the peak id which is used to annotate the adduct type for this peak is insearted.

        [IgnoreMember]
        public bool IsMonoIsotopicIon => IsotopeWeightNumber == 0;
    }
}
