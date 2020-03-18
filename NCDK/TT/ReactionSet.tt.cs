



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2003-2007  Egon Willighagen <egonw@users.sf.net>
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Default
{
    /// <summary>
    /// A set of reactions, for example those taking part in a reaction.
    /// </summary>
    /// <example>
    /// To retrieve the reactions from the set, there are two options:
    /// <code>
    /// ReactionSet reactions = ...
    /// foreach (var reaction in reactions)
    /// {
    ///     //
    /// }
    /// </code>
    ///
    /// and
    ///
    /// <code>
    /// for (int i = 0; i &lt; reactionSet.Count; i++)
    /// {
    ///        IReaction reaction = reactionSet[i]; 
    /// }
    /// </code>
    /// </example>
    // @cdk.keyword reaction
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class ReactionSet
        : ChemObject, IReactionSet, IChemObjectListener, ICloneable
    {
        /// <summary>
        /// Array of Reactions.
        /// </summary>
        private IList<IReaction> reactions = new List<IReaction>();

        /// <summary>
        /// Constructs an empty ReactionSet.
        /// </summary>
        public ReactionSet()
        {
        }

        /// <summary>
        /// Returns the Reaction at position <paramref name="number"/> in the container.
        /// </summary>
        /// <param name="number">The position of the Reaction to be returned</param>
        /// <returns>The Reaction at position <paramref name="number"/></returns>
        public virtual IReaction this[int number]
        {
            get { return reactions[number]; }

            set
            {
                reactions[number] = value;
                value.Listeners.Add(this);
            }
        }

        public int Count => reactions.Count;
        public bool IsReadOnly => reactions.IsReadOnly;
        
        /// <summary>
        /// Adds an reaction to this container.
        /// </summary>
        /// <param name="reaction">The reaction to be added to this container</param>
        public void Add(IReaction reaction)
        {
            reactions.Add(reaction);
             NotifyChanged();         }

        public void Clear()
        {
            reactions.Clear();
             NotifyChanged();         }

        public bool Contains(IReaction reaction) => reactions.Contains(reaction);
        public void CopyTo(IReaction[] array, int arrayIndex)
        {
            reactions.CopyTo(array, arrayIndex);
             NotifyChanged();         }

        public IEnumerator<IReaction> GetEnumerator() => reactions.GetEnumerator();
        public int IndexOf(IReaction reaction) => reactions.IndexOf(reaction);

        public void Insert(int index, IReaction reaction)
        {
            reactions.Insert(index, reaction);
             NotifyChanged();         }

        public bool Remove(IReaction reaction)
        {
            bool ret = false;
            while (reactions.Contains(reaction))
            {
                reactions.Remove(reaction);
                ret = true;
            }
             NotifyChanged();             return ret;
        }

        /// <summary>
        /// Remove a reaction from this set.
        /// </summary>
        /// <param name="index">The position of the reaction to be removed.</param>
        public void RemoveAt(int index)
        {
            reactions.RemoveAt(index);
             NotifyChanged();         }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsEmpty() => reactions.Count == 0;

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clonedReactions = new List<IReaction>();
            foreach (var reaction in reactions)
                clonedReactions.Add((IReaction)reaction.Clone(map));
            var clone = (ReactionSet)base.Clone(map);
            clone.reactions = clonedReactions;
            return clone;
        }

        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
             NotifyChanged(evt);         }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A set of reactions, for example those taking part in a reaction.
    /// </summary>
    /// <example>
    /// To retrieve the reactions from the set, there are two options:
    /// <code>
    /// ReactionSet reactions = ...
    /// foreach (var reaction in reactions)
    /// {
    ///     //
    /// }
    /// </code>
    ///
    /// and
    ///
    /// <code>
    /// for (int i = 0; i &lt; reactionSet.Count; i++)
    /// {
    ///        IReaction reaction = reactionSet[i]; 
    /// }
    /// </code>
    /// </example>
    // @cdk.keyword reaction
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class ReactionSet
        : ChemObject, IReactionSet, IChemObjectListener, ICloneable
    {
        /// <summary>
        /// Array of Reactions.
        /// </summary>
        private IList<IReaction> reactions = new List<IReaction>();

        /// <summary>
        /// Constructs an empty ReactionSet.
        /// </summary>
        public ReactionSet()
        {
        }

        /// <summary>
        /// Returns the Reaction at position <paramref name="number"/> in the container.
        /// </summary>
        /// <param name="number">The position of the Reaction to be returned</param>
        /// <returns>The Reaction at position <paramref name="number"/></returns>
        public virtual IReaction this[int number]
        {
            get { return reactions[number]; }

            set
            {
                reactions[number] = value;
                value.Listeners.Add(this);
            }
        }

        public int Count => reactions.Count;
        public bool IsReadOnly => reactions.IsReadOnly;
        
        /// <summary>
        /// Adds an reaction to this container.
        /// </summary>
        /// <param name="reaction">The reaction to be added to this container</param>
        public void Add(IReaction reaction)
        {
            reactions.Add(reaction);
                    }

        public void Clear()
        {
            reactions.Clear();
                    }

        public bool Contains(IReaction reaction) => reactions.Contains(reaction);
        public void CopyTo(IReaction[] array, int arrayIndex)
        {
            reactions.CopyTo(array, arrayIndex);
                    }

        public IEnumerator<IReaction> GetEnumerator() => reactions.GetEnumerator();
        public int IndexOf(IReaction reaction) => reactions.IndexOf(reaction);

        public void Insert(int index, IReaction reaction)
        {
            reactions.Insert(index, reaction);
                    }

        public bool Remove(IReaction reaction)
        {
            bool ret = false;
            while (reactions.Contains(reaction))
            {
                reactions.Remove(reaction);
                ret = true;
            }
                        return ret;
        }

        /// <summary>
        /// Remove a reaction from this set.
        /// </summary>
        /// <param name="index">The position of the reaction to be removed.</param>
        public void RemoveAt(int index)
        {
            reactions.RemoveAt(index);
                    }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsEmpty() => reactions.Count == 0;

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clonedReactions = new List<IReaction>();
            foreach (var reaction in reactions)
                clonedReactions.Add((IReaction)reaction.Clone(map));
            var clone = (ReactionSet)base.Clone(map);
            clone.reactions = clonedReactions;
            return clone;
        }

        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
                    }
    }
}
