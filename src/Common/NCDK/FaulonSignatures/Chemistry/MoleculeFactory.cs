namespace NCDK.FaulonSignatures.Chemistry
{
    public class MoleculeFactory
    {
        public static Molecule Methane()
        {
            Molecule molecule = new Molecule();
            molecule.AddAtom("C");
            molecule.AddAtom("H");
            molecule.AddAtom("H");
            molecule.AddAtom("H");
            molecule.AddAtom("H");
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(0, 3);
            molecule.AddSingleBond(0, 4);
            return molecule;
        }

        public static Molecule ThreeCycle()
        {
            Molecule molecule = new Molecule("C", 3);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(1, 2);
            return molecule;
        }

        public static Molecule FourCycle()
        {
            Molecule molecule = new Molecule("C", 4);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 3);
            molecule.AddSingleBond(1, 2);
            molecule.AddSingleBond(2, 3);
            return molecule;
        }

        public static Molecule FiveCycle()
        {
            Molecule molecule = new Molecule("C", 5);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 4);
            molecule.AddSingleBond(1, 2);
            molecule.AddSingleBond(2, 3);
            molecule.AddSingleBond(3, 4);
            return molecule;
        }

        public static Molecule SixCycle()
        {
            Molecule molecule = new Molecule("C", 6);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 5);
            molecule.AddSingleBond(1, 2);
            molecule.AddSingleBond(2, 3);
            molecule.AddSingleBond(3, 4);
            molecule.AddSingleBond(4, 5);
            return molecule;
        }

        public static Molecule ThreeStar()
        {
            Molecule molecule = new Molecule("C", 4);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(0, 3);
            return molecule;
        }

        public static Molecule FourStar()
        {
            Molecule molecule = new Molecule("C", 5);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(0, 3);
            molecule.AddSingleBond(0, 4);
            return molecule;
        }

        public static Molecule FiveStar()
        {
            Molecule molecule = new Molecule("C", 6);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(0, 3);
            molecule.AddSingleBond(0, 4);
            molecule.AddSingleBond(0, 5);
            return molecule;
        }

        public static Molecule Propellane()
        {
            Molecule molecule = new Molecule("C", 5);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 4);
            molecule.AddSingleBond(1, 2);
            molecule.AddSingleBond(1, 3);
            molecule.AddSingleBond(1, 4);
            molecule.AddSingleBond(2, 4);
            molecule.AddSingleBond(3, 4);
            return molecule;
        }

        public static Molecule MethylatedCyclobutane()
        {
            Molecule molecule = new Molecule("C", 5);
            //        molecule.AddSingleBond(0, 1);
            //        molecule.AddSingleBond(1, 2);
            //        molecule.AddSingleBond(1, 3);
            //        molecule.AddSingleBond(1, 4);
            //        molecule.AddSingleBond(2, 4);
            //        molecule.AddSingleBond(3, 4);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(0, 3);
            molecule.AddSingleBond(0, 4);
            molecule.AddSingleBond(2, 3);
            molecule.AddSingleBond(3, 4);
            return molecule;
        }

        public static Molecule Pseudopropellane()
        {
            Molecule molecule = new Molecule("C", 5);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 4);
            molecule.AddSingleBond(1, 2);
            molecule.AddSingleBond(1, 3);
            molecule.AddSingleBond(2, 4);
            molecule.AddSingleBond(3, 4);
            return molecule;
        }

        // note that this cannot physically exist, 
        // as the bond strain would be too high 
        public static Molecule SixCage()
        {
            Molecule molecule = new Molecule("C", 6);
            molecule.AddSingleBond(0, 1);
            molecule.AddSingleBond(0, 2);
            molecule.AddSingleBond(0, 3);
            molecule.AddSingleBond(1, 2);
            molecule.AddSingleBond(1, 3);
            molecule.AddSingleBond(2, 5);
            molecule.AddSingleBond(3, 4);
            molecule.AddSingleBond(3, 5);
            molecule.AddSingleBond(4, 5);
            return molecule;
        }
    }
}
