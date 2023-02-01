/*
 *  Copyright (C) 2010  Rajarshi Guha <rajarshi.guha@gmail.com>
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

using NCDK.Fragments;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// An implementation of the FMF descriptor characterizing complexity of a molecule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The descriptor is described in <token>cdk-cite-YANG2010</token> and is an approach to
    /// characterizing molecular complexity based on the Murcko framework present
    /// in the molecule. The descriptor is the ratio of heavy atoms in the framework to the
    /// total number of heavy atoms in the molecule. By definition, acyclic molecules
    /// which have no frameworks, will have a value of 0.
    /// </para>
    /// <para>
    /// Note that the authors consider an isolated ring system to be a framework (even
    /// though there is no linker).
    /// </para>
    /// <para>
    /// This descriptor returns a single double value, labeled as "FMF"
    /// </para>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:FMF
    // @see org.openscience.cdk.fragment.MurckoFragmenter
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#fmf")]
    public class FMFDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public FMFDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.FMF = value;
            }

            [DescriptorResultProperty("FMF")]
            public double FMF { get; private set; }

            public double Value => FMF;
        }

        /// <summary>
        /// Calculates the FMF descriptor value for the given <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>An object of <see cref="Result"/> that contains the
        /// calculated FMF descriptor value as well as specification details</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            var fragmenter = new MurckoFragmenter(true, 3);
            fragmenter.GenerateFragments(container);
            var framework = fragmenter.GetFrameworksAsContainers().ToReadOnlyList();
            var ringSystems = fragmenter.GetRingSystemsAsContainers().ToReadOnlyList();
            {
                double result;

                if (framework.Count == 1)
                    result = framework[0].Atoms.Count / (double)container.Atoms.Count;
                else if (framework.Count == 0 && ringSystems.Count == 1)
                    result = ringSystems[0].Atoms.Count / (double)container.Atoms.Count;
                else
                    result = 0;

                return new Result(result);
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
