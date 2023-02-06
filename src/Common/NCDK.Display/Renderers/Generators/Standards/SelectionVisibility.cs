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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Windows.Media;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Extended existing symbol visibility options to account for selection of atoms in the standard
    /// generator.
    /// <para>
    /// The selection viability displays an atom symbol regardless as to whether it is normally 'shown'.
    /// By default, the symbol is shown if the atom is selected an not next to any selected bonds
    /// (disconnected). Alternatively, all select atoms can be displayed. 
    /// </para>
    /// <para>
    /// An atom or bond is selected if the <see cref="StandardGenerator.HighlightColorKey"/> is non-null.
    /// </para>
    /// </summary>
    // @author John May
    public sealed class SelectionVisibility : SymbolVisibility
    {
        private readonly SymbolVisibility delegate_;
        private readonly bool showAll;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="delegate">default viability</param>
        /// <param name="showAll">all select atoms are displayed</param>
        private SelectionVisibility(SymbolVisibility @delegate, bool showAll)
        {
            this.delegate_ = @delegate;
            this.showAll = showAll;
        }

        /// <summary>
        /// Display the atom symbol if is disconnected from any other selected atoms or bonds. The
        /// provided visibility is used when the atom is not selected.
        /// </summary>
        /// <param name="visibility">visibility when not selected</param>
        /// <returns>visibility instance</returns>
        public static SymbolVisibility Disconnected(SymbolVisibility visibility)
        {
            return new SelectionVisibility(visibility, false);
        }

        /// <summary>
        /// Display the atom symbol if is selected, otherwise use the provided visibility.
        /// </summary>
        /// <param name="visibility">visibility when not selected</param>
        /// <returns>visibility instance</returns>
        public static SymbolVisibility GetAll(SymbolVisibility visibility)
        {
            return new SelectionVisibility(visibility, true);
        }

        /// <inheritdoc/>
        public override bool Visible(IAtom atom, IEnumerable<IBond> neighbors, RendererModel model)
        {
            if (IsSelected(atom, model) && (showAll || !HasSelectedBond(neighbors, model))) return true;
            return delegate_.Visible(atom, neighbors, model);
        }

        /// <summary>
        /// Determine if an object is selected.
        /// </summary>
        /// <param name="obj">the object</param>
        /// <returns>object is selected</returns>
        internal static bool IsSelected(IChemObject obj, RendererModel model)
        {
            if (obj.GetProperty<Color?>(StandardGenerator.HighlightColorKey) != null)
                return true;
            if (model.GetSelection() != null)
                return model.GetSelection().Contains(obj);
            return false;
        }

        /// <summary>
        /// Determines if any bond in the list is selected
        /// </summary>
        /// <param name="bonds">list of bonds</param>
        /// <returns>at least bond is selected</returns>
        internal static bool HasSelectedBond(IEnumerable<IBond> bonds, RendererModel model)
        {
            foreach (var bond in bonds)
            {
                if (IsSelected(bond, model))
                    return true;
            }
            return false;
        }
    }
}

