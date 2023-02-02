/*
 *  Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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

using NCDK.Graphs;
using NCDK.Graphs.Matrix;
using NCDK.Tools.Manipulator;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// A topological descriptor combining distance and adjacency information.
    /// </summary>
    /// <remarks>
    /// This descriptor is described by Sharma et al. <token>cdk-cite-SHA97</token> and has been shown
    /// to correlate well with a number of physical properties. The descriptor is also reported to
    /// have good discriminatory ability.
    /// <para>
    /// The eccentric connectivity index for a hydrogen supressed molecular graph is given by the
    /// expression
    /// <pre>
    /// \xi^{c} = \sum_{i = 1}{n} E(i) V(i)
    /// </pre>
    /// where E(i) is the eccentricity of the i<sup>th</sup> atom (path length from the
    /// i<sup>th</sup> atom to the atom farthest from it) and V(i) is the vertex degree of the
    /// i<sup>th</sup> atom.
    /// </para>
    /// <para>This descriptor uses these parameters:
    /// <list type="table">
    ///   <item>
    ///     <term>Name</term>
    ///     <term>Default</term>
    ///     <term>Description</term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term></term>
    ///     <term>no parameters</term>
    ///   </item>
    /// </list> 
    /// </para>
    /// </remarks>
    /// Returns a single value with name <i>ECCEN</i>
    // @author      Rajarshi Guha
    // @cdk.created     2005-03-19
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:eccentricConnectivityIndex
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#eccentricConnectivityIndex")]
    public class EccentricConnectivityIndexDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public EccentricConnectivityIndexDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.ECCEN = value;
            }

            [DescriptorResultProperty("ECCEN")]
            public int ECCEN { get; private set; }

            public int Value => ECCEN;
        }

        /// <summary>
        /// Calculates the eccentric connectivity
        /// </summary>
        /// <returns>A <see cref="Result"/> value representing the eccentric connectivity index</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = AtomContainerManipulator.RemoveHydrogens(container);

            var natom = container.Atoms.Count;
            var admat = AdjacencyMatrix.GetMatrix(container);
            var distmat = PathTools.ComputeFloydAPSP(admat);

            int eccenindex = 0;
            for (int i = 0; i < natom; i++)
            {
                int max = -1;
                for (int j = 0; j < natom; j++)
                {
                    if (distmat[i][j] > max)
                        max = distmat[i][j];
                }
                var degree = container.GetConnectedBonds(container.Atoms[i]).Count();
                eccenindex += max * degree;
            }
            return new Result(eccenindex);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
