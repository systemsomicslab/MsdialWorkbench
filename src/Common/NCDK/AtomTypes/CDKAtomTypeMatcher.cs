/* Copyright (C) 2007-2015  Egon Willighagen <egonw@users.sf.net>
 *                    2011  Nimish Gopal <nimishg@ebi.ac.uk>
 *                    2011  Syed Asad Rahman <asad@ebi.ac.uk>
 *                    2011  Gilleain Torrance <gilleain.torrance@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, version 2.1.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config;
using NCDK.RingSearches;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.AtomTypes
{
    /// <summary>
    /// Atom Type matcher that perceives atom types as defined in the CDK atom type list
    /// "NCDK.Dict.Data.cdk-atom-types.owl".
    /// If there is not an atom type defined for the tested atom, then <see langword="null"/> is returned.
    /// </summary>
    // @author         egonw
    // @cdk.created    2007-07-20
    // @cdk.module     core
    public class CDKAtomTypeMatcher
        : IAtomTypeMatcher
    {
        public enum Mode
        {
            RequireNothing = 1,
            RequireExplicitHydrogens = 2,
        }

        private readonly AtomTypeFactory factory = CDK.CdkAtomTypeFactory;
        private readonly Mode mode;

        private static ConcurrentDictionary<Mode, CDKAtomTypeMatcher> factories = new ConcurrentDictionary<Mode, CDKAtomTypeMatcher>();
        private static readonly object syncFactorie = new object();

        private CDKAtomTypeMatcher(Mode mode)
        {
            this.mode = mode;
        }

        public static CDKAtomTypeMatcher GetInstance()
        {
            return GetInstance(Mode.RequireNothing);
        }

        public static CDKAtomTypeMatcher GetInstance(Mode mode)
        {
            return factories.GetOrAdd(mode, n => new CDKAtomTypeMatcher(n));
        }

        public IEnumerable<IAtomType> FindMatchingAtomTypes(IAtomContainer atomContainer)
        {
            return FindMatchingAtomTypes(atomContainer, null);
        }

        private IEnumerable<IAtomType> FindMatchingAtomTypes(IAtomContainer atomContainer, RingSearch searcher)
        {
            // cache the ring information
            if (searcher == null)
                searcher = new RingSearch(atomContainer);
            // cache atom bonds
            var connectedBonds = new Dictionary<IAtom, List<IBond>>(atomContainer.Atoms.Count);
            foreach (var bond in atomContainer.Bonds)
            {
                foreach (var atom in bond.Atoms)
                {
                    if (!connectedBonds.TryGetValue(atom, out List<IBond> atomBonds))
                    {
                        atomBonds = new List<IBond>();
                        connectedBonds.Add(atom, atomBonds);
                    }
                    atomBonds.Add(bond);
                }
            }

            foreach (var atom in atomContainer.Atoms)
            {
                yield return FindMatchingAtomType(atomContainer, atom, searcher, connectedBonds.ContainsKey(atom) ? connectedBonds[atom] : null);
            }
            yield break;
        }

        public IAtomType FindMatchingAtomType(IAtomContainer atomContainer, IAtom atom)
        {
            return FindMatchingAtomType(atomContainer, atom, null, null);
        }

        private IAtomType FindMatchingAtomType(IAtomContainer atomContainer, IAtom atom, RingSearch searcher, IReadOnlyList<IBond> connectedBonds)
        {
            IAtomType type = null;
            if (atom is IPseudoAtom)
            {
                return factory.GetAtomType("X");
            }
            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.C:
                    type = PerceiveCarbons(atomContainer, atom, searcher, connectedBonds);
                    break;
                case AtomicNumbers.H:
                    type = PerceiveHydrogens(atomContainer, atom, connectedBonds);
                    break;
                case AtomicNumbers.O:
                    type = PerceiveOxygens(atomContainer, atom, searcher, connectedBonds);
                    break;
                case AtomicNumbers.N:
                    type = PerceiveNitrogens(atomContainer, atom, searcher, connectedBonds);
                    break;
                case AtomicNumbers.S:
                    type = PerceiveSulphurs(atomContainer, atom, searcher, connectedBonds);
                    break;
                case AtomicNumbers.P:
                    type = PerceivePhosphors(atomContainer, atom, connectedBonds);
                    break;
                case AtomicNumbers.Si:
                    type = PerceiveSilicon(atomContainer, atom);
                    break;
                case AtomicNumbers.Li:
                    type = PerceiveLithium(atomContainer, atom);
                    break;
                case AtomicNumbers.B:
                    type = PerceiveBorons(atomContainer, atom);
                    break;
                case AtomicNumbers.Be:
                    type = PerceiveBeryllium(atomContainer, atom);
                    break;
                case AtomicNumbers.Cr:
                    type = PerceiveChromium(atomContainer, atom);
                    break;
                case AtomicNumbers.Se:
                    type = PerceiveSelenium(atomContainer, atom, connectedBonds);
                    break;
                case AtomicNumbers.Mo:
                    type = PerceiveMolybdenum(atomContainer, atom);
                    break;
                case AtomicNumbers.Rb:
                    type = PerceiveRubidium(atomContainer, atom);
                    break;
                case AtomicNumbers.Te:
                    type = PerceiveTellurium(atomContainer, atom);
                    break;
                case AtomicNumbers.Cu:
                    type = PerceiveCopper(atomContainer, atom);
                    break;
                case AtomicNumbers.Ba:
                    type = PerceiveBarium(atomContainer, atom);
                    break;
                case AtomicNumbers.Ga:
                    type = PerceiveGallium(atomContainer, atom);
                    break;
                case AtomicNumbers.Ru:
                    type = PerceiveRuthenium(atomContainer, atom);
                    break;
                case AtomicNumbers.Zn:
                    type = PerceiveZinc(atomContainer, atom);
                    break;
                case AtomicNumbers.Al:
                    type = PerceiveAluminium(atomContainer, atom);
                    break;
                case AtomicNumbers.Ni:
                    type = PerceiveNickel(atomContainer, atom);
                    break;
                case AtomicNumbers.Gd:
                    type = PerceiveGadolinum(atomContainer, atom);
                    break;
                case AtomicNumbers.Ge:
                    type = PerceiveGermanium(atomContainer, atom);
                    break;
                case AtomicNumbers.Co:
                    type = PerceiveCobalt(atomContainer, atom);
                    break;
                case AtomicNumbers.Br:
                    type = PerceiveBromine(atomContainer, atom);
                    break;
                case AtomicNumbers.V:
                    type = PerceiveVanadium(atomContainer, atom);
                    break;
                case AtomicNumbers.Ti:
                    type = PerceiveTitanium(atomContainer, atom);
                    break;
                case AtomicNumbers.Sr:
                    type = PerceiveStrontium(atomContainer, atom);
                    break;
                case AtomicNumbers.Pb:
                    type = PerceiveLead(atomContainer, atom);
                    break;
                case AtomicNumbers.Tl:
                    type = PerceiveThallium(atomContainer, atom);
                    break;
                case AtomicNumbers.Sb:
                    type = PerceiveAntimony(atomContainer, atom);
                    break;
                case AtomicNumbers.Pt:
                    type = PerceivePlatinum(atomContainer, atom);
                    break;
                case AtomicNumbers.Hg:
                    type = PerceiveMercury(atomContainer, atom);
                    break;
                case AtomicNumbers.Fe:
                    type = PerceiveIron(atomContainer, atom);
                    break;
                case AtomicNumbers.Ra:
                    type = PerceiveRadium(atomContainer, atom);
                    break;
                case AtomicNumbers.Au:
                    type = PerceiveGold(atomContainer, atom);
                    break;
                case AtomicNumbers.Ag:
                    type = PerceiveSilver(atomContainer, atom);
                    break;
                case AtomicNumbers.Cl:
                    type = PerceiveChlorine(atomContainer, atom, connectedBonds);
                    break;
                case AtomicNumbers.In:
                    type = PerceiveIndium(atomContainer, atom);
                    break;
                case AtomicNumbers.Pu:
                    type = PerceivePlutonium(atomContainer, atom);
                    break;
                case AtomicNumbers.Th:
                    type = PerceiveThorium(atomContainer, atom);
                    break;
                case AtomicNumbers.K:
                    type = PerceivePotassium(atomContainer, atom);
                    break;
                case AtomicNumbers.Mn:
                    type = PerceiveManganese(atomContainer, atom);
                    break;
                case AtomicNumbers.Mg:
                    type = PerceiveMagnesium(atomContainer, atom);
                    break;
                case AtomicNumbers.Na:
                    type = PerceiveSodium(atomContainer, atom);
                    break;
                case AtomicNumbers.As:
                    type = PerceiveArsenic(atomContainer, atom);
                    break;
                case AtomicNumbers.Cd:
                    type = PerceiveCadmium(atomContainer, atom);
                    break;
                case AtomicNumbers.Ca:
                    type = PerceiveCalcium(atomContainer, atom);
                    break;
                default:
                    if (type == null) type = PerceiveHalogens(atomContainer, atom, connectedBonds);
                    if (type == null) type = PerceiveCommonSalts(atomContainer, atom);
                    if (type == null) type = PerceiveOrganometallicCenters(atomContainer, atom);
                    if (type == null) type = PerceiveNobelGases(atomContainer, atom);
                    break;
            }

            // if no atom type can be assigned we set the atom type to 'X', this flags
            // to other methods that atom typing was performed but did not yield a match
            if (type == null)
            {
                type = GetAtomType("X");
            }

            return type;
        }

        private IAtomType PerceiveGallium(IAtomContainer atomContainer, IAtom atom)
        {
            var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
            if (!IsCharged(atom) && maxBondOrder == BondOrder.Single && atomContainer.GetConnectedBonds(atom).Count() <= 3)
            {
                var type = GetAtomType("Ga");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == 3)
            {
                var type = GetAtomType("Ga.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveGermanium(IAtomContainer atomContainer, IAtom atom)
        {
            var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
            if (!IsCharged(atom) && maxBondOrder == BondOrder.Single && atomContainer.GetConnectedBonds(atom).Count() <= 4)
            {
                var type = GetAtomType("Ge");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            if (atom.FormalCharge == 0 && atomContainer.GetConnectedBonds(atom).Count() == 3)
            {
                var type = GetAtomType("Ge.3");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveSelenium(IAtomContainer atomContainer, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            if (AtomicNumbers.Se.Equals(atom.AtomicNumber))
            {
                if (connectedBonds == null)
                    connectedBonds = atomContainer.GetConnectedBonds(atom).ToReadOnlyList();
                int doublebondcount = CountAttachedDoubleBonds(connectedBonds, atom);
                if (atom.FormalCharge != null && atom.FormalCharge == 0)
                {
                    if (atomContainer.GetConnectedBonds(atom).Count() == 0)
                    {
                        if (atom.ImplicitHydrogenCount != null && atom.ImplicitHydrogenCount == 0)
                        {
                            var type = GetAtomType("Se.2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else
                        {
                            var type = GetAtomType("Se.3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (atomContainer.GetConnectedBonds(atom).Count() == 1)
                    {
                        if (doublebondcount == 1)
                        {
                            var type = GetAtomType("Se.1");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (doublebondcount == 0)
                        {
                            var type = GetAtomType("Se.3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (atomContainer.GetConnectedBonds(atom).Count() == 2)
                    {
                        if (doublebondcount == 0)
                        {
                            var type = GetAtomType("Se.3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (doublebondcount == 2)
                        {
                            var type = GetAtomType("Se.sp2.2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (atomContainer.GetConnectedBonds(atom).Count() == 3)
                    {
                        var type = GetAtomType("Se.sp3.3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (atomContainer.GetConnectedBonds(atom).Count() == 4)
                    {
                        if (doublebondcount == 2)
                        {
                            var type = GetAtomType("Se.sp3.4");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (doublebondcount == 0)
                        {
                            var type = GetAtomType("Se.sp3d1.4");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (atomContainer.GetConnectedBonds(atom).Count() == 5)
                    {
                        var type = GetAtomType("Se.5");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 4)
                      && atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("Se.4plus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 1)
                      && atomContainer.GetConnectedBonds(atom).Count() == 3)
                {
                    var type = GetAtomType("Se.plus.3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == -2)
                      && atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("Se.2minus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveTellurium(IAtomContainer atomContainer, IAtom atom)
        {
            var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
            if (!IsCharged(atom) && maxBondOrder == BondOrder.Single && atomContainer.GetConnectedBonds(atom).Count() <= 2)
            {
                var type = GetAtomType("Te.3");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == 4)
            {
                if (atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("Te.4plus");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveBorons(IAtomContainer atomContainer, IAtom atom)
        {
            var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
            if (atom.FormalCharge == -1 && maxBondOrder == BondOrder.Single
                    && atomContainer.GetConnectedBonds(atom).Count() <= 4)
            {
                var type = GetAtomType("B.minus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == +3 && atomContainer.GetConnectedBonds(atom).Count() == 4)
            {
                var type = GetAtomType("B.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() <= 3)
            {
                var type = GetAtomType("B");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveBeryllium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge == -2
             && atomContainer.GetMaximumBondOrder(atom) == BondOrder.Single
             && atomContainer.GetConnectedBonds(atom).Count() <= 4)
            {
                var type = GetAtomType("Be.2minus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == 0 && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Be.neutral");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveCarbonRadicals(IAtomContainer atomContainer, IAtom atom)
        {
            if (atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("C.radical.planar");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() <= 3)
            {
                var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("C.radical.planar");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Double)
                {
                    var type = GetAtomType("C.radical.sp2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Triple)
                {
                    var type = GetAtomType("C.radical.sp1");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveCarbons(IAtomContainer atomContainer, IAtom atom, RingSearch searcher, IReadOnlyList<IBond> connectedBonds)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return PerceiveCarbonRadicals(atomContainer, atom);
            }
            if (connectedBonds == null)
                connectedBonds = atomContainer.GetConnectedBonds(atom).ToReadOnlyList();
            // if hybridization is given, use that
            if (HasHybridization(atom) && !IsCharged(atom))
            {
                if (atom.Hybridization == Hybridization.SP2)
                {
                    var type = GetAtomType("C.sp2");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.Hybridization == Hybridization.SP3)
                {
                    var type = GetAtomType("C.sp3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.Hybridization == Hybridization.SP1)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (maxBondOrder == BondOrder.Triple)
                    {
                        var type = GetAtomType("C.sp");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("C.allene");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
            }
            else if (IsCharged(atom))
            {
                if (atom.FormalCharge == 1)
                {
                    if (!connectedBonds.Any())
                    {
                        var type = GetAtomType("C.plus.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                        if (maxBondOrder == BondOrder.Triple)
                        {
                            var type = GetAtomType("C.plus.sp1");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (maxBondOrder == BondOrder.Double)
                        {
                            var type = GetAtomType("C.plus.sp2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (maxBondOrder == BondOrder.Single)
                        {
                            var type = GetAtomType("C.plus.planar");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                }
                else if (atom.FormalCharge == -1)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    var connectedBondList = connectedBonds.ToReadOnlyList();
                    if (maxBondOrder == BondOrder.Single && connectedBondList.Count <= 3)
                    {
                        if (BothNeighborsAreSp2(atom, atomContainer, connectedBonds) && IsRingAtom(atom, atomContainer, searcher))
                        {
                            var type = GetAtomType("C.minus.planar");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        {
                            var type = GetAtomType("C.minus.sp3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (maxBondOrder == BondOrder.Double
                          && connectedBondList.Count <= 3)
                    {
                        var type = GetAtomType("C.minus.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (maxBondOrder == BondOrder.Triple
                          && connectedBondList.Count <= 1)
                    {
                        var type = GetAtomType("C.minus.sp1");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                return null;
            }
            else if (atom.IsAromatic)
            {
                var type = GetAtomType("C.sp2");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else if (HasOneOrMoreSingleOrDoubleBonds(connectedBonds))
            {
                var type = GetAtomType("C.sp2");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else if (connectedBonds.Count > 4)
            {
                // FIXME: I don't perceive carbons with more than 4 connections yet
                return null;
            }
            else
            { // OK, use bond order info
                var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                if (maxBondOrder == BondOrder.Quadruple)
                {
                    // WTF??
                    return null;
                }
                else if (maxBondOrder == BondOrder.Triple)
                {
                    var type = GetAtomType("C.sp");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Double)
                {
                    // OK, one or two double bonds?
                    int doubleBondCount = CountAttachedDoubleBonds(connectedBonds, atom);
                    if (doubleBondCount == 2)
                    {
                        var type = GetAtomType("C.allene");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (doubleBondCount == 1)
                    {
                        var type = GetAtomType("C.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else
                {
                    if (HasAromaticBond(connectedBonds))
                    {
                        var type = GetAtomType("C.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    {
                        var type = GetAtomType("C.sp3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
            }
            return null;
        }

        private static BondOrder GetMaximumBondOrder(IReadOnlyList<IBond> connectedBonds)
        {
            var max = BondOrder.Single;
            foreach (var bond in connectedBonds)
            {
                if (bond.Order.Numeric() > max.Numeric())
                    max = bond.Order;
            }
            return max;
        }

        private static bool HasOneOrMoreSingleOrDoubleBonds(IReadOnlyList<IBond> bonds)
        {
            foreach (var bond in bonds)
            {
                if (bond.IsSingleOrDouble)
                    return true;
            }
            return false;
        }

        private static bool HasOneSingleElectron(IAtomContainer atomContainer, IAtom atom)
        {
            return atomContainer.SingleElectrons.Any(n => n.Contains(atom));
        }

        private static int CountSingleElectrons(IAtomContainer atomContainer, IAtom atom)
        {
            return atomContainer.SingleElectrons.Count(n => n.Contains(atom));
        }

        private IAtomType PerceiveOxygenRadicals(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge == 0)
            {
                if (atomContainer.GetConnectedBonds(atom).Count() <= 1)
                {
                    var type = GetAtomType("O.sp3.radical");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (atom.FormalCharge == +1)
            {
                if (atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("O.plus.radical");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (atomContainer.GetConnectedBonds(atom).Count() <= 2)
                {
                    var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                    if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("O.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                    else if (maxBondOrder == BondOrder.Double)
                    {
                        var type = GetAtomType("O.plus.sp2.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                }
            }
            return null;
        }

        private static bool IsCharged(IAtom atom)
        {
            return (atom.FormalCharge != null && atom.FormalCharge != 0);
        }

        private static bool HasHybridization(IAtom atom)
        {
            return !atom.Hybridization.IsUnset();
        }

        private IAtomType PerceiveOxygens(IAtomContainer atomContainer, IAtom atom, RingSearch searcher, IReadOnlyList<IBond> connectedBonds)
        {
            if (HasOneSingleElectron(atomContainer, atom))
                return PerceiveOxygenRadicals(atomContainer, atom);

            // if hybridization is given, use that
            if (connectedBonds == null)
                connectedBonds = atomContainer.GetConnectedBonds(atom).ToReadOnlyList();
            if (HasHybridization(atom) && !IsCharged(atom))
            {
                if (atom.Hybridization == Hybridization.SP2)
                {
                    int connectedAtomsCount = connectedBonds.Count;
                    if (connectedAtomsCount == 1)
                    {
                        if (IsCarboxylate(atomContainer, atom, connectedBonds))
                        {
                            var type = GetAtomType("O.sp2.co2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else
                        {
                            var type = GetAtomType("O.sp2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (connectedAtomsCount == 2)
                    {
                        var type = GetAtomType("O.planar3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (atom.Hybridization == Hybridization.SP3)
                {
                    var type = GetAtomType("O.sp3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.Hybridization == Hybridization.Planar3)
                {
                    var type = GetAtomType("O.planar3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (IsCharged(atom))
            {
                if (atom.FormalCharge == -1 && connectedBonds.Count <= 1)
                {
                    if (IsCarboxylate(atomContainer, atom, connectedBonds))
                    {
                        var type = GetAtomType("O.minus.co2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("O.minus");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (atom.FormalCharge == -2 && connectedBonds.Count == 0)
                {
                    var type = GetAtomType("O.minus2");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == +1)
                {
                    if (connectedBonds.Count == 0)
                    {
                        var type = GetAtomType("O.plus");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (maxBondOrder == BondOrder.Double)
                    {
                        var type = GetAtomType("O.plus.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (maxBondOrder == BondOrder.Triple)
                    {
                        var type = GetAtomType("O.plus.sp1");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("O.plus");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                return null;
            }
            else if (connectedBonds.Count > 2)
            {
                // FIXME: I don't perceive carbons with more than 4 connections yet
                return null;
            }
            else if (connectedBonds.Count == 0)
            {
                var type = GetAtomType("O.sp3");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else
            { // OK, use bond order info
                var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                if (maxBondOrder == BondOrder.Double)
                {
                    if (IsCarboxylate(atomContainer, atom, connectedBonds))
                    {
                        var type = GetAtomType("O.sp2.co2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("O.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (maxBondOrder == BondOrder.Single)
                {
                    int explicitHydrogens = CountExplicitHydrogens(atom, connectedBonds);
                    int connectedHeavyAtoms = connectedBonds.Count - explicitHydrogens;
                    if (connectedHeavyAtoms == 2)
                    {
                        // a O.sp3 which is expected to take part in an aromatic system
                        if (BothNeighborsAreSp2(atom, atomContainer, connectedBonds) && IsRingAtom(atom, atomContainer, searcher))
                        {
                            var type = GetAtomType("O.planar3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        {
                            var type = GetAtomType("O.sp3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else
                    {
                        var type = GetAtomType("O.sp3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
            }
            return null;
        }

        private static bool IsCarboxylate(IAtomContainer container, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            // assumes that the oxygen only has one neighbor (C=O, or C-[O-])
            if (connectedBonds.Count != 1)
                return false;
            var carbon = connectedBonds.First().GetOther(atom);
            if (!AtomicNumbers.C.Equals(carbon.AtomicNumber))
                return false;

            var carbonBonds = container.GetConnectedBonds(carbon).ToReadOnlyList();
            if (carbonBonds.Count < 2)
                return false;
            int oxygenCount = 0;
            int singleBondedNegativeOxygenCount = 0;
            int doubleBondedOxygenCount = 0;
            foreach (var cBond in carbonBonds)
            {
                var neighbor = cBond.GetOther(carbon);
                if (AtomicNumbers.O.Equals(neighbor.AtomicNumber))
                {
                    oxygenCount++;
                    var order = cBond.Order;
                    var charge = neighbor.FormalCharge;
                    if (order == BondOrder.Single && charge.HasValue && charge.Value == -1)
                    {
                        singleBondedNegativeOxygenCount++;
                    }
                    else if (order == BondOrder.Double)
                    {
                        doubleBondedOxygenCount++;
                    }
                }
            }
            return (oxygenCount == 2) && (singleBondedNegativeOxygenCount == 1) && (doubleBondedOxygenCount == 1);
        }

        private static bool AtLeastTwoNeighborsAreSp2(IAtom atom, IAtomContainer atomContainer, IReadOnlyList<IBond> connectedBonds)
        {
            int count = 0;
            foreach (var bond in connectedBonds)
            {
                if (bond.Order == BondOrder.Double || bond.IsAromatic)
                {
                    count++;
                }
                else
                {
                    var nextAtom = bond.GetOther(atom);
                    if (nextAtom.Hybridization == Hybridization.SP2)
                    {
                        // OK, it's SP2
                        count++;
                    }
                    else
                    {
                        var nextConnectBonds = atomContainer.GetConnectedBonds(nextAtom).ToReadOnlyList();
                        if (CountAttachedDoubleBonds(nextConnectBonds, nextAtom) > 0)
                        {
                            // OK, it's SP2
                            count++;
                        }
                    }
                }
                if (count >= 2)
                    return true;
            }
            return false;
        }

        private static bool BothNeighborsAreSp2(IAtom atom, IAtomContainer atomContainer, IReadOnlyList<IBond> connectedBonds)
        {
            return AtLeastTwoNeighborsAreSp2(atom, atomContainer, connectedBonds);
        }

        private IAtomType PerceiveNitrogenRadicals(IAtomContainer atomContainer, IAtom atom)
        {
            if (atomContainer.GetConnectedBonds(atom).Count() >= 1 && atomContainer.GetConnectedBonds(atom).Count() <= 2)
            {
                var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                if (atom.FormalCharge != null && atom.FormalCharge == +1)
                {
                    if (maxBondOrder == BondOrder.Double)
                    {
                        var type = GetAtomType("N.plus.sp2.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                    else if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("N.plus.sp3.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                }
                else if (atom.FormalCharge == null || atom.FormalCharge == 0)
                {
                    if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("N.sp3.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                    else if (maxBondOrder == BondOrder.Double)
                    {
                        var type = GetAtomType("N.sp2.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                }
            }
            else
            {
                var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                if (atom.FormalCharge != null && atom.FormalCharge == +1
                        && maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("N.plus.sp3.radical");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveMolybdenum(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == 0)
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 4)
                {
                    var type = GetAtomType("Mo.4");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                IAtomType type1 = GetAtomType("Mo.metallic");
                if (IsAcceptable(atom, atomContainer, type1))
                {
                    return type1;
                }
            }
            return null;
        }

        private IAtomType PerceiveNitrogens(IAtomContainer atomContainer, IAtom atom, RingSearch searcher, IReadOnlyList<IBond> connectedBonds)
        {
            // if hybridization is given, use that
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return PerceiveNitrogenRadicals(atomContainer, atom);
            }

            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();

            if (HasHybridization(atom) && !IsCharged(atom))
            {
                if (atom.Hybridization == Hybridization.SP1)
                {
                    int neighborCount = connectedBonds.Count;
                    if (neighborCount > 1)
                    {
                        var type = GetAtomType("N.sp1.2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("N.sp1");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (atom.Hybridization == Hybridization.SP2)
                {
                    if (IsAmide(atom, atomContainer, connectedBonds))
                    {
                        var type = GetAtomType("N.amide");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (IsThioAmide(atom, atomContainer, connectedBonds))
                    {
                        var type = GetAtomType("N.thioamide");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    // but an sp2 hyb N might N.sp2 or N.planar3 (pyrrole), so check for the latter
                    int neighborCount = connectedBonds.Count;
                    if (neighborCount == 4 && BondOrder.Double == GetMaximumBondOrder(connectedBonds))
                    {
                        var type = GetAtomType("N.oxide");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (neighborCount > 1 && BothNeighborsAreSp2(atom, atomContainer, connectedBonds))
                    {
                        if (IsRingAtom(atom, atomContainer, searcher))
                        {
                            if (neighborCount == 3)
                            {
                                BondOrder maxOrder = GetMaximumBondOrder(connectedBonds);
                                if (maxOrder == BondOrder.Double)
                                {
                                    var type = GetAtomType("N.sp2.3");
                                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                        return type;
                                }
                                else if (maxOrder == BondOrder.Single)
                                {
                                    var type = GetAtomType("N.planar3");
                                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                        return type;
                                }
                            }
                            else if (neighborCount == 2)
                            {
                                BondOrder maxOrder = GetMaximumBondOrder(connectedBonds);
                                if (maxOrder == BondOrder.Single)
                                {
                                    if (atom.ImplicitHydrogenCount != null
                                            && atom.ImplicitHydrogenCount == 1)
                                    {
                                        var type = GetAtomType("N.planar3");
                                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                            return type;
                                    }
                                    else
                                    {
                                        var type = GetAtomType("N.sp2");
                                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                            return type;
                                    }
                                }
                                else if (maxOrder == BondOrder.Double)
                                {
                                    var type = GetAtomType("N.sp2");
                                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                        return type;
                                }
                            }
                        }
                    }
                    {
                        var type = GetAtomType("N.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (atom.Hybridization == Hybridization.SP3)
                {
                    var type = GetAtomType("N.sp3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.Hybridization == Hybridization.Planar3)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (connectedBonds.Count == 3 && maxBondOrder == BondOrder.Double
                            && CountAttachedDoubleBonds(connectedBonds, atom, "O") == 2)
                    {
                        var type = GetAtomType("N.nitro");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    {
                        var type = GetAtomType("N.planar3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
            }
            else if (IsCharged(atom))
            {
                if (atom.FormalCharge == 1)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (maxBondOrder == BondOrder.Single || connectedBonds.Count == 0)
                    {
                        if (atom.Hybridization == Hybridization.SP2)
                        {
                            var type = GetAtomType("N.plus.sp2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        {
                            var type = GetAtomType("N.plus");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (maxBondOrder == BondOrder.Double)
                    {
                        int doubleBonds = CountAttachedDoubleBonds(connectedBonds, atom);
                        if (doubleBonds == 1)
                        {
                            var type = GetAtomType("N.plus.sp2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (doubleBonds == 2)
                        {
                            var type = GetAtomType("N.plus.sp1");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (maxBondOrder == BondOrder.Triple)
                    {
                        if (connectedBonds.Count == 2)
                        {
                            var type = GetAtomType("N.plus.sp1");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                }
                else if (atom.FormalCharge == -1)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (maxBondOrder == BondOrder.Single)
                    {
                        if (connectedBonds.Count >= 2 && BothNeighborsAreSp2(atom, atomContainer, connectedBonds)
                                && IsRingAtom(atom, atomContainer, searcher))
                        {
                            var type = GetAtomType("N.minus.planar3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (connectedBonds.Count <= 2)
                        {
                            var type = GetAtomType("N.minus.sp3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (maxBondOrder == BondOrder.Double)
                    {
                        if (connectedBonds.Count <= 1)
                        {
                            var type = GetAtomType("N.minus.sp2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                }
            }
            else if (connectedBonds.Count > 3)
            {
                if (connectedBonds.Count == 4 && CountAttachedDoubleBonds(connectedBonds, atom) == 1)
                {
                    var type = GetAtomType("N.oxide");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                return null;
            }
            else if (connectedBonds.Count == 0)
            {
                var type = GetAtomType("N.sp3");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else if (HasOneOrMoreSingleOrDoubleBonds(connectedBonds))
            {
                int connectedAtoms = connectedBonds.Count
                        + atom.ImplicitHydrogenCount ?? 0;
                if (connectedAtoms == 3)
                {
                    var type = GetAtomType("N.planar3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                {
                    var type = GetAtomType("N.sp2");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else
            { // OK, use bond order info
                var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                if (maxBondOrder == BondOrder.Single)
                {
                    if (IsAmide(atom, atomContainer, connectedBonds))
                    {
                        var type = GetAtomType("N.amide");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (IsThioAmide(atom, atomContainer, connectedBonds))
                    {
                        var type = GetAtomType("N.thioamide");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }

                    var heavy = HeavyBonds(connectedBonds);

                    int expHCount = heavy.Count - connectedBonds.Count;

                    if (heavy.Count == 2)
                    {
                        if (heavy[0].IsAromatic && heavy[1].IsAromatic)
                        {
                            int hCount = atom.ImplicitHydrogenCount != null ? atom.ImplicitHydrogenCount.Value
                                + expHCount : expHCount;
                            if (hCount == 0)
                            {
                                if (maxBondOrder == BondOrder.Single
                                        && IsSingleHeteroAtom(atom, atomContainer))
                                {
                                    var type = GetAtomType("N.planar3");
                                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                        return type;
                                }
                                else
                                {
                                    var type = GetAtomType("N.sp2");
                                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                        return type;
                                }
                            }
                            else if (hCount == 1)
                            {
                                var type = GetAtomType("N.planar3");
                                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                    return type;
                            }
                        }
                        else if (BothNeighborsAreSp2(atom, atomContainer, connectedBonds) && IsRingAtom(atom, atomContainer, searcher))
                        {
                            // a N.sp3 which is expected to take part in an aromatic system
                            var type = GetAtomType("N.planar3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else
                        {
                            var type = GetAtomType("N.sp3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (heavy.Count == 3)
                    {
                        if (BothNeighborsAreSp2(atom, atomContainer, connectedBonds) && IsRingAtom(atom, atomContainer, searcher))
                        {
                            var type = GetAtomType("N.planar3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        {
                            var type = GetAtomType("N.sp3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (heavy.Count == 1)
                    {
                        var type = GetAtomType("N.sp3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (heavy.Count == 0)
                    {
                        var type = GetAtomType("N.sp3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (maxBondOrder == BondOrder.Double)
                {
                    if (connectedBonds.Count == 3
                            && CountAttachedDoubleBonds(connectedBonds, atom, "O") == 2)
                    {
                        var type = GetAtomType("N.nitro");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (connectedBonds.Count == 3
                          && CountAttachedDoubleBonds(connectedBonds, atom) > 0)
                    {
                        var type = GetAtomType("N.sp2.3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    {
                        var type = GetAtomType("N.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (maxBondOrder == BondOrder.Triple)
                {
                    int neighborCount = connectedBonds.Count;
                    if (neighborCount > 1)
                    {
                        var type = GetAtomType("N.sp1.2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("N.sp1");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Determines whether the bonds (up to two spheres away) are only to non
        /// hetroatoms. Currently used in N.planar3 perception of (e.g. pyrrole).
        /// </summary>
        /// <param name="atom">an atom to test</param>
        /// <param name="container">container of the atom</param>
        /// <returns>whether the atom's only bonds are to heteroatoms</returns>
        /// <seealso cref="PerceiveNitrogens(IAtomContainer, IAtom, RingSearch, IReadOnlyList{IBond})"/>
        private static bool IsSingleHeteroAtom(IAtom atom, IAtomContainer container)
        {
            var connected = container.GetConnectedAtoms(atom);

            foreach (var atom1 in connected)
            {
                bool aromatic = container.GetBond(atom, atom1).IsAromatic;

                // ignoring non-aromatic bonds
                if (!aromatic) continue;

                // found a hetroatom - we're not a single hetroatom
                if (!AtomicNumbers.C.Equals(atom1.AtomicNumber))
                    return false;

                // check the second sphere
                foreach (var atom2 in container.GetConnectedAtoms(atom1))
                {
                    if (!atom2.Equals(atom) 
                     && container.GetBond(atom1, atom2).IsAromatic
                     && !AtomicNumbers.C.Equals(atom2.AtomicNumber))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsRingAtom(IAtom atom, IAtomContainer atomContainer, RingSearch searcher)
        {
            if (searcher == null) searcher = new RingSearch(atomContainer);
            return searcher.Cyclic(atom);
        }

        private static bool IsAmide(IAtom atom, IAtomContainer atomContainer, IReadOnlyList<IBond> connectedBonds)
        {
            if (connectedBonds.Count < 1)
                return false;
            foreach (var bond in connectedBonds)
            {
                var neighbor = bond.GetOther(atom);
                if (AtomicNumbers.C.Equals(neighbor.AtomicNumber))
                {
                    if (CountAttachedDoubleBonds(atomContainer.GetConnectedBonds(neighbor).ToReadOnlyList(), neighbor, "O") == 1)
                        return true;
                }
            }
            return false;
        }

        private static bool IsThioAmide(IAtom atom, IAtomContainer atomContainer, IReadOnlyList<IBond> connectedBonds)
        {
            if (connectedBonds.Count < 1) return false;
            foreach (var bond in connectedBonds)
            {
                var neighbor = bond.GetOther(atom);
                if (AtomicNumbers.C.Equals(neighbor.AtomicNumber))
                {
                    if (CountAttachedDoubleBonds(atomContainer.GetConnectedBonds(neighbor).ToReadOnlyList(), neighbor, "S") == 1)
                        return true;
                }
            }
            return false;
        }

        private static int CountExplicitHydrogens(IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            int count = 0;
            foreach (var bond in connectedBonds)
            {
                var aAtom = bond.GetOther(atom);
                if (aAtom.AtomicNumber.Equals(AtomicNumbers.H))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Filter a bond list keeping only bonds between heavy atoms.
        /// </summary>
        /// <param name="bonds">a list of bond</param>
        /// <returns>the bond list only with heavy bonds</returns>
        private static List<IBond> HeavyBonds(IReadOnlyList<IBond> bonds)
        {
            var heavy = new List<IBond>();
            foreach (var bond in bonds)
                if (!(bond.Begin.AtomicNumber.Equals(AtomicNumbers.H) && bond.End.AtomicNumber.Equals(AtomicNumbers.H)))
                    heavy.Add(bond);
            return heavy;
        }

        private IAtomType PerceiveIron(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.Fe.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
                {
                    int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                    if (neighbors == 0)
                    {
                        var type = GetAtomType("Fe.metallic");
                        if (IsAcceptable(atom, atomContainer, type))
                        {
                            return type;
                        }
                    }
                    else if (neighbors == 2)
                    {
                        IAtomType type5 = GetAtomType("Fe.2");
                        if (IsAcceptable(atom, atomContainer, type5))
                        {
                            return type5;
                        }
                    }
                    else if (neighbors == 3)
                    {
                        IAtomType type6 = GetAtomType("Fe.3");
                        if (IsAcceptable(atom, atomContainer, type6))
                        {
                            return type6;
                        }
                    }
                    else if (neighbors == 4)
                    {
                        IAtomType type7 = GetAtomType("Fe.4");
                        if (IsAcceptable(atom, atomContainer, type7))
                        {
                            return type7;
                        }
                    }
                    else if (neighbors == 5)
                    {
                        IAtomType type8 = GetAtomType("Fe.5");
                        if (IsAcceptable(atom, atomContainer, type8))
                        {
                            return type8;
                        }
                    }
                    else if (neighbors == 6)
                    {
                        IAtomType type9 = GetAtomType("Fe.6");
                        if (IsAcceptable(atom, atomContainer, type9))
                        {
                            return type9;
                        }
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 2))
                {
                    int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                    if (neighbors <= 1)
                    {
                        var type = GetAtomType("Fe.2plus");
                        if (IsAcceptable(atom, atomContainer, type))
                        {
                            return type;
                        }
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 1))
                {
                    int neighbors = atomContainer.GetConnectedBonds(atom).Count();

                    if (neighbors == 2)
                    {
                        IAtomType type0 = GetAtomType("Fe.plus");
                        if (IsAcceptable(atom, atomContainer, type0))
                        {
                            return type0;
                        }
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 3))
                {
                    IAtomType type1 = GetAtomType("Fe.3plus");
                    if (IsAcceptable(atom, atomContainer, type1))
                    {
                        return type1;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == -2))
                {
                    IAtomType type2 = GetAtomType("Fe.2minus");
                    if (IsAcceptable(atom, atomContainer, type2))
                    {
                        return type2;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == -3))
                {
                    IAtomType type3 = GetAtomType("Fe.3minus");
                    if (IsAcceptable(atom, atomContainer, type3))
                    {
                        return type3;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == -4))
                {
                    IAtomType type4 = GetAtomType("Fe.4minus");
                    if (IsAcceptable(atom, atomContainer, type4))
                    {
                        return type4;
                    }
                }
            }
            return null;
        }

        private IAtomType PerceiveMercury(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.Hg.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == -1))
                {
                    var type = GetAtomType("Hg.minus");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 2))
                {
                    var type = GetAtomType("Hg.2plus");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == +1))
                {
                    int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                    if (neighbors <= 1)
                    {
                        var type = GetAtomType("Hg.plus");
                        if (IsAcceptable(atom, atomContainer, type))
                        {
                            return type;
                        }
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
                {
                    int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                    if (neighbors == 2)
                    {
                        var type = GetAtomType("Hg.2");
                        if (IsAcceptable(atom, atomContainer, type))
                        {
                            return type;
                        }
                    }
                    else if (neighbors == 1)
                    {
                        var type = GetAtomType("Hg.1");
                        if (IsAcceptable(atom, atomContainer, type))
                        {
                            return type;
                        }
                    }
                    else if (neighbors == 0)
                    {
                        var type = GetAtomType("Hg.metallic");
                        if (IsAcceptable(atom, atomContainer, type))
                        {
                            return type;
                        }
                    }
                }
            }
            return null;
        }

        private IAtomType PerceiveSulphurs(IAtomContainer atomContainer, IAtom atom, RingSearch searcher, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();
            var maxBondOrder = GetMaximumBondOrder(connectedBonds);
            int neighborcount = connectedBonds.Count;
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if (atom.Hybridization == Hybridization.SP2
                  && atom.FormalCharge != null && atom.FormalCharge == +1)
            {
                if (neighborcount == 3)
                {
                    var type = GetAtomType("S.inyl.charged");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else
                {
                    var type = GetAtomType("S.plus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge != 0)
            {

                if (atom.FormalCharge == -1 && neighborcount == 1)
                {
                    var type = GetAtomType("S.minus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == +1 && neighborcount == 2)
                {
                    var type = GetAtomType("S.plus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == +1 && neighborcount == 3)
                {
                    var type = GetAtomType("S.inyl.charged");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == +2 && neighborcount == 4)
                {
                    var type = GetAtomType("S.onyl.charged");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == -2 && neighborcount == 0)
                {
                    var type = GetAtomType("S.2minus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 0)
            {
                if (atom.FormalCharge != null && atom.FormalCharge == 0)
                {
                    var type = GetAtomType("S.3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 1)
            {
                if (connectedBonds.First().Order == BondOrder.Double)
                {
                    var type = GetAtomType("S.2");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (connectedBonds.First().Order == BondOrder.Single)
                {
                    var type = GetAtomType("S.3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 2)
            {
                if (BothNeighborsAreSp2(atom, atomContainer, connectedBonds) && IsRingAtom(atom, atomContainer, searcher))
                {
                    if (CountAttachedDoubleBonds(connectedBonds, atom) == 2)
                    {
                        var type = GetAtomType("S.inyl.2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("S.planar3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (CountAttachedDoubleBonds(connectedBonds, atom, "O") == 2)
                {
                    var type = GetAtomType("S.oxide");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (CountAttachedDoubleBonds(connectedBonds, atom) == 2)
                {
                    var type = GetAtomType("S.inyl.2");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (CountAttachedDoubleBonds(connectedBonds, atom) <= 1)
                {
                    var type = GetAtomType("S.3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (CountAttachedDoubleBonds(connectedBonds, atom) == 0
                      && CountAttachedSingleBonds(connectedBonds, atom) == 2)
                {
                    var type = GetAtomType("S.octahedral");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 3)
            {
                int doubleBondedAtoms = CountAttachedDoubleBonds(connectedBonds, atom);
                if (doubleBondedAtoms == 1)
                {
                    var type = GetAtomType("S.inyl");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (doubleBondedAtoms == 3)
                {
                    var type = GetAtomType("S.trioxide");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (doubleBondedAtoms == 0)
                {
                    var type = GetAtomType("S.anyl");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 4)
            {
                // count the number of double bonded oxygens
                int doubleBondedOxygens = CountAttachedDoubleBonds(connectedBonds, atom, "O");
                int doubleBondedNitrogens = CountAttachedDoubleBonds(connectedBonds, atom, "N");
                int doubleBondedSulphurs = CountAttachedDoubleBonds(connectedBonds, atom, "S");
                int countAttachedDoubleBonds = CountAttachedDoubleBonds(connectedBonds, atom);

                if (doubleBondedOxygens + doubleBondedNitrogens == 2)
                {
                    var type = GetAtomType("S.onyl");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (doubleBondedSulphurs == 1 && doubleBondedOxygens == 1)
                {
                    var type = GetAtomType("S.thionyl");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("S.anyl");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (doubleBondedOxygens == 1 && countAttachedDoubleBonds == 1)
                {
                    var type = GetAtomType("S.sp3d1");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (countAttachedDoubleBonds == 2 && maxBondOrder == BondOrder.Double)
                {
                    var type = GetAtomType("S.sp3.4");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }

            }
            else if (neighborcount == 5)
            {

                if (maxBondOrder == BondOrder.Double)
                {

                    var type = GetAtomType("S.sp3d1");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("S.octahedral");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 6)
            {
                if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("S.octahedral");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceivePhosphors(IAtomContainer atomContainer, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();
            int neighborcount = connectedBonds.Count;
            var maxBondOrder = GetMaximumBondOrder(connectedBonds);
            if (CountSingleElectrons(atomContainer, atom) == 3)
            {
                var type = GetAtomType("P.se.3");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if (neighborcount == 0)
            {
                if (atom.FormalCharge == null || atom.FormalCharge.Value == 0)
                {
                    var type = GetAtomType("P.ine");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 1)
            {
                if (atom.FormalCharge == null || atom.FormalCharge.Value == 0)
                {
                    var type = GetAtomType("P.ide");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 3)
            {
                int doubleBonds = CountAttachedDoubleBonds(connectedBonds, atom);
                if (atom.FormalCharge != null && atom.FormalCharge.Value == 1)
                {
                    var type = GetAtomType("P.anium");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (doubleBonds == 1)
                {
                    var type = GetAtomType("P.ate");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else
                {
                    var type = GetAtomType("P.ine");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 2)
            {
                if (maxBondOrder == BondOrder.Double)
                {
                    if (atom.FormalCharge != null && atom.FormalCharge.Value == 1)
                    {
                        var type = GetAtomType("P.sp1.plus");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("P.irane");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("P.ine");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 4)
            {
                // count the number of double bonded oxygens
                int doubleBonds = CountAttachedDoubleBonds(connectedBonds, atom);
                if (atom.FormalCharge == 1 && doubleBonds == 0)
                {
                    var type = GetAtomType("P.ate.charged");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (doubleBonds == 1)
                {
                    var type = GetAtomType("P.ate");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 5)
            {
                if (atom.FormalCharge == null || atom.FormalCharge.Value == 0)
                {
                    var type = GetAtomType("P.ane");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveHydrogens(IAtomContainer atomContainer, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();
            int neighborcount = connectedBonds.Count;
            if (HasOneSingleElectron(atomContainer, atom))
            {
                if ((atom.FormalCharge == null || atom.FormalCharge == 0) && neighborcount == 0)
                {
                    var type = GetAtomType("H.radical");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                return null;
            }
            else if (neighborcount == 2)
            {
                // FIXME: bridging hydrogen as in B2H6
                return null;
            }
            else if (neighborcount == 1)
            {
                if (atom.FormalCharge == null || atom.FormalCharge == 0)
                {
                    var type = GetAtomType("H");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (neighborcount == 0)
            {
                if (atom.FormalCharge == null || atom.FormalCharge == 0)
                {
                    var type = GetAtomType("H");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == 1)
                {
                    var type = GetAtomType("H.plus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge == -1)
                {
                    var type = GetAtomType("H.minus");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveLithium(IAtomContainer atomContainer, IAtom atom)
        {
            var neighborcount = atomContainer.GetConnectedBonds(atom).Count();
            if (neighborcount == 1)
            {
                if (atom.FormalCharge == null || atom.FormalCharge == 0)
                {
                    var type = GetAtomType("Li");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (neighborcount == 0)
            {
                if (atom.FormalCharge == null || atom.FormalCharge == 0)
                {
                    var type = GetAtomType("Li.neutral");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                if (atom.FormalCharge == null || atom.FormalCharge == +1)
                {
                    var type = GetAtomType("Li.plus");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveHalogens(IAtomContainer atomContainer, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();
            if (AtomicNumbers.F.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    if (connectedBonds.Count == 0)
                    {
                        if (atom.FormalCharge != null && atom.FormalCharge == +1)
                        {
                            var type = GetAtomType("F.plus.radical");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (atom.FormalCharge == null || atom.FormalCharge == 0)
                        {
                            var type = GetAtomType("F.radical");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    else if (connectedBonds.Count <= 1)
                    {
                        var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                        if (maxBondOrder == BondOrder.Single)
                        {
                            var type = GetAtomType("F.plus.radical");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                    return null;
                }
                else if (atom.FormalCharge != null && atom.FormalCharge != 0)
                {
                    if (atom.FormalCharge == -1)
                    {
                        var type = GetAtomType("F.minus");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                    else if (atom.FormalCharge == 1)
                    {
                        var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                        if (maxBondOrder == BondOrder.Double)
                        {
                            var type = GetAtomType("F.plus.sp2");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                        else if (maxBondOrder == BondOrder.Single)
                        {
                            var type = GetAtomType("F.plus.sp3");
                            if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                                return type;
                        }
                    }
                }
                else if (connectedBonds.Count == 1 || connectedBonds.Count == 0)
                {
                    var type = GetAtomType("F");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (AtomicNumbers.I.Equals(atom.AtomicNumber))
            {
                return PerceiveIodine(atomContainer, atom, connectedBonds);
            }

            return null;
        }

        private IAtomType PerceiveArsenic(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +1 && atomContainer
                  .GetConnectedBonds(atom).Count() <= 4))
            {
                var type = GetAtomType("As.plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 4)
                {
                    var type = GetAtomType("As.5");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                if (neighbors == 2)
                {
                    var type = GetAtomType("As.2");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                {
                    var type = GetAtomType("As");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +3))
            {
                var type = GetAtomType("As.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == -1))
            {
                var type = GetAtomType("As.minus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveThorium(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.Th.Equals(atom.AtomicNumber))
            {
                if (atom.FormalCharge == 0 && atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("Th");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        private IAtomType PerceiveRubidium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == +1)
            {
                var type = GetAtomType("Rb.plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0)
            {
                var type = GetAtomType("Rb.neutral");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveCommonSalts(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.Mg.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
                {
                    var type = GetAtomType("Mg.2plus");
                    if (IsAcceptable(atom, atomContainer, type)) return type;
                }
            }
            else if (AtomicNumbers.Co.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
                {
                    var type = GetAtomType("Co.2plus");
                    if (IsAcceptable(atom, atomContainer, type)) return type;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == +3))
                {
                    var type = GetAtomType("Co.3plus");
                    if (IsAcceptable(atom, atomContainer, type)) return type;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("Co.metallic");
                    if (IsAcceptable(atom, atomContainer, type)) return type;
                }
            }
            else if (AtomicNumbers.W.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("W.metallic");
                    if (IsAcceptable(atom, atomContainer, type)) return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveCopper(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                var type = GetAtomType("Cu.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0)
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 1)
                {
                    var type = GetAtomType("Cu.1");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                else
                {
                    IAtomType type01 = GetAtomType("Cu.metallic");
                    if (IsAcceptable(atom, atomContainer, type01))
                    {
                        return type01;
                    }
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == +1)
            {
                IAtomType type02 = GetAtomType("Cu.plus");
                if (IsAcceptable(atom, atomContainer, type02))
                {
                    return type02;
                }
            }
            return null;
        }

        private IAtomType PerceiveBarium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 2))
            {
                var type = GetAtomType("Ba.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveAluminium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == 3)
            {
                int connectedBondsCount = atomContainer.GetConnectedBonds(atom).Count();
                if (connectedBondsCount == 0)
                {
                    var type = GetAtomType("Al.3plus");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0
                  && atomContainer.GetConnectedBonds(atom).Count() == 3)
            {
                var type = GetAtomType("Al");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == -3
                  && atomContainer.GetConnectedBonds(atom).Count() == 6)
            {
                var type = GetAtomType("Al.3minus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveZinc(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() == 0
                  && (atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                var type = GetAtomType("Zn.metallic");
                if (IsAcceptable(atom, atomContainer, type)) return type;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() == 0
                  && (atom.FormalCharge != null && atom.FormalCharge == 2))
            {
                var type = GetAtomType("Zn.2plus");
                if (IsAcceptable(atom, atomContainer, type)) return type;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() == 1
                  && (atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                var type = GetAtomType("Zn.1");
                if (IsAcceptable(atom, atomContainer, type)) return type;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() == 2
                  && (atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                var type = GetAtomType("Zn");
                if (IsAcceptable(atom, atomContainer, type)) return type;
            }
            return null;
        }

        private IAtomType PerceiveChromium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == 0
                    && atomContainer.GetConnectedBonds(atom).Count() == 6)
            {
                var type = GetAtomType("Cr");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0
                  && atomContainer.GetConnectedBonds(atom).Count() == 4)
            {
                var type = GetAtomType("Cr.4");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 6
                  && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Cr.6plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0
                  && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Cr.neutral");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if (AtomicNumbers.Cr.Equals(atom.AtomicNumber))
            {
                if (atom.FormalCharge != null && atom.FormalCharge == 3
                        && atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("Cr.3plus");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        private IAtomType PerceiveOrganometallicCenters(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.Po.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if (atomContainer.GetConnectedBonds(atom).Count() == 2)
                {
                    var type = GetAtomType("Po");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (AtomicNumbers.Sn.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 0 && atomContainer
                      .GetConnectedBonds(atom).Count() <= 4))
                {
                    var type = GetAtomType("Sn.sp3");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (AtomicNumbers.Sc.Equals(atom.AtomicNumber))
            {
                if (atom.FormalCharge != null && atom.FormalCharge == -3
                        && atomContainer.GetConnectedBonds(atom).Count() == 6)
                {
                    var type = GetAtomType("Sc.3minus");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveNickel(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                var type = GetAtomType("Ni.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0)
                  && atomContainer.GetConnectedBonds(atom).Count() == 2)
            {
                var type = GetAtomType("Ni");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0)
                  && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Ni.metallic");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 1)
                  && atomContainer.GetConnectedBonds(atom).Count() == 1)
            {
                var type = GetAtomType("Ni.plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveNobelGases(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.He.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("He");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (AtomicNumbers.Ne.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("Ne");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (AtomicNumbers.Ar.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("Ar");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (AtomicNumbers.Kr.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("Kr");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (AtomicNumbers.Xe.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    if (atomContainer.GetConnectedBonds(atom).Count() == 0)
                    {
                        var type = GetAtomType("Xe");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("Xe.3");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                }
            }
            else if (AtomicNumbers.Rn.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("Rn");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveSilicon(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0)
            {
                if (atomContainer.GetConnectedBonds(atom).Count() == 2)
                {
                    var type = GetAtomType("Si.2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (atomContainer.GetConnectedBonds(atom).Count() == 3)
                {
                    var type = GetAtomType("Si.3");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (atomContainer.GetConnectedBonds(atom).Count() == 4)
                {
                    var type = GetAtomType("Si.sp3");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == -2)
            {
                var type = GetAtomType("Si.2minus.6");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveManganese(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 2)
                {
                    IAtomType type02 = GetAtomType("Mn.2");
                    if (IsAcceptable(atom, atomContainer, type02))
                        return type02;
                }
                else if (neighbors == 0)
                {
                    IAtomType type03 = GetAtomType("Mn.metallic");
                    if (IsAcceptable(atom, atomContainer, type03))
                        return type03;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                var type = GetAtomType("Mn.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +3))
            {
                var type = GetAtomType("Mn.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveSodium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 1))
            {
                var type = GetAtomType("Na.plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge == null || atom.FormalCharge == 0)
                  && atomContainer.GetConnectedBonds(atom).Count() == 1)
            {
                var type = GetAtomType("Na");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0)
                  && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Na.neutral");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveIodine(IAtomContainer atomContainer, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();

            if (HasOneSingleElectron(atomContainer, atom))
            {
                if (connectedBonds.Count == 0)
                {
                    if (atom.FormalCharge != null && atom.FormalCharge == +1)
                    {
                        var type = GetAtomType("I.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (atom.FormalCharge == null || atom.FormalCharge == 0)
                    {
                        var type = GetAtomType("I.radical");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (connectedBonds.Count <= 1)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("I.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                return null;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge != 0)
            {
                if (atom.FormalCharge == -1)
                {
                    if (connectedBonds.Count == 0)
                    {
                        var type = GetAtomType("I.minus");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else
                    {
                        var type = GetAtomType("I.minus.5");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (atom.FormalCharge == 1)
                {
                    var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                    if (maxBondOrder == BondOrder.Double)
                    {
                        var type = GetAtomType("I.plus.sp2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("I.plus.sp3");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
            }
            else if (connectedBonds.Count == 3)
            {
                int doubleBondCount = CountAttachedDoubleBonds(connectedBonds, atom);
                if (doubleBondCount == 2)
                {
                    var type = GetAtomType("I.5");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (atom.FormalCharge != null && atom.FormalCharge == 0)
                {
                    var type = GetAtomType("I.sp3d2.3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (connectedBonds.Count == 2)
            {
                var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                if (maxBondOrder == BondOrder.Double)
                {
                    var type = GetAtomType("I.3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (connectedBonds.Count <= 1)
            {
                var type = GetAtomType("I");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveRuthenium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == 0)
            {
                var type = GetAtomType("Ru.6");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == -2)
            {
                var type = GetAtomType("Ru.2minus.6");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == -3)
            {
                var type = GetAtomType("Ru.3minus.6");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceivePotassium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +1))
            {
                var type = GetAtomType("K.plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == null || atom.FormalCharge == 0)
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 1)
                {
                    var type = GetAtomType("K.neutral");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                {
                    var type = GetAtomType("K.metallic");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceivePlutonium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge == 0 && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Pu");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveCadmium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                var type = GetAtomType("Cd.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                if (atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    var type = GetAtomType("Cd.metallic");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (atomContainer.GetConnectedBonds(atom).Count() == 2)
                {
                    var type = GetAtomType("Cd.2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveIndium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge == 0 && atomContainer.GetConnectedBonds(atom).Count() == 3)
            {
                var type = GetAtomType("In.3");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == 3 && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("In.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == 0 && atomContainer.GetConnectedBonds(atom).Count() == 1)
            {
                var type = GetAtomType("In.1");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else
            {
                var type = GetAtomType("In");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveChlorine(IAtomContainer atomContainer, IAtom atom, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? atomContainer.GetConnectedBonds(atom)).ToReadOnlyList();

            if (HasOneSingleElectron(atomContainer, atom))
            {
                if (connectedBonds.Count > 1)
                {
                    if (atom.FormalCharge != null && atom.FormalCharge == +1)
                    {
                        var type = GetAtomType("Cl.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (connectedBonds.Count == 1)
                {
                    var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                    if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("Cl.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (connectedBonds.Count == 0
                      && (atom.FormalCharge == null || atom.FormalCharge == 0))
                {
                    var type = GetAtomType("Cl.radical");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if (atom.FormalCharge == null || atom.FormalCharge == 0)
            {
                int neighborcount = connectedBonds.Count;
                var maxBondOrder = GetMaximumBondOrder(connectedBonds);

                if (maxBondOrder == BondOrder.Double)
                {
                    if (neighborcount == 2)
                    {
                        var type = GetAtomType("Cl.2");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (neighborcount == 3)
                    {
                        var type = GetAtomType("Cl.chlorate");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                    else if (neighborcount == 4)
                    {
                        var type = GetAtomType("Cl.perchlorate");
                        if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                            return type;
                    }
                }
                else if (neighborcount <= 1)
                {
                    var type = GetAtomType("Cl");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == -1))
            {
                var type = GetAtomType("Cl.minus");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 1)
            {
                var maxBondOrder = GetMaximumBondOrder(connectedBonds);
                if (maxBondOrder == BondOrder.Double)
                {
                    var type = GetAtomType("Cl.plus.sp2");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("Cl.plus.sp3");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +3)
                  && connectedBonds.Count == 4)
            {
                var type = GetAtomType("Cl.perchlorate.charged");
                if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                    return type;
            }
            else
            {
                int doubleBonds = CountAttachedDoubleBonds(connectedBonds, atom);
                if (connectedBonds.Count == 3 && doubleBonds == 2)
                {
                    var type = GetAtomType("Cl.chlorate");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
                else if (connectedBonds.Count == 4 && doubleBonds == 3)
                {
                    var type = GetAtomType("Cl.perchlorate");
                    if (IsAcceptable(atom, atomContainer, type, connectedBonds))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveSilver(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 1)
                {
                    var type = GetAtomType("Ag.1");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                {
                    var type = GetAtomType("Ag.neutral");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 1))
            {
                var type = GetAtomType("Ag.plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveGold(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            int neighbors = atomContainer.GetConnectedBonds(atom).Count();
            if ((atom.FormalCharge != null && atom.FormalCharge == 0) && neighbors == 1)
            {
                var type = GetAtomType("Au.1");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveRadium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                var type = GetAtomType("Ra.neutral");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveCalcium(IAtomContainer atomContainer, IAtom atom)
        {
            if (AtomicNumbers.Ca.Equals(atom.AtomicNumber))
            {
                if (HasOneSingleElectron(atomContainer, atom))
                {
                    // no idea how to deal with this yet
                    return null;
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 2 && atomContainer
                      .GetConnectedBonds(atom).Count() == 0))
                {
                    var type = GetAtomType("Ca.2plus");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 0 && atomContainer
                      .GetConnectedBonds(atom).Count() == 2))
                {
                    var type = GetAtomType("Ca.2");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
                else if ((atom.FormalCharge != null && atom.FormalCharge == 0 && atomContainer
                      .GetConnectedBonds(atom).Count() == 1))
                {
                    var type = GetAtomType("Ca.1");
                    if (IsAcceptable(atom, atomContainer, type))
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        private IAtomType PerceivePlatinum(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 4)
                {
                    var type = GetAtomType("Pt.2plus.4");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else
                {
                    var type = GetAtomType("Pt.2plus");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 2)
                {
                    var type = GetAtomType("Pt.2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 4)
                {
                    var type = GetAtomType("Pt.4");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 6)
                {
                    var type = GetAtomType("Pt.6");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveAntimony(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0 && atomContainer
                  .GetConnectedBonds(atom).Count() == 3))
            {
                var type = GetAtomType("Sb.3");
                if (IsAcceptable(atom, atomContainer, type)) return type;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0 && atomContainer
                  .GetConnectedBonds(atom).Count() == 4))
            {
                var type = GetAtomType("Sb.4");
                if (IsAcceptable(atom, atomContainer, type)) return type;
            }
            return null;
        }

        private IAtomType PerceiveGadolinum(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == +3
                    && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Gd.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                {
                    return type;
                }
            }
            return null;
        }

        private IAtomType PerceiveMagnesium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 4)
                {
                    var type = GetAtomType("Mg.neutral");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 2)
                {
                    var type = GetAtomType("Mg.neutral.2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 1)
                {
                    var type = GetAtomType("Mg.neutral.1");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else
                {
                    var type = GetAtomType("Mg.neutral");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                var type = GetAtomType("Mg.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveThallium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == +1
                    && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Tl.plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0
                  && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Tl");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0
                  && atomContainer.GetConnectedBonds(atom).Count() == 1)
            {
                var type = GetAtomType("Tl.1");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveLead(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == 0
                    && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Pb.neutral");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 2
                  && atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Pb.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == 0
                  && atomContainer.GetConnectedBonds(atom).Count() == 1)
            {
                var type = GetAtomType("Pb.1");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveStrontium(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 2))
            {
                var type = GetAtomType("Sr.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveTitanium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == -3
                    && atomContainer.GetConnectedBonds(atom).Count() == 6)
            {
                var type = GetAtomType("Ti.3minus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge == null || atom.FormalCharge == 0)
                  && atomContainer.GetConnectedBonds(atom).Count() == 4)
            {
                var type = GetAtomType("Ti.sp3");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == 0)
                  && atomContainer.GetConnectedBonds(atom).Count() == 2)
            {
                var type = GetAtomType("Ti.2");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveVanadium(IAtomContainer atomContainer, IAtom atom)
        {
            if (atom.FormalCharge != null && atom.FormalCharge == -3
                    && atomContainer.GetConnectedBonds(atom).Count() == 6)
            {
                var type = GetAtomType("V.3minus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge != null && atom.FormalCharge == -3
                  && atomContainer.GetConnectedBonds(atom).Count() == 4)
            {
                var type = GetAtomType("V.3minus.4");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private IAtomType PerceiveBromine(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                if (atomContainer.GetConnectedBonds(atom).Count() == 0)
                {
                    if (atom.FormalCharge != null && atom.FormalCharge == +1)
                    {
                        var type = GetAtomType("Br.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                    else if (atom.FormalCharge == null || atom.FormalCharge == 0)
                    {
                        var type = GetAtomType("Br.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                }
                else if (atomContainer.GetConnectedBonds(atom).Count() <= 1)
                {
                    var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                    if (maxBondOrder == BondOrder.Single)
                    {
                        var type = GetAtomType("Br.plus.radical");
                        if (IsAcceptable(atom, atomContainer, type))
                            return type;
                    }
                }
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == -1))
            {
                var type = GetAtomType("Br.minus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atom.FormalCharge == 1)
            {
                var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
                if (maxBondOrder == BondOrder.Double)
                {
                    var type = GetAtomType("Br.plus.sp2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (maxBondOrder == BondOrder.Single)
                {
                    var type = GetAtomType("Br.plus.sp3");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() == 1 || atomContainer.GetConnectedBonds(atom).Count() == 0)
            {
                var type = GetAtomType("Br");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if (atomContainer.GetConnectedBonds(atom).Count() == 3)
            {
                var type = GetAtomType("Br.3");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            return null;
        }

        private static int CountAttachedDoubleBonds(IReadOnlyList<IBond> connectedAtoms, IAtom atom, string symbol)
        {
            return CountAttachedBonds(connectedAtoms, atom, BondOrder.Double, symbol);
        }

        private IAtomType PerceiveCobalt(IAtomContainer atomContainer, IAtom atom)
        {
            if (HasOneSingleElectron(atomContainer, atom))
            {
                // no idea how to deal with this yet
                return null;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +2))
            {
                var type = GetAtomType("Co.2plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +3))
            {
                var type = GetAtomType("Co.3plus");
                if (IsAcceptable(atom, atomContainer, type))
                    return type;
            }
            else if ((atom.FormalCharge == null || atom.FormalCharge == 0))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 2)
                {
                    var type = GetAtomType("Co.2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 4)
                {
                    var type = GetAtomType("Co.4");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 6)
                {
                    var type = GetAtomType("Co.6");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 1)
                {
                    var type = GetAtomType("Co.1");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else
                {
                    var type = GetAtomType("Co.metallic");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            else if ((atom.FormalCharge != null && atom.FormalCharge == +1))
            {
                int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                if (neighbors == 2)
                {
                    var type = GetAtomType("Co.plus.2");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 4)
                {
                    var type = GetAtomType("Co.plus.4");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 1)
                {
                    var type = GetAtomType("Co.plus.1");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 6)
                {
                    var type = GetAtomType("Co.plus.6");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else if (neighbors == 5)
                {
                    var type = GetAtomType("Co.plus.5");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
                else
                {
                    var type = GetAtomType("Co.plus");
                    if (IsAcceptable(atom, atomContainer, type))
                        return type;
                }
            }
            return null;
        }

        private static int CountAttachedDoubleBonds(IReadOnlyList<IBond> connectedBonds, IAtom atom)
        {
            return CountAttachedBonds(connectedBonds, atom, BondOrder.Double, null);
        }

        private static int CountAttachedSingleBonds(IReadOnlyList<IBond> connectedBonds, IAtom atom)
        {
            return CountAttachedBonds(connectedBonds, atom, BondOrder.Single, null);
        }

        private static bool HasAromaticBond(IReadOnlyList<IBond> connectedBonds)
        {
            foreach (var bond in connectedBonds)
            {
                if (bond.IsAromatic)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Count the number of doubly bonded atoms.
        /// </summary>
        /// <param name="connectedBonds"></param>
        /// <param name="atom">the atom being looked at</param>
        /// <param name="order">the desired bond order of the attached bonds</param>
        /// <param name="symbol">If not null, then it only counts the double bonded atoms which match the given symbol.</param>
        /// <returns>the number of doubly bonded atoms</returns>
        private static int CountAttachedBonds(IReadOnlyList<IBond> connectedBonds, IAtom atom, BondOrder order, string symbol)
        {
            // count the number of double bonded oxygens
            int doubleBondedAtoms = 0;
            foreach (var bond in connectedBonds)
            {
                if (bond.Order == order)
                {
                    if (bond.Atoms.Count == 2)
                    {
                        if (symbol != null)
                        {
                            // if other atom is of the given element (by its symbol)
                            if (string.Equals(bond.GetOther(atom).Symbol, symbol, StringComparison.Ordinal))
                            {
                                doubleBondedAtoms++;
                            }
                        }
                        else
                        {
                            doubleBondedAtoms++;
                        }
                    }
                }
            }
            return doubleBondedAtoms;
        }

        private IAtomType GetAtomType(string identifier)
        {
            var type = factory.GetAtomType(identifier);
            return type;
        }

        private bool IsAcceptable(IAtom atom, IAtomContainer container, IAtomType type)
        {
            return IsAcceptable(atom, container, type, null);
        }

        private bool IsAcceptable(IAtom atom, IAtomContainer container, IAtomType type, IReadOnlyList<IBond> connectedBonds)
        {
            connectedBonds = (connectedBonds ?? container.GetConnectedBonds(atom)).ToReadOnlyList();
            if (mode == Mode.RequireExplicitHydrogens)
            {
                // make sure no implicit hydrogens were assumed
                var actualContainerCount = connectedBonds.Count;
                var requiredContainerCount = type.FormalNeighbourCount.Value; // TODO: this can throw exception? 
                if (actualContainerCount != requiredContainerCount)
                    return false;
            }
            else if (atom.ImplicitHydrogenCount != null)
            {
                // confirm correct neighbour count
                int connectedAtoms = connectedBonds.Count;
                int hCount = atom.ImplicitHydrogenCount.Value;
                int actualNeighbourCount = connectedAtoms + hCount;
                int requiredNeighbourCount = type.FormalNeighbourCount.Value; // TODO: this can throw exception? 
                if (actualNeighbourCount > requiredNeighbourCount)
                    return false;
            }

            // confirm correct bond orders
            var typeOrder = type.MaxBondOrder;
            if (!typeOrder.IsUnset())
            {
                foreach (var bond in connectedBonds)
                {
                    var order = bond.Order;
                    if (!order.IsUnset())
                    {
                        if (BondManipulator.IsHigherOrder(order, typeOrder))
                            return false;
                    }
                    else if (bond.IsSingleOrDouble)
                    {
                        if (typeOrder != BondOrder.Single && typeOrder != BondOrder.Double)
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // confirm correct valency
            if (type.Valency != null)
            {
                var valence = container.GetBondOrderSum(atom);
                valence += atom.ImplicitHydrogenCount ?? 0;
                if (valence > type.Valency)
                    return false;
            }

            // confirm correct formal charge
            if (atom.FormalCharge != null && !atom.FormalCharge.Equals(type.FormalCharge))
                return false;

            // confirm single electron count
            if (type.GetProperty<int?>(CDKPropertyName.SingleElectronCount) != null)
            {
                var count = CountSingleElectrons(container, atom);
                if (count != type.GetProperty<int>(CDKPropertyName.SingleElectronCount))
                    return false;
            }

            return true;
        }
    }
}
