



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

namespace NCDK.Beam
{
    /// <summary>
    /// Enumeration of atom-based relative configurations. Each value defines a
    /// configuration of a given topology.
    /// </summary>
    /// <seealso href="http://www.opensmiles.org/opensmiles.html#chirality">Chirality, OpenSMILES</seealso>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    partial class Configuration : System.IComparable<Configuration>, System.IComparable
    {
		/// <summary>
		/// The <see cref="Ordinal"/> values of <see cref="Configuration"/>.
		/// </summary>
		/// <seealso cref="Configuration"/>
        internal static partial class O
        {
            public const int Unknown = 0;
            public const int AntiClockwise = 1;
            public const int Clockwise = 2;
            public const int TH1 = 3;
            public const int TH2 = 4;
            public const int DB1 = 5;
            public const int DB2 = 6;
            public const int AL1 = 7;
            public const int AL2 = 8;
            public const int SP1 = 9;
            public const int SP2 = 10;
            public const int SP3 = 11;
            public const int TB1 = 12;
            public const int TB2 = 13;
            public const int TB3 = 14;
            public const int TB4 = 15;
            public const int TB5 = 16;
            public const int TB6 = 17;
            public const int TB7 = 18;
            public const int TB8 = 19;
            public const int TB9 = 20;
            public const int TB10 = 21;
            public const int TB11 = 22;
            public const int TB12 = 23;
            public const int TB13 = 24;
            public const int TB14 = 25;
            public const int TB15 = 26;
            public const int TB16 = 27;
            public const int TB17 = 28;
            public const int TB18 = 29;
            public const int TB19 = 30;
            public const int TB20 = 31;
            public const int OH1 = 32;
            public const int OH2 = 33;
            public const int OH3 = 34;
            public const int OH4 = 35;
            public const int OH5 = 36;
            public const int OH6 = 37;
            public const int OH7 = 38;
            public const int OH8 = 39;
            public const int OH9 = 40;
            public const int OH10 = 41;
            public const int OH11 = 42;
            public const int OH12 = 43;
            public const int OH13 = 44;
            public const int OH14 = 45;
            public const int OH15 = 46;
            public const int OH16 = 47;
            public const int OH17 = 48;
            public const int OH18 = 49;
            public const int OH19 = 50;
            public const int OH20 = 51;
            public const int OH21 = 52;
            public const int OH22 = 53;
            public const int OH23 = 54;
            public const int OH24 = 55;
            public const int OH25 = 56;
            public const int OH26 = 57;
            public const int OH27 = 58;
            public const int OH28 = 59;
            public const int OH29 = 60;
            public const int OH30 = 61;
          
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
            "Unknown", 
            "AntiClockwise", 
            "Clockwise", 
            "TH1", 
            "TH2", 
            "DB1", 
            "DB2", 
            "AL1", 
            "AL2", 
            "SP1", 
            "SP2", 
            "SP3", 
            "TB1", 
            "TB2", 
            "TB3", 
            "TB4", 
            "TB5", 
            "TB6", 
            "TB7", 
            "TB8", 
            "TB9", 
            "TB10", 
            "TB11", 
            "TB12", 
            "TB13", 
            "TB14", 
            "TB15", 
            "TB16", 
            "TB17", 
            "TB18", 
            "TB19", 
            "TB20", 
            "OH1", 
            "OH2", 
            "OH3", 
            "OH4", 
            "OH5", 
            "OH6", 
            "OH7", 
            "OH8", 
            "OH9", 
            "OH10", 
            "OH11", 
            "OH12", 
            "OH13", 
            "OH14", 
            "OH15", 
            "OH16", 
            "OH17", 
            "OH18", 
            "OH19", 
            "OH20", 
            "OH21", 
            "OH22", 
            "OH23", 
            "OH24", 
            "OH25", 
            "OH26", 
            "OH27", 
            "OH28", 
            "OH29", 
            "OH30", 
         
        };

        private Configuration(int ordinal)
        {
            this.ordinal = ordinal;
        }

        public static explicit operator Configuration(int ordinal)
        {
            return ToConfiguration(ordinal);
        }

        public static Configuration ToConfiguration(int ordinal)
        {
            if (!(0 <= ordinal && ordinal < values.Length))
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            return values[ordinal];
        }

		public static explicit operator int(Configuration o)
        {
            return ToInt32(o);
        }

        public static int ToInt32(Configuration o)
        {
            return o.Ordinal;
        }

        /// <summary>
        /// An atoms has Unknown/no configuration.
        /// </summary>
        public static readonly Configuration Unknown = new Configuration(0,ConfigurationType.None, "");
        /// <summary>
        /// Shorthand for TH1, AL1, DB1, TB1 or OH1 configurations.
        /// </summary>
        public static readonly Configuration AntiClockwise = new Configuration(1,ConfigurationType.Implicit, "@");
        /// <summary>
        /// Shorthand for TH2, AL2, DB2, TB2 or OH2 configurations.
        /// </summary>
        public static readonly Configuration Clockwise = new Configuration(2,ConfigurationType.Implicit, "@@");
        /// <summary>
        /// Tetrahedral, neighbors proceed anti-clockwise looking from the first atom.
        /// </summary>
        public static readonly Configuration TH1 = new Configuration(3,ConfigurationType.Tetrahedral, "@TH1", AntiClockwise);
        /// <summary>
        /// Tetrahedral, neighbors proceed clockwise looking from the first atom. 
        /// </summary>
        public static readonly Configuration TH2 = new Configuration(4,ConfigurationType.Tetrahedral, "@TH2", Clockwise);
        /// <summary>
        /// Atom-based double bond configuration, neighbors proceed anti-clockwise in a plane. <i>Note - this configuration is currently specific to grins.</i>
        /// </summary>
        public static readonly Configuration DB1 = new Configuration(5,ConfigurationType.DoubleBond, "@DB1", AntiClockwise);
        /// <summary>
        /// Atom-based double bond configuration, neighbors proceed clockwise in a plane.<i>Note - this configuration is currently specific to grins.</i>
        /// </summary>
        public static readonly Configuration DB2 = new Configuration(6,ConfigurationType.DoubleBond, "@DB2", Clockwise);
        public static readonly Configuration AL1 = new Configuration(7,ConfigurationType.ExtendedTetrahedral, "@AL1", AntiClockwise);
        public static readonly Configuration AL2 = new Configuration(8,ConfigurationType.ExtendedTetrahedral, "@AL2", Clockwise);
        public static readonly Configuration SP1 = new Configuration(9,ConfigurationType.SquarePlanar, "@SP1");
        public static readonly Configuration SP2 = new Configuration(10,ConfigurationType.SquarePlanar, "@SP2");
        public static readonly Configuration SP3 = new Configuration(11,ConfigurationType.SquarePlanar, "@SP3");
        public static readonly Configuration TB1 = new Configuration(12,ConfigurationType.TrigonalBipyramidal, "@TB1", AntiClockwise);
        public static readonly Configuration TB2 = new Configuration(13,ConfigurationType.TrigonalBipyramidal, "@TB2", Clockwise);
        public static readonly Configuration TB3 = new Configuration(14,ConfigurationType.TrigonalBipyramidal, "@TB3");
        public static readonly Configuration TB4 = new Configuration(15,ConfigurationType.TrigonalBipyramidal, "@TB4");
        public static readonly Configuration TB5 = new Configuration(16,ConfigurationType.TrigonalBipyramidal, "@TB5");
        public static readonly Configuration TB6 = new Configuration(17,ConfigurationType.TrigonalBipyramidal, "@TB6");
        public static readonly Configuration TB7 = new Configuration(18,ConfigurationType.TrigonalBipyramidal, "@TB7");
        public static readonly Configuration TB8 = new Configuration(19,ConfigurationType.TrigonalBipyramidal, "@TB8");
        public static readonly Configuration TB9 = new Configuration(20,ConfigurationType.TrigonalBipyramidal, "@TB9");
        public static readonly Configuration TB10 = new Configuration(21,ConfigurationType.TrigonalBipyramidal, "@TB10");
        public static readonly Configuration TB11 = new Configuration(22,ConfigurationType.TrigonalBipyramidal, "@TB11");
        public static readonly Configuration TB12 = new Configuration(23,ConfigurationType.TrigonalBipyramidal, "@TB12");
        public static readonly Configuration TB13 = new Configuration(24,ConfigurationType.TrigonalBipyramidal, "@TB13");
        public static readonly Configuration TB14 = new Configuration(25,ConfigurationType.TrigonalBipyramidal, "@TB14");
        public static readonly Configuration TB15 = new Configuration(26,ConfigurationType.TrigonalBipyramidal, "@TB15");
        public static readonly Configuration TB16 = new Configuration(27,ConfigurationType.TrigonalBipyramidal, "@TB16");
        public static readonly Configuration TB17 = new Configuration(28,ConfigurationType.TrigonalBipyramidal, "@TB17");
        public static readonly Configuration TB18 = new Configuration(29,ConfigurationType.TrigonalBipyramidal, "@TB18");
        public static readonly Configuration TB19 = new Configuration(30,ConfigurationType.TrigonalBipyramidal, "@TB19");
        public static readonly Configuration TB20 = new Configuration(31,ConfigurationType.TrigonalBipyramidal, "@TB20");
        public static readonly Configuration OH1 = new Configuration(32,ConfigurationType.Octahedral, "@OH1", AntiClockwise);
        public static readonly Configuration OH2 = new Configuration(33,ConfigurationType.Octahedral, "@OH2", Clockwise);
        public static readonly Configuration OH3 = new Configuration(34,ConfigurationType.Octahedral, "@OH3");
        public static readonly Configuration OH4 = new Configuration(35,ConfigurationType.Octahedral, "@OH4");
        public static readonly Configuration OH5 = new Configuration(36,ConfigurationType.Octahedral, "@OH5");
        public static readonly Configuration OH6 = new Configuration(37,ConfigurationType.Octahedral, "@OH6");
        public static readonly Configuration OH7 = new Configuration(38,ConfigurationType.Octahedral, "@OH7");
        public static readonly Configuration OH8 = new Configuration(39,ConfigurationType.Octahedral, "@OH8");
        public static readonly Configuration OH9 = new Configuration(40,ConfigurationType.Octahedral, "@OH9");
        public static readonly Configuration OH10 = new Configuration(41,ConfigurationType.Octahedral, "@OH10");
        public static readonly Configuration OH11 = new Configuration(42,ConfigurationType.Octahedral, "@OH11");
        public static readonly Configuration OH12 = new Configuration(43,ConfigurationType.Octahedral, "@OH12");
        public static readonly Configuration OH13 = new Configuration(44,ConfigurationType.Octahedral, "@OH13");
        public static readonly Configuration OH14 = new Configuration(45,ConfigurationType.Octahedral, "@OH14");
        public static readonly Configuration OH15 = new Configuration(46,ConfigurationType.Octahedral, "@OH15");
        public static readonly Configuration OH16 = new Configuration(47,ConfigurationType.Octahedral, "@OH16");
        public static readonly Configuration OH17 = new Configuration(48,ConfigurationType.Octahedral, "@OH17");
        public static readonly Configuration OH18 = new Configuration(49,ConfigurationType.Octahedral, "@OH18");
        public static readonly Configuration OH19 = new Configuration(50,ConfigurationType.Octahedral, "@OH19");
        public static readonly Configuration OH20 = new Configuration(51,ConfigurationType.Octahedral, "@OH20");
        public static readonly Configuration OH21 = new Configuration(52,ConfigurationType.Octahedral, "@OH21");
        public static readonly Configuration OH22 = new Configuration(53,ConfigurationType.Octahedral, "@OH22");
        public static readonly Configuration OH23 = new Configuration(54,ConfigurationType.Octahedral, "@OH23");
        public static readonly Configuration OH24 = new Configuration(55,ConfigurationType.Octahedral, "@OH24");
        public static readonly Configuration OH25 = new Configuration(56,ConfigurationType.Octahedral, "@OH25");
        public static readonly Configuration OH26 = new Configuration(57,ConfigurationType.Octahedral, "@OH26");
        public static readonly Configuration OH27 = new Configuration(58,ConfigurationType.Octahedral, "@OH27");
        public static readonly Configuration OH28 = new Configuration(59,ConfigurationType.Octahedral, "@OH28");
        public static readonly Configuration OH29 = new Configuration(60,ConfigurationType.Octahedral, "@OH29");
        public static readonly Configuration OH30 = new Configuration(61,ConfigurationType.Octahedral, "@OH30");
        private static readonly Configuration[] values = new Configuration[]
        {
            Unknown, 
            AntiClockwise, 
            Clockwise, 
            TH1, 
            TH2, 
            DB1, 
            DB2, 
            AL1, 
            AL2, 
            SP1, 
            SP2, 
            SP3, 
            TB1, 
            TB2, 
            TB3, 
            TB4, 
            TB5, 
            TB6, 
            TB7, 
            TB8, 
            TB9, 
            TB10, 
            TB11, 
            TB12, 
            TB13, 
            TB14, 
            TB15, 
            TB16, 
            TB17, 
            TB18, 
            TB19, 
            TB20, 
            OH1, 
            OH2, 
            OH3, 
            OH4, 
            OH5, 
            OH6, 
            OH7, 
            OH8, 
            OH9, 
            OH10, 
            OH11, 
            OH12, 
            OH13, 
            OH14, 
            OH15, 
            OH16, 
            OH17, 
            OH18, 
            OH19, 
            OH20, 
            OH21, 
            OH22, 
            OH23, 
            OH24, 
            OH25, 
            OH26, 
            OH27, 
            OH28, 
            OH29, 
            OH30, 
    
        };
        public static System.Collections.Generic.IReadOnlyList<Configuration> Values => values;


        public static bool operator==(Configuration a, Configuration b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            
            return a.Ordinal == b.Ordinal;
        }

        public static bool operator !=(Configuration a, Configuration b)
        {
            return !(a == b);
        }

		/// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var o = obj as Configuration;
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
            var o = (Configuration)obj;
            return ((int)Ordinal).CompareTo((int)o.Ordinal);
        }   

		/// <inheritdoc/>
        public int CompareTo(Configuration o)
        {
            return (Ordinal).CompareTo(o.Ordinal);
        }   	
        private Configuration(int ordinal, ConfigurationType type, string symbol, Configuration shorthand)
            : this(ordinal)
        {
            this.type = type;
            this.symbol = symbol;
            this.shorthand = shorthand;
        }

        private Configuration(int ordinal, ConfigurationType type, string symbol)
            : this(ordinal)
        {
            this.type = type;
            this.symbol = symbol;
            this.shorthand = this;
        }
	}
}
