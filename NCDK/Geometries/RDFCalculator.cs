/* Copyright (C) 2005-2007  The Chemistry Development Kit (CDK) project
 *                    2009  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Numerics;
using System;
using System.Diagnostics;

namespace NCDK.Geometries
{
    /// <summary>
    /// Calculator of radial distribution functions. The RDF has bins defined around
    /// a point, i.e. the first bin starts at 0 Å and ends at 0.5*resolution
    /// Å, and the second bins ends at 1.5*resolution Å.
    /// </summary>
    /// <example>
    /// By default, the RDF is unweighted. By implementing and registering a
    /// <see cref="WeightFunction"/>, the RDF can become weighted. For example,
    /// to weight according to partial charge interaction, this code could be used:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Geometries.RDFCalculator_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="WeightFunction"/>
    // @cdk.module  extra
    // @author      Egon Willighagen
    // @cdk.created 2005-01-10
    // @cdk.keyword radial distribution function
    // @cdk.keyword RDF
    public class RDFCalculator
    {
        private readonly double startCutoff;
        private readonly double cutoff;
        private readonly double resolution;
        private readonly double peakWidth;

        /// <summary>
        /// Calculates the weight for the interaction between the two atoms.
        /// </summary>
        /// <param name="atom">First atom.</param>
        /// <param name="atom2">Second atom.</param>
        /// <returns></returns>
        // @cdk.module  extra
        // @author      Egon Willighagen
        // @cdk.created 2005-01-14
        public delegate double WeightFunction(IAtom atom, IAtom atom2);

        private readonly WeightFunction weightFunction;

        /// <summary>
        /// Constructs a RDF calculator that calculates a unweighted, digitized
        /// RDF function.
        /// </summary>
        /// <param name="startCutoff">radial length in Ångstrom at which the RDF starts</param>
        /// <param name="cutoff">radial length in Ångstrom at which the RDF stops</param>
        /// <param name="resolution">width of the bins</param>
        /// <param name="peakWidth">width of the gaussian applied to the peaks in Ångstrom</param>
        public RDFCalculator(double startCutoff, double cutoff, double resolution, double peakWidth)
            : this(startCutoff, cutoff, resolution, peakWidth, null)
        { }

        /// <summary>
        /// Constructs a RDF calculator that calculates a digitized RDF function.
        /// </summary>
        /// <param name="startCutoff">radial length in Ångstrom at which the RDF starts</param>
        /// <param name="cutoff">radial length in Ångstrom at which the RDF stops</param>
        /// <param name="resolution">width of the bins</param>
        /// <param name="peakWidth">width of the gaussian applied to the peaks in Ångstrom</param>
        /// <param name="weightFunction">the weight function. If null, then an unweighted RDF is calculated</param>
        public RDFCalculator(double startCutoff, double cutoff, double resolution, double peakWidth,
                WeightFunction weightFunction)
        {
            this.startCutoff = startCutoff;
            this.cutoff = cutoff;
            this.resolution = resolution;
            this.peakWidth = peakWidth;
            this.weightFunction = weightFunction;
        }

        /// <summary>
        /// Calculates a RDF for <paramref name="atom"/> in the environment of the atoms in the <paramref name="container"/>.
        /// </summary>
        public double[] Calculate(IAtomContainer container, IAtom atom)
        {
            int length = (int)((cutoff - startCutoff) / resolution) + 1;
            Debug.WriteLine($"Creating RDF of length {length}");

            // the next we need for Gaussian smoothing
            int binsToFillOnEachSide = (int)(peakWidth * 3.0 / resolution);
            double sigmaSquare = Math.Pow(peakWidth, 2.0);
            // factors is only half a Gaussian, taking advantage of being symmetrical!
            double[] factors = new double[binsToFillOnEachSide];
            double totalArea = 0.0;
            if (factors.Length > 0)
            {
                factors[0] = 1;
                for (int binCounter = 1; binCounter < factors.Length; binCounter++)
                {
                    double height = Math.Exp(-1.0 * (Math.Pow(((double)binCounter) * resolution, 2.0)) / sigmaSquare);
                    factors[binCounter] = height;
                    totalArea += height;
                }
                // normalize the Gaussian to unit area
                for (int binCounter = 0; binCounter < factors.Length; binCounter++)
                {
                    factors[binCounter] = factors[binCounter] / totalArea;
                }
            }

            // this we need always
            double[] rdf = new double[length];
            double distance = 0.0;
            int index = 0;

            var atomPoint = atom.Point3D;
            foreach (var atomInContainer in container.Atoms)
            {
                if (atom == atomInContainer) continue; // don't include the central atom
                distance = Vector3.Distance(atomPoint.Value, atomInContainer.Point3D.Value);
                index = (int)((distance - startCutoff) / this.resolution);
                double weight = 1.0;
                if (weightFunction != null)
                {
                    weight = weightFunction(atom, atomInContainer);
                }
                if (factors.Length > 0)
                {
                    // apply Gaussian smoothing
                    rdf[index] += weight * factors[0];
                    for (int binCounter = 1; binCounter < factors.Length; binCounter++)
                    {
                        double diff = weight * factors[binCounter];
                        if ((index - binCounter) >= 0)
                        {
                            rdf[index - binCounter] += diff;
                        }
                        if ((index + binCounter) < length)
                        {
                            rdf[index + binCounter] += diff;
                        }
                    }
                }
                else
                {
                    rdf[index] += weight; // unweighted
                }
            }
            return rdf;
        }
    }
}
