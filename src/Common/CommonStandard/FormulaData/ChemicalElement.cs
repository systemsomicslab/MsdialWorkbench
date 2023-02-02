using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of the properties for natural isotope ratio calculation.
    /// This element is prepared for each chemical element such as C, H, N, O, S included in internal IUPAC queries.
    /// </summary>
    [Serializable()]
    public class ChemicalElement
    {
        private int iupacID;
        private string elementName;
        private int elementNumber;
        private List<IupacChemicalElement> iupacElementPropertyBeanList = new List<IupacChemicalElement>();
        private List<IsotopicPeak> isotopeElementPropertyBeanList = new List<IsotopicPeak>();

        public ChemicalElement()
        {
            iupacElementPropertyBeanList = new List<IupacChemicalElement>();
            isotopeElementPropertyBeanList = new List<IsotopicPeak>();
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

        public List<IupacChemicalElement> IupacElementPropertyBeanList
        {
            get { return iupacElementPropertyBeanList; }
            set { iupacElementPropertyBeanList = value; }
        }

        public List<IsotopicPeak> IsotopeElementPropertyBeanList
        {
            get { return isotopeElementPropertyBeanList; }
            set { isotopeElementPropertyBeanList = value; }
        }

    }
}
