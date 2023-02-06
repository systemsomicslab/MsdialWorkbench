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

using NCDK.Graphs;
using NCDK.Tools.Manipulator;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates Petitjean number.
    /// </summary>
    /// <remarks>
    /// According to the Petitjean definition, the eccentricity of a vertex corresponds to
    /// the distance from that vertex to the most remote vertex in the graph.
    /// The distance is obtained from the distance matrix as the count of edges between the two vertices.
    /// If r(i) is the largest matrix entry in row i of the distance matrix D, then the radius is defined as the smallest of the r(i).
    /// The graph diameter D is defined as the largest vertex eccentricity in the graph.
    /// (http://www.edusoft-lc.com/molconn/manuals/400/chaptwo.html)
    /// </remarks>
    // @author         mfe4
    // @cdk.created    December 7, 2004
    // @cdk.created    2004-11-03
    // @cdk.module     qsarmolecular
    // @cdk.dictref    qsar-descriptors:petitjeanNumber
    // @cdk.keyword    Petit-Jean, number
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#petitjeanNumber")]
    public class PetitjeanNumberDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public PetitjeanNumberDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.PetitjeanNumber = value;
            }

            [DescriptorResultProperty("PetitjeanNumber")]
            public double PetitjeanNumber { get; private set; }

            public double Value => PetitjeanNumber;
        }

        /// <summary>
        /// Evaluate the descriptor for the molecule.
        /// </summary>
        /// <returns>petitjean number</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = AtomContainerManipulator.RemoveHydrogens(container);

            var diameter = PathTools.GetMolecularGraphDiameter(container);
            var radius = PathTools.GetMolecularGraphRadius(container);

            var petitjeanNumber = diameter == 0 ? 0 : (diameter - radius) / (double)diameter;

            return new Result(petitjeanNumber);
        }
        
        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
