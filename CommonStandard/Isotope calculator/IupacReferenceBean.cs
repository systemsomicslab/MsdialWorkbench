using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of IUPAC queries described in 'IUPAC.txt' of Resources folder of MSDIAL assembry.
    /// Each chemical element such as C, N, O, S has the generic list of IupacChemicalElement.cs.
    /// This Iupac.cs has the queries of each chemical element detail as the dictionary.
    /// 
    /// Old version of Iupac.cs of Common assembly. (Just class name was changed.)
    /// This class is used in MS-DIAL though. MS-Finder program is useing new version.
    /// </summary>
    [Serializable()]
    [MessagePackObject]
    public class IupacReferenceBean
    {
        Dictionary<int, List<IupacElementPropertyBean>> iupacID_IupacElementPropertyBeanList = new Dictionary<int, List<IupacElementPropertyBean>>();
        Dictionary<string, List<IupacElementPropertyBean>> elementName_IupacElementPropertyBeanList = new Dictionary<string, List<IupacElementPropertyBean>>();

        [Key(0)]
        public Dictionary<int, List<IupacElementPropertyBean>> IupacID_IupacElementPropertyBeanList
        {
            get { return iupacID_IupacElementPropertyBeanList; }
            set { iupacID_IupacElementPropertyBeanList = value; }
        }

        [Key(1)]
        public Dictionary<string, List<IupacElementPropertyBean>> ElementName_IupacElementPropertyBeanList
        {
            get { return elementName_IupacElementPropertyBeanList; }
            set { elementName_IupacElementPropertyBeanList = value; }
        }

        public IupacReferenceBean()
        {
            iupacID_IupacElementPropertyBeanList = new Dictionary<int, List<IupacElementPropertyBean>>();
            elementName_IupacElementPropertyBeanList = new Dictionary<string, List<IupacElementPropertyBean>>();
        }
    }
}
