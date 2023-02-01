/* Copyright (C) 2007  Andreas Schueller <archvile18@users.sf.net>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    ///<para> Compares two IAtomContainers for order with the following criteria with decreasing priority:
    /// <list type="number">
    ///   <item>Compare atom count</item>
    ///   <item>Compare molecular weight (heavy atoms only)</item>
    ///   <item>Compare bond count</item>
    ///   <item>Compare sum of bond orders (heavy atoms only)</item>
    /// </list>
    /// </para>
    /// <para>If no difference can be found with the above criteria, the IAtomContainers are
    /// considered equal.</para>
    /// </summary>
    // @author Andreas Schueller
    // @cdk.created  2007-09-05
    // @cdk.module   standard
    public class AtomContainerComparator<T>
        : IComparer<T> 
        where T : IAtomContainer
    {
        /// <summary>Creates a new instance of AtomContainerComparator</summary>
        public AtomContainerComparator() { }

        /// <summary>
        /// <para>Compares two IAtomContainers for order with the following criteria
        /// with decreasing priority:
        /// <list type="number">
        /// <item>Compare atom count</item>
        /// <item>Compare molecular weight (heavy atoms only)</item>
        /// <item>Compare bond count</item>
        /// <item>Compare sum of bond orders (heavy atoms only)</item>
        /// </list> 
        /// </para>
        /// <para>If no difference can be
        /// found with the above criteria, the IAtomContainers are considered
        /// equal.</para> 
        /// <para>Returns a negative integer, zero, or a positive integer as
        /// the first argument is less than, equal to, or greater than the
        /// second.</para>
        /// </summary>
        /// <para>This method is null safe.</para>
        /// <param name="o1">the first IAtomContainer</param>
        /// <param name="o2">the second IAtomContainer</param>
        /// <returns>a negative integer, zero, or a positive integer as the first
        /// argument is less than, equal to, or greater than the second.</returns>
        public int Compare(T o1, T o2)
        {
            // Check for nulls
            if (o1 == null && o2 == null)
                return 0;
            if (o1 == null)
                return 1;
            if (o2 == null)
                return -1;

            T atomContainer1 = o1;
            T atomContainer2 = o2;

            // 1. Compare atom count
            if (atomContainer1.Atoms.Count > atomContainer2.Atoms.Count)
                return 1;
            else if (atomContainer1.Atoms.Count < atomContainer2.Atoms.Count)
                return -1;
            else
            {
                // 2. Atom count equal, compare molecular weight (heavy atoms only)
                double mw1 = 0;
                double mw2 = 0;
                try
                {
                    mw1 = GetMolecularWeight(atomContainer1);
                    mw2 = GetMolecularWeight(atomContainer2);
                }
                catch (CDKException)
                {
                    Trace.TraceWarning("Exception in molecular mass calculation.");
                    return 0;
                }
                if (mw1 > mw2)
                    return 1;
                else if (mw1 < mw2)
                    return -1;
                else
                {
                    // 3. Molecular weight equal, compare bond count
                    if (atomContainer1.Bonds.Count > atomContainer2.Bonds.Count)
                        return 1;
                    else if (atomContainer1.Bonds.Count < atomContainer2.Bonds.Count)
                        return -1;
                    else
                    {
                        // 4. Bond count equal, compare sum of bond orders (heavy atoms only)
                        var bondOrderSum1 = AtomContainerManipulator.GetSingleBondEquivalentSum(atomContainer1);
                        var bondOrderSum2 = AtomContainerManipulator.GetSingleBondEquivalentSum(atomContainer2);
                        if (bondOrderSum1 > bondOrderSum2)
                            return 1;
                        else if (bondOrderSum1 < bondOrderSum2) return -1;
                    }

                }
            }
            // AtomContainers are equal in terms of this comparator
            return 0;
        }

        /// <summary>
        /// Returns the molecular weight (exact mass) of the major isotopes
        /// of all heavy atoms of the given IAtomContainer.
        /// </summary>
        /// <param name="atomContainer">an IAtomContainer to calculate the mocular weight for</param>
        /// <returns>the molecularweight (exact mass) of the major isotopes
        ///         of all heavy atoms of the given IAtomContainer</returns>
        /// <exception cref="CDKException">if an error occurs with the IsotopeFactory</exception>
        private static double GetMolecularWeight(T atomContainer)
        {
            double mw = 0.0;
            try
            {
                var isotopeFactory = CDK.IsotopeFactory;

                foreach (var atom in atomContainer.Atoms)
                {
                    if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
                    {
                        var majorIsotope = isotopeFactory.GetMajorIsotope(atom.Symbol);

                        if (majorIsotope != null && majorIsotope.ExactMass != null)
                        {
                            mw += majorIsotope.ExactMass.Value;
                        }
                    }
                }
            }
            catch (IOException e)
            {
                throw new CDKException(e.Message, e);
            }
            return mw;
        }
    }
}
