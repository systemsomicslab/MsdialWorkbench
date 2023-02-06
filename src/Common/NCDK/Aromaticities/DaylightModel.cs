/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */
using NCDK.Common.Collections;
using NCDK.RingSearches;
using System;
using System.Collections.Generic;
using static NCDK.Common.Base.Preconditions;

namespace NCDK.Aromaticities
{
    /// <summary>
    /// Electron donation model closely mirroring the Daylight model for use in
    /// generating SMILES. The model was interpreted from various resources and as
    /// such may not match exactly. If you find an inconsistency please add a request
    /// for enhancement to the patch tracker. One known limitation is that this model
    /// does not currently consider unknown/pseudo atoms '*'. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The model makes a couple of assumptions which it will not correct for.
    /// Checked assumptions cause the model to throw a runtime exception.
    /// <list type="bullet">
    /// <item>there should be no valence errors (unchecked)</item>
    /// <item>every atom has a set implicit hydrogen count (checked)</item> 
    /// <item>every bond has defined order, single, double etc (checked)</item> 
    /// <item>atomic number of non-pseudo atoms is set (checked)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The aromaticity model in SMILES was designed to simplify canonicalisation and
    /// express symmetry in a molecule. The contributed electrons can be summarised
    /// as follows (refer to code for exact specification): 
    /// <list type="bullet">
    /// <item>carbon, nitrogen, oxygen, phosphorus, sulphur, arsenic and selenium are allow to be aromatic</item> 
    /// <item>atoms should be Sp2 hybridised - not actually computed</item>
    /// <item>atoms adjacent to a single cyclic pi bond contribute 1 electron</item>
    /// <item>neutral or negatively charged atoms with a lone pair contribute 2 </item> 
    /// <item>exocyclic pi bonds are allowed but if the exocyclic atom is more electronegative it consumes an electron. As an example ketone groups contribute '0' electrons.</item>
    /// </list>
    /// </para>
    /// </remarks>
    // @author John May
    // @cdk.module standard
    sealed class DaylightModel : ElectronDonation
    {
        private const int Carbon = 6;
        private const int Nitrogen = 7;
        private const int Oxygen = 8;
        private const int PHOSPHORUS = 15;
        private const int SULPHUR = 16;
        private const int ARSENIC = 33;
        private const int SELENIUM = 34;

