using NCDK;
using NCDK.AtomTypes;
using NCDK.Default;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public sealed class ExplicitHydrogenAdder
    {
        private ExplicitHydrogenAdder() { }

        //cite: http://www.programcreek.com/java-api-examples/index.php?class=org.openscience.cdk.tools.manipulator.AtomContainerManipulator&method=convertImplicitToExplicitHydrogens
        public static bool AddExplicitHydrogens(IAtomContainer molecule)
        {
            if (molecule == null) return false;

            try {
                //AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);

                //var builder = molecule.getBuilder();
                //var matcher = CDKAtomTypeMatcher.getInstance(builder);

                //AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);
                AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(molecule);
                var matcher = CDKAtomTypeMatcher.GetInstance();
                //foreach (var atom in molecule.atoms().ToWindowsEnumerable<IAtom>()) {
                foreach (var atom in molecule.Atoms) {
                    var type = matcher.FindMatchingAtomType(molecule, atom);
                    //var type = matcher.findMatchingAtomType(molecule, atom);

                    //there is a bug in AtomType matcher.
                    //To deal with the errors, I wrote the below code. However, addImplicitHydrogens will throw NoSuchAtomTypeException.
                    //However, at least in this purpose, AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule) can be enough.
                    if (type == null && atom.Symbol == "C") { 
                        type = getTypeCarbon(molecule, atom);
                    }
                    else if (type == null && atom.Symbol == "N") {
                        type = getTypeNitrogen(molecule, atom);
                    }
                    else if (type == null && atom.Symbol == "O") {
                        type = getTypeOxygen(molecule, atom);
                    }
                    else if (type == null && atom.Symbol == "S") {
                        type = getTypeSulphur(molecule, atom);
                    }
                    else if (type == null && atom.Symbol == "P") {
                        type = getTypePhosphors(molecule, atom);
                    }
                    else if (type == null && atom.Symbol == "Si") { //there is a bug in cdk?? error happens when there is a Si or Ti in the molecule
                        type = getTypeSilicon(molecule, atom);
                    }
                    else if (type == null && atom.Symbol == "Ti") {
                        type = getTypeTiTanium(molecule, atom);
                    }
                    else if (type == null && atom.Symbol != "Si" && atom.Symbol != "Ti") {
                        return false; // This means it failed to get the mass for this element, so its an unknown element like "R" for example
                    }

                    //if (type == null && atom.Symbol == "C") {
                    //    type = getTypeCarbon(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol == "N") {
                    //    type = getTypeNitrogen(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol == "O") {
                    //    type = getTypeOxygen(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol == "S") {
                    //    type = getTypeSulphur(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol == "P") {
                    //    type = getTypePhosphors(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol == "Si") { //there is a bug in cdk?? error happens when there is a Si or Ti in the molecule
                    //    type = getTypeSilicon(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol == "Ti") {
                    //    type = getTypeTiTanium(molecule, atom);
                    //}
                    //else if (type == null && atom.Symbol != "Si" && atom.Symbol != "Ti") {
                    //    return false; // This means it failed to get the mass for this element, so its an unknown element like "R" for example
                    //}

                    AtomTypeManipulator.Configure(atom, type);
                }

                var hAdder = CDKHydrogenAdder.GetInstance();
                hAdder.AddImplicitHydrogens(molecule);

                AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(molecule);
            }
            catch (NoSuchAtomTypeException ex) {
                Debug.WriteLine(ex);
                AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(molecule);
                return true;
            }
            catch (CDKException ex) {
                Debug.WriteLine(ex);
                return false;
            }

            return true;
        }

        private static IAtomType getTypeTiTanium(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.Ti);
            type.AtomicNumber = 22;
            type.AtomTypeName = "Ti";
            type.ExactMass = 47.948;

            //type.setAtomicNumber(Integer.valueOf(22));
            //type.setAtomTypeName("Ti");
            //type.setExactMass(Double.valueOf(47.948));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

        private static IAtomType getTypeSilicon(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.Si);
            type.AtomicNumber = 14;
            type.AtomTypeName = "Si";
            type.ExactMass = 27.976927;

            //var neighbors = atomContainer.getConnectedBondsList(atom);

            //var type = new AtomType(new Element("Si"));
            //type.setAtomicNumber(Integer.valueOf(14));
            //type.setAtomTypeName("Si");
            //type.setExactMass(Double.valueOf(27.976927));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

        private static IAtomType getTypeCarbon(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.C);
            type.AtomicNumber = 6;
            type.AtomTypeName = "C";
            type.ExactMass = 12;

            //var neighbors = atomContainer.getConnectedBondsList(atom);

            //var type = new AtomType(new Element("C"));
            //type.setAtomicNumber(Integer.valueOf(6));
            //type.setAtomTypeName("C");
            //type.setExactMass(Double.valueOf(12));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }


        private static IAtomType getTypeNitrogen(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.N);
            type.AtomicNumber = 7;
            type.AtomTypeName = "N";
            type.ExactMass = 14.003074;

            //var neighbors = atomContainer.getConnectedBondsList(atom);

            //var type = new AtomType(new Element("N"));
            //type.setAtomicNumber(Integer.valueOf(7));
            //type.setAtomTypeName("N");
            //type.setExactMass(Double.valueOf(14.003074));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }


        private static IAtomType getTypeOxygen(IAtomContainer atomContainer, IAtom atom)
        {

            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.O);
            type.AtomicNumber = 8;
            type.AtomTypeName = "O";
            type.ExactMass = 15.994915;

            //var neighbors = atomContainer.getConnectedBondsList(atom);

            //var type = new AtomType(new Element("O"));
            //type.setAtomicNumber(Integer.valueOf(8));
            //type.setAtomTypeName("O");
            //type.setExactMass(Double.valueOf(15.994915));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }


        private static IAtomType getTypePhosphors(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.P);
            type.AtomicNumber = 15;
            type.AtomTypeName = "P";
            type.ExactMass = 30.973763;

            //var neighbors = atomContainer.getConnectedBondsList(atom);

            //var type = new AtomType(new Element("P"));
            //type.setAtomicNumber(Integer.valueOf(15));
            //type.setAtomTypeName("P");
            //type.setExactMass(Double.valueOf(30.973763));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

       
        private static IAtomType getTypeSulphur(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom);

            var type = new AtomType(ChemicalElement.S);
            type.AtomicNumber = 16;
            type.AtomTypeName = "S";
            type.ExactMass = 31.972072;

            //var neighbors = atomContainer.getConnectedBondsList(atom);

            //var type = new AtomType(new Element("S"));
            //type.setAtomicNumber(Integer.valueOf(16));
            //type.setAtomTypeName("S");
            //type.setExactMass(Double.valueOf(31.972072));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

        private static void setAtopTypeProperties(AtomType type, IAtom atom, IEnumerable<IBond> neighbors)
        {
            var neighborcount = neighbors.Count();
            //var neighborcount = neighbors.size();
            int bondCount = 0;
            int charge = 0;

            for (int i = 0; i < neighborcount; i++) {
                if (neighbors.ElementAt(i).GetType() == typeof(Bond)) {
                    var neiborBond = (Bond)neighbors.ElementAt(i);
                    bondCount += getBondCount(neiborBond);
                }
                else if (neighbors.ElementAt(i).GetType() == typeof(Bond)) {
                    var neiborBond = (Bond)neighbors.ElementAt(i);
                    bondCount += getBondCount(neiborBond);
                }
                else {
                    bondCount += 1;
                }
            }

            if (atom.FormalCharge != null) {
                charge = (int)atom.FormalCharge;
            }

            type.FormalNeighbourCount = neighborcount;
            type.FormalCharge = charge;
            type.BondOrderSum = bondCount;
            type.Valency = bondCount;

            //if (atom.getFormalCharge() != null) {
            //    charge = atom.getFormalCharge().intValue();
            //}

            //type.setFormalNeighbourCount(Integer.valueOf(neighborcount));
            //type.setFormalCharge(Integer.valueOf(charge));
            //type.setBondOrderSum(Double.valueOf(bondCount));
            //type.setValency(Integer.valueOf(bondCount));
        }


        private static int getBondCount(Bond bond)
        {
            var order = bond.Order;
            if (order == BondOrder.Single) {
                return 1;
            }
            else if (order == BondOrder.Double) {
                return 2;
            }
            else if (order == BondOrder.Triple) {
                return 3;
            }
            else {
                return 1;
            }
        }

        //private static int getBondCount(org.openscience.cdk.Bond bond) {
        //    var order = bond.getOrder();
        //    if (order == IBond.Order.SINGLE) {
        //        return 1;
        //    }
        //    else if (order == IBond.Order.DOUBLE) {
        //        return 2;
        //    }
        //    else if (order == IBond.Order.TRIPLE) {
        //        return 3;
        //    }
        //    else {
        //        return 1;
        //    }
        //}
    }
}
