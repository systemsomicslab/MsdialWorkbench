using CompMs.Common.DataObj.Database;
using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace CompMs.Common.DataObj.Property {
    public class IsotopeProperty {

        public IsotopeProperty() {
        }

        public Formula Formula { get; set; } = new Formula();

        public double ExactMass { get; set; }

        public List<AtomProperty> ElementProfile { get; set; } = new List<AtomProperty>();

        public List<IsotopicPeak> IsotopeProfile { get; set; } = new List<IsotopicPeak>();
    }

    /// <summary>
    /// 'IsotopicPeak.cs' and 'Isotope.cs' are the storage of isotope calculation result for a fomula.
    /// The detail of the properties such as M+1, M+2, etc is stored in this class.
    /// Relative intensity is standardized to 100 as the maximum
    /// </summary>
    [MessagePackObject]
    public class IsotopicPeak {
        [SerializationConstructor]
        public IsotopicPeak() {
            
        }

        public IsotopicPeak(IsotopicPeak source) {
            RelativeAbundance = source.RelativeAbundance;
            AbsoluteAbundance = source.AbsoluteAbundance;
            Mass = source.Mass;
            MassDifferenceFromMonoisotopicIon = source.MassDifferenceFromMonoisotopicIon;
            Comment = source.Comment;
        }

        [Key(0)]
        public double RelativeAbundance { get; set; }
        [Key(4)]
        public double AbsoluteAbundance { get; set; }
        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double MassDifferenceFromMonoisotopicIon { get; set; }
        [Key(3)]
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// This is the storage of the properties for natural isotope ratio calculation.
    /// This element is prepared for each chemical element such as C, H, N, O, S included in internal IUPAC queries.
    /// </summary>
    public class AtomProperty {
        
        public AtomProperty() { }

        public int IupacID { get; set; }

        public string ElementName { get; set; }

        public int ElementNumber { get; set; }

        public List<AtomElementProperty> AtomElementProperties { get; set; } = new List<AtomElementProperty>();

        public List<IsotopicPeak> IsotopicPeaks { get; set; } = new List<IsotopicPeak>();

    }
}
