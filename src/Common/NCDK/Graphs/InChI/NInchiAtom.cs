/*
 * Copyright 2006-2011 Sam Adams <sea36 at users.sourceforge.net>
 *
 * This file is part of JNI-InChI.
 *
 * JNI-InChI is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * JNI-InChI is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with JNI-InChI.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Encapsulates properties of InChI Atom.  See <tt>inchi_api.h</tt>.
    /// </summary>
    // @author Sam Adams
    internal class NInchiAtom
    {
        /// <summary>
        /// Indicates relative rather than absolute isotopic mass. Value
        /// from inchi_api.h.
        /// </summary>
        protected internal const int ISOTOPIC_SHIFT_FLAG = 10000;

        /// <summary>
        /// Atom x-coordinate.
        /// </summary>
        public double X { get; private set; }

        /// <summary>
        /// Atom y-coordinate.
        /// </summary>
        public double Y { get; private set; }

        /// <summary>
        /// Atom z-coordinate.
        /// </summary>
        public double Z { get; private set; }

        /// <summary>
        /// Chemical element symbol eg C, O, Fe, Hg.
        /// </summary>
        public string ElementType { get; private set; }

        /// <summary>
        /// Number of implicit hydrogens on atom. If set to -1, InChI will add
        /// implicit H automatically.
        /// </summary>
        public int ImplicitH { get; set; } = -1;

        /// <summary>
        /// Number of implicit protiums (isotopic 1-H) on atom.
        /// </summary>
        public int ImplicitProtium { get; set; } = 0;

        /// <summary>
        /// Number of implicit deuteriums (isotopic 2-H) on atom.
        /// </summary>
        public int ImplicitDeuterium { get; set; } = 0;

        /// <summary>
        /// Number of implicit tritiums (isotopic 3-H) on atom.
        /// </summary>
        public int ImplicitTritium { get; set; } = 0;

        /// <summary>
        /// Mass of isotope. If set to 0, no isotopic mass set; otherwise, isotopic
        /// mass, or ISOTOPIC_SHIFT_FLAG + (mass - average atomic mass).
        /// </summary>
        public int IsotopicMass { get; set; } = 0;

        /// <summary>
        /// Radical status of atom.
        /// </summary>
        public INCHI_RADICAL Radical { get; set; } = INCHI_RADICAL.None;

        /// <summary>
        /// Charge on atom.
        /// </summary>
        public int Charge { get; set; } = 0;

        /// <summary>
        /// Create new atom.
        /// </summary>
        /// <remarks>
        /// Coordinates and element symbol must be set (unknown
        /// coordinates/dimensions should be set to zero).  All other
        /// parameters are initialised to default values:
        ///  <list type="bullet">
        ///    <item>Num Implicit H = 0</item>
        ///    <item>Num Implicit 1H = 0</item>
        ///    <item>Num Implicit 2H = 0</item>
        ///    <item>Num Implicit 3H = 0</item>
        ///    <item>Isotopic mass = 0 (non isotopic)</item>
        ///    <item>Radical status = None  (radical status not defined)</item>
        /// </list>
        /// </remarks>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <param name="z">z-coordinate</param>
        /// <param name="el">Chemical element symbol</param>
        /// <exception cref="ArgumentNullException">if the element symbol is null.</exception>
        public NInchiAtom(double x, double y, double z, string el)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.ElementType = el ?? throw new ArgumentNullException(nameof(el), "Chemical element must not be null");
        }

        /// <summary>
        /// Convenience method to create a new atom with zero coordinates.
        /// <param name="el"></param>
        /// </summary>
        public NInchiAtom(string el)
            : this(0.0, 0.0, 0.0, el)
        { }

        /// <summary>
        /// Sets isotopic mass, relative to standard mass.
        /// </summary>
        /// <param name="shift">Isotopic mass minus average atomic mass</param>
        public void SetIsotopicMassShift(int shift)
        {
            this.IsotopicMass = ISOTOPIC_SHIFT_FLAG + shift;
        }

        /// <summary>
        /// Generates string representation of information on atom,
        /// for debugging purposes.
        /// </summary>
        public string ToDebugString()
        {
            return "InChI Atom: "
                + ElementType
                + " [" + ToString(X) + "," + ToString(Y) + "," + ToString(Z) + "] "
                + "Charge:" + Charge + " // "
                + "Iso Mass:" + IsotopicMass + " // "
                + "Implicit H:" + ImplicitH
                + " P:" + ImplicitProtium
                + " D:" + ImplicitDeuterium
                + " T:" + ImplicitTritium
                + " // Radical: " + Radical;
        }

        /// <summary>
        /// Java compatible <see cref="string.ToString()"/>
        /// </summary>
        private static string ToString(double x)
        {
            var s = x.ToString(NumberFormatInfo.InvariantInfo);
            return s.Contains(".") ? s : s + ".0";
        }

        /// <summary>
        /// Outputs information on atom, for debugging purposes.
        /// </summary>
        public void PrintDebug()
        {
            Console.Out.WriteLine(ToDebugString());
        }
    }
}
