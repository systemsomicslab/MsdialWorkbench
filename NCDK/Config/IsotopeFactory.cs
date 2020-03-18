/*  Copyright (C) 2001-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                     2013  Egon Willighagen <egonw@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
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
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Config
{
    /// <summary>
    /// Used to store and return data of a particular isotope.
    /// </summary>
    // @cdk.module core
    // @author         steinbeck
    // @cdk.created    2001-08-29
    public abstract class IsotopeFactory
    {
        private readonly List<IIsotope>[] isotopes = new List<IIsotope>[256];
        private readonly IIsotope[] majorIsotope = new IIsotope[256];

        /// <summary>
        /// The number of isotopes defined by this class. The classes
        /// <see cref="Isotopes"/> extends this class and is to be used to get isotope
        /// information.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var isotope in isotopes)
                    if (isotope != null)
                        count += isotope.Count;
                return count;
            }
        }

        /// <summary>
        /// Protected methods only to be used by classes extending this class to add an <see cref="IIsotope"/>.
        /// </summary>
        /// <param name="isotope"></param>
        protected void Add(IIsotope isotope)
        {
            var atomicNum = isotope.AtomicNumber;
            var isotopesForElement = isotopes[atomicNum];
            if (isotopesForElement == null)
            {
                isotopesForElement = new List<IIsotope>();
                isotopes[atomicNum] = isotopesForElement;
            }
            isotopesForElement.Add(isotope);
        }

        /// <summary>
        /// Gets an array of all isotopes known to the IsotopeFactory for the given
        /// element symbol.
        /// </summary>
        /// <param name="elem">atomic number</param>
        /// <returns>An array of isotopes that matches the given element symbol</returns>
        public virtual IEnumerable<IIsotope> GetIsotopes(int elem)
        {
            if (isotopes[elem] == null)
                return Array.Empty<IIsotope>();
            return isotopes[elem];
        }

        /// <summary>
        /// Gets all isotopes known to the IsotopeFactory for the given element symbol.
        /// </summary>
        /// <param name="symbol">An element symbol to search for</param>
        /// <returns><see cref="IIsotope"/>s that matches the given element symbol</returns>
        public virtual IEnumerable<IIsotope> GetIsotopes(string symbol)
        {
            return GetIsotopes(ChemicalElement.OfSymbol(symbol).AtomicNumber);
        }

        /// <summary>
        /// Gets all isotopes known to the <see cref="IsotopeFactory"/>.
        /// </summary>
        /// <returns>All isotopes</returns>
        public virtual IEnumerable<IIsotope> GetIsotopes()
        {
            foreach (var isotopes in this.isotopes)
            {
                if (isotopes == null)
                    continue;
                foreach (var isotope in isotopes)
                {
                    yield return Clone(isotope);
                }
            }
            yield break;
        }

        /// <summary>
        /// Gets an array of all isotopes matching the searched exact mass within a certain difference.
        /// </summary>
        /// <param name="exactMass">search mass</param>
        /// <param name="difference">mass the isotope is allowed to differ from the search mass</param>
        /// <returns>All isotopes</returns>
        public virtual IEnumerable<IIsotope> GetIsotopes(double exactMass, double difference)
        {
            foreach (var isotopes in this.isotopes)
            {
                if (isotopes == null) continue;
                foreach (var isotope in isotopes)
                {
                    if (Math.Abs(isotope.ExactMass.Value - exactMass) <= difference)
                    {
                        yield return Clone(isotope);
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// Get isotope based on element symbol and mass number.
        /// </summary>
        /// <param name="symbol">the element symbol</param>
        /// <param name="massNumber">the mass number</param>
        /// <returns>the corresponding isotope</returns>
        public virtual IIsotope GetIsotope(string symbol, int massNumber)
        {
            int elem = ChemicalElement.OfSymbol(symbol).AtomicNumber;
            var isotopes = this.isotopes[elem];
            if (isotopes == null)
                return null;
            foreach (var isotope in isotopes)
            {
                if (isotope.Symbol.Equals(symbol, StringComparison.Ordinal) && isotope.MassNumber == massNumber)
                {
                    return Clone(isotope);
                }
            }
            return null;
        }

        /// <summary>
        /// Get an isotope based on the element symbol and exact mass.
        /// </summary>
        /// <param name="symbol">the element symbol</param>
        /// <param name="exactMass">the mass number</param>
        /// <param name="tolerance">allowed difference from provided exact mass</param>
        /// <returns>the corresponding isotope</returns>
        public virtual IIsotope GetIsotope(string symbol, double exactMass, double tolerance)
        {
            IIsotope ret = null;
            double minDiff = double.MaxValue;
            int elem = ChemicalElement.OfSymbol(symbol).AtomicNumber;
            var isotopes = this.isotopes[elem];
            if (isotopes == null)
                return null;
            foreach (var isotope in isotopes)
            {
                var diff = Math.Abs(isotope.ExactMass.Value - exactMass);
                if (isotope.Symbol.Equals(symbol, StringComparison.Ordinal) && diff <= tolerance && diff < minDiff)
                {
                    ret = Clone(isotope);
                    minDiff = diff;
                }
            }
            return ret;
        }

        /// <summary>
        /// Returns the most abundant (major) isotope with a given atomic number.
        /// </summary>
        /// <remarks>
        /// The isotope's abundance is for atoms with atomic number 60 and smaller
        /// defined as a number that is proportional to the 100 of the most abundant
        /// isotope. For atoms with higher atomic numbers, the abundance is defined
        /// as a percentage.
        /// </remarks>
        /// <param name="elem">The atomicNumber for which an isotope is to be returned</param>
        /// <returns>The isotope corresponding to the given atomic number</returns>
        /// <seealso cref="GetMajorIsotope(string)"/>
        public IIsotope GetMajorIsotope(int elem)
        {
            IIsotope major = null;
            if (this.majorIsotope[elem] != null)
            {
                return Clone(this.majorIsotope[elem]);
            }
            var isotopes = this.isotopes[elem];
            if (isotopes != null)
            {
                foreach (var isotope in isotopes)
                {
                    if (isotope.Abundance <= 0)
                        continue;
                    if (major == null ||
                        isotope.Abundance > major.Abundance)
                    {
                        major = isotope;
                    }
                }
                if (major != null)
                    this.majorIsotope[elem] = major;
                else
                    Trace.TraceError($"Could not find major isotope for: {elem}");
            }
            return Clone(major);
        }

        /// <summary>
        /// Get the mass of the most abundant (major) isotope, if there is no
        /// major isotopes 0 is returned.
        /// </summary>
        /// <param name="elem">the atomic number</param>
        /// <returns>the major isotope mass</returns>
        public double GetMajorIsotopeMass(int elem)
        {
            if (this.majorIsotope[elem] != null)
                return this.majorIsotope[elem].ExactMass.Value;
            var major = GetMajorIsotope(elem);
            return major != null ? major.ExactMass.Value : 0;
        }

         /// <summary>
         /// Get the exact mass of a specified isotope for an atom.
         /// </summary>
         /// <param name="atomicNumber">atomic number</param>
         /// <param name="massNumber">the mass number</param>
         /// <returns>the exact mass</returns>
        public double GetExactMass(int atomicNumber, int massNumber)
        {
            foreach (var isotope in this.isotopes[atomicNumber])
            {
                if (isotope.MassNumber.Equals(massNumber))
                    return isotope.ExactMass.Value;
            }
            return 0;
        }

        private static IIsotope Clone(IIsotope isotope)
        {
            if (isotope == null)
                return null;
            return (IIsotope)isotope.Clone();
        }

        /// <summary>
        /// Checks whether the given element exists.
        /// </summary>
        /// <param name="elementName">The element name to test</param>
        /// <returns><see langword="true"/> is the element exists, <see langword="false"/> otherwise</returns>
        public virtual bool IsElement(string elementName)
        {
            return GetElement(elementName) != null;
        }

        /// <summary>
        /// Returns the most abundant (major) isotope whose symbol equals element.
        /// </summary>
        /// <param name="symbol">the symbol of the element in question</param>
        /// <returns>The Major Isotope value</returns>
        public virtual IIsotope GetMajorIsotope(string symbol)
        {
            return GetMajorIsotope(ChemicalElement.OfSymbol(symbol).AtomicNumber);
        }

        /// <summary>
        /// Returns an <see cref="ChemicalElement"/> with a given element symbol.
        /// </summary>
        /// <param name="symbol">The element symbol for the requested element</param>
        /// <returns>The configured element</returns>
        public virtual ChemicalElement GetElement(string symbol)
        {
            return GetMajorIsotope(symbol)?.Element;
        }

        /// <summary>
        /// Returns an element according to a given atomic number.
        /// </summary>
        /// <param name="atomicNumber">The elements atomic number</param>
        /// <returns>The <see cref="ChemicalElement"/> of the given atomic number</returns>
        public virtual ChemicalElement GetElement(int atomicNumber)
        {
            return GetMajorIsotope(atomicNumber)?.Element;
        }

        /// <summary>
        /// Returns the symbol matching the element with the given atomic number.
        /// </summary>
        /// <param name="atomicNumber">The elements atomic number</param>
        /// <returns>The symbol of the given atomic number</returns>
        public string GetElementSymbol(int atomicNumber)
        {
            var isotope = GetMajorIsotope(atomicNumber);
            return isotope.Symbol;
        }
        
        /// <summary>
        /// Configures an atom. Finds the correct element type
        /// by looking at the atoms element symbol.
        /// </summary>
        /// <param name="atom">The atom to be configured</param>
        /// <returns>The configured atom</returns>
        /// <exception cref="ArgumentException">If the element symbol is not recognised</exception>
        public virtual IAtom Configure(IAtom atom)
        {
            IIsotope isotope;
            if (atom.MassNumber == null)
                return atom;
            else
                isotope = GetIsotope(atom.Symbol, atom.MassNumber.Value);
            if (isotope == null)
                throw new ArgumentException($"Cannot configure an unrecognized element/mass: {atom.MassNumber} {atom}");
            return Configure(atom, isotope);
        }

        /// <summary>
        /// Configures an atom to have all the data of the given isotope.
        /// </summary>
        /// <param name="atom">The atom to be configure</param>
        /// <param name="isotope">The isotope to read the data from</param>
        /// <returns>The configured atom</returns>
        public virtual IAtom Configure(IAtom atom, IIsotope isotope)
        {
            atom.MassNumber = isotope.MassNumber;
            atom.Symbol = isotope.Symbol;
            atom.ExactMass = isotope.ExactMass;
            atom.AtomicNumber = isotope.AtomicNumber;
            atom.Abundance = isotope.Abundance;
            return atom;
        }

        /// <summary>
        /// Configures atoms in an <see cref="IAtomContainer"/> to carry all the correct data according to their element type.
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> to be configured</param>
        public virtual void ConfigureAtoms(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
                Configure(atom);
        }

        /// <summary>
        /// Gets the natural mass of this element, defined as average of masses of isotopes, weighted by abundance.
        /// </summary>
        /// <param name="atomicNum">the element in question</param>
        /// <returns>The natural mass value</returns>
        public double GetNaturalMass(int atomicNum)
        {
            var isotopes = this.isotopes[atomicNum];
            if (isotopes == null)
                return 0;
            double summedAbundances = 0;
            double summedWeightedAbundances = 0;
            double getNaturalMass = 0;
            foreach (var isotope in isotopes)
            {
                summedAbundances += isotope.Abundance.Value;
                summedWeightedAbundances += isotope.Abundance.Value * isotope.ExactMass.Value;
                getNaturalMass = summedWeightedAbundances / summedAbundances;
            }
            return getNaturalMass;
        }

        /// <summary>
        /// Gets the natural mass of this element, defined as average of masses of isotopes, weighted by abundance.
        /// </summary>
        /// <param name="element">the element in question</param>
        /// <returns>The natural mass value</returns>
        public virtual double GetNaturalMass(ChemicalElement element)
        {
            return GetNaturalMass(element.AtomicNumber);
        }
    }
}
