/* Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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
    /// Evaluates chi path descriptors.
    /// </summary>
    /// <remarks>
    /// It utilizes the graph isomorphism code of the CDK to find fragments matching
    /// SMILES strings representing the fragments corresponding to each type of chain.
    /// <para>
    /// The order of the values returned is
    /// <list type="bullet"> 
    /// <item>SP-0, SP-1, ..., SP-7 - Simple path, orders 0 to 7</item>
    /// <item>VP-0, VP-1, ..., VP-7 - Valence path, orders 0 to 7</item>
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
    // @cdk.dictref qsar-descriptors:chiPath
    // @cdk.keyword chi path index
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#chiPath")]
    public class ChiPathDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public ChiPathDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(
                double sp0,
                double sp1,
                double sp2,
                double sp3,
                double sp4,
                double sp5,
                double sp6,
                double sp7,
                double vp0,
                double vp1,
                double vp2,
                double vp3,
                double vp4,
                double vp5,
                double vp6,
                double vp7)
            {
                this.Values = new[]
                {
                    sp0,
                    sp1,
                    sp2,
                    sp3,
                    sp4,
                    sp5,
                    sp6,
                    sp7,
                    vp0,
                    vp1,
                    vp2,
                    vp3,
                    vp4,
                    vp5,
                    vp6,
                    vp7,
                };
            }

            public Result(Exception e) : base(e) { }

            [DescriptorResultProperty("SP-0")]
            public double SP0 => Values[0];
            [DescriptorResultProperty("SP-1")]
            public double SP1 => Values[1];
            [DescriptorResultProperty("SP-2")]
            public double SP2 => Values[2];
            [DescriptorResultProperty("SP-3")]
            public double SP3 => Values[3];
            [DescriptorResultProperty("SP-4")]
            public double SP4 => Values[4];
            [DescriptorResultProperty("SP-5")]
            public double SP5 => Values[5];
            [DescriptorResultProperty("SP-6")]
            public double SP6 => Values[6];
            [DescriptorResultProperty("SP-7")]
            public double SP7 => Values[7];
            [DescriptorResultProperty("VP-0")]
            public double VP0 => Values[0 + 8];
            [DescriptorResultProperty("VP-1")]
            public double VP1 => Values[1 + 8];
            [DescriptorResultProperty("VP-2")]
            public double VP2 => Values[2 + 8];
            [DescriptorResultProperty("VP-3")]
            public double VP3 => Values[3 + 8];
            [DescriptorResultProperty("VP-4")]
            public double VP4 => Values[4 + 8];
            [DescriptorResultProperty("VP-5")]
            public double VP5 => Values[5 + 8];
            [DescriptorResultProperty("VP-6")]
            public double VP6 => Values[6 + 8];
            [DescriptorResultProperty("VP-7")]
            public double VP7 => Values[7 + 8];

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

            var subgraph0 = Order0(container);
            var subgraph1 = Order1(container);
            var subgraph2 = Order2(container);
            var subgraph3 = Order3(container);
            var subgraph4 = Order4(container);
            var subgraph5 = Order5(container);
            var subgraph6 = Order6(container);
            var subgraph7 = Order7(container);

            try
            {
                var order0s = ChiIndexUtils.EvalSimpleIndex(container, subgraph0);
                var order1s = ChiIndexUtils.EvalSimpleIndex(container, subgraph1);
                var order2s = ChiIndexUtils.EvalSimpleIndex(container, subgraph2);
                var order3s = ChiIndexUtils.EvalSimpleIndex(container, subgraph3);
                var order4s = ChiIndexUtils.EvalSimpleIndex(container, subgraph4);
                var order5s = ChiIndexUtils.EvalSimpleIndex(container, subgraph5);
                var order6s = ChiIndexUtils.EvalSimpleIndex(container, subgraph6);
                var order7s = ChiIndexUtils.EvalSimpleIndex(container, subgraph7);

                var order0v = ChiIndexUtils.EvalValenceIndex(container, subgraph0);
                var order1v = ChiIndexUtils.EvalValenceIndex(container, subgraph1);
                var order2v = ChiIndexUtils.EvalValenceIndex(container, subgraph2);
                var order3v = ChiIndexUtils.EvalValenceIndex(container, subgraph3);
                var order4v = ChiIndexUtils.EvalValenceIndex(container, subgraph4);
                var order5v = ChiIndexUtils.EvalValenceIndex(container, subgraph5);
                var order6v = ChiIndexUtils.EvalValenceIndex(container, subgraph6);
                var order7v = ChiIndexUtils.EvalValenceIndex(container, subgraph7);

                return new Result(
                    order0s,
                    order1s,
                    order2s,
                    order3s,
                    order4s,
                    order5s,
                    order6s,
                    order7s,
                    order0v,
                    order1v,
                    order2v,
                    order3v,
                    order4v,
                    order5v,
                    order6v,
                    order7v);
            }
            catch (CDKException e)
            {
                return new Result(e);
            }
        }

        private static List<List<int>> Order0(IAtomContainer atomContainer)
        {
            var fragments = new List<List<int>>();
            foreach (var atom in atomContainer.Atoms)
            {
                var tmp = new List<int> { atomContainer.Atoms.IndexOf(atom) };
                fragments.Add(tmp);
            }
            return fragments;
        }

        private static List<List<int>> Order1(IAtomContainer atomContainer)
        {
            var fragments = new List<List<int>>();
            foreach (var bond in atomContainer.Bonds)
            {
                if (bond.Atoms.Count != 2)
                    throw new CDKException("We only consider 2 center bonds");
                var tmp = new List<int>
                {
                    atomContainer.Atoms.IndexOf(bond.Atoms[0]),
                    atomContainer.Atoms.IndexOf(bond.Atoms[1])
                };
                fragments.Add(tmp);
            }
            return fragments;
        }

        private static readonly QueryAtomContainer[] queries2 = ChiIndexUtils.MakeQueries("CCC");
        private static readonly QueryAtomContainer[] queries3 = ChiIndexUtils.MakeQueries("CCCC");
        private static readonly QueryAtomContainer[] queries4 = ChiIndexUtils.MakeQueries("CCCCC");
        private static readonly QueryAtomContainer[] queries5 = ChiIndexUtils.MakeQueries("CCCCCC");
        private static readonly QueryAtomContainer[] queries6 = ChiIndexUtils.MakeQueries("CCCCCCC");
        private static readonly QueryAtomContainer[] queries7 = ChiIndexUtils.MakeQueries("CCCCCCCC");

        private static List<List<int>> Order2(IAtomContainer atomContainer) => ChiIndexUtils.GetFragments(atomContainer, queries2);
        private static List<List<int>> Order3(IAtomContainer atomContainer) => ChiIndexUtils.GetFragments(atomContainer, queries3);
        private static List<List<int>> Order4(IAtomContainer atomContainer) => ChiIndexUtils.GetFragments(atomContainer, queries4);
        private static List<List<int>> Order5(IAtomContainer atomContainer) => ChiIndexUtils.GetFragments(atomContainer, queries5);
        private static List<List<int>> Order6(IAtomContainer atomContainer) => ChiIndexUtils.GetFragments(atomContainer, queries6);
        private static List<List<int>> Order7(IAtomContainer atomContainer) => ChiIndexUtils.GetFragments(atomContainer, queries7);

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
