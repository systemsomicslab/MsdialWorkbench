/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
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

namespace NCDK
{
     /// <summary>
     /// An class providing predefined properties of physical constants.
     /// </summary>
     // @cdk.keyword physical properties
    public static class PhysicalConstants
    {
        /// <summary>
        /// Mass of a proton.
        /// </summary>
        public const double MassProton = 1.6726485E-27;

        /// <summary>
        /// Mass of an electron.
        /// </summary>
        public const double MassElectron = 9.109534E-31;

        /// <summary>
        /// Factor for the conversion of Bohr's to Ångström's.
        /// </summary>
        public const double BohrToAngstrom = 0.529177249;
    }
}
