using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of IUPAC queries described in 'IUPAC.txt' of Resources folder of MSDIAL assembry.
    /// Each chemical element such as C, N, O, S has the generic list of IupacChemicalElement.cs.
    /// This Iupac.cs has the queries of each chemical element detail as the dictionary.
    /// </summary>
    public class Iupac
    {
        Dictionary<int, List<IupacChemicalElement>> iupacID_IupacElementPropertyBeanList = new Dictionary<int, List<IupacChemicalElement>>();
        Dictionary<string, List<IupacChemicalElement>> elementName_IupacElementPropertyBeanList = new Dictionary<string, List<IupacChemicalElement>>();

        public Dictionary<int, List<IupacChemicalElement>> IupacID_IupacElementPropertyBeanList
        {
            get { return iupacID_IupacElementPropertyBeanList; }
            set { iupacID_IupacElementPropertyBeanList = value; }
        }

        public Dictionary<string, List<IupacChemicalElement>> ElementName_IupacElementPropertyBeanList
        {
            get { return elementName_IupacElementPropertyBeanList; }
            set { elementName_IupacElementPropertyBeanList = value; }
        }

        public Iupac()
        {
            iupacID_IupacElementPropertyBeanList = new Dictionary<int, List<IupacChemicalElement>>();
            elementName_IupacElementPropertyBeanList = new Dictionary<string, List<IupacChemicalElement>>();
        }
    }
}
