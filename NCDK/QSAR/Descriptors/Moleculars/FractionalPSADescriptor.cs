/* Copyright (c) 2014 Collaborative Drug Discovery, Inc. <alex@collaborativedrug.com>
 *
 * Implemented by Alex M. Clark, produced by Collaborative Drug Discovery, Inc.
 * Made available to the CDK community under the terms of the GNU LGPL.
 *
 *    http://collaborativedrug.com
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using NCDK.Tools.Manipulator;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Polar surface area expressed as a ratio to molecular size. 
    /// </summary>
    /// <remarks>
    /// Calculates <b>tpsaEfficiency</b>, which is
    /// to <see cref="TPSADescriptor"/> / <b>molecular weight</b>, in units of square Angstroms per Dalton.
    /// Other related descriptors may also be useful to add, e.g. ratio of polar to hydrophobic surface area.
    /// </remarks>
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:fractionalPSA
    // @cdk.keyword volume
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#fractionalPSA")]
    public class FractionalPSADescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public FractionalPSADescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.TPSAEfficiency = value;
            }

            [DescriptorResultProperty("tpsaEfficiency")]
            public double TPSAEfficiency { get; private set; }

            public double Value => TPSAEfficiency;
        }

        /// <summary>
        /// Calculates the topological polar surface area and expresses it as a ratio to molecule size.
        /// </summary>
        /// <returns>Descriptor(s) retaining to polar surface area</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            // type & assign implicit hydrogens
            var matcher = CDK.AtomTypeMatcher;
            foreach (var atom in container.Atoms)
            {
                var type = matcher.FindMatchingAtomType(container, atom);
                AtomTypeManipulator.Configure(atom, type);
            }
            var adder = CDK.HydrogenAdder;
            adder.AddImplicitHydrogens(container);

            double polar = 0;
            double weight = 0;

            // polar surface area: chain it off the TPSADescriptor
            var tpsa = new TPSADescriptor();
            var value = tpsa.Calculate(container);
            polar = value.Value;

            //  molecular weight
            foreach (var atom in container.Atoms)
            {
                weight += CDK.IsotopeFactory.GetMajorIsotope(atom.Symbol).ExactMass.Value;
                weight += (atom.ImplicitHydrogenCount ?? 0) * 1.00782504;
            }

            return new Result(weight == 0 ? 0 : polar / weight);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
