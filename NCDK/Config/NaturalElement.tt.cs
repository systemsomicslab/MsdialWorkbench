
/* 
 * Copyright (C) 2017-2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */


using System.Collections.Generic;

namespace NCDK.Config
{
    internal static class NaturalElement
    {
        /// <summary>
        /// Return the period in the periodic table this element belongs to. If
        /// the element is <see cref="ChemicalElement.R"/> it's period is 0.
        /// </summary>
        internal static IReadOnlyList<int> Periods { get; } = new int[] 
            { 0, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,  };

        /// <summary>
        /// Return the group in the periodic table this element belongs to. If
        /// the element does not belong to a group then it's group is '0'.
        /// </summary>
        internal static IReadOnlyList<int> Groups { get; } = new int[] 
            { 0, 1, 18, 1, 2, 13, 14, 15, 16, 17, 18, 1, 2, 13, 14, 15, 16, 17, 18, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 1, 2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 1, 2, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18,  };

        /// <summary>
        /// Covalent radius (<i>r<sub>cov</sub></i>).
        /// </summary>
        /// <seealso href="http://en.wikipedia.org/wiki/Covalent_radius">Covalent radius</seealso>
        internal static IReadOnlyList<double?> CovalentRadiuses { get; } = new double?[] 
            { null, 0.37, 0.32, 1.34, 0.90, 0.82, 0.77, 0.75, 0.73, 0.71, 0.69, 1.54, 1.30, 1.18, 1.11, 1.06, 1.02, 0.99, 0.97, 1.96, 1.74, 1.44, 1.36, 1.25, 1.27, 1.39, 1.25, 1.26, 1.21, 1.38, 1.31, 1.26, 1.22, 1.19, 1.16, 1.14, 1.10, 2.11, 1.92, 1.62, 1.48, 1.37, 1.45, 1.56, 1.26, 1.35, 1.31, 1.53, 1.48, 1.44, 1.41, 1.38, 1.35, 1.33, 1.30, 2.25, 1.98, 1.69, null, null, null, null, null, 2.40, null, null, null, null, null, null, null, 1.60, 1.50, 1.38, 1.46, 1.59, 1.28, 1.37, 1.28, 1.44, 1.49, 1.48, 1.47, 1.46, 1.46, null, 1.45, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,  };

        /// <summary>
        /// van der Waals radius (<i>r<sub>w</sub></i>).
        /// </summary>
        internal static IReadOnlyList<double?> VdwRadiuses { get; } = new double?[] 
            { null, 1.20, 1.40, 2.20, 1.90, 1.80, 1.70, 1.60, 1.55, 1.50, 1.54, 2.40, 2.20, 2.10, 2.10, 1.95, 1.80, 1.80, 1.88, 2.80, 2.40, 2.30, 2.15, 2.05, 2.05, 2.05, 2.05, null, null, null, 2.10, 2.10, 2.10, 2.05, 1.90, 1.90, 2.02, 2.90, 2.55, 2.40, 2.30, 2.15, 2.10, 2.05, 2.05, null, 2.05, 2.10, 2.20, 2.20, 2.25, 2.20, 2.10, 2.10, 2.16, 3.00, 2.70, 2.50, 2.48, 2.47, 2.45, 2.43, 2.42, 2.40, 2.38, 2.37, 2.35, 2.33, 2.32, 2.30, 2.28, 2.27, 2.25, 2.20, 2.10, 2.05, null, null, 2.05, 2.10, 2.05, 2.20, 2.30, 2.30, null, null, null, null, null, null, 2.40, null, 2.30, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,  };

        /// <summary>
        /// Pauling electronegativity, symbol χ, is a chemical property that describes
        /// the tendency of an atom or a functional group to attract electrons
        /// (or electron density) towards itself. This method provides access to the
        /// Pauling electronegativity value for a chemical element. If no value is
        /// available <see langword="null"/> is returned.
        /// </summary>
        /// <seealso href="http://en.wikipedia.org/wiki/Electronegativity#Pauling_electronegativity">Pauling Electronegativity</seealso>
        internal static IReadOnlyList<double?> Electronegativities { get; } = new double?[] 
            { null, 2.20, null, 0.98, 1.57, 2.04, 2.55, 3.04, 3.44, 3.98, null, 0.93, 1.31, 1.61, 1.90, 2.19, 2.58, 3.16, null, 0.82, 1.00, 1.36, 1.54, 1.63, 1.66, 1.55, 1.83, 1.88, 1.91, 1.90, 1.65, 1.81, 2.01, 2.18, 2.55, 2.96, 3.00, 0.82, 0.95, 1.22, 1.33, 1.60, 2.16, 1.90, 2.20, 2.28, 2.20, 1.93, 1.69, 1.78, 1.96, 2.05, 2.10, 2.66, 2.60, 0.79, 0.89, 1.10, 1.12, 1.13, 1.14, null, 1.17, null, 1.20, null, 1.22, 1.23, 1.24, 1.25, null, 1.27, 1.30, 1.50, 2.36, 1.90, 2.20, 2.20, 2.28, 2.54, 2.00, 1.62, 2.33, 2.02, 2.00, 2.20, null, 0.70, 0.90, 1.10, 1.30, 1.50, 1.38, 1.36, 1.28, 1.30, 1.30, 1.30, 1.30, 1.30, 1.30, 1.30, 1.30, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,  };
    }
}

