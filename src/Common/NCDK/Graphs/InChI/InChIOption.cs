/*
 * Copyright 2006-2011 Sam Adams <sea36 at users.sourceforge.net>
 *
 * This file is part of JNI-InChI.
 *
 * JNI-InChI is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation= new INCHI_OPTION("Foundation"); either version 3 of the License= new INCHI_OPTION("License"); or
 * (at your option) any later version.
 *
 * JNI-InChI is distributed in the hope that it will be useful= new INCHI_OPTION("useful");
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with JNI-InChI.  If not= new INCHI_OPTION("not"); see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Type-safe enumeration of InChI options.
    /// </summary>
    /// <remarks>See <tt>inchi_api.h</tt>.</remarks>
    // @author Sam Adams
    public class InChIOption
    {
        public string Name { get; private set; }

        private InChIOption(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Use Chiral Flag.
        /// </summary>
        public static readonly InChIOption SUCF = new InChIOption("SUCF");

        /// <summary>
        /// Set Chiral Flag.
        /// </summary>
        public static readonly InChIOption ChiralFlagON = new InChIOption("ChiralFlagON");

        /// <summary>
        /// Set Not-Chiral Flag.
        /// </summary>
        public static readonly InChIOption ChiralFlagOFF = new InChIOption("ChiralFlagOFF");

        /// <summary>
        /// Exclude stereo (Default: Include Absolute stereo).
        /// </summary>
        public static readonly InChIOption SNon = new InChIOption("SNon");

        /// <summary>
        /// Absolute stereo.
        /// </summary>
        public static readonly InChIOption SAbs = new InChIOption("SAbs");
        
        /// <summary>
        /// Relative stereo.
        /// </summary>
        public static readonly InChIOption SRel = new InChIOption("SRel");
        
        /// <summary>
        /// Racemic stereo.
        /// </summary>
        public static readonly InChIOption SRac = new InChIOption("SRac");

        /// <summary>
        /// Include omitted unknown/undefined stereo.
        /// </summary>
        public static readonly InChIOption SUU = new InChIOption("SUU");

        /// <summary>
        /// Narrow end of wedge points to stereocentre (default: both).
        /// </summary>
        public static readonly InChIOption NEWPS = new InChIOption("NEWPS");
        
        /// <summary>
        /// Include reconnected bond to metal results.
        /// </summary>
        public static readonly InChIOption RecMet = new InChIOption("RecMet");

        /// <summary>
        /// Mobile H Perception Off (Default: On).
        /// </summary>
        public static readonly InChIOption FixedH = new InChIOption("FixedH");
        
        /// <summary>
        /// Omit auxiliary information (default: Include).
        /// </summary>
        public static readonly InChIOption AuxNone = new InChIOption("AuxNone");

        /// <summary>
        /// Disable Aggressive Deprotonation (for testing only).
        /// </summary>
        public static readonly InChIOption NoADP = new InChIOption("NoADP");

        /// <summary>
        /// Compressed output.
        /// </summary>
        public static readonly InChIOption Compress = new InChIOption("Compress");

        /// <summary>
        /// Overrides inchi_Atom::num_iso_H[0] == -1.
        /// </summary>
        public static readonly InChIOption DoNotAddH = new InChIOption("DoNotAddH");
        
        /// <summary>
        /// Set time-out per structure in seconds; W0 means unlimited. In InChI
        /// library the default value is unlimited
        /// </summary>
        public static readonly InChIOption Wnumber = new InChIOption("Wnumber");

        /// <summary>
        /// Output SDfile instead of InChI.
        /// </summary>
        public static readonly InChIOption OutputSDF = new InChIOption("OutputSDF");

        /// <summary>
        /// Warn and produce empty InChI for empty structure.
        /// </summary>
        public static readonly InChIOption WarnOnEmptyStructure = new InChIOption("WarnOnEmptyStructure");
        
        /// <summary>
        /// Fix bug leading to missing or undefined sp3 parity.
        /// </summary>
        public static readonly InChIOption FixSp3Bug = new InChIOption("FixSp3Bug");

        /// <summary>
        /// Same as FixSp3Bug.
        /// </summary>
        public static readonly InChIOption FB = new InChIOption("FB");

        /// <summary>
        /// Include Phosphines Stereochemistry.
        /// </summary>
        public static readonly InChIOption SPXYZ = new InChIOption("SPXYZ");

        /// <summary>
        /// Include Arsines Stereochemistry
        /// </summary>
        public static readonly InChIOption SAsXYZ = new InChIOption("SAsXYZ");

        // -- DOESN'T WORK
        // Generate InChIKey
        // INCHI_OPTION Key= new INCHI_OPTION("Key");
        
        public static IReadOnlyList<InChIOption> Values { get; } = new[]
        {
            SUCF, ChiralFlagON, ChiralFlagOFF,
            SNon, SAbs, SRel, SRac, SUU, NEWPS, RecMet, FixedH,
            AuxNone, NoADP, Compress, DoNotAddH,
            Wnumber, OutputSDF,
            WarnOnEmptyStructure, FixSp3Bug, FB, SPXYZ, SAsXYZ
        };

        public static InChIOption ValueOfIgnoreCase(string str)
        {
            str = str.ToUpperInvariant();
            foreach (var option in Values)
            {
                if (option.Name.ToUpperInvariant() == str)
                    return option;
            }
            return null;
        }
    }
}
