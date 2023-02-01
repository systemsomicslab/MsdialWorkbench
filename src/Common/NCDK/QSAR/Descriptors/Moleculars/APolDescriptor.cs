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

using NCDK.Config;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Sum of the atomic polarizabilities (including implicit hydrogens).
    /// </summary>
    /// <remarks>
    /// Polarizabilities are taken from
    /// <see href="http://www.sunysccc.edu/academic/mst/ptable/p-table2.htm" />.
    /// This class need explicit hydrogens.
    /// Returns a single value with name <i>apol</i>.
    /// </remarks>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:apol
    // @cdk.keyword polarizability, atomic
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#apol")]
    public class APolDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        /* Atomic polarizabilities ordered by atomic number from 1 to 102. */
        internal static readonly double[] polarizabilities = new double[] 
            {
                0, 0.666793, 0.204956, 24.3, 5.6, 3.03, 1.76, 1.1, 0.802, 0.557, 0.3956,
                23.6, 10.6, 6.8, 5.38, 3.63, 2.9, 2.18, 1.6411, 43.4, 22.8, 17.8, 14.6, 12.4, 11.6, 9.4, 8.4, 7.5,
                6.8, 6.1, 7.1, 8.12, 6.07, 4.31, 3.77, 3.05, 2.4844, 47.3, 27.6, 22.7, 17.9, 15.7, 12.8, 11.4, 9.6,
                8.6, 4.8, 7.2, 7.2, 10.2, 7.7, 6.6, 5.5, 5.35, 4.044, 59.6, 39.7, 31.1, 29.6, 28.2, 31.4, 30.1,
                28.8, 27.7, 23.5, 25.5, 24.5, 23.6, 22.7, 21.8, 21, 21.9, 16.2, 13.1, 11.1, 9.7, 8.5, 7.6, 6.5,
                5.8, 5.7, 7.6, 6.8, 7.4, 6.8, 6, 5.3, 48.7, 38.3, 32.1, 32.1, 25.4, 27.4, 24.8, 24.5, 23.3, 23,
                22.7, 20.5, 19.7, 23.8, 18.2, 17.5
            };

        public APolDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.AtomicPolarizabilities = value;
            }

            [DescriptorResultProperty("apol")]
            public double AtomicPolarizabilities { get; private set; }

            public double Value => AtomicPolarizabilities;
        }

        /// <summary>
        /// Calculate the sum of atomic polarizabilities in an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>The sum of atomic polarizabilities <see cref="IsotopeFactory"/></returns>
        public Result Calculate(IAtomContainer container)
        {
            var polarizabilitiesH = polarizabilities[AtomicNumbers.H];
            var apol = container.Atoms
                .Select(
                    atom => polarizabilities[atom.AtomicNumber]
                          + polarizabilitiesH * (atom.ImplicitHydrogenCount ?? 0))
                .Sum();
            return new Result(apol);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
