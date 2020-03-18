/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System.Diagnostics;
using System.Linq;

namespace NCDK.StructGen
{
    /// <summary>
    /// <see cref="RandomGenerator"/> is a generator of constitutional isomers. It needs to be
    /// provided with a starting constitution and it makes random moves in
    /// constitutional space from there.
    /// This generator was first suggested by J.-L. Faulon <token>cdk-cite-FAU96</token>.
    /// </summary>
    /// <remarks>
    /// <para>Unlike the <see cref="VicinitySampler"/>, this methods does not sample
    /// the full Faulon vicinity.</para>
    /// </remarks>
    /// <seealso cref="VicinitySampler"/>
    // @cdk.keyword structure generator
    // @cdk.module structgen
    public class RandomGenerator
    {
        private static System.Random random = new System.Random();

        private IAtomContainer molecule = null;
        private IAtomContainer proposedStructure = null;
        private IAtomContainer trial = null;

        /// <summary>
        /// Constructs a RandomGenerator with a given starting structure.
        /// </summary>
        /// <param name="molecule">The starting structure</param>
        public RandomGenerator(IAtomContainer molecule)
        {
            this.molecule = molecule;
        }

        /// <summary>
        /// Proposes a structure which can be accepted or rejected by an external
        /// entity. If rejected, the structure is not used as a starting point
        /// for the next random move in structure space.
        /// </summary>
        /// <returns>A proposed molecule</returns>
        public IAtomContainer ProposeStructure()
        {
            Debug.WriteLine("RandomGenerator->ProposeStructure() Start");
            do
            {
                trial = (IAtomContainer)Molecule.Clone();
                Mutate(trial);
                Debug.WriteLine("BondCounts:    " + string.Join(" ", trial.Atoms.Select(n => trial.GetConnectedBonds(n).Count())));
                Debug.WriteLine("BondOrderSums: " + string.Join(" ", trial.Atoms.Select(n => trial.GetBondOrderSum(n))));
            } while (trial == null || !ConnectivityChecker.IsConnected(trial));
            proposedStructure = trial;

            return proposedStructure;
        }

        /// <summary>
        /// Tell the RandomGenerator to accept the last structure that had been proposed.
        /// </summary>
        public void AcceptStructure()
        {
            if (proposedStructure != null)
            {
                molecule = proposedStructure;
            }
        }

