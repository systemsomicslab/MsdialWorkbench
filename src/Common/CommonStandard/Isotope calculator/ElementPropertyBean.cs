using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of the properties for natural isotope ratio calculation.
    /// This element is prepared for each chemical element such as C, H, N, O, S included in internal IUPAC queries.
    /// 
    /// Old version of ChemicalElement.cs of Common assembly. (Just class name was changed.)
    /// This class is used in MS-DIAL though. MS-Finder program is useing new version.
    /// </summary>
    [Serializable()]
    public class ElementPropertyBean
    {
        private int iupacID;
        private string elementName;
        private int elementNumber;
        private List<IupacElementPropertyBean> iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
        private List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList = new List<IsotopeElementPropertyBean>();

        public ElementPropertyBean()
        {
            iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
            isotopeElementPropertyBeanList = new List<IsotopeElementPropertyBean>();
        }

        public int IupacID
        {
            get { return iupacID; }
            set { iupacID = value; }
        }

        public string ElementName
        {
            get { return elementName; }
            set { elementName = value; }
        }

        public int ElementNumber
        {
            get { return elementNumber; }
            set { elementNumber = value; }
        }

        public List<IupacElementPropertyBean> IupacElementPropertyBeanList
        {
            get { return iupacElementPropertyBeanList; }
            set { iupacElementPropertyBeanList = value; }
        }

        public List<IsotopeElementPropertyBean> IsotopeElementPropertyBeanList
        {
            get { return isotopeElementPropertyBeanList; }
            set { isotopeElementPropertyBeanList = value; }
        }

    }
}