        /// <inheritdoc/>
        public override IReadOnlyList<int> Contribution(IAtomContainer container, RingSearch ringSearch)
        {
            int n = container.Atoms.Count;

            // we compute values we need for all atoms and then make the decisions
            // - this avoids costly operations such as looking up connected
            // bonds on each atom at the cost of memory
            var degree = new int[n];
            var bondOrderSum = new int[n];
            var nCyclicPiBonds = new int[n];
            var exocyclicPiBond = new int[n];
            var electrons = new int[n];

            Arrays.Fill(exocyclicPiBond, -1);

            // index atoms and set the degree to the number of implicit hydrogens
            var atomIndex = new Dictionary<IAtom, int>(n);
            for (int i = 0; i < n; i++)
            {
                var a = container.Atoms[i];
                atomIndex.Add(a, i);
                degree[i] = CheckNotNull(a.ImplicitHydrogenCount,
                        "Aromaticity model requires implicit hydrogen count is set.");
            }

            // for each bond we increase the degree count and check for cyclic and
            // exocyclic pi bonds. if there is a cyclic pi bond the atom is marked.
            // if there is an exocyclic pi bond we store the adjacent atom for
            // lookup later.
            foreach (var bond in container.Bonds)
            {
                var u = atomIndex[bond.Begin];
                var v = atomIndex[bond.End];
                degree[u]++;
                degree[v]++;

                var order = CheckNotNull(bond.Order, "Aromaticity model requires that bond orders must be set");

                if (order == BondOrder.Unset)
                {
                    Console.WriteLine("Aromaticity model requires that bond orders must be set. Here, replaced by Single");
                    order = BondOrder.Single;
                    //throw new ArgumentException("Aromaticity model requires that bond orders must be set");
                }
                else if (order == BondOrder.Double)
                {
                    if (ringSearch.Cyclic(u, v))
                    {
                        nCyclicPiBonds[u]++;
                        nCyclicPiBonds[v]++;
                    }
                    else
                    {
                        exocyclicPiBond[u] = v;
                        exocyclicPiBond[v] = u;
                    }
                    // note - fall through
                }

                if (order == BondOrder.Single
                 || order == BondOrder.Double
                 || order == BondOrder.Triple
                 || order == BondOrder.Quadruple)
                {
                    bondOrderSum[u] += order.Numeric();
                    bondOrderSum[v] += order.Numeric();
                }
            }

            // now make a decision on how many electrons each atom contributes
            for (int i = 0; i < n; i++)
            {
                var element = Element(container.Atoms[i]);
                var charge = Charge(container.Atoms[i]);

                // abnormal valence, usually indicated a radical. these cause problems
                // with kekulisations
                var bondedValence = bondOrderSum[i] + container.Atoms[i].ImplicitHydrogenCount.Value;
                if (!Normal(element, charge, bondedValence))
                {
                    electrons[i] = -1;
                }

                // non-aromatic element, acyclic atoms, atoms with more than three
                // neighbors and atoms with more than 1 cyclic pi bond are not
                // considered
                else if (!AromaticElement(element) || !ringSearch.Cyclic(i) || degree[i] > 3 || nCyclicPiBonds[i] > 1)
                {
                    electrons[i] = -1;
                }

                // exocyclic bond contributes 0 or 1 electrons depending on
                // preset electronegativity - check the exocyclicContribution method
                else if (exocyclicPiBond[i] >= 0)
                {
                    electrons[i] = ExocyclicContribution(element, Element(container.Atoms[exocyclicPiBond[i]]), charge,
                            nCyclicPiBonds[i]);
                }

                // any atom (except arsenic) with one cyclic pi bond contributes a
                // single electron
                else if (nCyclicPiBonds[i] == 1)
                {
                    electrons[i] = element == ARSENIC ? -1 : 1;
                }

                // a anion with a lone pair contributes 2 electrons - simplification
                // here is we count the number free valence electrons but also
                // check if the bonded valence is okay (i.e. not a radical)
                else if (charge <= 0 && charge > -3)
                {
                    if (Valence(element, charge) - bondOrderSum[i] >= 2)
                        electrons[i] = 2;
                    else
                        electrons[i] = -1;
                }

                else
                {
                    // cation with no double bonds - single exception?
                    if (element == Carbon && charge > 0)
                        electrons[i] = 0;
                    else
                        electrons[i] = -1;
                }
            }

            return electrons;
        }

        /// <summary>
        /// Defines the number of electrons contributed when a pi bond is exocyclic
        /// (spouting). 
        /// </summary>
        /// <remarks>
        /// When an atom is connected to an more electronegative atom
        /// then the electrons are 'pulled' from the ring. The preset conditions are
        /// as follows:
        /// <list type="bullet">
        /// <item>A cyclic carbon with an exocyclic pi bond to anything but carbon
        /// contributes 0 electrons. If the exocyclic atom is also a carbon then 1
        /// electron is contributed.</item>
        /// <item>A cyclic 4 valent nitrogen or
        /// phosphorus cation with an exocyclic pi bond will always contribute 1
        /// electron. A 5 valent neutral nitrogen or phosphorus with an exocyclic
        /// bond to an oxygen contributes 1 electron. </item>
        /// <item>A neutral sulphur connected to an oxygen contributes 2 electrons</item>
        /// <item>If none of the previous conditions are met the atom is not considered as being able to
        /// participate in an aromatic system and -1 is returned.</item>
        /// </list>
        /// </remarks>
        /// <param name="element">the element of the cyclic atom</param>
        /// <param name="otherElement">the element of the exocyclic atom which is connected to the cyclic atom by a pi bond</param>
        /// <param name="charge">the charge of the cyclic atom</param>
        /// <param name="nCyclic">the number of cyclic pi bonds adjacent to cyclic atom</param>
        /// <returns>number of contributed electrons</returns>
        private static int ExocyclicContribution(int element, int otherElement, int charge, int nCyclic)
        {
            switch (element)
            {
                case Carbon:
                    return otherElement != Carbon ? 0 : 1;
                case Nitrogen:
                case PHOSPHORUS:
                    if (charge == 1)
                        return 1;
                    else if (charge == 0 && otherElement == Oxygen && nCyclic == 1) return 1;
                    return -1;
                case SULPHUR:
                    // quirky but try - O=C1C=CS(=O)C=C1
                    return charge == 0 && otherElement == Oxygen ? 2 : -1;
            }
            return -1;
        }

