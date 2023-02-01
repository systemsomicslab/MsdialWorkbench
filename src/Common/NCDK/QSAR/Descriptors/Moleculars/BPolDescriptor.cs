/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Sum of the absolute value of the difference between atomic polarizabilities
    /// of all bonded atoms in the molecule (including implicit hydrogens) with polarizabilities taken from
    /// <see ref="http://www.sunysccc.edu/academic/mst/ptable/p-table2.htm"/>.
    /// </summary>
    /// <remarks>
    /// This descriptor assumes 2-centered bonds.
    /// Returns a single value with name <i>bpol</i>.
    /// </remarks>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:bpol
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#bpol")]
    public class BPolDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        internal static readonly double[] polarizabilities = APolDescriptor.polarizabilities;        

        public BPolDescriptor()
        {            
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.BondPolarizabilities = value;
            }

            [DescriptorResultProperty("bpol")]
            public double BondPolarizabilities { get; private set; }

            public double Value => BondPolarizabilities;
        }

        /// <summary>
        /// This method calculate the sum of the absolute value of
        /// the difference between atomic polarizabilities of all bonded atoms in the molecule
        /// </summary>
        /// <returns>The sum of atomic polarizabilities</returns>
        public Result Calculate(IAtomContainer container)
        {
            double bpol = 0;

            bpol += container.Bonds
                .Select(bond => Math.Abs(polarizabilities[bond.Atoms[0].AtomicNumber]
                                       - polarizabilities[bond.Atoms[1].AtomicNumber]))
                .Sum();

            // after going through the bonds, we go through the atoms and see if they have
            // implicit H's and if so, consider the associated "implicit" bonds. Note that
            // if the count is UNSET, we assume it is 0
            var polarizabilitiesH = polarizabilities[1];
            bpol += container.Atoms
                .Select(atom => Math.Abs((polarizabilities[atom.AtomicNumber] - polarizabilitiesH)
                                       * (atom.ImplicitHydrogenCount ?? 0)))
                .Sum();

            return new Result(bpol);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
