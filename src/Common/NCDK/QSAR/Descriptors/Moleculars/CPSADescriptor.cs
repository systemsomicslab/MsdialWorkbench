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

using NCDK.Charges;
using NCDK.Geometries;
using NCDK.Geometries.Surface;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Calculates 29 Charged Partial Surface Area (CPSA) descriptors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The CPSA's were developed by Stanton et al. (<token>cdk-cite-STA90</token>) and
    /// are related to the Polar Surface Area descriptors. The original
    /// implementation was in the ADAPT software package and the definitions
    /// of the individual descriptors are presented in the following table. This class
    /// returns a <see cref="Result"/> containing the 29 descriptors in the order
    /// described in the table.
    /// </para>
    /// <para>
    /// <a name="cpsa">A Summary of the 29 CPSA Descriptors</a>
    /// <list type="table"> 
    /// <listheader><term>IDescriptor</term><term>Meaning</term></listheader>
    /// <item>
    /// <term>PPSA-1</term><term> partial positive surface area -- sum of surface area on positive parts of molecule</term></item><item>
    /// <term>PPSA-2</term><term> partial positive surface area * total positive charge on the molecule </term></item><item>
    /// <term>PPSA-3</term><term> charge weighted partial positive surface area</term></item><item>
    /// <term>PNSA-1</term><term> partial negative surface area -- sum of surface area on negative parts of molecule</term></item><item>
    /// <term>PNSA-2</term><term> partial negative surface area * total negative charge on the molecule</term></item><item>
    /// <term>PNSA-3</term><term> charge weighted partial negative surface area</term></item><item>
    /// <term>DPSA-1</term><term> difference of PPSA-1 and PNSA-1</term></item><item>
    /// <term>DPSA-2</term><term> difference of FPSA-2 and PNSA-2</term></item><item>
    /// <term>DPSA-3</term><term> difference of PPSA-3 and PNSA-3</term></item><item>
    /// <term>FPSA-1</term><term> PPSA-1 / total molecular surface area</term></item><item>
    /// <term>FFSA-2  </term><term>PPSA-2 / total molecular surface area</term></item><item>
    /// <term>FPSA-3</term><term> PPSA-3 / total molecular surface area</term></item><item>
    /// <term>FNSA-1</term><term> PNSA-1 / total molecular surface area</term></item><item>
    /// <term>FNSA-2</term><term> PNSA-2 / total molecular surface area</term></item><item>
    /// <term>FNSA-3</term><term> PNSA-3 / total molecular surface area</term></item><item>
    /// <term>WPSA-1</term><term> PPSA-1 *  total molecular surface area / 1000</term></item><item>
    /// <term>WPSA-2</term><term> PPSA-2 * total molecular surface area /1000</term></item><item>
    /// <term>WPSA-3</term><term> PPSA-3 * total molecular surface area / 1000</term></item><item>
    /// <term>WNSA-1</term><term> PNSA-1 *  total molecular surface area /1000</term></item><item>
    /// <term>WNSA-2</term><term> PNSA-2 * total molecular surface area / 1000</term></item><item>
    /// <term>WNSA-3</term><term> PNSA-3 * total molecular surface area / 1000</term></item><item>
    /// <term>RPCG</term><term> relative positive charge --  most positive charge / total positive charge</term></item><item>
    /// <term>RNCG</term><term>relative negative charge -- most negative charge / total negative charge</term></item><item>
    /// <term>RPCS</term><term>relative positive charge surface area -- most positive surface area * RPCG</term></item><item>
    /// <term>RNCS</term><term>relative negative charge surface area -- most negative surface area * RNCG</term></item>
    /// <item>
    /// <term>THSA</term>
    /// <term>sum of solvent accessible surface areas of
    /// atoms with absolute value of partial charges
    /// less than 0.2
    /// </term>
    /// </item>
    /// <item>
    /// <term>TPSA</term>
    /// <term>sum of solvent accessible surface areas of
    /// atoms with absolute value of partial charges
    /// greater than or equal 0.2
    /// </term>
    /// </item>
    /// <item>
    /// <term>RHSA</term>
    /// <term>THSA / total molecular surface area
    /// </term>
    /// </item>
    /// <item>
    /// <term>RPSA</term>
    /// <term>TPSA / total molecular surface area
    /// </term>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>NOTE</b>: The values calculated by this implementation will differ from those
    /// calculated by the original ADAPT implementation of the CPSA descriptors. This
    /// is because the original implementation used an analytical surface area algorithm
    /// and used partial charges obtained from MOPAC using the AM1 Hamiltonian.
    /// This implementation uses a numerical
    /// algorithm to obtain surface areas (see <see cref="NumericalSurface"/>) and obtains partial
    /// charges using the Gasteiger-Marsilli algorithm (see <see cref="GasteigerMarsiliPartialCharges"/>).
    /// </para>
    /// <para>
    /// However, a comparison of the values calculated by the two implementations indicates
    /// that they are qualitatively the same.
    /// </para>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2005-05-16
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:CPSA
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#CPSA", Requirements = DescriptorRequirements.Geometry3D)]
    public class CPSADescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public CPSADescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<double> values)
            {
                this.Values = values;
            }

            [DescriptorResultProperty("PPSA-1")]
            public double PPSA1 => Values[0];

            [DescriptorResultProperty("PPSA-2")]
            public double PPSA2 => Values[1];

            [DescriptorResultProperty("PPSA-3")]
            public double PPSA3 => Values[2];

            [DescriptorResultProperty("PNSA-1")]
            public double PNSA1 => Values[3];

            [DescriptorResultProperty("PNSA-2")]
            public double PNSA2 => Values[4];

            [DescriptorResultProperty("PNSA-3")]
            public double PNSA3 => Values[5];

            [DescriptorResultProperty("DPSA-1")]
            public double DPSA1 => Values[6];

            [DescriptorResultProperty("DPSA-2")]
            public double DPSA2 => Values[7];

            [DescriptorResultProperty("DPSA-3")]
            public double DPSA3 => Values[8];

            [DescriptorResultProperty("FPSA-1")]
            public double FPSA1 => Values[9];

            [DescriptorResultProperty("FPSA-2")]
            public double FPSA2 => Values[10];

            [DescriptorResultProperty("FPSA-3")]
            public double FPSA3 => Values[11];

            [DescriptorResultProperty("FNSA-1")]
            public double FNSA1 => Values[12];

            [DescriptorResultProperty("FNSA-2")]
            public double FNSA2 => Values[13];

            [DescriptorResultProperty("FNSA-3")]
            public double FNSA3 => Values[14];

            [DescriptorResultProperty("WPSA-1")]
            public double WPSA1 => Values[15];

            [DescriptorResultProperty("WPSA-2")]
            public double WPSA2 => Values[16];

            [DescriptorResultProperty("WPSA-3")]
            public double WPSA3 => Values[17];

            [DescriptorResultProperty("WNSA-1")]
            public double WNSA1 => Values[18];

            [DescriptorResultProperty("WNSA-2")]
            public double WNSA2 => Values[19];

            [DescriptorResultProperty("WNSA-3")]
            public double WNSA3 => Values[20];

            [DescriptorResultProperty("RPCG")]
            public double RPCG => Values[21];

            [DescriptorResultProperty("RNCG")]
            public double RNCG => Values[22];

            [DescriptorResultProperty("RPCS")]
            public double RPCS => Values[23];

            [DescriptorResultProperty("RNCS")]
            public double RNCS => Values[24];

            [DescriptorResultProperty("THSA")]
            public double THSA => Values[25];

            [DescriptorResultProperty("TPSA")]
            public double TPSA => Values[26];

            [DescriptorResultProperty("RHSA")]
            public double RHSA => Values[27];

            [DescriptorResultProperty("RPSA")]
            public double RPSA => Values[28];
            
            public new IReadOnlyList<double> Values { get; private set; }
        }

        /// <summary>
        /// Evaluates the 29 CPSA descriptors using Gasteiger-Marsilli charges.
        /// </summary>
        /// <returns>An ArrayList containing 29 elements in the order described above</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            if (!GeometryUtil.Has3DCoordinates(container)) {
                Console.WriteLine("Error: Molecule must have 3D coordinates");
                return null;
                //throw new ThreeDRequiredException("Molecule must have 3D coordinates");
            }

            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(container, true);

            var surface = new NumericalSurface(container);
            surface.CalculateSurface();

            var atomSurfaces = surface.GetAllSurfaceAreas();
            var totalSA = surface.GetTotalSurfaceArea();

            double ppsa1 = 0.0;
            double ppsa3 = 0.0;
            double pnsa1 = 0.0;
            double pnsa3 = 0.0;
            double totpcharge = 0.0;
            double totncharge = 0.0;
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                if (container.Atoms[i].Charge > 0)
                {
                    ppsa1 += atomSurfaces[i];
                    ppsa3 += container.Atoms[i].Charge.Value * atomSurfaces[i];
                    totpcharge += container.Atoms[i].Charge.Value;
                }
                else
                {
                    pnsa1 += atomSurfaces[i];
                    pnsa3 += container.Atoms[i].Charge.Value * atomSurfaces[i];
                    totncharge += container.Atoms[i].Charge.Value;
                }
            }

            var ppsa2 = ppsa1 * totpcharge;
            var pnsa2 = pnsa1 * totncharge;

            // fractional +ve & -ve SA
            var fpsa1 = ppsa1 / totalSA;
            var fpsa2 = ppsa2 / totalSA;
            var fpsa3 = ppsa3 / totalSA;
            var fnsa1 = pnsa1 / totalSA;
            var fnsa2 = pnsa2 / totalSA;
            var fnsa3 = pnsa3 / totalSA;

            // surface wtd +ve & -ve SA
            var wpsa1 = ppsa1 * totalSA / 1000;
            var wpsa2 = ppsa2 * totalSA / 1000;
            var wpsa3 = ppsa3 * totalSA / 1000;
            var wnsa1 = pnsa1 * totalSA / 1000;
            var wnsa2 = pnsa2 * totalSA / 1000;
            var wnsa3 = pnsa3 * totalSA / 1000;

            // hydrophobic and poalr surface area
            double phobic = 0.0;
            double polar = 0.0;
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                if (Math.Abs(container.Atoms[i].Charge.Value) < 0.2)
                {
                    phobic += atomSurfaces[i];
                }
                else
                {
                    polar += atomSurfaces[i];
                }
            }
            var thsa = phobic;
            var tpsa = polar;
            var rhsa = phobic / totalSA;
            var rpsa = polar / totalSA;

            // differential +ve & -ve SA
            var dpsa1 = ppsa1 - pnsa1;
            var dpsa2 = ppsa2 - pnsa2;
            var dpsa3 = ppsa3 - pnsa3;

            double maxpcharge = 0.0;
            double maxncharge = 0.0;
            int pidx = 0;
            int nidx = 0;
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var charge = container.Atoms[i].Charge.Value;
                if (charge > maxpcharge)
                {
                    maxpcharge = charge;
                    pidx = i;
                }
                if (charge < maxncharge)
                {
                    maxncharge = charge;
                    nidx = i;
                }
            }

            // relative descriptors
            var rpcg = maxpcharge / totpcharge;
            var rncg = maxncharge / totncharge;
            var rpcs = atomSurfaces[pidx] * rpcg;
            var rncs = atomSurfaces[nidx] * rncg;

            return new Result(new double[]
                {
                    ppsa1,
                    ppsa2,
                    ppsa3,
                    pnsa1,
                    pnsa2,
                    pnsa3,

                    dpsa1,
                    dpsa2,
                    dpsa3,

                    fpsa1,
                    fpsa2,
                    fpsa3,
                    fnsa1,
                    fnsa2,
                    fnsa3,

                    wpsa1,
                    wpsa2,
                    wpsa3,
                    wnsa1,
                    wnsa2,
                    wnsa3,

                    rpcg,
                    rncg,
                    rpcs,
                    rncs,

                    thsa,
                    tpsa,
                    rhsa,
                    rpsa,
                });
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
