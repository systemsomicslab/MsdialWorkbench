/* Copyright (C) 2008 Miguel Rojas <miguelrojasch@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Aromaticities;
using NCDK.Reactions.Types.Parameters;
using NCDK.RingSearches;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Reactions.Types
{
    // @author         Miguel Rojas
    // @cdk.created    2006-10-20
    // @cdk.module     reaction
    public abstract class AbstractRadicalSiteReaction : ReactionEngine, IReactionProcess
    {
        /// <inheritdoc/>
        public abstract ReactionSpecification Specification { get; }

        /// <inheritdoc/>
        public abstract IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents);

        internal delegate bool AtomCheck(IAtom atom);
        
        internal IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents, int length, bool checkPrev, AtomCheck atomCheck)
        {
            CheckInitiateParams(reactants, agents);

            IReactionSet setOfReactions = reactants.Builder.NewReactionSet();
            IAtomContainer reactant = reactants[0];

            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactant);
            Aromaticity.CDKLegacy.Apply(reactant);
            AllRingsFinder arf = new AllRingsFinder();
            IRingSet ringSet = arf.FindAllRings(reactant);
            for (int ir = 0; ir < ringSet.Count; ir++)
            {
                IRing ring = (IRing)ringSet[ir];
                for (int jr = 0; jr < ring.Atoms.Count; jr++)
                {
                    IAtom aring = ring.Atoms[jr];
                    aring.IsInRing = true;
                }
            }
            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter) SetActiveCenters(reactant, length, checkPrev, atomCheck);

            HOSECodeGenerator hcg = new HOSECodeGenerator();
            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.IsReactiveCenter && reactant.GetConnectedSingleElectrons(atomi).Count() == 1)
                {
                    IEnumerable<IAtom> atom1s = null;
                    if (checkPrev)
                    {
                        hcg.GetSpheres(reactant, atomi, length - 1, true);
                        atom1s = hcg.GetNodesInSphere(length - 1);
                    }

                    hcg.GetSpheres(reactant, atomi, length, true);
                    foreach (var atoml in hcg.GetNodesInSphere(length))
                    {
                        if (atoml != null 
                         && atoml.IsReactiveCenter
                         && !atoml.IsInRing
                         && (atoml.FormalCharge ?? 0) == 0
                         && !atoml.AtomicNumber.Equals(AtomicNumbers.H) 
                         && reactant.GetMaximumBondOrder(atoml) == BondOrder.Single)
                        {
                            foreach (var atomR in reactant.GetConnectedAtoms(atoml))
                            {
                                if (atom1s != null && atom1s.Contains(atomR))
                                    continue;

                                if (reactant.GetBond(atomR, atoml).IsReactiveCenter
                                 && atomR.IsReactiveCenter
                                 && atomCheck(atomR))
                                {
                                    var atomList = new List<IAtom>
                                    {
                                        atomR,
                                        atomi,
                                        atoml
                                    };
                                    var bondList = new List<IBond>
                                    {
                                        reactant.GetBond(atomR, atoml)
                                    };

                                    var moleculeSet = reactant.Builder.NewChemObjectSet<IAtomContainer>();
                                    moleculeSet.Add(reactant);
                                    var reaction = Mechanism.Initiate(moleculeSet, atomList, bondList);
                                    if (reaction == null)
                                        continue;
                                    else
                                        setOfReactions.Add(reaction);
                                }
                            }
                        }
                    }
                }
            }
            return setOfReactions;
        }
        
        private static void SetActiveCenters(IAtomContainer reactant, int length, bool checkPrev, AtomCheck atomCheck)
        {
            var hcg = new HOSECodeGenerator();
            foreach (var atomi in reactant.Atoms)
            {
                if (reactant.GetConnectedSingleElectrons(atomi).Count() == 1)
                {
                    IEnumerable<IAtom> atom1s = null;
                    if (checkPrev)
                    {
                        hcg.GetSpheres(reactant, atomi, length - 1, true);
                        atom1s = hcg.GetNodesInSphere(length - 1);
                    }

                    hcg.GetSpheres(reactant, atomi, length, true);
                    foreach (var atoml in hcg.GetNodesInSphere(length))
                    {
                        if (atoml != null 
                         && !atoml.IsInRing
                         && (atoml.FormalCharge ?? 0) == 0
                         && !atoml.AtomicNumber.Equals(AtomicNumbers.H)
                         && reactant.GetMaximumBondOrder(atoml) == BondOrder.Single)
                        {
                            foreach (var atomR in reactant.GetConnectedAtoms(atoml))
                            {
                                if (atom1s != null && atom1s.Contains(atomR))
                                    continue;

                                if (atomCheck(atomR))
                                {
                                    atomi.IsReactiveCenter = true;
                                    atoml.IsReactiveCenter = true;
                                    atomR.IsReactiveCenter = true;
                                    reactant.GetBond(atomR, atoml).IsReactiveCenter = true; ;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
