using java.lang;
using org.openscience.cdk.atomtype;
using org.openscience.cdk.exception;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.silent;
using org.openscience.cdk.tools;
using org.openscience.cdk.tools.manipulator;
using System.Diagnostics;

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

                var builder = molecule.getBuilder();
                var matcher = CDKAtomTypeMatcher.getInstance(builder);

                //AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);

                foreach (var atom in molecule.atoms().ToWindowsEnumerable<IAtom>()) {
                    var type = matcher.findMatchingAtomType(molecule, atom);

                    //there is a bug in AtomType matcher.
                    //To deal with the errors, I wrote the below code. However, addImplicitHydrogens will throw NoSuchAtomTypeException.
                    //However, at least in this purpose, AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule) can be enough.
                    if (type == null && atom.getSymbol() == "C") { 
                        type = getTypeCarbon(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() == "N") {
                        type = getTypeNitrogen(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() == "O") {
                        type = getTypeOxygen(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() == "S") {
                        type = getTypeSulphur(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() == "P") {
                        type = getTypePhosphors(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() == "Si") { //there is a bug in cdk?? error happens when there is a Si or Ti in the molecule
                        type = getTypeSilicon(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() == "Ti") {
                        type = getTypeTiTanium(molecule, atom);
                    }
                    else if (type == null && atom.getSymbol() != "Si" && atom.getSymbol() != "Ti") {
                        return false; // This means it failed to get the mass for this element, so its an unknown element like "R" for example
                    }

                    AtomTypeManipulator.configure(atom, type);
                }

                var hAdder = CDKHydrogenAdder.getInstance(builder);
                hAdder.addImplicitHydrogens(molecule);

                AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);
            }
            catch (IllegalArgumentException ex) {
                Debug.WriteLine(ex);
                return false;
            }
            catch (NoSuchAtomTypeException ex) {
                Debug.WriteLine(ex);
                AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);
                return true;
            }

            return true;
        }

        private static IAtomType getTypeTiTanium(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("Ti"));
            type.setAtomicNumber(Integer.valueOf(22));
            type.setAtomTypeName("Ti");
            type.setExactMass(Double.valueOf(47.948));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

        private static IAtomType getTypeSilicon(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("Si"));
            type.setAtomicNumber(Integer.valueOf(14));
            type.setAtomTypeName("Si");
            type.setExactMass(Double.valueOf(27.976927));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

        private static IAtomType getTypeCarbon(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("C"));
            type.setAtomicNumber(Integer.valueOf(6));
            type.setAtomTypeName("C");
            type.setExactMass(Double.valueOf(12));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }


        private static IAtomType getTypeNitrogen(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("N"));
            type.setAtomicNumber(Integer.valueOf(7));
            type.setAtomTypeName("N");
            type.setExactMass(Double.valueOf(14.003074));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }


        private static IAtomType getTypeOxygen(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("O"));
            type.setAtomicNumber(Integer.valueOf(8));
            type.setAtomTypeName("O");
            type.setExactMass(Double.valueOf(15.994915));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }


        private static IAtomType getTypePhosphors(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("P"));
            type.setAtomicNumber(Integer.valueOf(15));
            type.setAtomTypeName("P");
            type.setExactMass(Double.valueOf(30.973763));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

       
        private static IAtomType getTypeSulphur(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.getConnectedBondsList(atom);

            var type = new AtomType(new Element("S"));
            type.setAtomicNumber(Integer.valueOf(16));
            type.setAtomTypeName("S");
            type.setExactMass(Double.valueOf(31.972072));

            setAtopTypeProperties(type, atom, neighbors);
            return type;
        }

        private static void setAtopTypeProperties(AtomType type, IAtom atom, java.util.List neighbors)
        {
            var neighborcount = neighbors.size();
            int bondCount = 0;
            int charge = 0;

            for (int i = 0; i < neighborcount; i++) {
                if (neighbors.get(i).GetType() == typeof(org.openscience.cdk.silent.Bond)) {
                    var neiborBond = (org.openscience.cdk.silent.Bond)neighbors.get(i);
                    bondCount += getBondCount(neiborBond);
                }
                else if (neighbors.get(i).GetType() == typeof(org.openscience.cdk.Bond)) {
                    var neiborBond = (org.openscience.cdk.Bond)neighbors.get(i);
                    bondCount += getBondCount(neiborBond);
                }
                else {
                    bondCount += 1;
                }
            }

            if (atom.getFormalCharge() != null) {
                charge = atom.getFormalCharge().intValue();
            }

            type.setFormalNeighbourCount(Integer.valueOf(neighborcount));
            type.setFormalCharge(Integer.valueOf(charge));
            type.setBondOrderSum(Double.valueOf(bondCount));
            type.setValency(Integer.valueOf(bondCount));
        }


        private static int getBondCount(org.openscience.cdk.silent.Bond bond)
        {
            var order = bond.getOrder();
            if (order == IBond.Order.SINGLE) {
                return 1;
            }
            else if (order == IBond.Order.DOUBLE) {
                return 2;
            }
            else if (order == IBond.Order.TRIPLE) {
                return 3;
            }
            else {
                return 1;
            }
        }

        private static int getBondCount(org.openscience.cdk.Bond bond) {
            var order = bond.getOrder();
            if (order == IBond.Order.SINGLE) {
                return 1;
            }
            else if (order == IBond.Order.DOUBLE) {
                return 2;
            }
            else if (order == IBond.Order.TRIPLE) {
                return 3;
            }
            else {
                return 1;
            }
        }
    }
}
