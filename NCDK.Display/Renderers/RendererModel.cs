/* Copyright (C) 2008-2009  Gilleain Torrance <gilleain@users.sf.net>
 *               2008-2009  Arvid Berg <goglepox@users.sf.net>
 *                    2009  Stefan Kuhn <shk3@users.sf.net>
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

using NCDK.Events;
using NCDK.Renderers.Selection;
using System;
using System.Collections.Generic;

namespace NCDK.Renderers
{
    /// <summary>
    /// Model for <see cref="IRenderer{T}"/> that contains settings for drawing objects.
    /// </summary>
    // @author maclean
    // @cdk.module render
    public class RendererModel
        : IChemObjectListener
    {
        [NonSerialized]
        private HashSet<ICDKChangeListener> listeners = null;

        private readonly IDictionary<string, object> parameters;
        private IDictionary<IAtom, string> toolTipTextMap;
        private IAtom highlightedAtom = null;
        private IBond highlightedBond = null;
        private IAtomContainer externalSelectedPart = null;
        private IChemObjectSelection selection;

        /// <summary>
        /// Construct a renderer model with no parameters. To put parameters into
        /// the model, use the registerParameters method.
        /// </summary>
        public RendererModel()
        {
            parameters = new ObserbableDictionary<string, object>(this);
            toolTipTextMap = new ObserbableDictionary<IAtom, string>(this);
        }

        public IDictionary<string, object> Parameters => parameters;

        /// <summary>
        /// Set the selected <see cref="IChemObject"/>s.
        /// </summary>
        /// <param name="selection">an <see cref="IChemObjectSelection"/> with selected <see cref="IChemObject"/>s</param>
        public void SetSelection(IChemObjectSelection selection)
        {
            this.selection = selection;
        }

        /// <summary>
        /// Returns an <see cref="IChemObjectSelection"/> with the currently selected
        /// <see cref="IChemObject"/>s.
        /// </summary>
        /// <returns>the current selected <see cref="IChemObject"/>s</returns>
        public IChemObjectSelection GetSelection()
        {
            return this.selection;
        }

        /// <summary>
        /// This is the central facility for handling "merges" of atoms. A merge occurs if during moving atoms an atom is in Range of another atom.
        /// These atoms are then put into the merge map as a key-value pair. During the move, the atoms are then marked by a circle and on releasing the mouse
        /// they get actually merged, meaning one atom is removed and bonds pointing to this atom are made to point to the atom it has been merged with.
        /// </summary>
        public IDictionary<IAtom, IAtom> Merge { get; } = new Dictionary<IAtom, IAtom>();

        /// <summary>
        /// Returns the atom currently highlighted.
        /// </summary>
        /// <returns>the atom currently highlighted</returns>
        public IAtom GetHighlightedAtom()
        {
            return this.highlightedAtom;
        }

        /// <summary>
        /// Sets the atom currently highlighted.
        /// </summary>
        /// <param name="highlightedAtom">The atom to be highlighted</param>
        public void SetHighlightedAtom(IAtom highlightedAtom)
        {
            if ((this.highlightedAtom != null) || (highlightedAtom != null))
            {
                this.highlightedAtom = highlightedAtom;
                OnStateChanged(null);
            }
        }

        /// <summary>
        /// Returns the Bond currently highlighted.
        /// </summary>
        /// <returns>the Bond currently highlighted</returns>
        public IBond GetHighlightedBond()
        {
            return this.highlightedBond;
        }

        /// <summary>
        /// Sets the Bond currently highlighted.
        /// </summary>
        /// <param name="highlightedBond">The Bond to be currently highlighted</param>
        public void SetHighlightedBond(IBond highlightedBond)
        {
            if ((this.highlightedBond != null) || (highlightedBond != null))
            {
                this.highlightedBond = highlightedBond;
                OnStateChanged(null);
            }
        }

        /// <summary>
        /// Change listeners.
        /// </summary>
        public ICollection<ICDKChangeListener> Listeners
        {
            get
            {
                if (listeners == null)
                    listeners = new HashSet<ICDKChangeListener>();
                return listeners;
            }
        }

        /// <summary>
        /// Notifies registered listeners of certain changes that have occurred in
        /// this model.
        /// </summary>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
            if (Notification && listeners != null)
            {
                evt = new ChemObjectChangeEventArgs(this);
                foreach (var listener in listeners)
                {
                    listener.StateChanged(evt);
                }
            }
        }

        /// <summary>
        /// Gets the toolTipText for atom certain atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The toolTipText value.</returns>
        public string GetToolTipText(IAtom atom)
        {
            if (toolTipTextMap.TryGetValue(atom, out string text))
                return text;
            return null;
        }

        /// <summary>
        /// Sets the toolTipTextMap.
        /// </summary>
        /// <param name="map">A map containing Atoms of the current molecule as keys and strings to display as values. A line break will be inserted where a \n is in the string.</param>
        public void SetToolTipTextMap(IDictionary<IAtom, string> map)
        {
            toolTipTextMap = map;
            OnStateChanged(null);
        }

        /// <summary>
        /// Gets the toolTipTextMap.
        /// </summary>
        /// <returns>The toolTipTextValue.</returns>
        public IDictionary<IAtom, string> GetToolTipTextMap()
        {
            return toolTipTextMap;
        }

        /// <summary>
        /// Get externally selected atoms. These are atoms selected externally in e.
        /// g. Bioclipse via the ChemObjectTree, painted in externalSelectedPartColor
        /// </summary>
        /// <returns>the selected part</returns>
        public IAtomContainer GetExternalSelectedPart()
        {
            return externalSelectedPart;
        }

        /// <summary>
        /// Set externally selected atoms. These are atoms selected externally in e.
        /// g. Bioclipse via the ChemObjectTree, painted in externalSelectedPartColor
        /// </summary>
        /// <param name="externalSelectedPart">the selected part</param>
        public void SetExternalSelectedPart(IAtomContainer externalSelectedPart)
        {
            this.externalSelectedPart = externalSelectedPart;
            var colorHash = this.GetColorHash();
            colorHash.Clear();
            if (externalSelectedPart != null)
            {
                for (int i = 0; i < externalSelectedPart.Atoms.Count; i++)
                {
                    colorHash[externalSelectedPart.Atoms[i]] = this.GetExternalHighlightColor();
                }
                var bonds = externalSelectedPart.Bonds;
                foreach (var bond in bonds)
                {
                    colorHash[bond] = this.GetExternalHighlightColor();
                }
            }
            OnStateChanged(null);
        }

        /// <summary>
        /// Determines if the model sends around change notifications.
        /// </summary>
        public bool Notification { get; set; } = true;
    }
}
