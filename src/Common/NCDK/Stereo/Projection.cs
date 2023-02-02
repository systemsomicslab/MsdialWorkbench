/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *   
 * Contact: cdk-devel@lists.sourceforge.net
 *   
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above 
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System.Reflection;
using static NCDK.Stereo.Projection;

namespace NCDK.Stereo
{
    /// <summary>
    /// Stereochemistry projection types. 
    /// </summary>
    // @author John May
    [Obfuscation(ApplyToMembers = true, Exclude = true)]
    public enum Projection
    {
        Unset = 0,

        /// <summary>
        /// Fischer projections are used for linear chain-form carbohydrates. 
        /// They are drawn vertically with all atoms at right angles around stereocenters.
        /// </summary>
        Fischer,

        /// <summary>
        /// Haworth projection are used to depict ring-form carbohydrates. 
        /// The ring may be of size 5, 6, or 7 (rarer). 
        /// Here the ring is flat and the substituents connected to stereocenters are drawn directly above or below the plane of the ring.
        /// </summary>
        Haworth,

        /// <summary>
        /// Projection of the low energy conformation (chair) of a cyclohexane. 
        /// Used for carbohydrates.
        /// </summary>
        Chair,
    }

    public static class ProjectionTools
    {
        public static bool IsUnset(this Projection value)
            => value == Unset;
    }
}
