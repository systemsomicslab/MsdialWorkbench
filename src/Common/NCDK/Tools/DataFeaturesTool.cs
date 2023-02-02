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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Features;
using NCDK.Geometries;

namespace NCDK.Tools
{
    /// <summary>
    /// Utility that helps determine which data features are present.
    /// </summary>
    /// <seealso cref="Tools.DataFeatures"/>
    // @author egonw
    public static class DataFeaturesTool
    {
        /// <summary>
        /// Determines the features present in the given <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="molecule">IAtomContainer to determine the features off</param>
        /// <returns>integer representation of the present features</returns>
        public static DataFeatures GetSupportedDataFeatures(IAtomContainer molecule)
        {
            DataFeatures features = DataFeatures.None;
            if (MoleculeFeaturesTool.HasElementSymbols(molecule))
                features = features | DataFeatures.HasAtomElementSymbol;
            if (GeometryUtil.Has2DCoordinates(molecule))
                features = features | DataFeatures.Has2DCoordinates;
            if (GeometryUtil.Has3DCoordinates(molecule))
                features = features | DataFeatures.Has3DCoordinates;
            if (CrystalGeometryTools.HasCrystalCoordinates(molecule))
                features = features | DataFeatures.HasFractionalCrystalCoordinates;
            if (MoleculeFeaturesTool.HasFormalCharges(molecule))
                features = features | DataFeatures.HasAtomFormalCharges;
            if (MoleculeFeaturesTool.HasPartialCharges(molecule))
                features = features | DataFeatures.HasAtomPartialCharges;
            if (MoleculeFeaturesTool.HasGraphRepresentation(molecule))
                features = features | DataFeatures.HasGraphRepresentation;
            return features;
        }
    }
}
