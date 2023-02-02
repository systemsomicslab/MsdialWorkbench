/* Copyright (C) 2004-2007  Matteo Floris <mfe4@users.sf.net>
 *                    2010  Egon Willighagen <egonw@users.sf.net>
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

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This descriptor calculates the Wiener numbers. This includes the Wiener Path number
    /// and the Wiener Polarity Number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Further information is given in
    /// Wiener path number: half the sum of all the distance matrix entries; Wiener
    /// polarity number: half the sum of all the distance matrix entries with a
    /// value of 3. For more information see <token>cdk-cite-Wiener1947</token>; <token>cdk-cite-TOD2000</token>.
    /// </para>
    /// <para>
    /// This descriptor works properly with AtomContainers whose atoms contain <b>implicit hydrogens</b>
    /// or <b>explicit hydrogens</b>.
    /// </para>
    /// </remarks>
    // @author         mfe4
    // @cdk.created        December 7, 2004
    // @cdk.created    2004-11-03
    // @cdk.module     qsarmolecular
    // @cdk.dictref    qsar-descriptors:wienerNumbers
    // @cdk.keyword    Wiener number
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#wienerNumbers")]
    public class WienerNumbersDescriptor : AbstractDescriptor, IMolecularDescriptor
    {        
        public WienerNumbersDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double path, double polarity)
            {
                this.PathNumber = path;
                this.PolarityNumber = polarity;
            }

            /// <summary>
            /// Wiener path number
            /// </summary>
            [DescriptorResultProperty("WPATH")]
            public double PathNumber { get; private set; }

            /// <summary>
            /// Wiener polarity number
            /// </summary>
            [DescriptorResultProperty("WPOL")]
            public double PolarityNumber { get; private set; }

            public double Value => PathNumber;
        }

        /// <summary>
        /// Calculate the Wiener numbers.
        /// </summary>
        /// <returns>wiener numbers as array of 2 doubles</returns>
        public Result Calculate(IAtomContainer container)
        {
            // RemoveHydrogens does not break container
            var matr = ConnectionMatrix.GetMatrix(AtomContainerManipulator.RemoveHydrogens(container));

            int wienerPathNumber = 0; //wienerPath
            int wienerPolarityNumber = 0; //wienerPol

            var distances = PathTools.ComputeFloydAPSP(matr);

            int partial;
            for (int i = 0; i < distances.Length; i++)
            {
                for (int j = 0; j < distances.Length; j++)
                {
                    partial = distances[i][j];
                    wienerPathNumber += partial;
                    if (partial == 3)
                        wienerPolarityNumber += 1;
                }
            }

            return new Result((double)wienerPathNumber / 2, (double)wienerPolarityNumber / 2);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
