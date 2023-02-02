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

using System.Collections.Generic;

namespace NCDK.Graphs.InChI
{
    // @author Sam Adams
    internal class NInchiStructure
    {
        /// <summary>
        /// List of atoms.
        /// </summary>
        public IList<NInchiAtom> Atoms { get; private set; } = new List<NInchiAtom>();

        /// <summary>
        /// List of bonds.
        /// </summary>
        public IList<NInchiBond> Bonds { get; private set; } = new List<NInchiBond>();

        /// <summary>
        /// List of stereo parities.
        /// </summary>
        public IList<NInchiStereo0D> Stereos { get; private set; } = new List<NInchiStereo0D>();

        public void SetStructure(NInchiStructure structure)
        {
            this.Atoms = structure.Atoms;
            this.Bonds = structure.Bonds;
            this.Stereos = structure.Stereos;
        }

        public NInchiAtom Add(NInchiAtom atom)
        {
            Atoms.Add(atom);
            return atom;
        }

        public NInchiBond Add(NInchiBond bond)
        {
            Bonds.Add(bond);
            return bond;
        }
    }
}
