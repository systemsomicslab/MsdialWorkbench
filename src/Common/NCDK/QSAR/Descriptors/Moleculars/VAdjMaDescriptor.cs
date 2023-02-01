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
    /// Vertex adjacency information (magnitude):
    /// 1 + log2 m where m is the number of heavy-heavy bonds. If m is zero, then zero is returned.
    /// (definition from MOE tutorial on line)
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:vAdjMa
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#vAdjMa")]
    public class VAdjMaDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public VAdjMaDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.VertexAdjacency = value;
            }

            [DescriptorResultProperty("VAdjMat")]
            public double VertexAdjacency { get; private set; }

            public double Value => VertexAdjacency;
        }

        public Result Calculate(IAtomContainer container)
        {
            var n = container.Bonds
                .Count(bond => bond.Atoms[0].AtomicNumber != 1 
                            && bond.Atoms[1].AtomicNumber != 1);

            var vadjMa = n > 0 ? (Math.Log(n) / Math.Log(2)) + 1 : 0;

            return new Result(vadjMa);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
