/* Copyright (C) 2002-2003  The Jmol Project
 *  Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config;
using NCDK.Graphs.Invariant;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Geometries
{
    /// <summary>
    /// A set of static utility classes for geometric calculations on <see cref="IBond"/>s.
    /// The methods for detecting stereo configurations are described in CDK news, vol 2, p. 64 - 66.
    /// </summary>
    // @author      shk3
    // @cdk.created 2005-08-04
    // @cdk.module  standard
    public static class BondTools
    {
        // FIXME: class JavaDoc should use <token>cdk-cite-BLA</token> for the CDK News article

        /// <summary>
        /// Tells if a certain bond is center of a valid double bond configuration.
        /// </summary>
        /// <param name="container">The atom container.</param>
        /// <param name="bond">The bond.</param>
        /// <returns>true=is a potential configuration, false=is not.</returns>
        public static bool IsValidDoubleBondConfiguration(IAtomContainer container, IBond bond)
        {
            //IAtom[] atoms = bond.GetAtoms();
            var connectedAtoms = container.GetConnectedAtoms(bond.Begin);
            IAtom from = null;
            foreach (var connectedAtom in connectedAtoms)
            {
                if (connectedAtom != bond.End)
                {
                    from = connectedAtom;
                }
            }
            bool[] array = new bool[container.Bonds.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = true;
            }
            if (IsStartOfDoubleBond(container, bond.Begin, from, array)
                    && IsEndOfDoubleBond(container, bond.End, bond.Begin, array)
                    && !bond.IsAromatic)
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        /// <summary>
        /// Says if two atoms are in cis or trans position around a double bond.
        /// The atoms have to be given to the method like this:  firstOuterAtom - firstInnerAtom = secondInnterAtom - secondOuterAtom
        /// </summary>
        /// <param name="firstOuterAtom">See above.</param>
        /// <param name="firstInnerAtom">See above.</param>
        /// <param name="secondInnerAtom">See above.</param>
        /// <param name="secondOuterAtom">See above.</param>
        /// <param name="ac">The atom container the atoms are in.</param>
        /// <returns>true=trans, false=cis.</returns>
        /// <exception cref="CDKException"> The atoms are not in a double bond configuration (no double bond in the middle, same atoms on one side)</exception>
        public static bool IsCisTrans(IAtom firstOuterAtom, IAtom firstInnerAtom, IAtom secondInnerAtom,
                IAtom secondOuterAtom, IAtomContainer ac)
        {
            if (!IsValidDoubleBondConfiguration(ac, ac.GetBond(firstInnerAtom, secondInnerAtom)))
            {
                throw new CDKException("There is no valid double bond configuration between your inner atoms!");
            }
            bool firstDirection = IsLeft(firstOuterAtom, firstInnerAtom, secondInnerAtom);
            bool secondDirection = IsLeft(secondOuterAtom, secondInnerAtom, firstInnerAtom);
            return firstDirection == secondDirection;
        }

        /// <summary>
        /// Says if an atom is on the left side of a another atom seen from a certain
        /// atom or not.
        /// </summary>
        /// <param name="whereIs">The atom the position of which is returned</param>
        /// <param name="viewFrom">The atom from which to look</param>
        /// <param name="viewTo">The atom to which to look</param>
        /// <returns>true=is left, false = is not</returns>
        public static bool IsLeft(IAtom whereIs, IAtom viewFrom, IAtom viewTo)
        {
            double angle = GiveAngleBothMethods(viewFrom, viewTo, whereIs, false);
            if (angle < 0)
            {
                return (false);
            }
            else
            {
                return (true);
            }
        }

        /// <summary>
        /// Returns true if the two atoms are within the distance fudge
        /// factor of each other.
        /// </summary>
        /// <param name="atom1">Description of Parameter</param>
        /// <param name="atom2">Description of Parameter</param>
        /// <param name="distanceFudgeFactor">Description of Parameter</param>
        /// <returns>Description of the Returned Value</returns>
       // @cdk.keyword                 join-the-dots
       // @cdk.keyword                 bond creation
        public static bool CloseEnoughToBond(IAtom atom1, IAtom atom2, double distanceFudgeFactor)
        {
            if (atom1 != atom2)
            {
                double distanceBetweenAtoms = (atom1.Point3D.Value - atom2.Point3D.Value).Length();
                double bondingDistance = atom1.CovalentRadius.Value + atom2.CovalentRadius.Value;
                if (distanceBetweenAtoms <= (distanceFudgeFactor * bondingDistance))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gives the angle between two lines starting at atom from and going to to1
        /// and to2. If <paramref name="flag"/> is <see langword="false"/> the angle starts from the middle line and goes from
        /// 0 to PI or 0 to -PI if the to2 is on the left or right side of the line. If
        /// <paramref name="flag"/> is <see langword="true"/> the angle goes from 0 to 2ƒÎ.
        /// </summary>
        /// <param name="from">the atom to view from.</param>
        /// <param name="to1">first direction to look in.</param>
        /// <param name="to2">second direction to look in.</param>
        /// <param name="flag">true=angle is 0 to 2PI, false=angel is -PI to PI.</param>
        /// <returns>The angle in rad.</returns>
        public static double GiveAngleBothMethods(IAtom from, IAtom to1, IAtom to2, bool flag)
        {
            return GiveAngleBothMethods(from.Point2D.Value, to1.Point2D.Value, to2.Point2D.Value, flag);
        }

        public static double GiveAngleBothMethods(Vector2 from, Vector2 to1, Vector2 to2, bool flag)
        {
            double[] A = new double[2];
            A[0] = from.X;
            A[1] = from.Y;
            double[] B = new double[2];
            B[0] = to1.X;
            B[1] = to1.Y;
            double[] C = new double[2];
            C[0] = to2.X;
            C[1] = to2.Y;
            double angle1 = Math.Atan2((B[1] - A[1]), (B[0] - A[0]));
            double angle2 = Math.Atan2((C[1] - A[1]), (C[0] - A[0]));
            double angle = angle2 - angle1;
            if (angle2 < 0 && angle1 > 0 && angle2 < -(Math.PI / 2))
            {
                angle = Math.PI + angle2 + Math.PI - angle1;
            }
            if (angle2 > 0 && angle1 < 0 && angle1 < -(Math.PI / 2))
            {
                angle = -Math.PI + angle2 - Math.PI - angle1;
            }
            if (flag && angle < 0)
            {
                return (2 * Math.PI + angle);
            }
            else
            {
                return (angle);
            }
        }

        /// <summary>
        /// Says if an atom is the end of a double bond configuration
        /// </summary>
        /// <param name="atom">The atom which is the end of configuration</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <param name="parent">The atom we came from</param>
        /// <param name="doubleBondConfiguration">The array indicating where double bond
        ///     configurations are specified (this method ensures that there is
        ///     actually the possibility of a double bond configuration)</param>
        /// <returns>false=is not end of configuration, true=is</returns>
        private static bool IsEndOfDoubleBond(IAtomContainer container, IAtom atom, IAtom parent,
                bool[] doubleBondConfiguration)
        {
            var bondNumber = container.Bonds.IndexOf(container.GetBond(atom, parent));
            if (bondNumber == -1
                    || doubleBondConfiguration.Length <= bondNumber
                    || !doubleBondConfiguration[bondNumber])
            {
                return false;
            }

            int hcount = atom.ImplicitHydrogenCount ?? 0;

            int lengthAtom = container.GetConnectedAtoms(atom).Count() + hcount;

            hcount = parent.ImplicitHydrogenCount ?? 0;

            int lengthParent = container.GetConnectedAtoms(parent).Count() + hcount;

            if (container.GetBond(atom, parent) != null)
            {
                if (container.GetBond(atom, parent).Order == BondOrder.Double
                 && (lengthAtom == 3 || (lengthAtom == 2 && atom.AtomicNumber.Equals(AtomicNumbers.N)))
                 && (lengthParent == 3 || (lengthParent == 2 && parent.AtomicNumber.Equals(AtomicNumbers.N))))
                {
                    var atoms = container.GetConnectedAtoms(atom);
                    IAtom one = null;
                    IAtom two = null;
                    foreach (var conAtom in atoms)
                    {
                        if (conAtom != parent && one == null)
                        {
                            one = conAtom;
                        }
                        else if (conAtom != parent && one != null)
                        {
                            two = conAtom;
                        }
                    }
                    string[] morgannumbers = MorganNumbersTools.GetMorganNumbersWithElementSymbol(container);
                    if ((one != null && two == null 
                      && atom.AtomicNumber.Equals(AtomicNumbers.N) 
                      && Math.Abs(GiveAngleBothMethods(parent, atom, one, true)) > Math.PI / 10)
                     || (!atom.AtomicNumber.Equals(AtomicNumbers.N) 
                      && one != null && two != null 
                      && !morgannumbers[container.Atoms.IndexOf(one)].Equals(morgannumbers[container.Atoms.IndexOf(two)], StringComparison.Ordinal)))
                    {
                        return (true);
                    }
                    else
                    {
                        return (false);
                    }
                }
            }
            return (false);
        }

        /// <summary>
        /// Says if an atom is the start of a double bond configuration
        /// </summary>
        /// <param name="a">The atom which is the start of configuration</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <param name="parent">The atom we came from</param>
        /// <param name="doubleBondConfiguration">The array indicating where double bond
        ///     configurations are specified (this method ensures that there is
        ///     actually the possibility of a double bond configuration)</param>
        /// <returns>false=is not start of configuration, true=is</returns>
        private static bool IsStartOfDoubleBond(IAtomContainer container, IAtom a, IAtom parent, bool[] doubleBondConfiguration)
        {
            int hcount;
            hcount = a.ImplicitHydrogenCount ?? 0;

            int lengthAtom = container.GetConnectedAtoms(a).Count() + hcount;

            if (lengthAtom != 3 && (lengthAtom != 2 && !(a.AtomicNumber.Equals(AtomicNumbers.N))))
            {
                return (false);
            }
            var atoms = container.GetConnectedAtoms(a);
            IAtom one = null;
            IAtom two = null;
            bool doubleBond = false;
            IAtom nextAtom = null;
            foreach (var atom in atoms)
            {
                if (atom != parent && container.GetBond(atom, a).Order == BondOrder.Double
                        && IsEndOfDoubleBond(container, atom, a, doubleBondConfiguration))
                {
                    doubleBond = true;
                    nextAtom = atom;
                }
                if (atom != nextAtom && one == null)
                {
                    one = atom;
                }
                else if (atom != nextAtom && one != null)
                {
                    two = atom;
                }
            }
            string[] morgannumbers = MorganNumbersTools.GetMorganNumbersWithElementSymbol(container);
            if (one != null
             && ((!a.AtomicNumber.Equals(AtomicNumbers.N)
              && two != null
              && !morgannumbers[container.Atoms.IndexOf(one)].Equals(morgannumbers[container.Atoms.IndexOf(two)], StringComparison.Ordinal)
              && doubleBond
              && doubleBondConfiguration[container.Bonds.IndexOf(container.GetBond(a, nextAtom))])
              || (doubleBond && a.AtomicNumber.Equals(AtomicNumbers.N)
              && Math.Abs(GiveAngleBothMethods(nextAtom, a, parent, true)) > Math.PI / 10)))
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        /// <summary>
        /// Says if an atom as a center of a tetrahedral chirality.
        /// This method uses wedge bonds. 3D coordinates are not taken into account. If there
        /// are no wedge bonds around a potential stereo center, it will not be found.
        /// </summary>
        /// <param name="atom">The atom which is the center</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <param name="strict"></param>
        /// <returns>0=is not tetrahedral; &gt;1 is a certain depiction of
        ///     tetrahedrality (evaluated in parse chain)</returns>
        public static int IsTetrahedral(IAtomContainer container, IAtom atom, bool strict)
        {
            var atoms = container.GetConnectedAtoms(atom);
            if (atoms.Count() != 4)
            {
                return (0);
            }
            var bonds = container.GetConnectedBonds(atom);
            int up = 0;
            int down = 0;
            foreach (var bond in bonds)
            {
                switch (bond.Stereo)
                {
                    case BondStereo.None:
                        break;
                    case BondStereo.Up:
                        up++;
                        break;
                    case BondStereo.Down:
                        down++;
                        break;
                }
            }
            if (up == 1 && down == 1)
            {
                return 1;
            }
            if (up == 2 && down == 2)
            {
                if (StereosAreOpposite(container, atom))
                {
                    return 2;
                }
                return 0;
            }
            if (up == 1 && down == 0 && !strict)
            {
                return 3;
            }
            if (down == 1 && up == 0 && !strict)
            {
                return 4;
            }
            if (down == 2 && up == 1 && !strict)
            {
                return 5;
            }
            if (down == 1 && up == 2 && !strict)
            {
                return 6;
            }
            return 0;
        }

        /// <summary>
        /// Says if an atom as a center of a trigonal-bipyramidal or actahedral
        /// chirality. This method uses wedge bonds. 3D coordinates are not taken into account. If there
        /// are no wedge bonds around a potential stereo center, it will not be found.
        /// </summary>
        /// <param name="atom">The atom which is the center</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <returns>true=is square planar, false=is not</returns>
        public static int IsTrigonalBipyramidalOrOctahedral(IAtomContainer container, IAtom atom)
        {
            var atoms = container.GetConnectedAtoms(atom).ToReadOnlyList();
            if (atoms.Count < 5 || atoms.Count > 6)
            {
                return (0);
            }
            var bonds = container.GetConnectedBonds(atom);
            int up = 0;
            int down = 0;
            foreach (var bond in bonds)
            {
                switch (bond.Stereo)
                {
                    case BondStereo.None:
                        break;
                    case BondStereo.Up:
                        up++;
                        break;
                    case BondStereo.Down:
                        down++;
                        break;
                }
            }
            if (up == 1 && down == 1)
            {
                if (atoms.Count == 5)
                    return 1;
                else
                    return 2;
            }
            return 0;
        }

        /// <summary>
        /// Says if an atom as a center of any valid stereo configuration or not.
        /// This method uses wedge bonds. 3D coordinates are not taken into account. If there
        /// are no wedge bonds around a potential stereo center, it will not be found.
        /// </summary>
        /// <param name="stereoAtom">The atom which is the center</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <returns>true=is a stereo atom, false=is not</returns>
        public static bool IsStereo(IAtomContainer container, IAtom stereoAtom)
        {
            var atoms = container.GetConnectedAtoms(stereoAtom).ToReadOnlyList();
            if (atoms.Count < 4 || atoms.Count > 6)
            {
                return (false);
            }
            var bonds = container.GetConnectedBonds(stereoAtom);
            int stereo = 0;
            foreach (var bond in bonds)
            {
                if (bond.Stereo != BondStereo.None)
                {
                    stereo++;
                }
            }
            if (stereo == 0)
            {
                return false;
            }
            int differentAtoms = 0;
            for (int i = 0; i < atoms.Count; i++)
            {
                bool isDifferent = true;
                for (int k = 0; k < i; k++)
                {
                    if (atoms[i].Symbol.Equals(atoms[k].Symbol, StringComparison.Ordinal))
                    {
                        isDifferent = false;
                        break;
                    }
                }
                if (isDifferent)
                {
                    differentAtoms++;
                }
            }
            if (differentAtoms != atoms.Count)
            {
                long[] morgannumbers = MorganNumbersTools.GetMorganNumbers(container);
                var differentSymbols = new List<string>();
                foreach (var atom in atoms)
                {
                    if (!differentSymbols.Contains(atom.Symbol))
                    {
                        differentSymbols.Add(atom.Symbol);
                    }
                }
                int[] onlyRelevantIfTwo = new int[2];
                if (differentSymbols.Count == 2)
                {
                    foreach (var atom in atoms)
                    {
                        if (differentSymbols.IndexOf(atom.Symbol) == 0)
                        {
                            onlyRelevantIfTwo[0]++;
                        }
                        else
                        {
                            onlyRelevantIfTwo[1]++;
                        }
                    }
                }
                bool[] symbolsWithDifferentMorganNumbers = new bool[differentSymbols.Count];
                var symbolsMorganNumbers = new List<long>[symbolsWithDifferentMorganNumbers.Length];
                for (int i = 0; i < symbolsWithDifferentMorganNumbers.Length; i++)
                {
                    symbolsWithDifferentMorganNumbers[i] = true;
                    symbolsMorganNumbers[i] = new List<long>();
                }
                foreach (var atom in atoms)
                {
                    int elementNumber = differentSymbols.IndexOf(atom.Symbol);
                    if (symbolsMorganNumbers[elementNumber].Contains(morgannumbers[container.Atoms.IndexOf(atom)]))
                    {
                        symbolsWithDifferentMorganNumbers[elementNumber] = false;
                    }
                    else
                    {
                        symbolsMorganNumbers[elementNumber].Add(morgannumbers[container.Atoms.IndexOf(atom)]);
                    }
                }
                int numberOfSymbolsWithDifferentMorganNumbers = 0;
                foreach (var symbolWithDifferentMorganNumber in symbolsWithDifferentMorganNumbers)
                {
                    if (symbolWithDifferentMorganNumber)
                    {
                        numberOfSymbolsWithDifferentMorganNumbers++;
                    }
                }
                if (numberOfSymbolsWithDifferentMorganNumbers != differentSymbols.Count)
                {
                    if ((atoms.Count == 5 || atoms.Count == 6)
                            && (numberOfSymbolsWithDifferentMorganNumbers + differentAtoms > 2 || (differentAtoms == 2
                                    && onlyRelevantIfTwo[0] > 1 && onlyRelevantIfTwo[1] > 1)))
                    {
                        return (true);
                    }
                    return IsSquarePlanar(container, stereoAtom)
                            && (numberOfSymbolsWithDifferentMorganNumbers + differentAtoms > 2 || (differentAtoms == 2
                                    && onlyRelevantIfTwo[0] > 1 && onlyRelevantIfTwo[1] > 1));
                }
            }
            return (true);
        }

        /// <summary>
        /// Says if an atom as a center of a square planar chirality.
        /// This method uses wedge bonds. 3D coordinates are not taken into account. If there
        /// are no wedge bonds around a potential stereo center, it will not be found.
        /// </summary>
        /// <param name="atom">The atom which is the center</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <returns>true=is square planar, false=is not</returns>
        public static bool IsSquarePlanar(IAtomContainer container, IAtom atom)
        {
            var atoms = container.GetConnectedAtoms(atom);
            if (atoms.Count() != 4)
            {
                return (false);
            }
            var bonds = container.GetConnectedBonds(atom);
            int up = 0;
            int down = 0;
            foreach (var bond in bonds)
            {
                switch (bond.Stereo)
                {
                    case BondStereo.None:
                        break;
                    case BondStereo.Up:
                        up++;
                        break;
                    case BondStereo.Down:
                        down++;
                        break;
                }
            }
            return up == 2 && down == 2 && !StereosAreOpposite(container, atom);
        }

        /// <summary>
        /// Says if of four atoms connected two one atom the up and down bonds are
        /// opposite or not, i. e.if it's tetrahedral or square planar. The method
        /// does not check if there are four atoms and if two or up and two are down
        /// </summary>
        /// <param name="atom">The atom which is the center</param>
        /// <param name="container">The atomContainer the atom is in</param>
        /// <returns>true=are opposite, false=are not</returns>
        public static bool StereosAreOpposite(IAtomContainer container, IAtom atom)
        {
            var atoms = container.GetConnectedAtoms(atom).ToReadOnlyList();
            var hm = new SortedDictionary<double, int>();
            for (int i = 1; i < atoms.Count; i++)
            {
                hm.Add(GiveAngle(atom, atoms[0], atoms[i]), i);
            }
            var ohere = hm.Values.ToArray();
            var stereoOne = container.GetBond(atom, atoms[0]).Stereo;
            var stereoOpposite = container.GetBond(atom, atoms[ohere[1]]).Stereo;
            return stereoOpposite == stereoOne;
        }

        /// <summary>
        /// Calls <see cref="GiveAngleBothMethods(Vector2, Vector2, Vector2, bool)"/> with <see langword="true"/> parameter.
        /// </summary>
        /// <param name="from">the atom to view from</param>
        /// <param name="to1">first direction to look in</param>
        /// <param name="to2">second direction to look in</param>
        /// <returns>The angle in rad from 0 to 2*PI</returns>
        public static double GiveAngle(IAtom from, IAtom to1, IAtom to2)
        {
            return (GiveAngleBothMethods(from, to1, to2, true));
        }

        /// <summary>
        /// Calls <see cref="GiveAngleBothMethods(Vector2, Vector2, Vector2, bool)"/> with <see langword="false"/> parameter.
        /// </summary>
        /// <param name="from">the atom to view from</param>
        /// <param name="to1">first direction to look in</param>
        /// <param name="to2">second direction to look in</param>
        /// <returns>The angle in rad from -PI to PI</returns>
        public static double GiveAngleFromMiddle(IAtom from, IAtom to1, IAtom to2)
        {
            return (GiveAngleBothMethods(from, to1, to2, false));
        }

        public static void MakeUpDownBonds(IAtomContainer container)
        {
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var a = container.Atoms[i];
                var connectedAtoms = container.GetConnectedAtoms(a).ToReadOnlyList();
                if (connectedAtoms.Count == 4)
                {
                    int up = 0;
                    int down = 0;
                    int hs = 0;
                    IAtom h = null;
                    for (int k = 0; k < 4; k++)
                    {
                        IAtom conAtom = connectedAtoms[k];
                        BondStereo stereo = container.GetBond(a, conAtom).Stereo;
                        if (stereo == BondStereo.Up)
                        {
                            up++;
                        }
                        else if (stereo == BondStereo.Down)
                        {
                            down++;
                        }
                        else if (stereo == BondStereo.None && conAtom.AtomicNumber.Equals(AtomicNumbers.H))
                        {
                            h = conAtom;
                            hs++;
                        }
                        else
                        {
                            h = null;
                        }
                    }
                    if (up == 0 && down == 1 && h != null && hs == 1)
                    {
                        container.GetBond(a, h).Stereo = BondStereo.Up;
                    }
                    if (up == 1 && down == 0 && h != null && hs == 1)
                    {
                        container.GetBond(a, h).Stereo = BondStereo.Down;
                    }
                }
            }
        }
    }
}
