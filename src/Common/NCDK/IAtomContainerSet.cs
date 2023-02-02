/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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

using System.Collections.Generic;

namespace NCDK
{
    public interface IAtomContainerSet
        : IChemObjectSet<IAtomContainer>
    { }

    public interface IEnumerableChemObject<out T>
        : IEnumerable<T>, IChemObject where T : IChemObject
    {
    }

    /// <summary>
    /// A generic set of <see cref="IAtomContainer"/>s.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    // @author     egonw
    // @cdk.module interfaces
    public interface IChemObjectSet<T>
        : IChemObject, IList<T>, IEnumerableChemObject<T>
        where T : IChemObject
    {
        /// <summary>
        /// Sets the coefficient of a <see cref="IAtomContainer"/> to a given value.
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> for which the multiplier is set</param>
        /// <param name="multiplier">The new multiplier for the <see cref="IAtomContainer"/></param>
        /// <seealso cref="GetMultiplier(T)"/>
        void SetMultiplier(T container, double? multiplier);

        /// <summary>
        /// Adds an <paramref name="atomContainer"/> to this container with the given multiplier.
        /// </summary>
        /// <param name="atomContainer">The <paramref name="atomContainer"/> to be added to this container</param>
        /// <param name="multiplier">The multiplier of this atomContainer</param>
        void Add(T atomContainer, double? multiplier);

        /// <summary>
        /// Adds all <see cref="IAtomContainer"/> in the <paramref name="atomContainerSet"/> to this container.
        /// </summary>
        /// <param name="atomContainerSet"><see cref="IAtomContainer"/>s to add.</param>
        void AddRange(IEnumerable<T> atomContainerSet);

        /// <summary>
        /// Returns the multiplier of the given <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> for which the multiplier is given</param>
        /// <returns>-1, if the given molecule is not a container in this set</returns>
        double? GetMultiplier(T container);

        /// <summary>
        /// Sort the <see cref="IAtomContainer"/>s using a provided <see cref="IComparer{T}"/> .
        /// </summary>
        /// <param name="comparator">defines the sorting method</param>
        void Sort(IComparer<T> comparator);

        /// <summary>
        /// Sets the coefficient of a <see cref="IAtomContainer"/> to a given value.
        /// </summary>
        /// <param name="position">The position of the <see cref="IAtomContainer"/> for which the multiplier is set in [0,..]</param>
        /// <param name="multiplier">The new multiplier for the <see cref="IAtomContainer"/> at <paramref name="position"/></param>
        /// <seealso cref="GetMultiplier(int)"/>
        void SetMultiplier(int position, double? multiplier);

        /// <summary>
        /// Returns an array of double with the stoichiometric coefficients
        /// of the products.
        /// </summary>
        /// <returns>The multipliers for the AtomContainer's in this set</returns>
        /// <seealso cref="SetMultipliers(IEnumerable{double?})"/>
        IReadOnlyList<double?> GetMultipliers();

        /// <summary>
        /// Sets the multipliers of the <see cref="IAtomContainer"/>s.
        /// </summary>
        /// <param name="multipliers">The new multipliers for the <see cref="IAtomContainer"/>s in this set</param>
        /// <returns>true if multipliers have been set.</returns>
        /// <seealso cref="GetMultipliers()"/>
        bool SetMultipliers(IEnumerable<double?> multipliers);

        /// <summary>
        /// Returns the multiplier of the given AtomContainer.
        /// </summary>
        /// <param name="number">The <see cref="IAtomContainer"/> for which the multiplier is given</param>
        /// <returns>-1, if the given molecule is not a container in this set</returns>
        /// <seealso cref="SetMultiplier(int, double?)"/>
        double? GetMultiplier(int number);

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="IAtomContainerSet"/> is empty.
        /// </summary>
        /// <returns>a boolean indicating if this set contains no atom containers</returns>
        bool IsEmpty();
    }
}
