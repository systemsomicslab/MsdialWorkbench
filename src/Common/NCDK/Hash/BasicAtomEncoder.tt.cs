



/*
 * Copyright (c) 2013 John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */
 
using System;
using System.Linq;

namespace NCDK.Hash
{
    /// <summary>
    /// Enumeration of atom encoders for seeding atomic hash codes. Generally these
    /// encoders return the direct value or a prime number if that value is null.
    /// These encoders are considered <i>basic</i> as the values generated are all in
    /// the same range. Better encoding can be achieved by assigning discrete values
    /// a section of the prime number table. However, In practice using a
    /// pseudorandom number generator to distribute the encoded values provides a
    /// good distribution.
    /// </summary>
    /// <seealso href="http://www.bigprimes.net/archive/prime/">Prime numbers archive</seealso>
    /// <seealso cref="ConjugatedAtomEncoder"/>
    // @author John May
    // @cdk.module hash
    public
    partial class BasicAtomEncoder : System.IComparable<BasicAtomEncoder>, System.IComparable
			 , IAtomEncoder 
    {
		/// <summary>
		/// The <see cref="Ordinal"/> values of <see cref="BasicAtomEncoder"/>.
		/// </summary>
		/// <seealso cref="BasicAtomEncoder"/>
        internal static partial class O
        {
            public const int AtomicNumber = 0;
            public const int MassNumber = 1;
            public const int FormalCharge = 2;
            public const int NConnectedAtoms = 3;
            public const int BondOrderSum = 4;
            public const int OrbitalHybridization = 5;
            public const int FreeRadicals = 6;
          
        }

        private readonly int ordinal;
		/// <summary>
		/// The ordinal of this enumeration constant. The list is in <see cref="O"/>.
		/// </summary>
		/// <seealso cref="O"/>
        public int Ordinal => ordinal;

		/// <inheritdoc/>
        public override string ToString()
        {
            return names[Ordinal];
        }

        private static readonly string[] names = new string[] 
        {
            "AtomicNumber", 
            "MassNumber", 
            "FormalCharge", 
            "NConnectedAtoms", 
            "BondOrderSum", 
            "OrbitalHybridization", 
            "FreeRadicals", 
         
        };

        private BasicAtomEncoder(int ordinal)
        {
            this.ordinal = ordinal;
        }

        public static explicit operator BasicAtomEncoder(int ordinal)
        {
            return ToBasicAtomEncoder(ordinal);
        }

        public static BasicAtomEncoder ToBasicAtomEncoder(int ordinal)
        {
            if (!(0 <= ordinal && ordinal < values.Length))
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            return values[ordinal];
        }

		public static explicit operator int(BasicAtomEncoder o)
        {
            return ToInt32(o);
        }

        public static int ToInt32(BasicAtomEncoder o)
        {
            return o.Ordinal;
        }

        /// <summary>
        /// Encode the atomic number of an atom.
        /// </summary>
        public static readonly BasicAtomEncoder AtomicNumber = new BasicAtomEncoder(0);
        /// <summary>
        /// Encode the mass number of an atom, allowing distinction of isotopes.
        /// </summary>
        public static readonly BasicAtomEncoder MassNumber = new BasicAtomEncoder(1);
        /// <summary>
        /// Encode the formal charge of an atom, allowing distinction of different protonation states.
        /// </summary>
        public static readonly BasicAtomEncoder FormalCharge = new BasicAtomEncoder(2);
        /// <summary>
        /// Encode the number of explicitly connected atoms (degree).
        /// </summary>
        public static readonly BasicAtomEncoder NConnectedAtoms = new BasicAtomEncoder(3);
        /// <summary>
        /// Encode the explicit bond order sum of an atom.
        /// </summary>
        public static readonly BasicAtomEncoder BondOrderSum = new BasicAtomEncoder(4);
        /// <summary>
        /// Encode the orbital hybridization of an atom.
        /// </summary>
        public static readonly BasicAtomEncoder OrbitalHybridization = new BasicAtomEncoder(5);
        public static readonly BasicAtomEncoder FreeRadicals = new BasicAtomEncoder(6);
        private static readonly BasicAtomEncoder[] values = new BasicAtomEncoder[]
        {
            AtomicNumber, 
            MassNumber, 
            FormalCharge, 
            NConnectedAtoms, 
            BondOrderSum, 
            OrbitalHybridization, 
            FreeRadicals, 
    
        };
        public static System.Collections.Generic.IReadOnlyList<BasicAtomEncoder> Values => values;


        public static bool operator==(BasicAtomEncoder a, BasicAtomEncoder b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            
            return a.Ordinal == b.Ordinal;
        }

        public static bool operator !=(BasicAtomEncoder a, BasicAtomEncoder b)
        {
            return !(a == b);
        }

		/// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var o = obj as BasicAtomEncoder;
            return this.Ordinal == o.Ordinal;
        }

		/// <inheritdoc/>
        public override int GetHashCode()
        {
            return Ordinal;
        }

		/// <inheritdoc/>
        public int CompareTo(object obj)
        {
            var o = (BasicAtomEncoder)obj;
            return ((int)Ordinal).CompareTo((int)o.Ordinal);
        }   

		/// <inheritdoc/>
        public int CompareTo(BasicAtomEncoder o)
        {
            return (Ordinal).CompareTo(o.Ordinal);
        }   	
        private delegate int EncodeDelegate(IAtom atom, IAtomContainer container);
        private static EncodeDelegate[] listOnEncode = MakeListOnEncode();

        public int Encode(IAtom atom, IAtomContainer container)
        {
            return listOnEncode[Ordinal](atom, container);
        }

        private static EncodeDelegate[] MakeListOnEncode()
        {
            var listOnEncode = new EncodeDelegate[values.Length];
            listOnEncode[O.AtomicNumber] = (atom, container) => atom.AtomicNumber;
            listOnEncode[O.MassNumber] = (atom, container) => atom.MassNumber ?? 32451179;
            listOnEncode[O.FormalCharge] = (atom, container) => atom.FormalCharge ?? 32451193;
            listOnEncode[O.NConnectedAtoms] = (atom, container) => container.GetConnectedBonds(atom).Count();
            listOnEncode[O.BondOrderSum] = (atom, container) => container.GetBondOrderSum(atom).GetHashCode(); // Fixed CDK's bug?? HashCode() removed.
            listOnEncode[O.OrbitalHybridization] = (atom, container) =>
                {
                    var hybridization = atom.Hybridization;
                    return !hybridization.IsUnset() ? (int)hybridization : 32451301;
                };
            listOnEncode[O.FreeRadicals] = (atom, container) => container.GetConnectedSingleElectrons(atom).Count();
            return listOnEncode;
        }
	}
}
