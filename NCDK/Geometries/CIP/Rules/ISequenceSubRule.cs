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

using System.Collections.Generic;

namespace NCDK.Geometries.CIP.Rules
{
    /// <summary>
    /// Sequence sub rule used in the CIP method to decide which of the two ligands takes
    /// precedence <token>cdk-cite-Cahn1966</token>. A list ordered based on these rules will be
    /// sorted from low to high precedence.
    /// </summary>
    /// <remarks>
    /// Compares two ligands according to the particular sequence sub rule. It returns
    /// 1 if ligand1 takes precedence over ligand2, -1 if ligand2 takes precedence over
    /// ligand1, and 0 if they are equal.
    /// </remarks>
    // @cdk.module cip
    public interface ISequenceSubRule<T> : IComparer<T> where T : ILigand
    {
    }
}
