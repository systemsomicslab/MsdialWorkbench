



/* Copyright (C) 2006-2007  Miguel Rojas <miguelrojasch@yahoo.es>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;

namespace NCDK.Default
{
    /// <summary>
    /// Classes that extends the definition of reaction to a chain reaction.
    /// This is designed to contains a set of reactions which are lineal linked as
    /// chain. That would mean no exist branches or cycles and in this concept
    /// you have a start reaction and final reaction. Each reaction is included
    /// in a step of the chain.
    /// </summary>
    // @author      miguelrojasch <miguelrojasch@yahoo.es>
    // @cdk.module  extra
    public class ReactionChain 
        : ReactionSet
    {
        Dictionary<IReaction, int> hashMapChain = new Dictionary<IReaction, int>();

        /// <summary>
        /// Added a IReaction for this chain in position.
        /// </summary>
        /// <param name="reaction">The IReaction</param>
        /// <param name="position">The position in this chain where the reaction is to be inserted</param>
        public void Add(IReaction reaction, int position)
        {
            hashMapChain[reaction] = position;
            this.Add(reaction);
        }

        /// <summary>
        /// Get the position of the reaction into this chain reaction object.
        /// </summary>
        /// <param name="reaction">The IReaction to look at</param>
        /// <returns>The position of the IReaction in this chain</returns>
        public int GetReactionStep(IReaction reaction)
        {
            if (hashMapChain.ContainsKey(reaction))
                return hashMapChain[reaction];
            else
                return -1;
        }

        /// <summary>
        /// The reaction of this chain reaction object at the position.
        /// </summary>
        /// <param name="position">The position of the IReaction in this chain to look for</param>
        /// <returns>Reaction The IReaction to look at</returns>
        public override IReaction this[int position]
        {
            get
            {
                if (!hashMapChain.ContainsValue(position)) return null;

                foreach (var entry in hashMapChain)
                {
                    if (entry.Value.Equals(position)) return entry.Key;
                }
                return null;
            }
            set
            {
                base[position] = value;
            }
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// Classes that extends the definition of reaction to a chain reaction.
    /// This is designed to contains a set of reactions which are lineal linked as
    /// chain. That would mean no exist branches or cycles and in this concept
    /// you have a start reaction and final reaction. Each reaction is included
    /// in a step of the chain.
    /// </summary>
    // @author      miguelrojasch <miguelrojasch@yahoo.es>
    // @cdk.module  extra
    public class ReactionChain 
        : ReactionSet
    {
        Dictionary<IReaction, int> hashMapChain = new Dictionary<IReaction, int>();

        /// <summary>
        /// Added a IReaction for this chain in position.
        /// </summary>
        /// <param name="reaction">The IReaction</param>
        /// <param name="position">The position in this chain where the reaction is to be inserted</param>
        public void Add(IReaction reaction, int position)
        {
            hashMapChain[reaction] = position;
            this.Add(reaction);
        }

        /// <summary>
        /// Get the position of the reaction into this chain reaction object.
        /// </summary>
        /// <param name="reaction">The IReaction to look at</param>
        /// <returns>The position of the IReaction in this chain</returns>
        public int GetReactionStep(IReaction reaction)
        {
            if (hashMapChain.ContainsKey(reaction))
                return hashMapChain[reaction];
            else
                return -1;
        }

        /// <summary>
        /// The reaction of this chain reaction object at the position.
        /// </summary>
        /// <param name="position">The position of the IReaction in this chain to look for</param>
        /// <returns>Reaction The IReaction to look at</returns>
        public override IReaction this[int position]
        {
            get
            {
                if (!hashMapChain.ContainsValue(position)) return null;

                foreach (var entry in hashMapChain)
                {
                    if (entry.Value.Equals(position)) return entry.Key;
                }
                return null;
            }
            set
            {
                base[position] = value;
            }
        }
    }
}
