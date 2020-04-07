using Riken.Metabolomics.StructureFinder.Database;
using Riken.Metabolomics.StructureFinder.Property;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public sealed class BondEnergyCalculator
    {
        private BondEnergyCalculator() { }

        public static double TotalBondEnergy(Structure structure)
        {
            return structure.BondDictionary.Values.Sum(n => BondEnergies.Lookup(n));
        }

        public static double TotalBondEnergy(List<BondProperty> bonds)
        {
            return bonds.Sum(n => BondEnergies.Lookup(n));
        }

        public static double TotalBondEnergy(Dictionary<int, BondProperty> bondDictionary) 
        {
            var bde = 0.0;
            foreach (var bond in bondDictionary.Values) {
                bde += BondEnergies.Lookup(bond);
            }
            return bde;
        }

        public static double BondEnergyLookUp(BondProperty bond)
        {
            return BondEnergies.Lookup(bond);
        }
    }
}
