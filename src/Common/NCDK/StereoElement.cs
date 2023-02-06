/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *                             2017  John Mayfield
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using System;
using System.Reflection;

namespace NCDK
{

    /// <summary>
    /// There stereo class defines the type of stereochemistry/geometry that is
    /// captured. The stereo class is also defined as a integral value. The
    /// following classes are available with varied support through out the
    /// toolkit.        
    /// </summary>
    /// <seealso cref="StereoConfigurations"/>
    [Obfuscation(ApplyToMembers = true, Exclude = true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028: Enum Storage should be Int32", Justification = "Ignored")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1717: Only FlagsAttribute enums should have plural names", Justification = "Ignored")]
    public enum StereoClass : short
    {
        Unset = 0,

        /// <summary>Geometric cis/trans (e.g. but-2-ene)</summary>
        CisTrans = 0x21,

        /// <summary>Tetrahedral (T-4) (e.g. butan-2-ol)</summary>
        Tetrahedral = 0x42,

        /// <summary>ExtendedTetrahedral a.k.a. allene (e.g. 2,3-pentadiene)</summary>
        Allenal = 0x43,

        /// <summary>ExtendedCisTrans a.k.a. cumulene (e.g. hexa-2,3,4-triene)</summary>
        Cumulene = 0x22,

        /// <summary>Atropisomeric (e.g. BiNAP)</summary>
        Atropisomeric = 0x44,

        /// <summary>Square Planar (SP-4) (e.g. cisplatin)</summary>
        SquarePlanar = 0x45,

        /// <summary>Square Pyramidal (SPY-5)</summary>
        SquarePyramidal = 0x51,

        /// <summary>Trigonal Bipyramidal (TBPY-5)</summary>
        TrigonalBipyramidal = 0x52,

        /// <summary>Octahedral (OC-6)</summary>
        Octahedral = 0x61,

        /// <summary>Pentagonal Bipyramidal (PBPY-7)</summary>
        PentagonalBipyramidal = 0x71,

        /// <summary>Hexagonal Bipyramidal (HBPY-8)</summary>
        HexagonalBipyramidal = 0x81,

        /// <summary>Heptagonal Bipyramidal (HBPY-9)</summary>
        HeptagonalBipyramidal = 0x91,
    }

    /// <summary>
    /// The configuration is stored as an integral value. Although the common
    /// geometries like tetrahedral and cis/trans bonds only have 2 possible
    /// configurations (e.g. left vs right) more complex geometries like square
    /// planar and octahedral require more to describe. For convenience the
    /// constants <see cref="Left"/> and <see cref="Right"/> are provided but are synonymous
    /// with the values 1 (odd) and 2 (even).
    /// </summary>
    /// <remarks>
    /// Special values (e.g. 0) may be used to represent unknown/unspecified or
    /// racemic in future but are currently undefined.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028: Enum Storage should be Int32", Justification = "Ignored")]
    [Obfuscation(ApplyToMembers = true, Exclude = true)]
    [Flags]
    public enum StereoConfigurations : short
    {
        Unset = 0,
        Left = 1,
        Right = 2,

        Opposite = Left,
        Together = Right,
    }

    public struct StereoElement : IEquatable<StereoElement>
    {        
        public StereoClass Class { get; private set; }
        public StereoConfigurations Configuration { get; set; }

        public StereoElement(StereoClass cls)
        {
            this.Class = cls;
            this.Configuration = StereoConfigurations.Unset;
        }

        public StereoElement(StereoConfigurations configure)
        {
            this.Class = StereoClass.Unset;
            this.Configuration = configure;
        }

        public StereoElement(StereoClass cls, StereoConfigurations configure)
        {
            this.Class = cls;
            this.Configuration = configure;
        }

        public StereoElement(StereoClass cls, int configure)
            : this(cls, (StereoConfigurations)configure)
        {
        }

        /*
         * Important! The forth nibble of the stereo-class defines the number of
         * carriers (or coordination number) the third nibble just increments when
         * there are two geometries with the same number of carriers.
         */

        public int CarrierLength => Class.GetLength();

        public static bool operator ==(StereoElement a, StereoElement b)
        {
            return a.Configuration == b.Configuration && a.Class == b.Class;
        }

        public static bool operator !=(StereoElement a, StereoElement b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StereoElement))
                return false;
            return this == (StereoElement)obj;
        }

        public override string ToString()
        {
            return Class.ToString() + " " + Configuration.ToString();
        }

        public override int GetHashCode()
        {
            return Class.GetHashCode() << 16 + Configuration.GetHashCode();
        }

        /// <summary>Square Planar Configuration in U Shape</summary>
        public static readonly StereoElement SquarePlanarU = new StereoElement(StereoClass.SquarePlanar, 1);

        /// <summary>Square Planar Configuration in 4 Shape</summary>
        public static readonly StereoElement SquarePlanar4 = new StereoElement(StereoClass.SquarePlanar, 2);

        /// <summary>Square Planar Configuration in Z Shape</summary>
        public static readonly StereoElement SquarePlanarZ = new StereoElement(StereoClass.SquarePlanar, 3);

        public bool Equals(StereoElement other)
        {
            return this == other;
        }
    }

    public static class StereoElementTools
    {
        public static int GetLength(this StereoClass value)
            => (int)(((uint)value) >> 4);

        /// <summary>
        /// The configuration order of the stereochemistry.
        /// </summary>
        public static short Order(this StereoConfigurations value)
            => (short)value;

        public static StereoConfigurations Flip(this StereoConfigurations value)
        {
            return (StereoConfigurations)((int)value ^ 0x3);
        }
    }
}
