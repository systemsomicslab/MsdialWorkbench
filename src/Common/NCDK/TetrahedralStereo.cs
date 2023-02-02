/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using static NCDK.TetrahedralStereo;

namespace NCDK
{
    /// <summary>
    /// Enumeration that defines the two possible chiralities for this stereochemistry type.
    /// </summary>
    public enum TetrahedralStereo
    {
        Unset = 0,
        Clockwise,
        AntiClockwise,
    }

    public static class TetrahedralStereoTools
    {
        public static bool IsUnset(this TetrahedralStereo value)
            => value == Unset;

        /// <summary>
        /// Get inverted conformation,
        /// </summary>
        /// <remarks>
        /// <see cref="Invert(TetrahedralStereo)"/> of <see cref="Clockwise"/> == <see cref="AntiClockwise"/>
        /// <see cref="Invert(TetrahedralStereo)"/> of <see cref="AntiClockwise"/> == <see cref="Clockwise"/>
        /// </remarks>
        /// <returns>The inverse conformation</returns>
        public static TetrahedralStereo Invert(this TetrahedralStereo value)
        {
            if (value == Clockwise)
                return AntiClockwise;
            if (value == AntiClockwise)
                return Clockwise;
            return value;
        }

        public static StereoConfigurations ToConfiguration(this TetrahedralStereo value)
        {
            switch (value)
            {
                case AntiClockwise:
                    return StereoConfigurations.Left;
                case Clockwise:
                    return StereoConfigurations.Right;
                default:
                    throw new System.ArgumentException("Unknown enum value: " + value);
            }
        }

        public static TetrahedralStereo ToStereo(this StereoConfigurations configure)
        {
            switch (configure)
            {
                case StereoConfigurations.Left:
                    return AntiClockwise;
                case StereoConfigurations.Right:
                    return Clockwise;
                default:
                    throw new System.ArgumentException("Cannot map to enum value: " + configure);
            }
        }
    }
}