        /// <summary>
        /// Randomly chooses four atoms and alters the bonding
        /// pattern between them according to rules described
        /// in "Faulon, JCICS 1996, 36, 731".
        /// </summary>
        public virtual void Mutate(IAtomContainer ac)
        {
            Debug.WriteLine("RandomGenerator->Mutate() Start");
            int nrOfAtoms = ac.Atoms.Count;
            int x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            double a11 = 0, a12 = 0, a22 = 0, a21 = 0;
            double b11 = 0, lowerborder = 0, upperborder = 0;

            IAtom ax1 = null, ax2 = null, ay1 = null, ay2 = null;
            IBond b1 = null, b2 = null, b3 = null, b4 = null;
            int[] choices = new int[3];
            int choiceCounter = 0;
            /* We need at least two non-zero bonds in order to be successful */
            int nonZeroBondsCounter = 0;
            do
            {
                do
                {
                    nonZeroBondsCounter = 0;
                    /* Randomly choose four distinct atoms */
                    do
                    {
                        // this yields numbers between 0 and (nrOfAtoms - 1)
                        x1 = (int)(random.NextDouble() * nrOfAtoms);
                        x2 = (int)(random.NextDouble() * nrOfAtoms);
                        y1 = (int)(random.NextDouble() * nrOfAtoms);
                        y2 = (int)(random.NextDouble() * nrOfAtoms);
                        Debug.WriteLine($"RandomGenerator->Mutate(): x1, x2, y1, y2: {x1}, {x2}, {y1}, {y2}");
                    } while (!(x1 != x2 && x1 != y1 && x1 != y2 && x2 != y1 && x2 != y2 && y1 != y2));
                    ax1 = ac.Atoms[x1];
                    ay1 = ac.Atoms[y1];
                    ax2 = ac.Atoms[x2];
                    ay2 = ac.Atoms[y2];
                    /* Get four bonds for these four atoms */

                    b1 = ac.GetBond(ax1, ay1);
                    if (b1 != null)
                    {
                        a11 = BondManipulator.DestroyBondOrder(b1.Order);
                        nonZeroBondsCounter++;
                    }
                    else
                    {
                        a11 = 0;
                    }

                    b2 = ac.GetBond(ax1, ay2);
                    if (b2 != null)
                    {
                        a12 = BondManipulator.DestroyBondOrder(b2.Order);
                        nonZeroBondsCounter++;
                    }
                    else
                    {
                        a12 = 0;
                    }

                    b3 = ac.GetBond(ax2, ay1);
                    if (b3 != null)
                    {
                        a21 = BondManipulator.DestroyBondOrder(b3.Order);
                        nonZeroBondsCounter++;
                    }
                    else
                    {
                        a21 = 0;
                    }

                    b4 = ac.GetBond(ax2, ay2);
                    if (b4 != null)
                    {
                        a22 = BondManipulator.DestroyBondOrder(b4.Order);
                        nonZeroBondsCounter++;
                    }
                    else
                    {
                        a22 = 0;
                    }
                    Debug.WriteLine($"RandomGenerator->Mutate()->The old bond orders: a11, a12, a21, a22: {a11}, {a12}, {a21}, {a22}");
                } while (nonZeroBondsCounter < 2);

                /* Compute the range for b11 (see Faulons formulae for details) */
                double[] cmax = { 0, a11 - a22, a11 + a12 - 3, a11 + a21 - 3 };
                double[] cmin = { 3, a11 + a12, a11 + a21, a11 - a22 + 3 };
                lowerborder = MathTools.Max(cmax);
                upperborder = MathTools.Min(cmin);
                /* Randomly choose b11 != a11 in the range max > r > min */
                Debug.WriteLine("*** New Try ***");
                Debug.WriteLine($"a11 = {a11}");
                Debug.WriteLine($"upperborder = {upperborder}");
                Debug.WriteLine($"lowerborder = {lowerborder}");
                choiceCounter = 0;
                for (double f = lowerborder; f <= upperborder; f++)
                {
                    if (f != a11)
                    {
                        choices[choiceCounter] = (int)f;
                        choiceCounter++;
                    }
                }
                if (choiceCounter > 0)
                {
                    b11 = choices[(int)(random.NextDouble() * choiceCounter)];
                }

                Debug.WriteLine($"b11 = {b11}");
            } while (!(b11 != a11 && (b11 >= lowerborder && b11 <= upperborder)));

            var b12 = a11 + a12 - b11;
            var b21 = a11 + a21 - b11;
            var b22 = a22 - a11 + b11;

            if (b11 > 0)
            {
                if (b1 == null)
                {
                    b1 = ac.Builder.NewBond(ax1, ay1, BondManipulator.CreateBondOrder(b11));
                    ac.Bonds.Add(b1);
                }
                else
                {
                    b1.Order = BondManipulator.CreateBondOrder(b11);
                }
            }
            else if (b1 != null)
            {
                ac.Bonds.Remove(b1);
            }

            if (b12 > 0)
            {
                if (b2 == null)
                {
                    b2 = ac.Builder.NewBond(ax1, ay2, BondManipulator.CreateBondOrder(b12));
                    ac.Bonds.Add(b2);
                }
                else
                {
                    b2.Order = BondManipulator.CreateBondOrder(b12);
                }
            }
            else if (b2 != null)
            {
                ac.Bonds.Remove(b2);
            }

            if (b21 > 0)
            {
                if (b3 == null)
                {
                    b3 = ac.Builder.NewBond(ax2, ay1, BondManipulator.CreateBondOrder(b21));
                    ac.Bonds.Add(b3);
                }
                else
                {
                    b3.Order = BondManipulator.CreateBondOrder(b21);
                }
            }
            else if (b3 != null)
            {
                ac.Bonds.Remove(b3);
            }

            if (b22 > 0)
            {
                if (b4 == null)
                {
                    b4 = ac.Builder.NewBond(ax2, ay2, BondManipulator.CreateBondOrder(b22));
                    ac.Bonds.Add(b4);
                }
                else
                {
                    b4.Order = BondManipulator.CreateBondOrder(b22);
                }
            }
            else if (b4 != null)
            {
                ac.Bonds.Remove(b4);
            }

            Debug.WriteLine($"a11 a12 a21 a22: {a11} {a12} {a21} {a22}");
            Debug.WriteLine($"b11 b12 b21 b22: {b11} {b12} {b21} {b22}");
        }

        /// <summary>
        /// The molecule which reflects the current state of this
        /// stochastic structure generator.
        /// </summary>
        public IAtomContainer Molecule
        {
            get => molecule;
            set => this.molecule = value;
        }
    }
}
