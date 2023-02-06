/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project. 
 */

using System;

namespace NCDK.Beam
{
    /// <summary>
    /// A builder for <see cref="IAtom"/> instantiation.
    /// </summary>
    /// <example><code>
    /// // [C]
    /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
    ///                     .Build();
    /// 
    /// // [CH4]
    /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
    ///                     .NumOfHydrogens(4)
    ///                     .Build();
    /// 
    /// // [13CH4]
    /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
    ///                     .NumOfHydrogens(4)
    ///                     .Isotope(13)
    ///                     .Build();
    /// 
    /// // [CH3-]
    /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
    ///                     .NumOfHydrogens(3)
    ///                     .Charge(-1)
    ///                     .Build();
    /// 
    /// // or
    /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
    ///                     .NumOfHydrogens(3)
    ///                     .Anion
    ///                     .Build();
    /// 
    /// // [CH4:1]
    /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
    ///                     .NumOfHydrogens(4)
    ///                     .AtomClass(1)
    ///                     .Build();
    /// </code></example>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    sealed class AtomBuilder
    {
        private readonly Element element;
        private int isotope = -1,
                hCount = 0,
                charge = 0,
                atomClass = 0;
        private readonly bool aromatic;

        private AtomBuilder(Element element, bool aromatic)
        {
            this.element = element;
            this.aromatic = aromatic;
        }

        public static AtomBuilder FromExisting(IAtom a)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a), "no atom provided");
            return new AtomBuilder(a.Element, a.IsAromatic())
                    .Charge(a.Charge)
                    .NumOfHydrogens(a.NumOfHydrogens)
                    .Isotope(a.Isotope)
                    .AtomClass(a.AtomClass);
        }

        /// <summary>
        /// Start building an aliphatic atom of the given element.
        /// </summary>
        /// <code>
        /// Atom a = AtomBuilder.Aliphatic(Element.Carbon)
        ///                     .Build();
        /// </code>
        /// <param name="e">element type</param>
        /// <returns>an atom builder to configure additional properties</returns>
        /// <exception cref="ArgumentNullException">the element was null</exception>
        public static AtomBuilder Aliphatic(Element e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            return new AtomBuilder(e, false);
        }

        /// <summary>
        /// Start building an aromatic atom of the given element.
        /// </summary>
        /// <example>
        /// Atom a = AtomBuilder.Aromatic(Element.Carbon)
        /// Atom a = AtomBuilder.AromaticElement.Carbon)
        ///                     .Build();
        /// </example>
        /// <param name="e">element type</param>
        /// <returns>an atom builder to configure additional properties</returns> 
        /// <exception cref="ArgumentNullException">the element was null</exception>
        /// <exception cref="ArgumentException">the element cannot be aromatic</exception>
        public static AtomBuilder Aromatic(Element e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e), "no element provided");
            if (e == Element.Unknown)
                return new AtomBuilder(e, false);
            if (!e.IsAromatic(Element.AromaticSpecification.General))
                throw new ArgumentException("The element '" + e + "' cannot be aromatic by the Daylight specification.");
            return new AtomBuilder(e, true);
        }

        /// <summary>
        /// Start building an aliphatic atom of the given element symbol. If an
        /// element of the symbol could not be found then the element type is set to
        /// <see cref="Element.Unknown"/>.
        /// </summary>
        /// <example>
        /// Atom a = AtomBuilder.Aliphatic("C")
        ///                     .Build();
        /// </example>
        /// <param name="symbol">symbol of an element</param>
        /// <returns>an atom builder to configure additional properties</returns>
        /// <exception cref="ArgumentNullException">the element was null</exception>
        public static AtomBuilder Aliphatic(string symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol), "no symbol provided");
            return Aliphatic(OfSymbolOrUnknown(symbol));
        }

        /// <summary>
        /// Start building an aromatic atom of the given element symbol. If an
        /// element of the symbol could not be found then the element type is set to
        /// <see cref="Element.Unknown"/>.
        /// </summary>
        ///
        /// <example>
        /// Atom a = AtomBuilder.IsAromatic("C")
        ///                     .Build();
        /// </example>
        /// <param name="symbol">symbol of an element</param>
        /// <returns>an atom builder to configure additional properties</returns>
        /// <exception cref="ArgumentNullException">the element was null</exception>
        /// <exception cref="ArgumentException">the element cannot be aromatic</exception>
        public static AtomBuilder Aromatic(string symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol), "no symbol provided");
            return Aromatic(OfSymbolOrUnknown(symbol));
        }

        /// <summary>
        /// Start building an aliphatic or aromatic atom of the given element symbol.
        /// If an element of the symbol could not be found then the element type is
        /// set to <see cref="Element.Unknown"/>.
        /// </summary>
        ///
        /// <example>
        /// Atom a = AtomBuilder.Create("C") // aliphatic
        ///                     .Build();
        /// Atom a = AtomBuilder.Create("c") // aromatic
        ///                     .Build();
        /// </example>
        ///
        /// <param name="symbol">symbol of an element - lower case indicates the atom should 
        /// be aromatic</param>
        /// <returns>an atom builder to configure additional properties</returns>
        /// <exception cref="ArgumentNullException">the element was null</exception>
        /// <exception cref="ArgumentException">the element cannot be aromatic</exception>
        public static AtomBuilder Create(string symbol)
        {
            Element e = OfSymbolOrUnknown(symbol);
            if (!string.IsNullOrEmpty(symbol) && char.IsLower(symbol[0]))
            {
                if (!e.IsAromatic())
                    throw new ArgumentException("Attempting to create an aromatic atom for an element which cannot be aromatic");
                return new AtomBuilder(e, true);
            }
            return new AtomBuilder(e, false);
        }

        /// <summary>
        /// Get the element of the given symbol - if no symbol is found then the
        /// <see cref="Element.Unknown"/> is returned.
        /// </summary>
        /// <param name="symbol">an atom symbol</param>
        /// <returns>the element of the given symbol (or Unknown)</returns>
        private static Element OfSymbolOrUnknown(string symbol)
        {
            Element e = Element.OfSymbol(symbol);
            return e ?? Element.Unknown;
        }

        /// <summary>
        /// Assign the given hydrogen count to the atom which will be created.
        /// </summary>
        /// <param name="hCount">number of hydrogens</param>
        /// <returns>an atom builder to configure additional properties</returns>
        public AtomBuilder NumOfHydrogens(int hCount)
        {
            if (hCount < 0)
                throw new ArgumentOutOfRangeException(nameof(hCount), "the number of hydrogens must be positive");
            this.hCount = hCount;
            return this;
        }

        /// <summary>
        /// Assign the given formal charge to the atom which will be created.
        /// </summary>
        /// <param name="charge">formal-charge</param>
        /// <returns>an atom builder to configure additional properties</returns>
        public AtomBuilder Charge(int charge)
        {
            this.charge = charge;
            return this;
        }

        /// <summary>
        /// Assign a formal-charge of -1 to the atom which will be created.
        /// </summary>
        /// <returns>an atom builder to configure additional properties</returns>
        public AtomBuilder Anion => Charge(-1);

        /// <summary>
        /// Assign a formal-charge of +1 to the atom which will be created.
        /// </summary>
        /// <returns>an atom builder to configure additional properties</returns>
        public AtomBuilder Cation => Charge(+1);

        /// <summary>
        /// Assign the isotope number to the atom which will be created. An isotope
        /// number of '-1' means unspecified (default).
        /// </summary>
        /// <param name="isotope">isotope number ÅÜ 0.</param>
        /// <returns>an atom builder to configure additional properties</returns>
        public AtomBuilder Isotope(int isotope)
        {
            this.isotope = isotope;
            return this;
        }

        /// <summary>
        /// Assign the atom class to the atom which will be created. A class of '0'
        /// means unspecified (default).
        /// </summary>
        /// <param name="c">atom class 1..n</param>
        /// <returns>an atom builder to configure additional properties</returns>
        /// <exception cref="ArgumentOutOfRangeException">the atom class was negative</exception>
        public AtomBuilder AtomClass(int c)
        {
            if (c < 0)
                throw new ArgumentOutOfRangeException(nameof(c), "atom class must be positive");
            this.atomClass = c;
            return this;
        }

        /// <summary>
        /// Create the atom with the configured attributed.
        /// </summary>
        /// <returns>an atom</returns>
        public IAtom Build()
        {
            return new AtomImpl.BracketAtom(isotope,
                                            element,
                                            hCount,
                                            charge,
                                            atomClass,
                                            aromatic);
        }

        /// <summary>
        /// Access an atom implementation which can be used for all explicit
        /// hydrogens.
        /// </summary>
        /// <returns>an explicit hydrogen to be used in assembly molecules</returns>
        public static IAtom ExplicitHydrogen()
        {
            return AtomImpl.EXPLICIT_HYDROGEN;
        }
    }
}
