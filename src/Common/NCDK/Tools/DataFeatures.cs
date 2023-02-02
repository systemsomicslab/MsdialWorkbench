/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;

namespace NCDK.Tools
{
    /// <summary>
    /// Class with constants for possible data features defined in the
    /// a Data Feature Ontology. Actual integers are random
    /// and should <b>not</b> be used directly.
    /// </summary>
    /// <example>
    /// To test whether a IChemFormat supports a certain feature, the
    /// following code can be used:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.DataFeatures_Example.cs+1"]/*' />
    /// This list of constants matches the latest <see href="http://qsar.sourceforge.net/ontologies/data-features/index.xhtml">Blue Obelisk Data Features Ontology</see>.
    /// </example>
    // @author     Egon Willighagen <ewilligh@uni-koeln.de>
    // @cdk.module annotation
    [Flags]
    public enum DataFeatures
    {
        /// <summary>Indicated that no feature are defined.</summary>
        None = 0,

        // The int allows for up to 750 different properties. Should
        // be enough for now.

        // COORDINATE SYSTEMS

        /// <summary>@cdk.dictref bodf:coordinates2D</summary>
        Has2DCoordinates = 1 << 0,
        /// <summary>@cdk.dictref bodf:coordinates3D</summary>
        Has3DCoordinates = 1 << 1,
        /// <summary>@cdk.dictref bodf:fractionalUnitCellCoordinatesCoordinates</summary>
        HasFractionalCrystalCoordinates = 1 << 2,

        // ATOMIC FEATURES
        //                      HAS_ATOMS ??

        /// <summary>@cdk.dictref bodf:hasAtomElementSymbol</summary>
        HasAtomElementSymbol = 1 << 3,
        /// <summary>@cdk.dictref bodf:partialAtomicCharges</summary>
        HasAtomPartialCharges = 1 << 4,
        /// <summary>@cdk.dictref bodf:formalAtomicCharges</summary>
        HasAtomFormalCharges = 1 << 5,
        /// <summary>FIXME: NOT YET IN BODF !!! </summary>
        HasAtomHybridizations = 1 << 6,
        /// <summary>@cdk.dictref bodf:massNumbers</summary>
        HasAtomMassNumbers = 1 << 7,
        /// <summary>@cdk.dictref bodf:isotopeNumbers</summary>
        HasAtomIsotopeNumbers = 1 << 8,

        // GRAPH FEATURES

        /// <summary>@cdk.dictref bodf:graphRepresentation</summary>
        HasGraphRepresentation = 1 << 9,
        /// <summary>@cdk.dictref bodf:dietzRepresentation</summary>
        HasDietzRepresentation = 1 << 10,

        // MODEL FEATURES

        /// <summary>FIXME: NOT YET IN BODF !!! </summary>
        HasUnitcellParameters = 1 << 11,
        /// <summary>FIXME: NOT YET IN BODF !!! </summary>
        HasReactions = 1 << 12,
    }
}
