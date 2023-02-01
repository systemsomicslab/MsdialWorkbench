



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2003-2008  Egon Willighagen <egonw@users.sf.net>
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

using System;
using System.Collections.Generic;

namespace NCDK.Default
{
    /// <inheritdoc cref="IReaction"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Reaction
        : ChemObject, IReaction, ICloneable
    {
        private IChemObjectSet<IAtomContainer> reactants;
        /// <inheritdoc/>
        public IChemObjectSet<IAtomContainer> Reactants => reactants;

        private IChemObjectSet<IAtomContainer> products;
        /// <inheritdoc/>
        public IChemObjectSet<IAtomContainer> Products => products;

        private IChemObjectSet<IAtomContainer> agents;
        /// <inheritdoc/>
        public IChemObjectSet<IAtomContainer> Agents => agents;

        private List<IMapping> mappings;
        /// <inheritdoc/>
        public IList<IMapping> Mappings => mappings;

        private ReactionDirection direction;
        /// <inheritdoc/>
        public ReactionDirection Direction
        {
            get { return direction; }

            set
            {
                direction = value;
                 NotifyChanged();             }
        }

        /// <summary>
        /// Constructs an empty, forward reaction.
        /// </summary>
        public Reaction()
        {
            this.reactants = Builder.NewAtomContainerSet();
            this.products = Builder.NewAtomContainerSet();
            this.agents = Builder.NewAtomContainerSet();
            this.mappings = new List<IMapping>();
            direction = ReactionDirection.Forward;
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone_reactants = (IChemObjectSet<IAtomContainer>)reactants.Clone(map);
            var clone_agents = (IChemObjectSet<IAtomContainer>)agents.Clone(map);
            var clone_products = (IChemObjectSet<IAtomContainer>)products.Clone(map);

            var clone_mappings = new List<IMapping>();
            foreach (var mapping in mappings)
                clone_mappings.Add((IMapping)mapping.Clone(map));

            var clone = (Reaction)base.Clone(map);
            clone.reactants = clone_reactants;
            clone.agents = clone_agents;
            clone.products = clone_products;
            clone.mappings = clone_mappings;

            return clone;
        }
    }
}
namespace NCDK.Silent
{
    /// <inheritdoc cref="IReaction"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Reaction
        : ChemObject, IReaction, ICloneable
    {
        private IChemObjectSet<IAtomContainer> reactants;
        /// <inheritdoc/>
        public IChemObjectSet<IAtomContainer> Reactants => reactants;

        private IChemObjectSet<IAtomContainer> products;
        /// <inheritdoc/>
        public IChemObjectSet<IAtomContainer> Products => products;

        private IChemObjectSet<IAtomContainer> agents;
        /// <inheritdoc/>
        public IChemObjectSet<IAtomContainer> Agents => agents;

        private List<IMapping> mappings;
        /// <inheritdoc/>
        public IList<IMapping> Mappings => mappings;

        private ReactionDirection direction;
        /// <inheritdoc/>
        public ReactionDirection Direction
        {
            get { return direction; }

            set
            {
                direction = value;
                            }
        }

        /// <summary>
        /// Constructs an empty, forward reaction.
        /// </summary>
        public Reaction()
        {
            this.reactants = Builder.NewAtomContainerSet();
            this.products = Builder.NewAtomContainerSet();
            this.agents = Builder.NewAtomContainerSet();
            this.mappings = new List<IMapping>();
            direction = ReactionDirection.Forward;
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone_reactants = (IChemObjectSet<IAtomContainer>)reactants.Clone(map);
            var clone_agents = (IChemObjectSet<IAtomContainer>)agents.Clone(map);
            var clone_products = (IChemObjectSet<IAtomContainer>)products.Clone(map);

            var clone_mappings = new List<IMapping>();
            foreach (var mapping in mappings)
                clone_mappings.Add((IMapping)mapping.Clone(map));

            var clone = (Reaction)base.Clone(map);
            clone.reactants = clone_reactants;
            clone.agents = clone_agents;
            clone.products = clone_products;
            clone.mappings = clone_mappings;

            return clone;
        }
    }
}
