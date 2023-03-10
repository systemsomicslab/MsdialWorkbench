
using NCDK.Aromaticities;
using NCDK.Charges;
using NCDK.Common.Mathematics;
using NCDK.Graphs.Invariant;
using NCDK.Numerics;
using NCDK.RingSearches;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Atomic
{
    public partial class RDFProtonDescriptorG3R
    {
        [DescriptorResult(prefix: prefix, baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(Exception e) : base(e) { }

            public Result(IReadOnlyList<double> values) : base(values) { }
        }

        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;
        private IEnumerable<IAtomContainer> varAtomContainerSet;
        private readonly IAtomContainer mol;

        public RDFProtonDescriptorG3R(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedContainer = (IAtomContainer)container.Clone();

            /////////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            /////////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            mol = CDK.Builder.NewAtomContainer(clonedContainer);

            // DETECTION OF pi SYSTEMS
            varAtomContainerSet = ConjugatedPiSystemsDetector.Detect(mol);
            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(mol, true);

            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);
                Aromaticity.CDKLegacy.Apply(clonedContainer);
            }

            this.container = container;
        }

        private IRingSet allRings;
        private readonly object syncAllRings = new object();

        private IRingSet AllRings
        {
            get
            {
                if (allRings == null)
                    lock (syncAllRings)
                    {
                        allRings = new AllRingsFinder().FindAllRings(clonedContainer);
                    }
                return allRings;
            }
        }

        public virtual Result Calculate(IAtom atom)
        {
            return Calculate(atom, null);
        }

        public Result Calculate(IAtom atom, IRingSet precalculatedringset)
        {
            var atomPosition = container.Atoms.IndexOf(atom);
            var clonedAtom = clonedContainer.Atoms[atomPosition];
            var rdfProtonCalculatedValues = new List<double>(desc_length);
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                return new Result(new CDKException("Invalid atom specified"));
            }

            // ///////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            // ///////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            var varRingSet = precalculatedringset ?? AllRings;

            // SET ISINRING FLAGS FOR BONDS
            var bondsInContainer = clonedContainer.Bonds;
            foreach (var bond in bondsInContainer)
            {
                var ringsWithThisBond = varRingSet.GetRings(bond);
                if (ringsWithThisBond.Any())
                {
                    bond.IsInRing = true;
                }
            }
            // SET ISINRING FLAGS FOR ATOMS
            foreach (var vatom in clonedContainer.Atoms)
            {
                var ringsWithThisAtom = varRingSet.GetRings(vatom);
                if (ringsWithThisAtom.Any())
                {
                    vatom.IsInRing = true;
                }
            }

            var detected = varAtomContainerSet.FirstOrDefault();

            // neighbors[0] is the atom joined to the target proton:
            var neighbors = mol.GetConnectedAtoms(clonedAtom);
            var neighbour0 = neighbors.First();

            // 2', 3', 4', 5', 6', and 7' atoms up to the target are detected:
            var atomsInSecondSphere = mol.GetConnectedAtoms(neighbour0);

            // SOME LISTS ARE CREATED FOR STORING OF INTERESTING ATOMS AND BONDS DURING DETECTION
            var singles = new List<int>(); // list of any bond not rotatable
            var doubles = new List<int>(); // list with only double bonds
            var atoms = new List<int>();   // list with all the atoms in spheres
                                           // atoms.Add( int.ValueOf( mol.Atoms.IndexOf(neighbors[0]) ) );
            var bondsInCycloex = new List<int>(); // list for bonds in cycloexane-like rings

            // 2', 3', 4', 5', 6', and 7' bonds up to the target are detected:
            IBond secondBond;  // (remember that first bond is proton bond)
            IBond thirdBond;   //
            IBond fourthBond;  //
            IBond fifthBond;   //
            IBond sixthBond;   //
            IBond seventhBond; //

            // definition of some variables used in the main FOR loop for detection of interesting atoms and bonds:
            bool theBondIsInA6MemberedRing; // this is like a flag for bonds which are in cycloexane-like rings (rings with more than 4 at.)
            BondOrder bondOrder;
            int bondNumber;
            int sphere;

            // THIS MAIN FOR LOOP DETECT RIGID BONDS IN 7 SPHERES:
            foreach (var curAtomSecond in atomsInSecondSphere)
            {
                secondBond = mol.GetBond(neighbour0, curAtomSecond);
                if (mol.Atoms.IndexOf(curAtomSecond) != atomPosition && GetIfBondIsNotRotatable(mol, secondBond, detected))
                {
                    sphere = 2;
                    bondOrder = secondBond.Order;
                    bondNumber = mol.Bonds.IndexOf(secondBond);
                    theBondIsInA6MemberedRing = false;
                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex, mol.Atoms.IndexOf(curAtomSecond), atoms, sphere, theBondIsInA6MemberedRing);
                    var atomsInThirdSphere = mol.GetConnectedAtoms(curAtomSecond);
                    foreach (var curAtomThird in atomsInThirdSphere)
                    {
                        thirdBond = mol.GetBond(curAtomThird, curAtomSecond);
                        // IF THE ATOMS IS IN THE THIRD SPHERE AND IN A CYCLOEXANE-LIKE RING, IT IS STORED IN THE PROPER LIST:
                        if (mol.Atoms.IndexOf(curAtomThird) != atomPosition
                         && GetIfBondIsNotRotatable(mol, thirdBond, detected))
                        {
                            sphere = 3;
                            bondOrder = thirdBond.Order;
                            bondNumber = mol.Bonds.IndexOf(thirdBond);
                            theBondIsInA6MemberedRing = false;

                            // if the bond is in a cyclohexane-like ring (a ring with 5 or more atoms, not aromatic)
                            // the bool "theBondIsInA6MemberedRing" is set to true
                            if (!thirdBond.IsAromatic)
                            {
                                if (!curAtomThird.Equals(neighbour0))
                                {
                                    var rsAtom = varRingSet.GetRings(thirdBond);
                                    foreach (var ring in rsAtom)
                                    {
                                        if (ring.RingSize > 4 && ring.Contains(thirdBond))
                                        {
                                            theBondIsInA6MemberedRing = true;
                                        }
                                    }
                                }
                            }
                            CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                    mol.Atoms.IndexOf(curAtomThird), atoms, sphere, theBondIsInA6MemberedRing);
                            theBondIsInA6MemberedRing = false;
                            var atomsInFourthSphere = mol.GetConnectedAtoms(curAtomThird);
                            foreach (var curAtomFourth in atomsInFourthSphere)
                            {
                                fourthBond = mol.GetBond(curAtomThird, curAtomFourth);
                                if (mol.Atoms.IndexOf(curAtomFourth) != atomPosition
                                        && GetIfBondIsNotRotatable(mol, fourthBond, detected))
                                {
                                    sphere = 4;
                                    bondOrder = fourthBond.Order;
                                    bondNumber = mol.Bonds.IndexOf(fourthBond);
                                    theBondIsInA6MemberedRing = false;
                                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                            mol.Atoms.IndexOf(curAtomFourth), atoms, sphere,
                                            theBondIsInA6MemberedRing);
                                    var atomsInFifthSphere = mol.GetConnectedAtoms(curAtomFourth);
                                    foreach (var curAtomFifth in atomsInFifthSphere)
                                    {
                                        fifthBond = mol.GetBond(curAtomFifth, curAtomFourth);
                                        if (mol.Atoms.IndexOf(curAtomFifth) != atomPosition
                                                && GetIfBondIsNotRotatable(mol, fifthBond, detected))
                                        {
                                            sphere = 5;
                                            bondOrder = fifthBond.Order;
                                            bondNumber = mol.Bonds.IndexOf(fifthBond);
                                            theBondIsInA6MemberedRing = false;
                                            CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                    bondsInCycloex, mol.Atoms.IndexOf(curAtomFifth), atoms,
                                                    sphere, theBondIsInA6MemberedRing);
                                            var atomsInSixthSphere = mol.GetConnectedAtoms(curAtomFifth);
                                            foreach (var curAtomSixth in atomsInSixthSphere)
                                            {
                                                sixthBond = mol.GetBond(curAtomFifth, curAtomSixth);
                                                if (mol.Atoms.IndexOf(curAtomSixth) != atomPosition
                                                        && GetIfBondIsNotRotatable(mol, sixthBond, detected))
                                                {
                                                    sphere = 6;
                                                    bondOrder = sixthBond.Order;
                                                    bondNumber = mol.Bonds.IndexOf(sixthBond);
                                                    theBondIsInA6MemberedRing = false;
                                                    CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                            bondsInCycloex,
                                                            mol.Atoms.IndexOf(curAtomSixth), atoms, sphere,
                                                            theBondIsInA6MemberedRing);
                                                    var atomsInSeventhSphere = mol.GetConnectedAtoms(curAtomSixth);
                                                    foreach (var curAtomSeventh in atomsInSeventhSphere)
                                                    {
                                                        seventhBond = mol.GetBond(curAtomSeventh, curAtomSixth);
                                                        if (mol.Atoms.IndexOf(curAtomSeventh) != atomPosition && GetIfBondIsNotRotatable(mol, seventhBond, detected))
                                                        {
                                                            sphere = 7;
                                                            bondOrder = seventhBond.Order;
                                                            bondNumber = mol.Bonds.IndexOf(seventhBond);
                                                            theBondIsInA6MemberedRing = false;
                                                            CheckAndStore(bondNumber, bondOrder,
                                                                    singles, doubles, bondsInCycloex,
                                                                    mol.Atoms.IndexOf(curAtomSeventh),
                                                                    atoms, sphere,
                                                                    theBondIsInA6MemberedRing);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (MakeDescriptorLastStage(
                    rdfProtonCalculatedValues, 
                    atom, 
                    clonedAtom,
                    mol,
                    neighbour0, 
                    singles,
                    doubles,
                    atoms, 
                    bondsInCycloex))
                return new Result(rdfProtonCalculatedValues);
            else
                return new Result(new CDKException("Some error occurred."));
        }

        // Others definitions

        private static bool GetIfBondIsNotRotatable(IAtomContainer mol, IBond bond, IAtomContainer detected)
        {
            bool isBondNotRotatable = false;
            int counter = 0;
            IAtom atom0 = bond.Atoms[0];
            IAtom atom1 = bond.Atoms[1];
            if (detected != null)
            {
                if (detected.Contains(bond)) counter += 1;
            }
            if (atom0.IsInRing)
            {
                if (atom1.IsInRing)
                {
                    counter += 1;
                }
                else
                {
                    if (atom1.AtomicNumber.Equals(AtomicNumbers.H))
                        counter += 1;
                    else
                        counter += 0;
                }
            }
            if (atom0.AtomicNumber.Equals(AtomicNumbers.N) && atom1.AtomicNumber.Equals(AtomicNumbers.C))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom1)) 
                    counter += 1;
            if (atom0.AtomicNumber.Equals(AtomicNumbers.C) && atom1.AtomicNumber.Equals(AtomicNumbers.N))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom0)) 
                    counter += 1;
            if (counter > 0) 
                isBondNotRotatable = true;
            return isBondNotRotatable;
        }

        private static bool GetIfACarbonIsDoubleBondedToAnOxygen(IAtomContainer mol, IAtom carbonAtom)
        {
            bool isDoubleBondedToOxygen = false;
            var neighToCarbon = mol.GetConnectedAtoms(carbonAtom);
            IBond tmpBond;
            int counter = 0;
            foreach (var neighbour in neighToCarbon)
            {
                if (neighbour.AtomicNumber.Equals(AtomicNumbers.O))
                {
                    tmpBond = mol.GetBond(neighbour, carbonAtom);
                    if (tmpBond.Order == BondOrder.Double) 
                        counter += 1;
                }
            }
            if (counter > 0) 
                isDoubleBondedToOxygen = true;
            return isDoubleBondedToOxygen;
        }

        // this method calculates the angle between two bonds given coordinates of their atoms

        internal static double CalculateAngleBetweenTwoLines(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var firstLine = a - b;
            var secondLine = c - d;
            var firstVec = firstLine;
            var secondVec = secondLine;
            return Vectors.Angle(firstVec, secondVec);
        }

        // this method store atoms and bonds in proper lists:
        private static void CheckAndStore(int bondToStore, BondOrder bondOrder, List<int> singleVec,
                List<int> doubleVec, List<int> cycloexVec, int a1, List<int> atomVec,
                int sphere, bool isBondInCycloex)
        {
            if (!atomVec.Contains(a1))
                if (sphere < 6) 
                    atomVec.Add(a1);
            if (!cycloexVec.Contains(bondToStore))
                if (isBondInCycloex)
                    cycloexVec.Add(bondToStore);
            if (bondOrder == BondOrder.Double)
                if (!doubleVec.Contains(bondToStore))
                    doubleVec.Add(bondToStore);
            if (bondOrder == BondOrder.Single)
                if (!singleVec.Contains(bondToStore))
                    singleVec.Add(bondToStore);
        }

        // generic method for calculation of distance btw 2 atoms
        internal static double CalculateDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            var firstPoint = atom1.Point3D.Value;
            var secondPoint = atom2.Point3D.Value;
            var distance = Vector3.Distance(firstPoint, secondPoint);
            return distance;
        }

        // given a double bond
        // this method returns a bond bonded to this double bond
        internal static int GetNearestBondtoAGivenAtom(IAtomContainer mol, IAtom atom, IBond bond)
        {
            int nearestBond = -1;
            double distance = 0;
            var atom0 = bond.Atoms[0];
            var bondsAtLeft = mol.GetConnectedBonds(atom0);
            foreach (var curBond in bondsAtLeft)
            {
                var values = CalculateDistanceBetweenAtomAndBond(atom, curBond);
                var partial = mol.Bonds.IndexOf(curBond);
                if (nearestBond == -1)
                {
                    nearestBond = mol.Bonds.IndexOf(curBond);
                    distance = values[0];
                }
                else
                {
                    if (values[0] < distance)
                    {
                        nearestBond = partial;
                    }
                }
            }
            return nearestBond;
        }

        // method which calculated distance btw an atom and the middle point of a bond
        // and returns distance and coordinates of middle point
        internal static double[] CalculateDistanceBetweenAtomAndBond(IAtom proton, IBond theBond)
        {
            var middlePoint = theBond.GetGeometric3DCenter();
            var protonPoint = proton.Point3D.Value;
            var values = new double[4];
            values[0] = Vector3.Distance(middlePoint, protonPoint);
            values[1] = middlePoint.X;
            values[2] = middlePoint.Y;
            values[3] = middlePoint.Z;
            return values;
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
    public partial class RDFProtonDescriptorGDR
    {
        [DescriptorResult(prefix: prefix, baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(Exception e) : base(e) { }

            public Result(IReadOnlyList<double> values) : base(values) { }
        }

        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;
        private IEnumerable<IAtomContainer> varAtomContainerSet;
        private readonly IAtomContainer mol;

        public RDFProtonDescriptorGDR(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedContainer = (IAtomContainer)container.Clone();

            /////////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            /////////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            mol = CDK.Builder.NewAtomContainer(clonedContainer);

            // DETECTION OF pi SYSTEMS
            varAtomContainerSet = ConjugatedPiSystemsDetector.Detect(mol);
            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(mol, true);

            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);
                Aromaticity.CDKLegacy.Apply(clonedContainer);
            }

            this.container = container;
        }

        private IRingSet allRings;
        private readonly object syncAllRings = new object();

        private IRingSet AllRings
        {
            get
            {
                if (allRings == null)
                    lock (syncAllRings)
                    {
                        allRings = new AllRingsFinder().FindAllRings(clonedContainer);
                    }
                return allRings;
            }
        }

        public virtual Result Calculate(IAtom atom)
        {
            return Calculate(atom, null);
        }

        public Result Calculate(IAtom atom, IRingSet precalculatedringset)
        {
            var atomPosition = container.Atoms.IndexOf(atom);
            var clonedAtom = clonedContainer.Atoms[atomPosition];
            var rdfProtonCalculatedValues = new List<double>(desc_length);
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                return new Result(new CDKException("Invalid atom specified"));
            }

            // ///////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            // ///////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            var varRingSet = precalculatedringset ?? AllRings;

            // SET ISINRING FLAGS FOR BONDS
            var bondsInContainer = clonedContainer.Bonds;
            foreach (var bond in bondsInContainer)
            {
                var ringsWithThisBond = varRingSet.GetRings(bond);
                if (ringsWithThisBond.Any())
                {
                    bond.IsInRing = true;
                }
            }
            // SET ISINRING FLAGS FOR ATOMS
            foreach (var vatom in clonedContainer.Atoms)
            {
                var ringsWithThisAtom = varRingSet.GetRings(vatom);
                if (ringsWithThisAtom.Any())
                {
                    vatom.IsInRing = true;
                }
            }

            var detected = varAtomContainerSet.FirstOrDefault();

            // neighbors[0] is the atom joined to the target proton:
            var neighbors = mol.GetConnectedAtoms(clonedAtom);
            var neighbour0 = neighbors.First();

            // 2', 3', 4', 5', 6', and 7' atoms up to the target are detected:
            var atomsInSecondSphere = mol.GetConnectedAtoms(neighbour0);

            // SOME LISTS ARE CREATED FOR STORING OF INTERESTING ATOMS AND BONDS DURING DETECTION
            var singles = new List<int>(); // list of any bond not rotatable
            var doubles = new List<int>(); // list with only double bonds
            var atoms = new List<int>();   // list with all the atoms in spheres
                                           // atoms.Add( int.ValueOf( mol.Atoms.IndexOf(neighbors[0]) ) );
            var bondsInCycloex = new List<int>(); // list for bonds in cycloexane-like rings

            // 2', 3', 4', 5', 6', and 7' bonds up to the target are detected:
            IBond secondBond;  // (remember that first bond is proton bond)
            IBond thirdBond;   //
            IBond fourthBond;  //
            IBond fifthBond;   //
            IBond sixthBond;   //
            IBond seventhBond; //

            // definition of some variables used in the main FOR loop for detection of interesting atoms and bonds:
            bool theBondIsInA6MemberedRing; // this is like a flag for bonds which are in cycloexane-like rings (rings with more than 4 at.)
            BondOrder bondOrder;
            int bondNumber;
            int sphere;

            // THIS MAIN FOR LOOP DETECT RIGID BONDS IN 7 SPHERES:
            foreach (var curAtomSecond in atomsInSecondSphere)
            {
                secondBond = mol.GetBond(neighbour0, curAtomSecond);
                if (mol.Atoms.IndexOf(curAtomSecond) != atomPosition && GetIfBondIsNotRotatable(mol, secondBond, detected))
                {
                    sphere = 2;
                    bondOrder = secondBond.Order;
                    bondNumber = mol.Bonds.IndexOf(secondBond);
                    theBondIsInA6MemberedRing = false;
                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex, mol.Atoms.IndexOf(curAtomSecond), atoms, sphere, theBondIsInA6MemberedRing);
                    var atomsInThirdSphere = mol.GetConnectedAtoms(curAtomSecond);
                    foreach (var curAtomThird in atomsInThirdSphere)
                    {
                        thirdBond = mol.GetBond(curAtomThird, curAtomSecond);
                        // IF THE ATOMS IS IN THE THIRD SPHERE AND IN A CYCLOEXANE-LIKE RING, IT IS STORED IN THE PROPER LIST:
                        if (mol.Atoms.IndexOf(curAtomThird) != atomPosition
                         && GetIfBondIsNotRotatable(mol, thirdBond, detected))
                        {
                            sphere = 3;
                            bondOrder = thirdBond.Order;
                            bondNumber = mol.Bonds.IndexOf(thirdBond);
                            theBondIsInA6MemberedRing = false;

                            // if the bond is in a cyclohexane-like ring (a ring with 5 or more atoms, not aromatic)
                            // the bool "theBondIsInA6MemberedRing" is set to true
                            if (!thirdBond.IsAromatic)
                            {
                                if (!curAtomThird.Equals(neighbour0))
                                {
                                    var rsAtom = varRingSet.GetRings(thirdBond);
                                    foreach (var ring in rsAtom)
                                    {
                                        if (ring.RingSize > 4 && ring.Contains(thirdBond))
                                        {
                                            theBondIsInA6MemberedRing = true;
                                        }
                                    }
                                }
                            }
                            CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                    mol.Atoms.IndexOf(curAtomThird), atoms, sphere, theBondIsInA6MemberedRing);
                            theBondIsInA6MemberedRing = false;
                            var atomsInFourthSphere = mol.GetConnectedAtoms(curAtomThird);
                            foreach (var curAtomFourth in atomsInFourthSphere)
                            {
                                fourthBond = mol.GetBond(curAtomThird, curAtomFourth);
                                if (mol.Atoms.IndexOf(curAtomFourth) != atomPosition
                                        && GetIfBondIsNotRotatable(mol, fourthBond, detected))
                                {
                                    sphere = 4;
                                    bondOrder = fourthBond.Order;
                                    bondNumber = mol.Bonds.IndexOf(fourthBond);
                                    theBondIsInA6MemberedRing = false;
                                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                            mol.Atoms.IndexOf(curAtomFourth), atoms, sphere,
                                            theBondIsInA6MemberedRing);
                                    var atomsInFifthSphere = mol.GetConnectedAtoms(curAtomFourth);
                                    foreach (var curAtomFifth in atomsInFifthSphere)
                                    {
                                        fifthBond = mol.GetBond(curAtomFifth, curAtomFourth);
                                        if (mol.Atoms.IndexOf(curAtomFifth) != atomPosition
                                                && GetIfBondIsNotRotatable(mol, fifthBond, detected))
                                        {
                                            sphere = 5;
                                            bondOrder = fifthBond.Order;
                                            bondNumber = mol.Bonds.IndexOf(fifthBond);
                                            theBondIsInA6MemberedRing = false;
                                            CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                    bondsInCycloex, mol.Atoms.IndexOf(curAtomFifth), atoms,
                                                    sphere, theBondIsInA6MemberedRing);
                                            var atomsInSixthSphere = mol.GetConnectedAtoms(curAtomFifth);
                                            foreach (var curAtomSixth in atomsInSixthSphere)
                                            {
                                                sixthBond = mol.GetBond(curAtomFifth, curAtomSixth);
                                                if (mol.Atoms.IndexOf(curAtomSixth) != atomPosition
                                                        && GetIfBondIsNotRotatable(mol, sixthBond, detected))
                                                {
                                                    sphere = 6;
                                                    bondOrder = sixthBond.Order;
                                                    bondNumber = mol.Bonds.IndexOf(sixthBond);
                                                    theBondIsInA6MemberedRing = false;
                                                    CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                            bondsInCycloex,
                                                            mol.Atoms.IndexOf(curAtomSixth), atoms, sphere,
                                                            theBondIsInA6MemberedRing);
                                                    var atomsInSeventhSphere = mol.GetConnectedAtoms(curAtomSixth);
                                                    foreach (var curAtomSeventh in atomsInSeventhSphere)
                                                    {
                                                        seventhBond = mol.GetBond(curAtomSeventh, curAtomSixth);
                                                        if (mol.Atoms.IndexOf(curAtomSeventh) != atomPosition && GetIfBondIsNotRotatable(mol, seventhBond, detected))
                                                        {
                                                            sphere = 7;
                                                            bondOrder = seventhBond.Order;
                                                            bondNumber = mol.Bonds.IndexOf(seventhBond);
                                                            theBondIsInA6MemberedRing = false;
                                                            CheckAndStore(bondNumber, bondOrder,
                                                                    singles, doubles, bondsInCycloex,
                                                                    mol.Atoms.IndexOf(curAtomSeventh),
                                                                    atoms, sphere,
                                                                    theBondIsInA6MemberedRing);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (MakeDescriptorLastStage(
                    rdfProtonCalculatedValues, 
                    atom, 
                    clonedAtom,
                    mol,
                    neighbour0, 
                    singles,
                    doubles,
                    atoms, 
                    bondsInCycloex))
                return new Result(rdfProtonCalculatedValues);
            else
                return new Result(new CDKException("Some error occurred."));
        }

        // Others definitions

        private static bool GetIfBondIsNotRotatable(IAtomContainer mol, IBond bond, IAtomContainer detected)
        {
            bool isBondNotRotatable = false;
            int counter = 0;
            IAtom atom0 = bond.Atoms[0];
            IAtom atom1 = bond.Atoms[1];
            if (detected != null)
            {
                if (detected.Contains(bond)) counter += 1;
            }
            if (atom0.IsInRing)
            {
                if (atom1.IsInRing)
                {
                    counter += 1;
                }
                else
                {
                    if (atom1.AtomicNumber.Equals(AtomicNumbers.H))
                        counter += 1;
                    else
                        counter += 0;
                }
            }
            if (atom0.AtomicNumber.Equals(AtomicNumbers.N) && atom1.AtomicNumber.Equals(AtomicNumbers.C))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom1)) 
                    counter += 1;
            if (atom0.AtomicNumber.Equals(AtomicNumbers.C) && atom1.AtomicNumber.Equals(AtomicNumbers.N))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom0)) 
                    counter += 1;
            if (counter > 0) 
                isBondNotRotatable = true;
            return isBondNotRotatable;
        }

        private static bool GetIfACarbonIsDoubleBondedToAnOxygen(IAtomContainer mol, IAtom carbonAtom)
        {
            bool isDoubleBondedToOxygen = false;
            var neighToCarbon = mol.GetConnectedAtoms(carbonAtom);
            IBond tmpBond;
            int counter = 0;
            foreach (var neighbour in neighToCarbon)
            {
                if (neighbour.AtomicNumber.Equals(AtomicNumbers.O))
                {
                    tmpBond = mol.GetBond(neighbour, carbonAtom);
                    if (tmpBond.Order == BondOrder.Double) 
                        counter += 1;
                }
            }
            if (counter > 0) 
                isDoubleBondedToOxygen = true;
            return isDoubleBondedToOxygen;
        }

        // this method calculates the angle between two bonds given coordinates of their atoms

        internal static double CalculateAngleBetweenTwoLines(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var firstLine = a - b;
            var secondLine = c - d;
            var firstVec = firstLine;
            var secondVec = secondLine;
            return Vectors.Angle(firstVec, secondVec);
        }

        // this method store atoms and bonds in proper lists:
        private static void CheckAndStore(int bondToStore, BondOrder bondOrder, List<int> singleVec,
                List<int> doubleVec, List<int> cycloexVec, int a1, List<int> atomVec,
                int sphere, bool isBondInCycloex)
        {
            if (!atomVec.Contains(a1))
                if (sphere < 6) 
                    atomVec.Add(a1);
            if (!cycloexVec.Contains(bondToStore))
                if (isBondInCycloex)
                    cycloexVec.Add(bondToStore);
            if (bondOrder == BondOrder.Double)
                if (!doubleVec.Contains(bondToStore))
                    doubleVec.Add(bondToStore);
            if (bondOrder == BondOrder.Single)
                if (!singleVec.Contains(bondToStore))
                    singleVec.Add(bondToStore);
        }

        // generic method for calculation of distance btw 2 atoms
        internal static double CalculateDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            var firstPoint = atom1.Point3D.Value;
            var secondPoint = atom2.Point3D.Value;
            var distance = Vector3.Distance(firstPoint, secondPoint);
            return distance;
        }

        // given a double bond
        // this method returns a bond bonded to this double bond
        internal static int GetNearestBondtoAGivenAtom(IAtomContainer mol, IAtom atom, IBond bond)
        {
            int nearestBond = -1;
            double distance = 0;
            var atom0 = bond.Atoms[0];
            var bondsAtLeft = mol.GetConnectedBonds(atom0);
            foreach (var curBond in bondsAtLeft)
            {
                var values = CalculateDistanceBetweenAtomAndBond(atom, curBond);
                var partial = mol.Bonds.IndexOf(curBond);
                if (nearestBond == -1)
                {
                    nearestBond = mol.Bonds.IndexOf(curBond);
                    distance = values[0];
                }
                else
                {
                    if (values[0] < distance)
                    {
                        nearestBond = partial;
                    }
                }
            }
            return nearestBond;
        }

        // method which calculated distance btw an atom and the middle point of a bond
        // and returns distance and coordinates of middle point
        internal static double[] CalculateDistanceBetweenAtomAndBond(IAtom proton, IBond theBond)
        {
            var middlePoint = theBond.GetGeometric3DCenter();
            var protonPoint = proton.Point3D.Value;
            var values = new double[4];
            values[0] = Vector3.Distance(middlePoint, protonPoint);
            values[1] = middlePoint.X;
            values[2] = middlePoint.Y;
            values[3] = middlePoint.Z;
            return values;
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
    public partial class RDFProtonDescriptorGHR
    {
        [DescriptorResult(prefix: prefix, baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(Exception e) : base(e) { }

            public Result(IReadOnlyList<double> values) : base(values) { }
        }

        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;
        private IEnumerable<IAtomContainer> varAtomContainerSet;
        private readonly IAtomContainer mol;

        public RDFProtonDescriptorGHR(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedContainer = (IAtomContainer)container.Clone();

            /////////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            /////////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            mol = CDK.Builder.NewAtomContainer(clonedContainer);

            // DETECTION OF pi SYSTEMS
            varAtomContainerSet = ConjugatedPiSystemsDetector.Detect(mol);
            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(mol, true);

            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);
                Aromaticity.CDKLegacy.Apply(clonedContainer);
            }

            this.container = container;
        }

        private IRingSet allRings;
        private readonly object syncAllRings = new object();

        private IRingSet AllRings
        {
            get
            {
                if (allRings == null)
                    lock (syncAllRings)
                    {
                        allRings = new AllRingsFinder().FindAllRings(clonedContainer);
                    }
                return allRings;
            }
        }

        public virtual Result Calculate(IAtom atom)
        {
            return Calculate(atom, null);
        }

        public Result Calculate(IAtom atom, IRingSet precalculatedringset)
        {
            var atomPosition = container.Atoms.IndexOf(atom);
            var clonedAtom = clonedContainer.Atoms[atomPosition];
            var rdfProtonCalculatedValues = new List<double>(desc_length);
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                return new Result(new CDKException("Invalid atom specified"));
            }

            // ///////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            // ///////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            var varRingSet = precalculatedringset ?? AllRings;

            // SET ISINRING FLAGS FOR BONDS
            var bondsInContainer = clonedContainer.Bonds;
            foreach (var bond in bondsInContainer)
            {
                var ringsWithThisBond = varRingSet.GetRings(bond);
                if (ringsWithThisBond.Any())
                {
                    bond.IsInRing = true;
                }
            }
            // SET ISINRING FLAGS FOR ATOMS
            foreach (var vatom in clonedContainer.Atoms)
            {
                var ringsWithThisAtom = varRingSet.GetRings(vatom);
                if (ringsWithThisAtom.Any())
                {
                    vatom.IsInRing = true;
                }
            }

            var detected = varAtomContainerSet.FirstOrDefault();

            // neighbors[0] is the atom joined to the target proton:
            var neighbors = mol.GetConnectedAtoms(clonedAtom);
            var neighbour0 = neighbors.First();

            // 2', 3', 4', 5', 6', and 7' atoms up to the target are detected:
            var atomsInSecondSphere = mol.GetConnectedAtoms(neighbour0);

            // SOME LISTS ARE CREATED FOR STORING OF INTERESTING ATOMS AND BONDS DURING DETECTION
            var singles = new List<int>(); // list of any bond not rotatable
            var doubles = new List<int>(); // list with only double bonds
            var atoms = new List<int>();   // list with all the atoms in spheres
                                           // atoms.Add( int.ValueOf( mol.Atoms.IndexOf(neighbors[0]) ) );
            var bondsInCycloex = new List<int>(); // list for bonds in cycloexane-like rings

            // 2', 3', 4', 5', 6', and 7' bonds up to the target are detected:
            IBond secondBond;  // (remember that first bond is proton bond)
            IBond thirdBond;   //
            IBond fourthBond;  //
            IBond fifthBond;   //
            IBond sixthBond;   //
            IBond seventhBond; //

            // definition of some variables used in the main FOR loop for detection of interesting atoms and bonds:
            bool theBondIsInA6MemberedRing; // this is like a flag for bonds which are in cycloexane-like rings (rings with more than 4 at.)
            BondOrder bondOrder;
            int bondNumber;
            int sphere;

            // THIS MAIN FOR LOOP DETECT RIGID BONDS IN 7 SPHERES:
            foreach (var curAtomSecond in atomsInSecondSphere)
            {
                secondBond = mol.GetBond(neighbour0, curAtomSecond);
                if (mol.Atoms.IndexOf(curAtomSecond) != atomPosition && GetIfBondIsNotRotatable(mol, secondBond, detected))
                {
                    sphere = 2;
                    bondOrder = secondBond.Order;
                    bondNumber = mol.Bonds.IndexOf(secondBond);
                    theBondIsInA6MemberedRing = false;
                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex, mol.Atoms.IndexOf(curAtomSecond), atoms, sphere, theBondIsInA6MemberedRing);
                    var atomsInThirdSphere = mol.GetConnectedAtoms(curAtomSecond);
                    foreach (var curAtomThird in atomsInThirdSphere)
                    {
                        thirdBond = mol.GetBond(curAtomThird, curAtomSecond);
                        // IF THE ATOMS IS IN THE THIRD SPHERE AND IN A CYCLOEXANE-LIKE RING, IT IS STORED IN THE PROPER LIST:
                        if (mol.Atoms.IndexOf(curAtomThird) != atomPosition
                         && GetIfBondIsNotRotatable(mol, thirdBond, detected))
                        {
                            sphere = 3;
                            bondOrder = thirdBond.Order;
                            bondNumber = mol.Bonds.IndexOf(thirdBond);
                            theBondIsInA6MemberedRing = false;

                            // if the bond is in a cyclohexane-like ring (a ring with 5 or more atoms, not aromatic)
                            // the bool "theBondIsInA6MemberedRing" is set to true
                            if (!thirdBond.IsAromatic)
                            {
                                if (!curAtomThird.Equals(neighbour0))
                                {
                                    var rsAtom = varRingSet.GetRings(thirdBond);
                                    foreach (var ring in rsAtom)
                                    {
                                        if (ring.RingSize > 4 && ring.Contains(thirdBond))
                                        {
                                            theBondIsInA6MemberedRing = true;
                                        }
                                    }
                                }
                            }
                            CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                    mol.Atoms.IndexOf(curAtomThird), atoms, sphere, theBondIsInA6MemberedRing);
                            theBondIsInA6MemberedRing = false;
                            var atomsInFourthSphere = mol.GetConnectedAtoms(curAtomThird);
                            foreach (var curAtomFourth in atomsInFourthSphere)
                            {
                                fourthBond = mol.GetBond(curAtomThird, curAtomFourth);
                                if (mol.Atoms.IndexOf(curAtomFourth) != atomPosition
                                        && GetIfBondIsNotRotatable(mol, fourthBond, detected))
                                {
                                    sphere = 4;
                                    bondOrder = fourthBond.Order;
                                    bondNumber = mol.Bonds.IndexOf(fourthBond);
                                    theBondIsInA6MemberedRing = false;
                                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                            mol.Atoms.IndexOf(curAtomFourth), atoms, sphere,
                                            theBondIsInA6MemberedRing);
                                    var atomsInFifthSphere = mol.GetConnectedAtoms(curAtomFourth);
                                    foreach (var curAtomFifth in atomsInFifthSphere)
                                    {
                                        fifthBond = mol.GetBond(curAtomFifth, curAtomFourth);
                                        if (mol.Atoms.IndexOf(curAtomFifth) != atomPosition
                                                && GetIfBondIsNotRotatable(mol, fifthBond, detected))
                                        {
                                            sphere = 5;
                                            bondOrder = fifthBond.Order;
                                            bondNumber = mol.Bonds.IndexOf(fifthBond);
                                            theBondIsInA6MemberedRing = false;
                                            CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                    bondsInCycloex, mol.Atoms.IndexOf(curAtomFifth), atoms,
                                                    sphere, theBondIsInA6MemberedRing);
                                            var atomsInSixthSphere = mol.GetConnectedAtoms(curAtomFifth);
                                            foreach (var curAtomSixth in atomsInSixthSphere)
                                            {
                                                sixthBond = mol.GetBond(curAtomFifth, curAtomSixth);
                                                if (mol.Atoms.IndexOf(curAtomSixth) != atomPosition
                                                        && GetIfBondIsNotRotatable(mol, sixthBond, detected))
                                                {
                                                    sphere = 6;
                                                    bondOrder = sixthBond.Order;
                                                    bondNumber = mol.Bonds.IndexOf(sixthBond);
                                                    theBondIsInA6MemberedRing = false;
                                                    CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                            bondsInCycloex,
                                                            mol.Atoms.IndexOf(curAtomSixth), atoms, sphere,
                                                            theBondIsInA6MemberedRing);
                                                    var atomsInSeventhSphere = mol.GetConnectedAtoms(curAtomSixth);
                                                    foreach (var curAtomSeventh in atomsInSeventhSphere)
                                                    {
                                                        seventhBond = mol.GetBond(curAtomSeventh, curAtomSixth);
                                                        if (mol.Atoms.IndexOf(curAtomSeventh) != atomPosition && GetIfBondIsNotRotatable(mol, seventhBond, detected))
                                                        {
                                                            sphere = 7;
                                                            bondOrder = seventhBond.Order;
                                                            bondNumber = mol.Bonds.IndexOf(seventhBond);
                                                            theBondIsInA6MemberedRing = false;
                                                            CheckAndStore(bondNumber, bondOrder,
                                                                    singles, doubles, bondsInCycloex,
                                                                    mol.Atoms.IndexOf(curAtomSeventh),
                                                                    atoms, sphere,
                                                                    theBondIsInA6MemberedRing);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (MakeDescriptorLastStage(
                    rdfProtonCalculatedValues, 
                    atom, 
                    clonedAtom,
                    mol,
                    neighbour0, 
                    singles,
                    doubles,
                    atoms, 
                    bondsInCycloex))
                return new Result(rdfProtonCalculatedValues);
            else
                return new Result(new CDKException("Some error occurred."));
        }

        // Others definitions

        private static bool GetIfBondIsNotRotatable(IAtomContainer mol, IBond bond, IAtomContainer detected)
        {
            bool isBondNotRotatable = false;
            int counter = 0;
            IAtom atom0 = bond.Atoms[0];
            IAtom atom1 = bond.Atoms[1];
            if (detected != null)
            {
                if (detected.Contains(bond)) counter += 1;
            }
            if (atom0.IsInRing)
            {
                if (atom1.IsInRing)
                {
                    counter += 1;
                }
                else
                {
                    if (atom1.AtomicNumber.Equals(AtomicNumbers.H))
                        counter += 1;
                    else
                        counter += 0;
                }
            }
            if (atom0.AtomicNumber.Equals(AtomicNumbers.N) && atom1.AtomicNumber.Equals(AtomicNumbers.C))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom1)) 
                    counter += 1;
            if (atom0.AtomicNumber.Equals(AtomicNumbers.C) && atom1.AtomicNumber.Equals(AtomicNumbers.N))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom0)) 
                    counter += 1;
            if (counter > 0) 
                isBondNotRotatable = true;
            return isBondNotRotatable;
        }

        private static bool GetIfACarbonIsDoubleBondedToAnOxygen(IAtomContainer mol, IAtom carbonAtom)
        {
            bool isDoubleBondedToOxygen = false;
            var neighToCarbon = mol.GetConnectedAtoms(carbonAtom);
            IBond tmpBond;
            int counter = 0;
            foreach (var neighbour in neighToCarbon)
            {
                if (neighbour.AtomicNumber.Equals(AtomicNumbers.O))
                {
                    tmpBond = mol.GetBond(neighbour, carbonAtom);
                    if (tmpBond.Order == BondOrder.Double) 
                        counter += 1;
                }
            }
            if (counter > 0) 
                isDoubleBondedToOxygen = true;
            return isDoubleBondedToOxygen;
        }

        // this method calculates the angle between two bonds given coordinates of their atoms

        internal static double CalculateAngleBetweenTwoLines(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var firstLine = a - b;
            var secondLine = c - d;
            var firstVec = firstLine;
            var secondVec = secondLine;
            return Vectors.Angle(firstVec, secondVec);
        }

        // this method store atoms and bonds in proper lists:
        private static void CheckAndStore(int bondToStore, BondOrder bondOrder, List<int> singleVec,
                List<int> doubleVec, List<int> cycloexVec, int a1, List<int> atomVec,
                int sphere, bool isBondInCycloex)
        {
            if (!atomVec.Contains(a1))
                if (sphere < 6) 
                    atomVec.Add(a1);
            if (!cycloexVec.Contains(bondToStore))
                if (isBondInCycloex)
                    cycloexVec.Add(bondToStore);
            if (bondOrder == BondOrder.Double)
                if (!doubleVec.Contains(bondToStore))
                    doubleVec.Add(bondToStore);
            if (bondOrder == BondOrder.Single)
                if (!singleVec.Contains(bondToStore))
                    singleVec.Add(bondToStore);
        }

        // generic method for calculation of distance btw 2 atoms
        internal static double CalculateDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            var firstPoint = atom1.Point3D.Value;
            var secondPoint = atom2.Point3D.Value;
            var distance = Vector3.Distance(firstPoint, secondPoint);
            return distance;
        }

        // given a double bond
        // this method returns a bond bonded to this double bond
        internal static int GetNearestBondtoAGivenAtom(IAtomContainer mol, IAtom atom, IBond bond)
        {
            int nearestBond = -1;
            double distance = 0;
            var atom0 = bond.Atoms[0];
            var bondsAtLeft = mol.GetConnectedBonds(atom0);
            foreach (var curBond in bondsAtLeft)
            {
                var values = CalculateDistanceBetweenAtomAndBond(atom, curBond);
                var partial = mol.Bonds.IndexOf(curBond);
                if (nearestBond == -1)
                {
                    nearestBond = mol.Bonds.IndexOf(curBond);
                    distance = values[0];
                }
                else
                {
                    if (values[0] < distance)
                    {
                        nearestBond = partial;
                    }
                }
            }
            return nearestBond;
        }

        // method which calculated distance btw an atom and the middle point of a bond
        // and returns distance and coordinates of middle point
        internal static double[] CalculateDistanceBetweenAtomAndBond(IAtom proton, IBond theBond)
        {
            var middlePoint = theBond.GetGeometric3DCenter();
            var protonPoint = proton.Point3D.Value;
            var values = new double[4];
            values[0] = Vector3.Distance(middlePoint, protonPoint);
            values[1] = middlePoint.X;
            values[2] = middlePoint.Y;
            values[3] = middlePoint.Z;
            return values;
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
    public partial class RDFProtonDescriptorGHRTopology
    {
        [DescriptorResult(prefix: prefix, baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(Exception e) : base(e) { }

            public Result(IReadOnlyList<double> values) : base(values) { }
        }

        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;
        private IEnumerable<IAtomContainer> varAtomContainerSet;
        private readonly IAtomContainer mol;

        public RDFProtonDescriptorGHRTopology(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedContainer = (IAtomContainer)container.Clone();

            /////////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            /////////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            mol = CDK.Builder.NewAtomContainer(clonedContainer);

            // DETECTION OF pi SYSTEMS
            varAtomContainerSet = ConjugatedPiSystemsDetector.Detect(mol);
            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(mol, true);

            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);
                Aromaticity.CDKLegacy.Apply(clonedContainer);
            }

            this.container = container;
        }

        private IRingSet allRings;
        private readonly object syncAllRings = new object();

        private IRingSet AllRings
        {
            get
            {
                if (allRings == null)
                    lock (syncAllRings)
                    {
                        allRings = new AllRingsFinder().FindAllRings(clonedContainer);
                    }
                return allRings;
            }
        }

        public virtual Result Calculate(IAtom atom)
        {
            return Calculate(atom, null);
        }

        public Result Calculate(IAtom atom, IRingSet precalculatedringset)
        {
            var atomPosition = container.Atoms.IndexOf(atom);
            var clonedAtom = clonedContainer.Atoms[atomPosition];
            var rdfProtonCalculatedValues = new List<double>(desc_length);
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                return new Result(new CDKException("Invalid atom specified"));
            }

            // ///////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            // ///////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            var varRingSet = precalculatedringset ?? AllRings;

            // SET ISINRING FLAGS FOR BONDS
            var bondsInContainer = clonedContainer.Bonds;
            foreach (var bond in bondsInContainer)
            {
                var ringsWithThisBond = varRingSet.GetRings(bond);
                if (ringsWithThisBond.Any())
                {
                    bond.IsInRing = true;
                }
            }
            // SET ISINRING FLAGS FOR ATOMS
            foreach (var vatom in clonedContainer.Atoms)
            {
                var ringsWithThisAtom = varRingSet.GetRings(vatom);
                if (ringsWithThisAtom.Any())
                {
                    vatom.IsInRing = true;
                }
            }

            var detected = varAtomContainerSet.FirstOrDefault();

            // neighbors[0] is the atom joined to the target proton:
            var neighbors = mol.GetConnectedAtoms(clonedAtom);
            var neighbour0 = neighbors.First();

            // 2', 3', 4', 5', 6', and 7' atoms up to the target are detected:
            var atomsInSecondSphere = mol.GetConnectedAtoms(neighbour0);

            // SOME LISTS ARE CREATED FOR STORING OF INTERESTING ATOMS AND BONDS DURING DETECTION
            var singles = new List<int>(); // list of any bond not rotatable
            var doubles = new List<int>(); // list with only double bonds
            var atoms = new List<int>();   // list with all the atoms in spheres
                                           // atoms.Add( int.ValueOf( mol.Atoms.IndexOf(neighbors[0]) ) );
            var bondsInCycloex = new List<int>(); // list for bonds in cycloexane-like rings

            // 2', 3', 4', 5', 6', and 7' bonds up to the target are detected:
            IBond secondBond;  // (remember that first bond is proton bond)
            IBond thirdBond;   //
            IBond fourthBond;  //
            IBond fifthBond;   //
            IBond sixthBond;   //
            IBond seventhBond; //

            // definition of some variables used in the main FOR loop for detection of interesting atoms and bonds:
            bool theBondIsInA6MemberedRing; // this is like a flag for bonds which are in cycloexane-like rings (rings with more than 4 at.)
            BondOrder bondOrder;
            int bondNumber;
            int sphere;

            // THIS MAIN FOR LOOP DETECT RIGID BONDS IN 7 SPHERES:
            foreach (var curAtomSecond in atomsInSecondSphere)
            {
                secondBond = mol.GetBond(neighbour0, curAtomSecond);
                if (mol.Atoms.IndexOf(curAtomSecond) != atomPosition && GetIfBondIsNotRotatable(mol, secondBond, detected))
                {
                    sphere = 2;
                    bondOrder = secondBond.Order;
                    bondNumber = mol.Bonds.IndexOf(secondBond);
                    theBondIsInA6MemberedRing = false;
                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex, mol.Atoms.IndexOf(curAtomSecond), atoms, sphere, theBondIsInA6MemberedRing);
                    var atomsInThirdSphere = mol.GetConnectedAtoms(curAtomSecond);
                    foreach (var curAtomThird in atomsInThirdSphere)
                    {
                        thirdBond = mol.GetBond(curAtomThird, curAtomSecond);
                        // IF THE ATOMS IS IN THE THIRD SPHERE AND IN A CYCLOEXANE-LIKE RING, IT IS STORED IN THE PROPER LIST:
                        if (mol.Atoms.IndexOf(curAtomThird) != atomPosition
                         && GetIfBondIsNotRotatable(mol, thirdBond, detected))
                        {
                            sphere = 3;
                            bondOrder = thirdBond.Order;
                            bondNumber = mol.Bonds.IndexOf(thirdBond);
                            theBondIsInA6MemberedRing = false;

                            // if the bond is in a cyclohexane-like ring (a ring with 5 or more atoms, not aromatic)
                            // the bool "theBondIsInA6MemberedRing" is set to true
                            if (!thirdBond.IsAromatic)
                            {
                                if (!curAtomThird.Equals(neighbour0))
                                {
                                    var rsAtom = varRingSet.GetRings(thirdBond);
                                    foreach (var ring in rsAtom)
                                    {
                                        if (ring.RingSize > 4 && ring.Contains(thirdBond))
                                        {
                                            theBondIsInA6MemberedRing = true;
                                        }
                                    }
                                }
                            }
                            CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                    mol.Atoms.IndexOf(curAtomThird), atoms, sphere, theBondIsInA6MemberedRing);
                            theBondIsInA6MemberedRing = false;
                            var atomsInFourthSphere = mol.GetConnectedAtoms(curAtomThird);
                            foreach (var curAtomFourth in atomsInFourthSphere)
                            {
                                fourthBond = mol.GetBond(curAtomThird, curAtomFourth);
                                if (mol.Atoms.IndexOf(curAtomFourth) != atomPosition
                                        && GetIfBondIsNotRotatable(mol, fourthBond, detected))
                                {
                                    sphere = 4;
                                    bondOrder = fourthBond.Order;
                                    bondNumber = mol.Bonds.IndexOf(fourthBond);
                                    theBondIsInA6MemberedRing = false;
                                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                            mol.Atoms.IndexOf(curAtomFourth), atoms, sphere,
                                            theBondIsInA6MemberedRing);
                                    var atomsInFifthSphere = mol.GetConnectedAtoms(curAtomFourth);
                                    foreach (var curAtomFifth in atomsInFifthSphere)
                                    {
                                        fifthBond = mol.GetBond(curAtomFifth, curAtomFourth);
                                        if (mol.Atoms.IndexOf(curAtomFifth) != atomPosition
                                                && GetIfBondIsNotRotatable(mol, fifthBond, detected))
                                        {
                                            sphere = 5;
                                            bondOrder = fifthBond.Order;
                                            bondNumber = mol.Bonds.IndexOf(fifthBond);
                                            theBondIsInA6MemberedRing = false;
                                            CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                    bondsInCycloex, mol.Atoms.IndexOf(curAtomFifth), atoms,
                                                    sphere, theBondIsInA6MemberedRing);
                                            var atomsInSixthSphere = mol.GetConnectedAtoms(curAtomFifth);
                                            foreach (var curAtomSixth in atomsInSixthSphere)
                                            {
                                                sixthBond = mol.GetBond(curAtomFifth, curAtomSixth);
                                                if (mol.Atoms.IndexOf(curAtomSixth) != atomPosition
                                                        && GetIfBondIsNotRotatable(mol, sixthBond, detected))
                                                {
                                                    sphere = 6;
                                                    bondOrder = sixthBond.Order;
                                                    bondNumber = mol.Bonds.IndexOf(sixthBond);
                                                    theBondIsInA6MemberedRing = false;
                                                    CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                            bondsInCycloex,
                                                            mol.Atoms.IndexOf(curAtomSixth), atoms, sphere,
                                                            theBondIsInA6MemberedRing);
                                                    var atomsInSeventhSphere = mol.GetConnectedAtoms(curAtomSixth);
                                                    foreach (var curAtomSeventh in atomsInSeventhSphere)
                                                    {
                                                        seventhBond = mol.GetBond(curAtomSeventh, curAtomSixth);
                                                        if (mol.Atoms.IndexOf(curAtomSeventh) != atomPosition && GetIfBondIsNotRotatable(mol, seventhBond, detected))
                                                        {
                                                            sphere = 7;
                                                            bondOrder = seventhBond.Order;
                                                            bondNumber = mol.Bonds.IndexOf(seventhBond);
                                                            theBondIsInA6MemberedRing = false;
                                                            CheckAndStore(bondNumber, bondOrder,
                                                                    singles, doubles, bondsInCycloex,
                                                                    mol.Atoms.IndexOf(curAtomSeventh),
                                                                    atoms, sphere,
                                                                    theBondIsInA6MemberedRing);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (MakeDescriptorLastStage(
                    rdfProtonCalculatedValues, 
                    atom, 
                    clonedAtom,
                    mol,
                    neighbour0, 
                    singles,
                    doubles,
                    atoms, 
                    bondsInCycloex))
                return new Result(rdfProtonCalculatedValues);
            else
                return new Result(new CDKException("Some error occurred."));
        }

        // Others definitions

        private static bool GetIfBondIsNotRotatable(IAtomContainer mol, IBond bond, IAtomContainer detected)
        {
            bool isBondNotRotatable = false;
            int counter = 0;
            IAtom atom0 = bond.Atoms[0];
            IAtom atom1 = bond.Atoms[1];
            if (detected != null)
            {
                if (detected.Contains(bond)) counter += 1;
            }
            if (atom0.IsInRing)
            {
                if (atom1.IsInRing)
                {
                    counter += 1;
                }
                else
                {
                    if (atom1.AtomicNumber.Equals(AtomicNumbers.H))
                        counter += 1;
                    else
                        counter += 0;
                }
            }
            if (atom0.AtomicNumber.Equals(AtomicNumbers.N) && atom1.AtomicNumber.Equals(AtomicNumbers.C))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom1)) 
                    counter += 1;
            if (atom0.AtomicNumber.Equals(AtomicNumbers.C) && atom1.AtomicNumber.Equals(AtomicNumbers.N))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom0)) 
                    counter += 1;
            if (counter > 0) 
                isBondNotRotatable = true;
            return isBondNotRotatable;
        }

        private static bool GetIfACarbonIsDoubleBondedToAnOxygen(IAtomContainer mol, IAtom carbonAtom)
        {
            bool isDoubleBondedToOxygen = false;
            var neighToCarbon = mol.GetConnectedAtoms(carbonAtom);
            IBond tmpBond;
            int counter = 0;
            foreach (var neighbour in neighToCarbon)
            {
                if (neighbour.AtomicNumber.Equals(AtomicNumbers.O))
                {
                    tmpBond = mol.GetBond(neighbour, carbonAtom);
                    if (tmpBond.Order == BondOrder.Double) 
                        counter += 1;
                }
            }
            if (counter > 0) 
                isDoubleBondedToOxygen = true;
            return isDoubleBondedToOxygen;
        }

        // this method calculates the angle between two bonds given coordinates of their atoms

        internal static double CalculateAngleBetweenTwoLines(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var firstLine = a - b;
            var secondLine = c - d;
            var firstVec = firstLine;
            var secondVec = secondLine;
            return Vectors.Angle(firstVec, secondVec);
        }

        // this method store atoms and bonds in proper lists:
        private static void CheckAndStore(int bondToStore, BondOrder bondOrder, List<int> singleVec,
                List<int> doubleVec, List<int> cycloexVec, int a1, List<int> atomVec,
                int sphere, bool isBondInCycloex)
        {
            if (!atomVec.Contains(a1))
                if (sphere < 6) 
                    atomVec.Add(a1);
            if (!cycloexVec.Contains(bondToStore))
                if (isBondInCycloex)
                    cycloexVec.Add(bondToStore);
            if (bondOrder == BondOrder.Double)
                if (!doubleVec.Contains(bondToStore))
                    doubleVec.Add(bondToStore);
            if (bondOrder == BondOrder.Single)
                if (!singleVec.Contains(bondToStore))
                    singleVec.Add(bondToStore);
        }

        // generic method for calculation of distance btw 2 atoms
        internal static double CalculateDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            var firstPoint = atom1.Point3D.Value;
            var secondPoint = atom2.Point3D.Value;
            var distance = Vector3.Distance(firstPoint, secondPoint);
            return distance;
        }

        // given a double bond
        // this method returns a bond bonded to this double bond
        internal static int GetNearestBondtoAGivenAtom(IAtomContainer mol, IAtom atom, IBond bond)
        {
            int nearestBond = -1;
            double distance = 0;
            var atom0 = bond.Atoms[0];
            var bondsAtLeft = mol.GetConnectedBonds(atom0);
            foreach (var curBond in bondsAtLeft)
            {
                var values = CalculateDistanceBetweenAtomAndBond(atom, curBond);
                var partial = mol.Bonds.IndexOf(curBond);
                if (nearestBond == -1)
                {
                    nearestBond = mol.Bonds.IndexOf(curBond);
                    distance = values[0];
                }
                else
                {
                    if (values[0] < distance)
                    {
                        nearestBond = partial;
                    }
                }
            }
            return nearestBond;
        }

        // method which calculated distance btw an atom and the middle point of a bond
        // and returns distance and coordinates of middle point
        internal static double[] CalculateDistanceBetweenAtomAndBond(IAtom proton, IBond theBond)
        {
            var middlePoint = theBond.GetGeometric3DCenter();
            var protonPoint = proton.Point3D.Value;
            var values = new double[4];
            values[0] = Vector3.Distance(middlePoint, protonPoint);
            values[1] = middlePoint.X;
            values[2] = middlePoint.Y;
            values[3] = middlePoint.Z;
            return values;
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
    public partial class RDFProtonDescriptorGSR
    {
        [DescriptorResult(prefix: prefix, baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(Exception e) : base(e) { }

            public Result(IReadOnlyList<double> values) : base(values) { }
        }

        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;
        private IEnumerable<IAtomContainer> varAtomContainerSet;
        private readonly IAtomContainer mol;

        public RDFProtonDescriptorGSR(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedContainer = (IAtomContainer)container.Clone();

            /////////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            /////////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            mol = CDK.Builder.NewAtomContainer(clonedContainer);

            // DETECTION OF pi SYSTEMS
            varAtomContainerSet = ConjugatedPiSystemsDetector.Detect(mol);
            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(mol, true);

            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);
                Aromaticity.CDKLegacy.Apply(clonedContainer);
            }

            this.container = container;
        }

        private IRingSet allRings;
        private readonly object syncAllRings = new object();

        private IRingSet AllRings
        {
            get
            {
                if (allRings == null)
                    lock (syncAllRings)
                    {
                        allRings = new AllRingsFinder().FindAllRings(clonedContainer);
                    }
                return allRings;
            }
        }

        public virtual Result Calculate(IAtom atom)
        {
            return Calculate(atom, null);
        }

        public Result Calculate(IAtom atom, IRingSet precalculatedringset)
        {
            var atomPosition = container.Atoms.IndexOf(atom);
            var clonedAtom = clonedContainer.Atoms[atomPosition];
            var rdfProtonCalculatedValues = new List<double>(desc_length);
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                return new Result(new CDKException("Invalid atom specified"));
            }

            // ///////////////////////FIRST SECTION OF MAIN METHOD: DEFINITION OF MAIN VARIABLES
            // ///////////////////////AND AROMATICITY AND PI-SYSTEM AND RINGS DETECTION

            var varRingSet = precalculatedringset ?? AllRings;

            // SET ISINRING FLAGS FOR BONDS
            var bondsInContainer = clonedContainer.Bonds;
            foreach (var bond in bondsInContainer)
            {
                var ringsWithThisBond = varRingSet.GetRings(bond);
                if (ringsWithThisBond.Any())
                {
                    bond.IsInRing = true;
                }
            }
            // SET ISINRING FLAGS FOR ATOMS
            foreach (var vatom in clonedContainer.Atoms)
            {
                var ringsWithThisAtom = varRingSet.GetRings(vatom);
                if (ringsWithThisAtom.Any())
                {
                    vatom.IsInRing = true;
                }
            }

            var detected = varAtomContainerSet.FirstOrDefault();

            // neighbors[0] is the atom joined to the target proton:
            var neighbors = mol.GetConnectedAtoms(clonedAtom);
            var neighbour0 = neighbors.First();

            // 2', 3', 4', 5', 6', and 7' atoms up to the target are detected:
            var atomsInSecondSphere = mol.GetConnectedAtoms(neighbour0);

            // SOME LISTS ARE CREATED FOR STORING OF INTERESTING ATOMS AND BONDS DURING DETECTION
            var singles = new List<int>(); // list of any bond not rotatable
            var doubles = new List<int>(); // list with only double bonds
            var atoms = new List<int>();   // list with all the atoms in spheres
                                           // atoms.Add( int.ValueOf( mol.Atoms.IndexOf(neighbors[0]) ) );
            var bondsInCycloex = new List<int>(); // list for bonds in cycloexane-like rings

            // 2', 3', 4', 5', 6', and 7' bonds up to the target are detected:
            IBond secondBond;  // (remember that first bond is proton bond)
            IBond thirdBond;   //
            IBond fourthBond;  //
            IBond fifthBond;   //
            IBond sixthBond;   //
            IBond seventhBond; //

            // definition of some variables used in the main FOR loop for detection of interesting atoms and bonds:
            bool theBondIsInA6MemberedRing; // this is like a flag for bonds which are in cycloexane-like rings (rings with more than 4 at.)
            BondOrder bondOrder;
            int bondNumber;
            int sphere;

            // THIS MAIN FOR LOOP DETECT RIGID BONDS IN 7 SPHERES:
            foreach (var curAtomSecond in atomsInSecondSphere)
            {
                secondBond = mol.GetBond(neighbour0, curAtomSecond);
                if (mol.Atoms.IndexOf(curAtomSecond) != atomPosition && GetIfBondIsNotRotatable(mol, secondBond, detected))
                {
                    sphere = 2;
                    bondOrder = secondBond.Order;
                    bondNumber = mol.Bonds.IndexOf(secondBond);
                    theBondIsInA6MemberedRing = false;
                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex, mol.Atoms.IndexOf(curAtomSecond), atoms, sphere, theBondIsInA6MemberedRing);
                    var atomsInThirdSphere = mol.GetConnectedAtoms(curAtomSecond);
                    foreach (var curAtomThird in atomsInThirdSphere)
                    {
                        thirdBond = mol.GetBond(curAtomThird, curAtomSecond);
                        // IF THE ATOMS IS IN THE THIRD SPHERE AND IN A CYCLOEXANE-LIKE RING, IT IS STORED IN THE PROPER LIST:
                        if (mol.Atoms.IndexOf(curAtomThird) != atomPosition
                         && GetIfBondIsNotRotatable(mol, thirdBond, detected))
                        {
                            sphere = 3;
                            bondOrder = thirdBond.Order;
                            bondNumber = mol.Bonds.IndexOf(thirdBond);
                            theBondIsInA6MemberedRing = false;

                            // if the bond is in a cyclohexane-like ring (a ring with 5 or more atoms, not aromatic)
                            // the bool "theBondIsInA6MemberedRing" is set to true
                            if (!thirdBond.IsAromatic)
                            {
                                if (!curAtomThird.Equals(neighbour0))
                                {
                                    var rsAtom = varRingSet.GetRings(thirdBond);
                                    foreach (var ring in rsAtom)
                                    {
                                        if (ring.RingSize > 4 && ring.Contains(thirdBond))
                                        {
                                            theBondIsInA6MemberedRing = true;
                                        }
                                    }
                                }
                            }
                            CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                    mol.Atoms.IndexOf(curAtomThird), atoms, sphere, theBondIsInA6MemberedRing);
                            theBondIsInA6MemberedRing = false;
                            var atomsInFourthSphere = mol.GetConnectedAtoms(curAtomThird);
                            foreach (var curAtomFourth in atomsInFourthSphere)
                            {
                                fourthBond = mol.GetBond(curAtomThird, curAtomFourth);
                                if (mol.Atoms.IndexOf(curAtomFourth) != atomPosition
                                        && GetIfBondIsNotRotatable(mol, fourthBond, detected))
                                {
                                    sphere = 4;
                                    bondOrder = fourthBond.Order;
                                    bondNumber = mol.Bonds.IndexOf(fourthBond);
                                    theBondIsInA6MemberedRing = false;
                                    CheckAndStore(bondNumber, bondOrder, singles, doubles, bondsInCycloex,
                                            mol.Atoms.IndexOf(curAtomFourth), atoms, sphere,
                                            theBondIsInA6MemberedRing);
                                    var atomsInFifthSphere = mol.GetConnectedAtoms(curAtomFourth);
                                    foreach (var curAtomFifth in atomsInFifthSphere)
                                    {
                                        fifthBond = mol.GetBond(curAtomFifth, curAtomFourth);
                                        if (mol.Atoms.IndexOf(curAtomFifth) != atomPosition
                                                && GetIfBondIsNotRotatable(mol, fifthBond, detected))
                                        {
                                            sphere = 5;
                                            bondOrder = fifthBond.Order;
                                            bondNumber = mol.Bonds.IndexOf(fifthBond);
                                            theBondIsInA6MemberedRing = false;
                                            CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                    bondsInCycloex, mol.Atoms.IndexOf(curAtomFifth), atoms,
                                                    sphere, theBondIsInA6MemberedRing);
                                            var atomsInSixthSphere = mol.GetConnectedAtoms(curAtomFifth);
                                            foreach (var curAtomSixth in atomsInSixthSphere)
                                            {
                                                sixthBond = mol.GetBond(curAtomFifth, curAtomSixth);
                                                if (mol.Atoms.IndexOf(curAtomSixth) != atomPosition
                                                        && GetIfBondIsNotRotatable(mol, sixthBond, detected))
                                                {
                                                    sphere = 6;
                                                    bondOrder = sixthBond.Order;
                                                    bondNumber = mol.Bonds.IndexOf(sixthBond);
                                                    theBondIsInA6MemberedRing = false;
                                                    CheckAndStore(bondNumber, bondOrder, singles, doubles,
                                                            bondsInCycloex,
                                                            mol.Atoms.IndexOf(curAtomSixth), atoms, sphere,
                                                            theBondIsInA6MemberedRing);
                                                    var atomsInSeventhSphere = mol.GetConnectedAtoms(curAtomSixth);
                                                    foreach (var curAtomSeventh in atomsInSeventhSphere)
                                                    {
                                                        seventhBond = mol.GetBond(curAtomSeventh, curAtomSixth);
                                                        if (mol.Atoms.IndexOf(curAtomSeventh) != atomPosition && GetIfBondIsNotRotatable(mol, seventhBond, detected))
                                                        {
                                                            sphere = 7;
                                                            bondOrder = seventhBond.Order;
                                                            bondNumber = mol.Bonds.IndexOf(seventhBond);
                                                            theBondIsInA6MemberedRing = false;
                                                            CheckAndStore(bondNumber, bondOrder,
                                                                    singles, doubles, bondsInCycloex,
                                                                    mol.Atoms.IndexOf(curAtomSeventh),
                                                                    atoms, sphere,
                                                                    theBondIsInA6MemberedRing);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (MakeDescriptorLastStage(
                    rdfProtonCalculatedValues, 
                    atom, 
                    clonedAtom,
                    mol,
                    neighbour0, 
                    singles,
                    doubles,
                    atoms, 
                    bondsInCycloex))
                return new Result(rdfProtonCalculatedValues);
            else
                return new Result(new CDKException("Some error occurred."));
        }

        // Others definitions

        private static bool GetIfBondIsNotRotatable(IAtomContainer mol, IBond bond, IAtomContainer detected)
        {
            bool isBondNotRotatable = false;
            int counter = 0;
            IAtom atom0 = bond.Atoms[0];
            IAtom atom1 = bond.Atoms[1];
            if (detected != null)
            {
                if (detected.Contains(bond)) counter += 1;
            }
            if (atom0.IsInRing)
            {
                if (atom1.IsInRing)
                {
                    counter += 1;
                }
                else
                {
                    if (atom1.AtomicNumber.Equals(AtomicNumbers.H))
                        counter += 1;
                    else
                        counter += 0;
                }
            }
            if (atom0.AtomicNumber.Equals(AtomicNumbers.N) && atom1.AtomicNumber.Equals(AtomicNumbers.C))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom1)) 
                    counter += 1;
            if (atom0.AtomicNumber.Equals(AtomicNumbers.C) && atom1.AtomicNumber.Equals(AtomicNumbers.N))
                if (GetIfACarbonIsDoubleBondedToAnOxygen(mol, atom0)) 
                    counter += 1;
            if (counter > 0) 
                isBondNotRotatable = true;
            return isBondNotRotatable;
        }

        private static bool GetIfACarbonIsDoubleBondedToAnOxygen(IAtomContainer mol, IAtom carbonAtom)
        {
            bool isDoubleBondedToOxygen = false;
            var neighToCarbon = mol.GetConnectedAtoms(carbonAtom);
            IBond tmpBond;
            int counter = 0;
            foreach (var neighbour in neighToCarbon)
            {
                if (neighbour.AtomicNumber.Equals(AtomicNumbers.O))
                {
                    tmpBond = mol.GetBond(neighbour, carbonAtom);
                    if (tmpBond.Order == BondOrder.Double) 
                        counter += 1;
                }
            }
            if (counter > 0) 
                isDoubleBondedToOxygen = true;
            return isDoubleBondedToOxygen;
        }

        // this method calculates the angle between two bonds given coordinates of their atoms

        internal static double CalculateAngleBetweenTwoLines(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var firstLine = a - b;
            var secondLine = c - d;
            var firstVec = firstLine;
            var secondVec = secondLine;
            return Vectors.Angle(firstVec, secondVec);
        }

        // this method store atoms and bonds in proper lists:
        private static void CheckAndStore(int bondToStore, BondOrder bondOrder, List<int> singleVec,
                List<int> doubleVec, List<int> cycloexVec, int a1, List<int> atomVec,
                int sphere, bool isBondInCycloex)
        {
            if (!atomVec.Contains(a1))
                if (sphere < 6) 
                    atomVec.Add(a1);
            if (!cycloexVec.Contains(bondToStore))
                if (isBondInCycloex)
                    cycloexVec.Add(bondToStore);
            if (bondOrder == BondOrder.Double)
                if (!doubleVec.Contains(bondToStore))
                    doubleVec.Add(bondToStore);
            if (bondOrder == BondOrder.Single)
                if (!singleVec.Contains(bondToStore))
                    singleVec.Add(bondToStore);
        }

        // generic method for calculation of distance btw 2 atoms
        internal static double CalculateDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            var firstPoint = atom1.Point3D.Value;
            var secondPoint = atom2.Point3D.Value;
            var distance = Vector3.Distance(firstPoint, secondPoint);
            return distance;
        }

        // given a double bond
        // this method returns a bond bonded to this double bond
        internal static int GetNearestBondtoAGivenAtom(IAtomContainer mol, IAtom atom, IBond bond)
        {
            int nearestBond = -1;
            double distance = 0;
            var atom0 = bond.Atoms[0];
            var bondsAtLeft = mol.GetConnectedBonds(atom0);
            foreach (var curBond in bondsAtLeft)
            {
                var values = CalculateDistanceBetweenAtomAndBond(atom, curBond);
                var partial = mol.Bonds.IndexOf(curBond);
                if (nearestBond == -1)
                {
                    nearestBond = mol.Bonds.IndexOf(curBond);
                    distance = values[0];
                }
                else
                {
                    if (values[0] < distance)
                    {
                        nearestBond = partial;
                    }
                }
            }
            return nearestBond;
        }

        // method which calculated distance btw an atom and the middle point of a bond
        // and returns distance and coordinates of middle point
        internal static double[] CalculateDistanceBetweenAtomAndBond(IAtom proton, IBond theBond)
        {
            var middlePoint = theBond.GetGeometric3DCenter();
            var protonPoint = proton.Point3D.Value;
            var values = new double[4];
            values[0] = Vector3.Distance(middlePoint, protonPoint);
            values[1] = middlePoint.X;
            values[2] = middlePoint.Y;
            values[3] = middlePoint.Z;
            return values;
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
