/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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

using System.Reflection;

namespace NCDK
{
    /// <summary>
    /// Enumeration of possible stereo types of two-atom bonds. The
    /// Stereo type defines not just define the stereochemistry, but also the
    /// which atom is the stereo center for which the Stereo is defined.
    /// The first atom in the IBond (index = 0) is the <i>start</i> atom, while
    /// the second atom (index = 1) is the <i>end</i> atom.
    /// </summary>
    [Obfuscation(ApplyToMembers = true, Exclude = true)]
    public enum BondStereo
    {
        /// <summary>
        /// A bond for which there is no stereochemistry.
        /// </summary>
        None = 0,

        /// <summary>
        /// A bond pointing up of which the start atom is the stereocenter and the end atom is above the drawing plane.
        /// </summary>
        Up,

        /// <summary>
        /// A bond pointing up of which the end atom is the stereocenter and the start atom is above the drawing plane.
        /// </summary>
        UpInverted,

        /// <summary>
        /// A bond pointing down of which the start atom is the stereocenter and the end atom is below the drawing plane.
        /// </summary>
        Down,

        /// <summary>
        /// A bond pointing down of which the end atom is the stereocenter and the start atom is below the drawing plane.
        /// </summary>
        DownInverted,

        /// <summary>
        /// A bond for which there is stereochemistry, we just do not know if it is <see cref="Up"/> or <see cref="Down"/>. The start atom is the stereocenter.
        /// </summary>
        UpOrDown,

        /// <summary>
        /// A bond for which there is stereochemistry, we just do not know if it is <see cref="Up"/> or <see cref="Down"/>. The end atom is the stereocenter.
        /// </summary>
        UpOrDownInverted,

        /// <summary>
        /// Indication that this double bond has a fixed, but unknown E/Z configuration.
        /// </summary>
        EOrZ,

        /// <summary>
        /// Indication that this double bond has a E configuration.
        /// </summary>
        E,

        /// <summary>
        /// Indication that this double bond has a Z configuration.
        /// </summary>
        Z,

        /// <summary>
        /// Indication that this double bond has a fixed configuration, defined by the 2D and/or 3D coordinates.
        /// </summary>
        EZByCoordinates,
    }
}

