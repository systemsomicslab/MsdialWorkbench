/* Copyright (C) 2002-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *               2009-2011  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Tools;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Generates a fingerprint for a given <see cref="IAtomContainer"/>. Fingerprints are
    /// one-dimensional bit arrays, where bits are set according to a the occurrence
    /// of a particular structural feature (See for example the Daylight inc. theory
    /// manual for more information). Fingerprints allow for a fast screening step to
    /// exclude candidates for a substructure search in a database. They are also a
    /// means for determining the similarity of chemical structures.
    /// </summary>
    /// <example>
    /// A fingerprint is generated for an AtomContainer with this code:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Fingerprints.HybridizationFingerprinter_Example.cs"]/*' />
    /// </example>
    /// <remarks>
    /// The FingerPrinter assumes that hydrogens are explicitly given!
    /// Furthermore, if pseudo atoms or atoms with malformed symbols are present,
    /// their atomic number is taken as one more than the last element currently
    /// supported in <see cref="PeriodicTable"/>.
    /// <para>Unlike the <see cref="Fingerprinter"/>, this fingerprinter does not take into
    /// account aromaticity. Instead, it takes into account SP2 <see cref="Hybridization"/>.
    /// </para>
    /// </remarks>
    // @cdk.keyword    fingerprint
    // @cdk.keyword    similarity
    // @cdk.module     standard
    public class HybridizationFingerprinter
        : Fingerprinter, IFingerprinter
    {
        /// <summary>
        /// Creates a fingerprint generator of length <see cref="Fingerprinter.DefaultSize"/>
        /// and with a search depth of <see cref="Fingerprinter.DefaultSearchDepth"/>.
        /// </summary>
        public HybridizationFingerprinter()
           : this(DefaultSize, DefaultSearchDepth)
        { }

        public HybridizationFingerprinter(int size)
            : this(size, DefaultSearchDepth)
        { }

        /// <summary>
        /// Constructs a fingerprint generator that creates fingerprints of
        /// the given size, using a generation algorithm with the given search
        /// depth.
        /// </summary>
        /// <param name="size">The desired size of the fingerprint</param>
        /// <param name="searchDepth">The desired depth of search</param>
        public HybridizationFingerprinter(int size, int searchDepth) : base(size, searchDepth)
        {
        }

        /// <summary>
        /// Gets the bond Symbol attribute of the Fingerprinter class.
        /// </summary>
        /// <returns>The bondSymbol value</returns>
        protected override string GetBondSymbol(IBond bond)
        {
            string bondSymbol = "";
            if (bond.Order == BondOrder.Single)
            {
                if (IsSP2Bond(bond))
                {
                    bondSymbol = ":";
                }
                else
                {
                    bondSymbol = "-";
                }
            }
            else if (bond.Order == BondOrder.Double)
            {
                if (IsSP2Bond(bond))
                {
                    bondSymbol = ":";
                }
                else
                {
                    bondSymbol = "=";
                }
            }
            else if (bond.Order == BondOrder.Triple)
            {
                bondSymbol = "#";
            }
            else if (bond.Order == BondOrder.Quadruple)
            {
                bondSymbol = "*";
            }
            return bondSymbol;
        }

        /// <summary>
        /// Returns true if the bond binds two atoms, and both atoms are SP2.
        /// </summary>
        private static bool IsSP2Bond(IBond bond)
        {
            return bond.Atoms.Count == 2 && bond.Begin.Hybridization == Hybridization.SP2
                   && bond.End.Hybridization == Hybridization.SP2;
        }
    }
}
