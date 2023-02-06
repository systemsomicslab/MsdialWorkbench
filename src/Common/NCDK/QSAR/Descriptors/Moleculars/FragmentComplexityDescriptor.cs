/*  Copyright (C) 2004-2007  Christian Hoppe
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

using NCDK.Tools.Manipulator;
using System;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Class that returns the complexity of a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The complexity is defined as <token>cdk-cite-Nilakantan06</token>:
    /// <pre>
    /// C=Abs(B^2-A^2+A)+H/100
    /// </pre>
    /// where C=complexity, A=number of non-hydrogen atoms, B=number of bonds and H=number of heteroatoms
    /// </para>
    /// <para>This descriptor uses no parameters.</para>
    /// </remarks>
    // @author      chhoppe from EUROSCREEN
    // @cdk.created 2006-8-22
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:NilaComplexity
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#NilaComplexity")]
    public class FragmentComplexityDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public FragmentComplexityDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Complexity = value;
            }

            [DescriptorResultProperty("fragC")]
            public double Complexity { get; private set; }

            public double Value => Complexity;
        }

        /// <summary>
        /// Calculate the complexity in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>the complexity</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            int a = 0;
            double h = 0;
            foreach (var atom in container.Atoms)
            {
                switch (atom.AtomicNumber)
                {
                    default:
                        h++;
                        goto case AtomicNumbers.C;
                    case AtomicNumbers.C:
                        a++;
                        goto case AtomicNumbers.H;
                    case AtomicNumbers.H:
                        break;
                }
            }
            var b = container.Bonds.Count + AtomContainerManipulator.GetImplicitHydrogenCount(container);
            var c = Math.Abs(b * b - a * a + a) + (h / 100);

            return new Result(c);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
