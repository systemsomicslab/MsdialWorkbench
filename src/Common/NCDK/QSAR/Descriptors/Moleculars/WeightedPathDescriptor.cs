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
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates the weighted path descriptors.
    /// </summary>
    /// <remarks>
    /// These decsriptors were described by Randic (<token>cdk-cite-RAN84</token>) and characterize molecular
    /// branching. Five descriptors are calculated, based on the implementation in the ADAPT
    /// software package. Note that the descriptor is based on identifying <b>all</b> pahs between pairs of
    /// atoms and so is NP-hard. This means that it can take some time for large, complex molecules.
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2006-01-15
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:weightedPath
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#weightedPath")]
    public class WeightedPathDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public WeightedPathDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<double> values)
            {
                this.Values = values;
            }

            /// <summary>
            /// molecular ID
            /// </summary>
            [DescriptorResultProperty("WTPT-1")]
            public double WTPT1 => Values[0];

            /// <summary>
            /// molecular ID / number of atoms
            /// </summary>
            [DescriptorResultProperty("WTPT-2")]
            public double WTPT2 => Values[1];

            /// <summary>
            /// sum of path lengths starting from heteroatoms
            /// </summary>
            [DescriptorResultProperty("WTPT-3")]
            public double WTPT3 => Values[2];

            /// <summary>
            /// sum of path lengths starting from oxygens
            /// </summary>
            [DescriptorResultProperty("WTPT-4")]
            public double WTPT4 => Values[3];

            /// <summary>
            /// sum of path lengths starting from nitrogens
            /// </summary>
            [DescriptorResultProperty("WTPT-5")]
            public double WTPT5 => Values[4];

            public new IReadOnlyList<double> Values { get; private set; }
        }

        /// <summary>
        /// Calculates the weighted path descriptors.
        /// </summary>
        /// <returns>A value representing the weighted path values</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = AtomContainerManipulator.RemoveHydrogens(container);

            int natom = container.Atoms.Count;
            var retval = new List<double>(5);

            var pathList = new List<IList<IAtom>>();

            // unique paths
            for (int i = 0; i < natom - 1; i++)
            {
                var a = container.Atoms[i];
                for (int j = i + 1; j < natom; j++)
                {
                    var b = container.Atoms[j];
                    pathList.AddRange(PathTools.GetAllPaths(container, a, b));
                }
            }

            // heteroatoms
            var pathWts = GetPathWeights(pathList, container);
            double mid = 0.0;
            foreach (var pathWt3 in pathWts)
                mid += pathWt3;
            mid += natom; // since we don't calculate paths of length 0 above

            retval.Add(mid);
            retval.Add(mid / (double)natom);

            pathList.Clear();
            int count = 0;
            for (int i = 0; i < natom; i++)
            {
                var a = container.Atoms[i];
                if (a.AtomicNumber.Equals(AtomicNumbers.C))
                    continue;
                count++;
                for (int j = 0; j < natom; j++)
                {
                    var b = container.Atoms[j];
                    if (a.Equals(b))
                        continue;
                    pathList.AddRange(PathTools.GetAllPaths(container, a, b));
                }
            }
            pathWts = GetPathWeights(pathList, container);
            mid = 0.0;
            foreach (var pathWt2 in pathWts)
                mid += pathWt2;
            mid += count;
            retval.Add(mid);

            // oxygens
            pathList.Clear();
            count = 0;
            for (int i = 0; i < natom; i++)
            {
                var a = container.Atoms[i];
                if (!a.AtomicNumber.Equals(AtomicNumbers.O))
                    continue;
                count++;
                for (int j = 0; j < natom; j++)
                {
                    var b = container.Atoms[j];
                    if (a.Equals(b))
                        continue;
                    pathList.AddRange(PathTools.GetAllPaths(container, a, b));
                }
            }
            pathWts = GetPathWeights(pathList, container);
            mid = 0.0;
            foreach (var pathWt1 in pathWts)
                mid += pathWt1;
            mid += count;
            retval.Add(mid);

            // nitrogens
            pathList.Clear();
            count = 0;
            for (int i = 0; i < natom; i++)
            {
                var a = container.Atoms[i];
                if (!a.AtomicNumber.Equals(AtomicNumbers.N))
                    continue;
                count++;
                for (int j = 0; j < natom; j++)
                {
                    var b = container.Atoms[j];
                    if (a.Equals(b))
                        continue;
                    pathList.AddRange(PathTools.GetAllPaths(container, a, b));
                }
            }
            pathWts = GetPathWeights(pathList, container);
            mid = 0.0;
            foreach (var pathWt in pathWts)
                mid += pathWt;
            mid += count;
            retval.Add(mid);

            return new Result(retval);
        }

        private static double[] GetPathWeights(List<IList<IAtom>> pathList, IAtomContainer atomContainer)
        {
            var pathWts = new double[pathList.Count];
            for (int i = 0; i < pathList.Count; i++)
            {
                var p = pathList[i];
                pathWts[i] = 1.0;
                for (int j = 0; j < p.Count - 1; j++)
                {
                    var a = p[j];
                    var b = p[j + 1];
                    var n1 = atomContainer.GetConnectedAtoms(a).Count();
                    var n2 = atomContainer.GetConnectedAtoms(b).Count();
                    pathWts[i] /= Math.Sqrt(n1 * n2);
                }
            }
            return pathWts;
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
