using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of natural abundance or mass properties of each chemical element such as 12C, 13C etc.
    /// </summary>
    public class IupacChemicalElement
    {
        private int iupacID;
        private int nominalMass;
        private string elementName;
        private double naturalRelativeAbundance;
        private double accurateMass;

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

        public int NominalMass
        {
            get { return nominalMass; }
            set { nominalMass = value; }
        }

        public double NaturalRelativeAbundance
        {
            get { return naturalRelativeAbundance; }
            set { naturalRelativeAbundance = value; }
        }

        public double AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }
    }
}
