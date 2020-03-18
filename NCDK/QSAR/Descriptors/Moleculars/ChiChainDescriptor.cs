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

using NCDK.Graphs;
using NCDK.Isomorphisms.Matchers;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates chi chain descriptors.
    /// </summary>
    /// <remarks>
    /// The code currently evaluates the simple and valence chi chain descriptors of orders 3, 4, 5, 6 and 7.
    /// It utilizes the graph isomorphism code of the CDK to find fragments matching
    /// SMILES strings representing the fragments corresponding to each type of chain.
    /// <para>
    /// The order of the values returned is
    /// <list type="bullet">
    /// <item>SCH-3 - Simple chain, order 3</item>
    /// <item>SCH-4 - Simple chain, order 4</item>
    /// <item>SCH-5 - Simple chain, order 5</item>
    /// <item>SCH-6 - Simple chain, order 6</item>
    /// <item>SCH-7 - Simple chain, order 7</item>
    /// <item>VCH-3 - Valence chain, order 3</item>
    /// <item>VCH-4 - Valence chain, order 4</item>
    /// <item>VCH-5 - Valence chain, order 5</item>
    /// <item>VCH-6 - Valence chain, order 6</item>
    /// <item>VCH-7 - Valence chain, order 7</item>
    /// </list>
    /// </para>
    /// <note type="note">
    /// These descriptors are calculated using graph isomorphism to identify
    /// the various fragments. As a result calculations may be slow. In addition, recent
    /// versions of Molconn-Z use simplified fragment definitions (i.e., rings without
    /// branches etc.) whereas these descriptors use the older more complex fragment
    /// definitions.
    /// </note>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2006-11-12
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:chiChain
    // @cdk.keyword chi chain index
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#chiChain")]
    public class ChiChainDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public ChiChainDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<double> values)
            {
                this.Values = values;
            }

            public Result(Exception e) : base(e) { }

            [DescriptorResultProperty("SCH-3")]
            public double SCH3 => Values[0];
            [DescriptorResultProperty("SCH-4")]
            public double SCH4 => Values[1];
            [DescriptorResultProperty("SCH-5")]
            public double SCH5 => Values[2];
            [DescriptorResultProperty("SCH-6")]
            public double SCH6 => Values[3];
            [DescriptorResultProperty("SCH-7")]
            public double SCH7 => Values[4];
            [DescriptorResultProperty("SCH-3")]
            public double VCH3 => Values[5];
            [DescriptorResultProperty("VCH-4")]
            public double VCH4 => Values[6];
            [DescriptorResultProperty("VCH-5")]
            public double VCH5 => Values[7];
            [DescriptorResultProperty("VCH-6")]
            public double VCH6 => Values[8];
            [DescriptorResultProperty("VCH-7")]
            public double VCH7 => Values[9];

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
            var subgraph7 = Order7(container);

            try
            {
                var order3s = ChiIndexUtils.EvalSimpleIndex(container, subgraph3);
                var order4s = ChiIndexUtils.EvalSimpleIndex(container, subgraph4);
                var order5s = ChiIndexUtils.EvalSimpleIndex(container, subgraph5);
                var order6s = ChiIndexUtils.EvalSimpleIndex(container, subgraph6);
                var order7s = ChiIndexUtils.EvalSimpleIndex(container, subgraph7);

                var order3v = ChiIndexUtils.EvalValenceIndex(container, subgraph3);
                var order4v = ChiIndexUtils.EvalValenceIndex(container, subgraph4);
                var order5v = ChiIndexUtils.EvalValenceIndex(container, subgraph5);
                var order6v = ChiIndexUtils.EvalValenceIndex(container, subgraph6);
                var order7v = ChiIndexUtils.EvalValenceIndex(container, subgraph7);

                return new Result(new double[]
                    {
                    order3s,
                    order4s,
                    order5s,
                    order6s,
                    order7s,
                    order3v,
                    order4v,
                    order5v,
                    order6v,
                    order7v,
                    });
            }
            catch (CDKException e)
            {
                return new Result(e);
            }
        }

        private static List<List<int>> Order3(IAtomContainer container)
        {
            var ret = new List<List<int>>();

            var rings = Cycles.FindSSSR(container).ToRingSet();

            int nring = rings.Count;
            for (int i = 0; i < nring; i++)
            {
                IAtomContainer ring = rings[i];
                if (ring.Atoms.Count == 3)
                {
                    var tmp = new List<int>();
                    foreach (var atom in ring.Atoms)
                    {
                        tmp.Add(container.Atoms.IndexOf(atom));
                    }
                    ret.Add(tmp);
                }
            }
            return ret;
        }

        private static readonly QueryAtomContainer[] queries4 = ChiIndexUtils.MakeQueries(
            "C1CCC1",
            "CC1CC1");

        private List<List<int>> Order4(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries4);
        }

        private static readonly QueryAtomContainer[] queries5 = ChiIndexUtils.MakeQueries(
            "C1CCCC1",
            "CC1CCC1",
            "CC1CC1(C)");

        private List<List<int>> Order5(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries5);
        }

        private static readonly QueryAtomContainer[] queries6 = ChiIndexUtils.MakeQueries(
            "CC1CCCC1",
            "CC1CC(C)C1",
            "CC1(C)(CCC1)",
            "CCC1CCC1",
            "C1CCCCC1",
            "CC1CCC1(C)",
            "CC1C(C)C1(C)",
            "CCCC1CC1",
            "CCC1CC1(C)");

        private List<List<int>> Order6(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries6);
        }

        private static readonly QueryAtomContainer[] queries7 = ChiIndexUtils.MakeQueries(
            "C1CCCCC1C",
            // 5-ring cases
            "C1CCCC1(C)(C)", "C1(C)C(C)CCC1", "C1(C)CC(C)CC1",
            "C1CCCC1(CC)",
            // 4-ring cases
            "C1(C)C(C)C(C)C1", "C1CC(C)C1(CC)", "C1C(C)CC1(CC)", "C1CCC1(CCC)", "C1CCC1C(C)(C)", "C1CCC1(C)(CC)",
            "C1CC(C)C1(C)(C)", "C1C(C)CC1(C)(C)",
            // 3-ring cases
            "C1(C)C(C)C1(CC)", "C1C(C)(C)C1(C)(C)", "C1CC1CCCC", "C1C(C)C1(CCC)", "C1C(CC)C1(CC)",
            "C1C(C)C1C(C)(C)", "C1C(C)C1(C)(CC)", "C1CC1CC(C)(C)", "C1CC1C(C)CC", "C1CC1C(C)(C)(C)");

        private List<List<int>> Order7(IAtomContainer atomContainer)
        {
            return ChiIndexUtils.GetFragments(atomContainer, queries7);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
