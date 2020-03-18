/* Copyright (C) 2009 Arvid Berg <goglepox@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
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

using System;
using System.Collections.Generic;

namespace NCDK.Renderers.Selection
{
    /// <summary>
    /// An abstract selection of <see cref="IChemObject"/>s.
    /// </summary>
    // @author Arvid
    // @cdk.module renderbasic
    public abstract class AbstractSelection : IChemObjectSelection
    {
        class AbstractSelection_EMPTY_SELECTION : AbstractSelection
        {
            /// <inheritdoc/>
            public override IAtomContainer GetConnectedAtomContainer() => null;
            /// <inheritdoc/>
            public override bool IsFilled() => false;
            /// <inheritdoc/>
            public override bool Contains(IChemObject obj) => false;
            /// <inheritdoc/>
            public override ICollection<T> Elements<T>() => Array.Empty<T>();
        }

        /// <summary>
        /// Static implementation of an empty selection.
        /// </summary>
        public static IChemObjectSelection EMPTY_SELECTION = new AbstractSelection_EMPTY_SELECTION();

        /// <inheritdoc/>
        public void Select(IChemModel chemModel)
        {
            // TODO Auto-generated method stub
        }

        /// <summary>
        /// Utility method to add an <see cref="IChemObject"/> to an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="ac">the <see cref="IAtomContainer"/> to add to</param>
        /// <param name="item">the <see cref="IChemObject"/> to add</param>
        protected void AddToAtomContainer(IAtomContainer ac, IChemObject item)
        {
            if (item is IAtomContainer)
            {
                ac.Add((IAtomContainer)item);
            }
            else if (item is IAtom)
            {
                ac.Atoms.Add((IAtom)item);
            }
            else if (item is IBond)
            {
                ac.Bonds.Add((IBond)item);
            }
        }

        public abstract IAtomContainer GetConnectedAtomContainer();
        public abstract bool IsFilled();
        public abstract bool Contains(IChemObject obj);
        public abstract ICollection<E> Elements<E>() where E : IChemObject;
    }
}
