/*
 * Copyright (C) 2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may
 * distribute with programs based on this work.
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

namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// Interface definition for Rgroup query classes. These must provide a root
    /// structure, root attachment points and Rgroup definitions.
    /// </summary>
    // @cdk.module  isomorphism
    // @cdk.keyword Rgroup
    // @cdk.keyword R group
    // @cdk.keyword R-group
    // @author Mark Rijnbeek
    public interface IRGroupQuery : IChemObject
    {
        /// <summary>
        /// The root structure (or scaffold) container
        /// </summary>
        IAtomContainer RootStructure { get; set; }

        /// <summary>
        /// Root attachment points = bonds that connect R pseudo-atoms to the scaffold.
        /// </summary>
        IReadOnlyDictionary<IAtom, IReadOnlyDictionary<int, IBond>> RootAttachmentPoints { get; set; }

        /// <summary>
        /// the R-group definitions (substituents).
        /// </summary>
        IReadOnlyDictionary<int, RGroupList> RGroupDefinitions { get; set; }

        /// <summary>
        /// the total number of atom containers (count the root plus all substituents).
        /// </summary>
        int Count { get; }

        /// <summary>
        /// All the substituent atom containers, in other words the atom containers
        /// defined in this <see cref="IRGroupQuery"/> except for the root structure.
        /// </summary>
        IEnumerable<IAtomContainer> GetSubstituents();

        /// <summary>
        /// Checks validity of the RGroupQuery.
        /// Each distinct R# in the root must have a
        /// a corresponding <see cref="RGroupList"/> definition.
        /// <para>
        /// In file terms: $RGP blocks must be defined for each R-group number.</para>
        /// </summary>
        bool AreSubstituentsDefined();

        /// <summary>
        /// Checks validity of RGroupQuery.
        /// Each <see cref="RGroupList"/> definition must have one or more corresponding
        /// R# atoms in the root block. 
        /// Returns <see langword="true"/> when valid
        /// </summary>
        bool AreRootAtomsDefined();

        /// <summary>
        /// Produces all combinations of the root structure (scaffold) with the R-groups
        /// substituted in valid ways, using each R-group's definitions and conditions.
        /// </summary>
        IEnumerable<IAtomContainer> GetAllConfigurations();
    }
}
