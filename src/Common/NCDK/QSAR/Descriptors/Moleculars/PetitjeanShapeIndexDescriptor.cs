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

using NCDK.Common.Collections;
using NCDK.Geometries;
using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates the Petitjean shape indices.
    /// </summary>
    /// <remarks>
    /// These original Petitjean number was described by Petitjean (<token>cdk-cite-PET92</token>)
    /// and considered the molecular graph. This class also implements the geometric analog
    /// of the topological shape index described by Bath et al (<token>cdk-cite-BAT95</token>).
    /// </remarks>
    // @author      Rajarshi Guha
    // @cdk.created 2006-01-14
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:petitjeanShapeIndex
    // @cdk.keyword Petit-Jean, shape index
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#petitjeanShapeIndex", Requirements = DescriptorRequirements.Geometry3D)]
    public class PetitjeanShapeIndexDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public PetitjeanShapeIndexDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double topoShape, double geomShape)
            {
                this.TopologicalShapeIndex = topoShape;
                this.GeometricShapeIndex = geomShape;
            }

            /// <summary>
            /// topological shape index
            /// </summary>
            [DescriptorResultProperty("topoShape")]
            public double TopologicalShapeIndex { get; private set; }

            /// <summary>
            /// geometric shape index
            /// </summary>
            [DescriptorResultProperty("geomShape")]
            public double GeometricShapeIndex { get; private set; }

            public double Value => TopologicalShapeIndex;
        }

        /// <summary>
        /// Calculates the two Petitjean shape indices.
        /// </summary>
        /// <returns>A <see cref="ResolveEventArgs"/> representing the Petitjean shape indices</returns>
        public Result Calculate(IAtomContainer container)
        {
            var local = AtomContainerManipulator.RemoveHydrogens(container);

            var tradius = PathTools.GetMolecularGraphRadius(local);
            var tdiameter = PathTools.GetMolecularGraphDiameter(local);

            var topoShape = (double)(tdiameter - tradius) / tradius;
            var geomShape = double.NaN;

            // get the 3D distance matrix
            if (GeometryUtil.Has3DCoordinates(container))
            {
                var natom = container.Atoms.Count;
                var distanceMatrix = Arrays.CreateJagged<double>(natom, natom);
                for (int i = 0; i < natom; i++)
                {
                    for (int j = 0; j < natom; j++)
                    {
                        if (i == j)
                        {
                            distanceMatrix[i][j] = 0.0;
                            continue;
                        }

                        var a = container.Atoms[i].Point3D.Value;
                        var b = container.Atoms[j].Point3D.Value;
                        distanceMatrix[i][j] = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
                    }
                }
                double gradius = 999999;
                double gdiameter = -999999;
                var geta = new double[natom];
                for (int i = 0; i < natom; i++)
                {
                    double max = -99999;
                    for (int j = 0; j < natom; j++)
                        if (distanceMatrix[i][j] > max)
                            max = distanceMatrix[i][j];
                    geta[i] = max;
                }
                for (int i = 0; i < natom; i++)
                {
                    if (geta[i] < gradius)
                        gradius = geta[i];
                    if (geta[i] > gdiameter)
                        gdiameter = geta[i];
                }
                geomShape = (gdiameter - gradius) / gradius;
            }

            return new Result(topoShape, geomShape);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
