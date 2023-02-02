using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// 'IsotopicPeak.cs' and 'Isotope.cs' are the storage of isotope calculation result for a fomula.
    /// The detail of the properties such as M+1, M+2, etc is stored in this class.
    /// </summary>
    [Serializable()]
    public class IsotopicPeak
    {
        private double relativeAbundance;
        private double mass;
        private double massDifferenceFromMonoisotopicIon;
        private string comment;

        public double RelativeAbundance
        {
            get { return relativeAbundance; }
            set { relativeAbundance = value; }
        }

        public double Mass
        {
            get { return mass; }
            set { mass = value; }
        }

        public double MassDifferenceFromMonoisotopicIon
        {
            get { return massDifferenceFromMonoisotopicIon; }
            set { massDifferenceFromMonoisotopicIon = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }
    }
}
