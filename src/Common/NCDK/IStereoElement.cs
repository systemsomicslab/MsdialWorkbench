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
 *
 */

using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// Representation of stereochemical configuration. The abstract configuration
    /// is described by three pieces of information:
    /// <ul>
    ///  <li>the <b>focus</b> of the stereo chemistry</li>
    ///  <li>the <b>carriers</b> of the configuration</li>
    ///  <li>the <b>configuration</b> of the <i>carriers</i></li>
    /// </ul>
    /// <para>
    /// The focus/carriers may be either atoms or bonds. For example in the case of
    /// common tetrahedral stereochemistry the focus is the chiral atom, and the
    /// carriers are the bonds (or atoms) connected to it. The configuration is then
    /// either left-handed (anti-clockwise) or right-handed (clockwise).
    /// </para>
    /// </summary>
    // @cdk.module interfaces
    // @author      Egon Willighagen
    // @author      John Mayfield
    // @cdk.keyword stereochemistry
    public interface IStereoElement<out TFocus, out TCarriers>
        : ICDKObject
        where TFocus : IChemObject
        where TCarriers : IChemObject
    {
        /// <summary>
        /// Does the stereo element contain the provided atom.
        /// </summary>
        /// <param name="atom">an atom to test membership</param>
        /// <returns>whether the atom is present</returns>
        bool Contains(IAtom atom);

        /// <summary>
        /// The focus atom or bond at the 'centre' of the stereo-configuration.
        /// </summary>
        TFocus Focus { get; }

        /// <summary>
        /// The carriers of the stereochemistry
        /// </summary>
        IReadOnlyList<TCarriers> Carriers { get; }

        /// <summary>
        /// The configuration class of the stereochemistry.
        /// </summary>
        StereoClass Class { get; }

        /// <summary>
        /// The configuration of the stereochemistry.
        /// </summary>
        StereoConfigurations Configure { get; set; }
    }
}
