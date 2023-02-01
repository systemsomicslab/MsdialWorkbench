/* Copyright (C) 2004-2007  Kai Hartmann <kaihartmann@users.sf.net>
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

using System.Collections.Generic;
using System.Linq;

namespace NCDK.Graphs.Invariant
{
    // @author       kaihartmann
    // @cdk.created  2004-09-17
    // @cdk.module   reaction
    // @cdk.todo add negatively charged atoms (e.g. O-) to the pi system
    public static class ConjugatedPiSystemsDetector
    {
        /// <summary>
        /// Detect all conjugated pi systems in an AtomContainer. This method returns a AtomContainerSet
        /// with Atom and Bond objects from the original AtomContainer. The aromaticity has to be known
        /// before calling this method.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.InChI.ConjugatedPiSystemsDetector_Example.cs+Detect"]/*' />
        /// </example>
        /// <param name="ac">The AtomContainer for which to detect conjugated pi systems</param>
        /// <returns>The set of AtomContainers with conjugated pi systems</returns>
        public static IChemObjectSet<IAtomContainer> Detect(IAtomContainer ac)
        {
            var piSystemSet = ac.Builder.NewChemObjectSet<IAtomContainer>();

            foreach (var atom in ac.Atoms)
                atom.IsVisited = false;

            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                var firstAtom = ac.Atoms[i];
                // if this atom was already visited in a previous DFS, continue
                if (firstAtom.IsVisited || CheckAtom(ac, firstAtom) == -1)
                {
                    continue;
                }
                var piSystem = ac.Builder.NewAtomContainer();
                var stack = new Stack<IAtom>();

                piSystem.Atoms.Add(firstAtom);
                stack.Push(firstAtom);
                firstAtom.IsVisited = true;
                // Start DFS from firstAtom
                while (stack.Count != 0)
                {
                    //bool AddAtom = false;
                    var currentAtom = stack.Pop();
                    var atoms = ac.GetConnectedAtoms(currentAtom).ToReadOnlyList();
                    var bonds = ac.GetConnectedBonds(currentAtom).ToReadOnlyList();

                    for (int j = 0; j < atoms.Count; j++)
                    {
                        var atom = atoms[j];
                        var bond = bonds[j];
                        if (!atom.IsVisited)
                        {
                            var check = CheckAtom(ac, atom);
                            if (check == 1)
                            {
                                piSystem.Atoms.Add(atom);
                                piSystem.Bonds.Add(bond);
                                continue;
                                // do not mark atom as visited if cumulative double bond
                            }
                            else if (check == 0)
                            {
                                piSystem.Atoms.Add(atom);
                                piSystem.Bonds.Add(bond);
                                stack.Push(atom);
                            }
                            atom.IsVisited = true;
                        }
                        // close rings with one bond
                        else if (!piSystem.Contains(bond) && piSystem.Contains(atom))
                        {
                            piSystem.Bonds.Add(bond);
                        }
                    }
                }

                if (piSystem.Atoms.Count > 2)
                {
                    piSystemSet.Add(piSystem);
                }
            }

            return piSystemSet;
        }

        /// <summary>
        ///  Check an Atom whether it may be conjugated or not.
        /// </summary>
        /// <param name="ac">The AtomContainer containing currentAtom</param>
        /// <param name="currentAtom">The Atom to check</param>
        /// <returns>-1 if isolated, 0 if conjugated, 1 if cumulative db</returns>
        private static int CheckAtom(IAtomContainer ac, IAtom currentAtom)
        {
            int check = -1;
            var atoms = ac.GetConnectedAtoms(currentAtom).ToReadOnlyList();
            var bonds = ac.GetConnectedBonds(currentAtom).ToReadOnlyList();
            if (currentAtom.IsAromatic)
            {
                check = 0;
            }
            else if (currentAtom.FormalCharge == 1) 
                /* && currentAtom.Symbol.Equals("C") */ 
            {
                check = 0;
            }
            else if (currentAtom.FormalCharge == -1)
            {
                //// NEGATIVE CHARGES WITH A NEIGHBOOR PI BOND ////
                int counterOfPi = 0;
                foreach (var atom in atoms)
                {
                    if (ac.GetMaximumBondOrder(atom) != BondOrder.Single)
                    {
                        counterOfPi++;
                    }
                }
                if (counterOfPi > 0) check = 0;
            }
            else
            {
                int se = ac.GetConnectedSingleElectrons(currentAtom).Count();
                if (se == 1)
                {
                    check = 0; //// DETECTION of radicals
                }
                else if (ac.GetConnectedLonePairs(currentAtom).Any()
                    /* && (currentAtom.Symbol.Equals("N") */)
                {
                    check = 0; //// DETECTION of lone pair
                }
                else
                {
                    int highOrderBondCount = 0;
                    for (int j = 0; j < atoms.Count; j++)
                    {
                        var bond = bonds[j];
                        if (bond == null || bond.Order != BondOrder.Single)
                        {
                            highOrderBondCount++;
                        }
                        else
                        {
                        }
                    }
                    if (highOrderBondCount == 1)
                    {
                        check = 0;
                    }
                    else if (highOrderBondCount > 1)
                    {
                        check = 1;
                    }
                }
            }
            return check;
        }
    }
}
