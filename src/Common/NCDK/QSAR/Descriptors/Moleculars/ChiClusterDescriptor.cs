/*  Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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

using NCDK.Isomorphisms.Matchers;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates chi cluster descriptors.
    /// </summary>
    /// <remarks>
    /// The code currently evluates the simple and valence chi chain descriptors of orders 3, 4,5 and 6.
    /// It utilizes the graph isomorphism code of the CDK to find fragments matching
    /// SMILES strings representing the fragments corresponding to each type of chain.
    /// <para>
    /// The order of the values returned is
    /// <list type="bullet">
    /// <item>SC-3 - Simple cluster, order 3</item>
    /// <item>SC-4 - Simple cluster, order 4</item>
    /// <item>SC-5 - Simple cluster, order 5</item>
    /// <item>SC-6 - Simple cluster, order 6</item>
    /// <item>VC-3 - Valence cluster, order 3</item>
    /// <item>VC-4 - Valence cluster, order 4</item>
    /// <item>VC-5 - Valence cluster, order 5</item>
    /// <item>VC-6 - Valence cluster, order 6</item>
    /// </list>
    /// </para>
    /// <note type="note">These descriptors are calculated using graph isomorphism to identify
    /// the various fragments. As a result calculations may be slow. In addition, recent
    /// versions of Molconn-Z use simplified fragment definitions (i.e., rings without
    /// branches etc.) whereas these descriptors use the older more complex fragment
    /// definitions.
    /// </note>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2006-11-13
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:chiCluster
    // @cdk.keyword chi cluster index
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#chiCluster")]
    public class ChiClusterDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public ChiClusterDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double sc3, double sc4, double sc5, double sc6, double vc3, double vc4, double vc5, double vc6)
            {
                this.Values = new[] { sc3, sc4, sc5, sc6, vc3, vc4, vc5, vc6 };
            }

            public Result(Exception e) : base(e) { }

            [DescriptorResultProperty("SC-3")]
            public double SC3 => Values[0];
            [DescriptorResultProperty("SC-4")]
            public double SC4 => Values[1];
            [DescriptorResultProperty("SC-5")]
            public double SC5 => Values[2];
            [DescriptorResultProperty("SC-6")]
            public double SC6 => Values[3];
            [DescriptorResultProperty("SC-3")]
            public double VC3 => Values[4];
            [DescriptorResultProperty("VC-4")]
            public double VC4 => Values[5];
            [DescriptorResultProperty("VC-5")]
            public double VC5 => Values[6];
            [DescriptorResultProperty("VC-6")]
            public double VC6 => Values[7];

            public new IReadOnlyList<double> Values { get; private set; }
        }

        public Result Calculate(IAtomContainer container)
        {
            // we don't make a clone, since removeHydrogens returns a deep copy
            container = AtomContainerManipulator.RemoveHydrogens(container);

            var matcher = CDK.AtomTypeMatcher;
            foreach (var atom in container.Atoms)
            {
                var type = matcher.FindMatchingAtomType(container, atom);
                AtomTypeManipulator.Configure(atom, type);
            }
            var hAdder = CDK.HydrogenAdder;
            hAdder.AddImplicitHydrogens(container);

            var subgraph3 = Order3(container);
            var subgraph4 = Order4(container);
            var subgraph5 = Order5(container);
            var subgraph6 = Order6(container);

            try
            {
                var order3s = ChiIndexUtils.EvalSimpleIndex(container, subgraph3);
                var order4s = ChiIndexUtils.EvalSimpleIndex(container, subgraph4);
                var order5s = ChiIndexUtils.EvalSimpleIndex(container, subgraph5);
                var order6s = ChiIndexUtils.EvalSimpleIndex(container, subgraph6);

                var order3v = ChiIndexUtils.EvalValenceIndex(container, subgraph3);
                var order4v = ChiIndexUtils.EvalValenceIndex(container, subgraph4);
                var order5v = ChiIndexUtils.EvalValenceIndex(container, subgraph5);
                var order6v = ChiIndexUtils.EvalValenceIndex(container, subgraph6);
                return new Result(
                    order3s,
                    order4s,
                    order5s,
                    order6s,
                    order3v,
                    order4v,
                    order5v,
                    order6v);
            }
            catch (CDKException e)
            {
                return new Result(e);
            }
        }

        private static readonly QueryAtomContainer[] queries3 = ChiIndexUtils.MakeQueries("C(C)(C)(C)");

        private List<List<int>> Order3(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries3);
        }

        private static readonly QueryAtomContainer[] queries4 = ChiIndexUtils.MakeQueries("C(C)(C)(C)(C)");

        private List<List<int>> Order4(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries4);
        }

        private static readonly QueryAtomContainer[] queries5 = ChiIndexUtils.MakeQueries("CC(C)C(C)(C)");

        private List<List<int>> Order5(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries5);
        }

        private static readonly QueryAtomContainer[] queries6 = ChiIndexUtils.MakeQueries("CC(C)C(C)(C)C");

        private List<List<int>> Order6(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries6);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
