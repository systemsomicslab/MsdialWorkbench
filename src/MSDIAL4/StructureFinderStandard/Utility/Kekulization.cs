using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NCDK;
using NCDK.RingSearches;
using NCDK.Graphs;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public sealed class Kekulization
    {
        private Kekulization() { }

        /**
         * Methods that takes a ring of which all bonds are aromatic, and assigns single
         * and double bonds. It does this in a non-general way by looking at the ring
         * size and take everything as a special case.
         *
         * @param ring Ring to dearomatize
         * @return  False if it could not convert the aromatic ring bond into single and double bonds
         */
        public static void Kekulize(IAtomContainer container)
        {
            //var finder = new SSSRFinder(container);
            //var sssr = finder.findEssentialRings();
            var finder = Cycles.FindSSSR(container);
            var sssr = finder.ToRingSet();
            var rings = RingPartitioner.PartitionRings(sssr);
            var atomBondsDict = getAtomBondsDictionary(container);
            if (rings.Count() == 0) return;

            foreach (var ring in rings) {
                ringKekulize(ring, atomBondsDict);
            }
        }

        private static void ringKekulize(IRingSet rings, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            for (int i = 0; i < rings.Count(); i++) {
                var ring = rings[i];
                if (!isAllAtomsAromatic(ring, 0)) continue;

                InvalidateBonds(ring);
                dearomatizeRing(ring, atomBondsDict);
                refineRingBondOrders(ring, atomBondsDict);
            }
        }

        private static void InvalidateBonds(IAtomContainer ring)
        {
            for (int i = 0; i < ring.Bonds.Count(); i++) {
                var bond = ring.Bonds[i];
                bond.Order = BondOrder.Quadruple;
                //bond.setOrder(IBond.Order.QUADRUPLE);
            }
        }

        private static void dearomatizeRing(IAtomContainer ring, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {

            IBond curBond = null; 
            IAtom curAtom = null;

            var nextIsSingle = true;

            // find the suitable bond as a start for double bond creation
            findStartAtomAndBond(ring, atomBondsDict, out curAtom, out curBond);
            for (int i = 0; i < ring.Bonds.Count(); ++i) {
                if (nextIsSingle) {
                    nextIsSingle = false;
                    curBond.Order = BondOrder.Single;
                    //curBond.setOrder(IBond.Order.SINGLE);
                }
                else {
                    nextIsSingle = true;
                    curBond.Order = BondOrder.Double;
                    //curBond.setOrder(IBond.Order.DOUBLE);
                }

                //go to next bond
                var connectedAtoms = curBond.GetConnectedAtoms(curAtom);
                //Console.WriteLine("Connected atoms in kekule form: {0}", connectedAtoms.Count());

                if (connectedAtoms.Count() != 1) continue;
                curAtom = connectedAtoms.ElementAt(0);
                var bonds = ring.GetConnectedBonds(curAtom);
                //curAtom = curBond.getConnectedAtom(curAtom);
                //var bonds = ring.getConnectedBondsList(curAtom);
                foreach (var bond in bonds) {
                    if (bond != curBond) {
                        curBond = bond;
                        break;
                    }
                }
            }
        }

        private static void findStartAtomAndBond(IAtomContainer ring, Dictionary<IAtom, List<IBond>> atomBondsDict, out IAtom startAtom, out IBond startBond)
        {
            var ringBondCount = ring.Bonds.Count;
            for (int i = 0; i < ringBondCount; i++) {
                var bond = ring.Bonds[i];
                var atom1 = bond.Atoms[0];
                var atom2 = bond.Atoms[1];
                var atom1Bonds = atomBondsDict[atom1];
                var atom2Bonds = atomBondsDict[atom2];

                var atom1DoubleBondFlg = false;
                foreach (var aBond in atom1Bonds) {
                    if (aBond.Order == BondOrder.Double) {
                        atom1DoubleBondFlg = true;
                        break;
                    }
                }
                var atom2DoubleBondFlg = false;
                foreach (var aBond in atom2Bonds) {
                    if (aBond.Order == BondOrder.Double) {
                        atom2DoubleBondFlg = true;
                        break;
                    }
                }
                
                if (atom1DoubleBondFlg && !atom2DoubleBondFlg) {
                    startAtom = atom1;
                    startBond = bond;
                    return;
                }
                else if (atom2DoubleBondFlg && !atom1DoubleBondFlg) {
                    startAtom = atom2;
                    startBond = bond;
                    return;
                }

                if (atom1.Symbol != "C" && atom2.Symbol == "C") {
                    startAtom = atom1;
                    startBond = bond;
                    return;
                }
                else if (atom2.Symbol != "C" && atom1.Symbol == "C") {
                    startAtom = atom2;
                    startBond = bond;
                    return;
                }
                
            }
            startBond = ring.Bonds[0];
            startAtom = startBond.Atoms[0];
        }

        private static bool isAllAtomsAromatic(IAtomContainer ring, int penalty)
        {
            for (int i = 0; i < ring.Atoms.Count(); i++) {
                var atom = ring.Atoms[i];
                if (atom.Hybridization != Hybridization.SP2 && atom.Hybridization != Hybridization.Planar3) return false;
            }

            return true;
        }

        private static Dictionary<IAtom, List<IBond>> getAtomBondsDictionary(IAtomContainer atomContainer)
        {
            var dict = new Dictionary<IAtom, List<IBond>>();
            foreach (var bond in atomContainer.Bonds) {
                foreach (var atom in bond.Atoms) {
                    if (!dict.ContainsKey(atom)) {
                        dict[atom] = new List<IBond>();
                    }
                    dict[atom].Add(bond);
                }
            }
            return dict;
        }

        private static void refineRingBondOrders(IRingSet rings, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            for (int i = 0; i < rings.Count(); i++) {
                var ring = rings[i];

                refineRingBondOrders(ring, atomBondsDict);
            }
        }

        private static void refineRingBondOrders(IAtomContainer ring, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            for (int j = 0; j < ring.Bonds.Count(); j++) {
                var bond = ring.Bonds[j];

                var atom1 = bond.Atoms[0];
                var atom2 = bond.Atoms[1];

                var atom1Valency = (int)atom1.Valency;
                var atom2Valency = (int)atom2.Valency;

                var atom1Bonds = atomBondsDict[atom1];
                var atom2Bonds = atomBondsDict[atom2];

                // Debug.WriteLine(atom1.getSymbol() + "\t" + atom2.getSymbol());

                var counter = 0;
                foreach (var cBond in atom1Bonds) {
                    var order = cBond.Order;
                    if (order == BondOrder.Single) counter += 1;
                    else if (order == BondOrder.Double) counter += 2;
                    else if (order == BondOrder.Triple) counter += 3;
                    else if (order == BondOrder.Quadruple) counter += 4;
                }

                if (atom1Valency < counter && bond.Order == BondOrder.Double) {
                    bond.Order = BondOrder.Single;
                    continue;
                }

                // Debug.WriteLine(atom1.getSymbol() + "\t" + atom1Valency + "\t" + counter + "\t" + bond.getOrder().toString().ToString());

                counter = 0;
                foreach (var cBond in atom2Bonds) {
                    var order = cBond.Order;
                    if (order == BondOrder.Single) counter += 1;
                    else if (order == BondOrder.Double) counter += 2;
                    else if (order == BondOrder.Triple) counter += 3;
                    else if (order == BondOrder.Quadruple) counter += 4;
                }

                if (atom2Valency < counter && bond.Order == BondOrder.Double) {
                    bond.Order = BondOrder.Single;
                    continue;
                }

                // Debug.WriteLine(atom2.getSymbol() + "\t" + atom2Valency + "\t" + counter + "\t" + bond.getOrder().toString().ToString());
            }
        }
    }
}
