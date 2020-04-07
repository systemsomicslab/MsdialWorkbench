using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;


namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of natural abundance or mass properties of each chemical element such as 12C, 13C etc.
    /// 
    /// Old version of IupacChemicalElement.cs of Common assembly. (Just class name was changed.)
    /// This class is used in MS-DIAL though. MS-Finder program is useing new version.
    /// </summary>
    [Serializable()]
    [MessagePackObject]
    public class IupacElementPropertyBean
    {
        private int iupacID;
        private int nominalMass;
        private string elementName;
        private double naturalRelativeAbundance;
        private double accurateMass;

        [Key(0)]
        public int IupacID
        {
            get { return iupacID; }
            set { iupacID = value; }
        }

        [Key(1)]
        public string ElementName
        {
            get { return elementName; }
            set { elementName = value; }
        }

        [Key(2)]
        public int NominalMass
        {
            get { return nominalMass; }
            set { nominalMass = value; }
        }

        [Key(3)]
        public double NaturalRelativeAbundance
        {
            get { return naturalRelativeAbundance; }
            set { naturalRelativeAbundance = value; }
        }

        [Key(4)]
        public double AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }
    }
}
