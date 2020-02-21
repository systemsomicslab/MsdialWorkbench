using org.openscience.cdk.exception;
using org.openscience.cdk.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.openscience.cdk.graph;
using org.openscience.cdk.tools.manipulator;
using java.util;
using org.openscience.cdk.config;
using org.openscience.cdk.ringsearch;
using java.lang;
using org.openscience.cdk.aromaticity;
using org.openscience.cdk;
using System.Diagnostics;

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
            var finder = new SSSRFinder(container);
            var sssr = finder.findEssentialRings();
            var rings = RingPartitioner.partitionRings(sssr);
            var atomBondsDict = getAtomBondsDictionary(container);
            if (rings.size() == 0) return;

            foreach (var ring in rings.ToWindowsEnumerable<IRingSet>()) {
                ringKekulize(ring, atomBondsDict);
            }
        }

        private static void ringKekulize(IRingSet rings, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            for (int i = 0; i < rings.getAtomContainerCount(); i++) {
                var ring = rings.getAtomContainer(i);
                if (!isAllAtomsAromatic(ring, 0)) continue;

                InvalidateBonds(ring);
                dearomatizeRing(ring, atomBondsDict);
                refineRingBondOrders(ring, atomBondsDict);
            }
        }

        private static void InvalidateBonds(IAtomContainer ring)
        {
            for (int i = 0; i < ring.getBondCount(); i++) {
                var bond = ring.getBond(i);
                bond.setOrder(IBond.Order.QUADRUPLE);
            }
        }

        private static void dearomatizeRing(IAtomContainer ring, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {

            IBond curBond = null; 
            IAtom curAtom = null;

            var nextIsSingle = true;

            // find the suitable bond as a start for double bond creation
            findStartAtomAndBond(ring, atomBondsDict, out curAtom, out curBond);
            for (int i = 0; i < ring.getBondCount(); ++i) {
                if (nextIsSingle) {
                    nextIsSingle = false;
                    curBond.setOrder(IBond.Order.SINGLE);
                }
                else {
                    nextIsSingle = true;
                    curBond.setOrder(IBond.Order.DOUBLE);
                }

                //go to next bond
                curAtom = curBond.getConnectedAtom(curAtom);
                var bonds = ring.getConnectedBondsList(curAtom);
                foreach (var bond in bonds.ToWindowsEnumerable<IBond>()) {
                    if (bond != curBond) {
                        curBond = bond;
                        break;
                    }
                }
            }
        }

        private static void findStartAtomAndBond(IAtomContainer ring, Dictionary<IAtom, List<IBond>> atomBondsDict, out IAtom startAtom, out IBond startBond)
        {
            var ringBondCount = ring.getBondCount();
            for (int i = 0; i < ring.getBondCount(); i++) {
                var bond = ring.getBond(i);
                var atom1 = bond.getAtom(0);
                var atom2 = bond.getAtom(1);
                var atom1Bonds = atomBondsDict[atom1];
                var atom2Bonds = atomBondsDict[atom2];

                var atom1DoubleBondFlg = false;
                foreach (var aBond in atom1Bonds) {
                    if (aBond.getOrder() == IBond.Order.DOUBLE) {
                        atom1DoubleBondFlg = true;
                        break;
                    }
                }
                var atom2DoubleBondFlg = false;
                foreach (var aBond in atom2Bonds) {
                    if (aBond.getOrder() == IBond.Order.DOUBLE) {
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

                if (atom1.getSymbol() != "C" && atom2.getSymbol() == "C") {
                    startAtom = atom1;
                    startBond = bond;
                    return;
                }
                else if (atom2.getSymbol() != "C" && atom1.getSymbol() == "C") {
                    startAtom = atom2;
                    startBond = bond;
                    return;
                }
                
            }
            startBond = ring.getBond(0);
            startAtom = startBond.getAtom(0);
        }

        private static bool isAllAtomsAromatic(IAtomContainer ring, int penalty)
        {
            for (int i = 0; i < ring.getAtomCount(); i++) {
                var atom = ring.getAtom(i);
                if (atom.getHybridization() != IAtomType.Hybridization.SP2 && atom.getHybridization() != IAtomType.Hybridization.PLANAR3) return false;
            }

            return true;
        }

        private static Dictionary<IAtom, List<IBond>> getAtomBondsDictionary(IAtomContainer atomContainer)
        {
            var dict = new Dictionary<IAtom, List<IBond>>();
            foreach (var bond in atomContainer.bonds().ToWindowsEnumerable<IBond>()) {
                foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>()) {
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
            for (int i = 0; i < rings.getAtomContainerCount(); i++) {
                var ring = rings.getAtomContainer(i);

                refineRingBondOrders(ring, atomBondsDict);
            }
        }

        private static void refineRingBondOrders(IAtomContainer ring, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            for (int j = 0; j < ring.getBondCount(); j++) {
                var bond = ring.getBond(j);

                var atom1 = bond.getAtom(0);
                var atom2 = bond.getAtom(1);

                var atom1Valency = atom1.getValency().intValue();
                var atom2Valency = atom2.getValency().intValue();

                var atom1Bonds = atomBondsDict[atom1];
                var atom2Bonds = atomBondsDict[atom2];

                // Debug.WriteLine(atom1.getSymbol() + "\t" + atom2.getSymbol());

                var counter = 0;
                foreach (var cBond in atom1Bonds) {
                    var order = cBond.getOrder();
                    if (order == IBond.Order.SINGLE) counter += 1;
                    else if (order == IBond.Order.DOUBLE) counter += 2;
                    else if (order == IBond.Order.TRIPLE) counter += 3;
                    else if (order == IBond.Order.QUADRUPLE) counter += 4;
                }

                if (atom1Valency < counter && bond.getOrder() == IBond.Order.DOUBLE) {
                    bond.setOrder(IBond.Order.SINGLE);
                    continue;
                }

                // Debug.WriteLine(atom1.getSymbol() + "\t" + atom1Valency + "\t" + counter + "\t" + bond.getOrder().toString().ToString());

                counter = 0;
                foreach (var cBond in atom2Bonds) {
                    var order = cBond.getOrder();
                    if (order == IBond.Order.SINGLE) counter += 1;
                    else if (order == IBond.Order.DOUBLE) counter += 2;
                    else if (order == IBond.Order.TRIPLE) counter += 3;
                    else if (order == IBond.Order.QUADRUPLE) counter += 4;
                }

                if (atom2Valency < counter && bond.getOrder() == IBond.Order.DOUBLE) {
                    bond.setOrder(IBond.Order.SINGLE);
                    continue;
                }

                // Debug.WriteLine(atom2.getSymbol() + "\t" + atom2Valency + "\t" + counter + "\t" + bond.getOrder().toString().ToString());
            }
        }
    }
}
