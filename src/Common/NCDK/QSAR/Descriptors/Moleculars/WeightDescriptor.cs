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
    /// Evaluates the weight of atoms of a certain element type.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:weight
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#weight")]
    public class WeightDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public WeightDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Weight = value;
            }

            public Result(Exception e) : base(e) { }

            [DescriptorResultProperty]
            public double Weight { get; private set; }

            public double Value => Weight;
        }

        /// <summary>
        /// Calculate the natural weight of specified elements type in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>The total natural weight of atoms of the specified element type</returns>
        /// <param name="container">
        /// The AtomContainer for which this descriptor is to be calculated. If 'H'
        /// is specified as the element symbol make sure that the AtomContainer has hydrogens.
        /// </param>
        /// <param name="symbol">If *, returns the molecular weight, otherwise the weight for the given element</param>
        public Result Calculate(IAtomContainer container, string symbol = "*")
        {
            var hydrogenNaturalMass = CDK.IsotopeFactory.GetNaturalMass(AtomicNumbers.H);

            double weight = 0;
            switch (symbol)
            {
                case "*":
                    foreach (var atom in container.Atoms)
                    {
                        weight += CDK.IsotopeFactory.GetNaturalMass(atom.AtomicNumber);
                        weight += (atom.ImplicitHydrogenCount ?? 0) * hydrogenNaturalMass;
                    }
                    break;
                case "H":
                    foreach (var atom in container.Atoms)
                    {
                        if (atom.Symbol.Equals(symbol, StringComparison.Ordinal))
                        {
                            weight += hydrogenNaturalMass;
                        }
                        else
                        {
                            weight += (atom.ImplicitHydrogenCount ?? 0) * hydrogenNaturalMass;
                        }
                    }
                    break;
                default:
                    foreach (var atom in container.Atoms)
                    {
                        if (atom.Symbol.Equals(symbol, StringComparison.Ordinal))
                        {
                            weight += CDK.IsotopeFactory.GetNaturalMass(atom.AtomicNumber);
                        }
                    }
                    break;
            }

            return new Result(weight);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