        /// <summary>
        /// Is the element specified by the atomic number, allowed to be aromatic by
        /// the daylight specification. Allowed elements are C, N, O, P, S, As, Se
        /// and *. This model allows all except for the unknown ('*') element.
        /// </summary>
        /// <param name="element">atomic number of element</param>
        /// <returns>the element can be aromatic</returns>
        private static bool AromaticElement(int element)
        {
            switch (element)
            {
                case Carbon:
                case Nitrogen:
                case Oxygen:
                case PHOSPHORUS:
                case SULPHUR:
                case ARSENIC:
                case SELENIUM:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The element has normal valence for the specified charge.
        /// </summary>
        /// <param name="element">atomic number</param>
        /// <param name="charge">formal charge</param>
        /// <param name="valence">bonded electrons</param>
        /// <returns>acceptable for this model</returns>
        private static bool Normal(int element, int charge, int valence)
        {
            switch (element)
            {
                case Carbon:
                    if (charge == -1 || charge == +1) return valence == 3;
                    return charge == 0 && valence == 4;
                case Nitrogen:
                case PHOSPHORUS:
                case ARSENIC:
                    if (charge == -1) return valence == 2;
                    if (charge == +1) return valence == 4;
                    return charge == 0 && (valence == 3 || (valence == 5 && element == Nitrogen));
                case Oxygen:
                    if (charge == +1) return valence == 3;
                    return charge == 0 && valence == 2;
                case SULPHUR:
                case SELENIUM:
                    if (charge == +1) return valence == 3;
                    return charge == 0 && (valence == 2 || valence == 4 || valence == 6);
            }
            return false;
        }

        /// <summary>
        /// Lookup of the number of valence electrons for the element at a given charge.
        /// </summary>
        /// <param name="element">the atomic number of an element</param>
        /// <param name="charge">the formal charge on the atom</param>
        /// <returns>the valence</returns>
        /// <exception cref="NotSupportedException">encountered an element which the valence was not encoded for</exception>
        private static int Valence(int element, int charge)
        {
            return Valence(element - charge);
        }

        /// <summary>
        /// Lookup of the number of valence electrons for elements near those which
        /// this model considers aromatic. As only the <see cref="AromaticElement(int)"/> 
        /// are checked we need only consider elements within a charge range.
        /// </summary>
        /// <param name="element">the atomic number of an element</param>
        /// <returns>the valence</returns>
        /// <exception cref="NotSupportedException">encountered an element which the valence was not encoded for</exception>
        private static int Valence(int element)
        {
            switch (element)
            {
                case 5: // boron
                case 13: // aluminium
                case 31: // gallium
                    return 3;
                case Carbon:
                case 14: // silicon
                case 32: // germanium
                    return 4;
                case Nitrogen:
                case PHOSPHORUS:
                case ARSENIC:
                    return 5;
                case Oxygen:
                case SULPHUR:
                case SELENIUM:
                    return 6;
                case 9: // fluorine
                case 17: // chlorine
                case 35: // bromine
                    return 7;
            }
            throw new NotSupportedException("Valence not yet handled for element with atomic number " + element);
        }

        /// <summary>
        /// Get the atomic number as an non-null integer value. Although pseudo atoms
        /// are not considered by this model the pseudo atoms are intercepted to have
        /// non-null atomic number (defaults to 0).
        /// </summary>
        /// <param name="atom">atom to get the element from</param>
        /// <returns>the formal charge</returns>
        private static int Element(IAtom atom)
        {
            int? element = atom.AtomicNumber;
            if (element.HasValue) return element.Value;
            if (atom is IPseudoAtom) return 0;
            throw new ArgumentException("Aromaiticty model requires atomic numbers to be set");
        }

        /// <summary>
        /// Get the formal charge as an integer value - null defaults to 0.
        /// </summary>
        /// <param name="atom">the atom to get the charge of</param>
        /// <returns>the formal charge</returns>
        private static int Charge(IAtom atom)
        {
            return atom.FormalCharge ?? 0;
        }
    }
}