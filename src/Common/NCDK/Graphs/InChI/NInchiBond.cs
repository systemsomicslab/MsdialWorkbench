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

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Encapsulates properties of InChI Bond.  See <tt>inchi_api.h</tt>.
    /// </summary>
    // @author Sam Adams
    internal class NInchiBond
    {
        /// <summary>
        /// Origin atom in bond.
        /// </summary>
        public NInchiAtom OriginAtom { get; set; }

        /// <summary>
        /// Target atom in bond.
        /// </summary>
        public NInchiAtom TargetAtom { get; set; }

        /// <summary>
        /// Bond type.
        /// </summary>
        public INCHI_BOND_TYPE BondType { get; set; } = INCHI_BOND_TYPE.None;

        /// <summary>
        /// Bond 2D stereo definition.
        /// </summary>
        public INCHI_BOND_STEREO BondStereo { get; set; } = INCHI_BOND_STEREO.None;

        /// <summary>
        /// Create bond.
        ///
        /// <param name="atO">Origin atom</param>
        /// <param name="atT">Target atom</param>
        /// <param name="type">Bond type</param>
        /// <param name="stereo">Bond 2D stereo definition</param>
        /// </summary>
        public NInchiBond(NInchiAtom atO, NInchiAtom atT,
                INCHI_BOND_TYPE type, INCHI_BOND_STEREO stereo)
        {
            this.OriginAtom = atO;
            this.TargetAtom = atT;
            this.BondType = type;
            this.BondStereo = stereo;
        }


        NInchiBond(NInchiAtom atO, NInchiAtom atT,
                int type, int stereo)

            : this(atO, atT, (INCHI_BOND_TYPE)type, (INCHI_BOND_STEREO)stereo)
        {
        }

        /// <summary>
        /// Create bond.
        ///
        /// <param name="atO">Origin atom</param>
        /// <param name="atT">Target atom</param>
        /// <param name="type">Bond type</param>
        /// </summary>
        public NInchiBond(NInchiAtom atO, NInchiAtom atT,
                INCHI_BOND_TYPE type)

            : this(atO, atT, type, INCHI_BOND_STEREO.None)
        { }

        /// <summary>
        /// Generates string representation of information on bond,
        /// for debugging purposes.
        /// </summary>
        public string ToDebugString()
        {
            return ("InChI Bond: "
            + OriginAtom.ElementType
            + "-" + TargetAtom.ElementType
            + " // Type: " + BondType
            + " // Stereo: " + BondStereo
            );
        }

        /// <summary>
        /// Outputs information on bond, for debugging purposes.
        /// </summary>
        public void PrintDebug()
        {
            Console.Out.WriteLine(ToDebugString());
        }
    }
}
