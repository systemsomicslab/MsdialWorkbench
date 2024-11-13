using CompMs.Common.StructureFinder.Database;
using CompMs.Common.StructureFinder.Property;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.StructureFinder.Utility
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
            foreach (var bond in bondDictionary.Values)
            {
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
