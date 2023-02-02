using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of natural abundance or mass properties of each chemical element such as 12C, 13C etc.
    /// 
    /// Old version of Isotope.cs of Common assembly. (Just class was changed.)
    /// This class is used in MS-DIAL though. MS-Finder program is useing new version.
    /// </summary>
    [Serializable()]
    public class CompoundPropertyBean
    {
        private string elementName;
        private double accurateMass;
        private List<ElementPropertyBean> elementProfile;
        private List<IsotopeElementPropertyBean> isotopeProfile;

        public CompoundPropertyBean()
        {
            this.elementProfile = new List<ElementPropertyBean>();
            this.isotopeProfile = new List<IsotopeElementPropertyBean>();
        }

        public string ElementName
        {
            get { return elementName; }
            set { elementName = value; }
        }

        public double AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        public List<ElementPropertyBean> ElementProfile
        {
            get { return elementProfile; }
            set { elementProfile = value; }
        }

        public List<IsotopeElementPropertyBean> IsotopeProfile
        {
            get { return isotopeProfile; }
            set { isotopeProfile = value; }
        }
    }
}
