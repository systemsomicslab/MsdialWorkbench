using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of isotope calculation result for each formula.
    /// The detail of the properties such as M+1, M+2, etc is stored in 'IsotopicPeak.cs'
    /// </summary>
    [Serializable()]
    public class Isotope
    {
        private string formula;
        private double accurateMass;
        private List<ChemicalElement> elementProfile;
        private List<IsotopicPeak> isotopeProfile;

        public Isotope()
        {
            this.elementProfile = new List<ChemicalElement>();
            this.isotopeProfile = new List<IsotopicPeak>();
        }

        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public double AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        public List<ChemicalElement> ElementProfile
        {
            get { return elementProfile; }
            set { elementProfile = value; }
        }

        public List<IsotopicPeak> IsotopeProfile
        {
            get { return isotopeProfile; }
            set { isotopeProfile = value; }
        }
    }
}
