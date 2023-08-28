using CompMs.Common.StructureFinder.Property;
using CompMs.Common.StructureFinder.Utility;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.Database
{
    public class BondEnergies
    {
        //Main source: http://2012books.lardbucket.org/books/principles-of-general-chemistry-v1.0/s12-08-properties-of-covalent-bonds.html

        private static readonly Dictionary<string, double> energies = new Dictionary<string, double>
        {
            //kJ/mol
			{"H-H", 432},
            {"C-H", 411},
            {"Si-H", 318},
            {"N-H", 386},
            {"P-H", 322},
            {"O-H", 459},
            {"S-H", 363},
            {"F-H", 565},
            {"Cl-H", 428},
            {"Br-H", 362},
            {"I-H", 295},

            {"C-C", 346},
            {"C=C", 602},
            {"C#C", 835},
            {"C-N", 305},
            {"C=N", 615},
            {"C#N", 887},
            {"C-O", 358},
            {"C=O", 749},
            {"C#O", 835},
            {"C-Cl", 327},
            {"C-F", 485},
            {"C-Br", 285},
            {"C-I", 213},
            {"C-Si", 318},
            {"C-S", 272},
            {"C=S", 573},
            {"C-P", 264},

            {"N-N", 167},
            {"N=N", 418},
            {"N#N", 942},
            {"N=O", 607},
            {"N-O", 201},
            {"N-F", 283},
            {"N-Cl", 313},
            {"N-Br", 243},

            {"O-O", 142},
            {"O=O", 494},
            {"O-Cl", 218},
            {"O-Br", 201},
            {"O-I", 201},
            {"O-P", 335},
            {"O=P", 544},
            {"O=S", 532},
            {"O-Si", 452},

            {"S=P", 335},
            {"S-S", 226},
            {"S-F", 284},
            {"S-Cl", 255},
            {"S-Br", 218},
            {"S=S", 425},

            {"P-P", 201},
            {"P-F", 490},
            {"P-Cl", 326},
            {"P-Br", 264},
            {"P-I", 184},

            {"F-F", 158},
            {"F-Cl", 313},
            {"F-I", 273},

            {"Cl-Cl", 242},
            {"Cl-I", 208},
            {"Br-Br", 193},
            {"Br-I", 175},
            {"I-I", 151},

            {"Si-Si", 222},
        };

        public static double Lookup(BondProperty bond)
        {

            var bondString = MoleculeConverter.BondPropertyToString(bond);
            return Lookup(bondString);
        }

        private static double Lookup(string bondString)
        {
            var bondEnergy = 0.0;
            if (energies.TryGetValue(bondString, out bondEnergy))
            {
                return bondEnergy;
            }

            //not a covalent bond? just assume a C-C bond 
            return 346;
        }

        public static double ConvertKJmolToEV(double energy)
        {
            return 0.010364 * energy;
        }

        public static double ConvertEVToKJmol(double energy)
        {
            return 96.485 * energy;
        }
    }
}
