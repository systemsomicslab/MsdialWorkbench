/* Copyright (C) 2018  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Returns the number of spiro atoms.
    /// </summary>
    // @author rguha
    // @cdk.dictref qsar-descriptors:nSpiroAtom
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#nSpiroAtom")]
    public class SpiroAtomCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public SpiroAtomCountDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfSpiroAtoms = value;
            }

            [DescriptorResultProperty("nSpiroAtoms")]
            public int NumberOfSpiroAtoms { get; private set; }

            public int Value => NumberOfSpiroAtoms;
        }

        private static void TraverseRings(IAtomContainer mol, IAtom atom, IBond prev)
        {
            atom.IsVisited = true;
            prev.IsVisited = true;
            foreach (var bond in mol.GetConnectedBonds(atom))
            {
                var nbr = bond.GetOther(atom);
                if (!nbr.IsVisited)
                    TraverseRings(mol, nbr, bond);
                else
                    bond.IsVisited = true;
            }
        }

        private static int GetSpiroDegree(IAtomContainer mol, IAtom atom)
        {
            if (!atom.IsInRing)
                return 0;
            var rbonds = new List<IBond>(4);
            foreach (var bond in mol.GetConnectedBonds(atom))
            {
                if (bond.IsInRing)
                    rbonds.Add(bond);
            }
            if (rbonds.Count < 4)
                return 0;
            int degree = 0;
            // clear flags
            foreach (var b in mol.Bonds)
                b.IsVisited = false;
            foreach (var a in mol.Atoms)
                a.IsVisited = false;
            // visit rings
            atom.IsVisited = true;
            foreach (var rbond in rbonds)
            {
                if (!rbond.IsVisited)
                {
                    TraverseRings(mol, rbond.GetOther(atom), rbond);
                    degree++;
                }
            }
            return degree < 2 ? 0 : degree;
        }

        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            int nSpiro = 0;

            Cycles.MarkRingAtomsAndBonds(container);
            foreach (var atom in container.Atoms)
            {
                if (GetSpiroDegree(container, atom) != 0)
                    nSpiro++;
            }

            return new Result(nSpiro);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
