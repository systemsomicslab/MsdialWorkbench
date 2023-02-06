/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

using System;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Class with utilities for the <see cref="IAtomType"/> class.
    /// </summary>
    // @author     mfe4
    // @author     egonw
    // @cdk.module standard
    public static class AtomTypeManipulator
    {
        /// <summary>
        /// Method that assign properties to an atom given a particular atomType.
        /// An <see cref="ArgumentException"/> is thrown if the given <see cref="IAtomType"/>
        /// is null. <b>This method overwrites non-null values.</b>
        /// </summary>
        /// <param name="atom">Atom to configure</param>
        /// <param name="atomType">AtomType. Must not be null.</param>
        public static void Configure(IAtom atom, IAtomType atomType)
        {
            if (atomType == null)
                throw new ArgumentNullException(nameof(atomType));

            if (string.Equals("X", atomType.AtomTypeName, StringComparison.Ordinal))
            {
                atom.AtomTypeName = "X";
                return;
            }

            // we set the atom type name, but nothing else
            atom.AtomTypeName = atomType.AtomTypeName;

            // configuring atom type information is not really valid
            // for pseudo atoms - first because they basically have no
            // type information and second because they may have information
            // associated with them from another context, which should not be
            // overwritten. So we only do the stuff below if we have a non pseudoatom
            //
            // a side effect of this is that it is probably not valid to get the atom
            // type of a pseudo atom. I think this is OK, since you can always check
            // whether an atom is a pseudo atom without looking at its atom type
            if (!(atom is IPseudoAtom))
            {
                atom.Symbol = atomType.Symbol;
                atom.MaxBondOrder = atomType.MaxBondOrder;
                atom.BondOrderSum = atomType.BondOrderSum;
                atom.CovalentRadius = atomType.CovalentRadius;
                atom.Valency = atomType.Valency;
                atom.FormalCharge = atomType.FormalCharge;
                atom.Hybridization = atomType.Hybridization;
                atom.FormalNeighbourCount = atomType.FormalNeighbourCount;
                atom.IsHydrogenBondAcceptor = atomType.IsHydrogenBondAcceptor;
                atom.IsHydrogenBondDonor = atomType.IsHydrogenBondDonor;
                var constant = atomType.GetProperty<int?>(CDKPropertyName.ChemicalGroupConstant);
                if (constant != null)
                {
                    atom.SetProperty(CDKPropertyName.ChemicalGroupConstant, constant);
                }
                if (atomType.IsAromatic)
                    atom.IsAromatic = atomType.IsAromatic;

                object color = atomType.GetProperty<object>(CDKPropertyName.Color);
                if (color != null)
                {
                    atom.SetProperty(CDKPropertyName.Color, color);
                }
                atom.AtomicNumber = atomType.AtomicNumber;
                if (atomType.ExactMass != null)
                    atom.ExactMass = atomType.ExactMass;
            }
        }

        /// <summary>
        /// Method that assign properties to an atom given a particular atomType.
        /// <b>This method only sets null values.</b>
        /// </summary>
        /// <param name="atom">Atom to configure</param>
        /// <param name="atomType">AtomType. Must not be null.</param>
        /// <exception cref="ArgumentException">if the given <see cref="IAtomType"/> is null.</exception>
        public static void ConfigureUnsetProperties(IAtom atom, IAtomType atomType)
        {
            if (atomType == null)
            {
                throw new ArgumentException("The IAtomType was null.");
            }
            if (string.Equals("X", atomType.AtomTypeName, StringComparison.Ordinal))
            {
                if (atom.AtomTypeName == null)
                    atom.AtomTypeName = "X";
                return;
            }

            if (atom.Symbol == null && atomType.Symbol != null)
                atom.Symbol = atomType.Symbol;
            if (atom.AtomTypeName == null && atomType.AtomTypeName != null)
                atom.AtomTypeName = atomType.AtomTypeName;
            if (atom.MaxBondOrder.IsUnset() && !atomType.MaxBondOrder.IsUnset())
                atom.MaxBondOrder = atomType.MaxBondOrder;
            if (atom.BondOrderSum == null && atomType.BondOrderSum != null)
                atom.BondOrderSum = atomType.BondOrderSum;
            if (atom.CovalentRadius == null && atomType.CovalentRadius != null)
                atom.CovalentRadius = atomType.CovalentRadius;
            if (atom.Valency == null && atomType.Valency != null)
                atom.Valency = atomType.Valency;
            if (atom.FormalCharge == null && atomType.FormalCharge != null)
                atom.FormalCharge = atomType.FormalCharge;
            if (atom.Hybridization.IsUnset() && !atomType.Hybridization.IsUnset())
                atom.Hybridization = atomType.Hybridization;
            if (atom.FormalNeighbourCount == null
                    && atomType.FormalNeighbourCount != null)
                atom.FormalNeighbourCount = atomType.FormalNeighbourCount;
            if (atom.AtomicNumber == 0 && atomType.AtomicNumber != 0)
                atom.AtomicNumber = atomType.AtomicNumber;
            if (atom.ExactMass == null && atomType.ExactMass != null)
                atom.ExactMass = atomType.ExactMass;
        }
    }
}
