/*  Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
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

using System.Diagnostics;
using System.IO;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// <see cref="IGenerator{T}"/> that can render mass number information of atoms.
    /// </summary>
    // @cdk.module renderextra
    public class AtomMassGenerator : BasicAtomGenerator
    {
        /// <summary>
        /// Returns true if the mass number of this element is set and not
        /// equal the mass number of the most abundant isotope of this element.
        /// </summary>
        /// <param name="atom"><see cref="IAtom"/> which is being examined</param>
        /// <param name="container"><see cref="IAtomContainer"/> of which the atom is part</param>
        /// <param name="model">the <see cref="RendererModel"/></param>
        /// <returns>true, when mass number information should be depicted</returns>
        public override bool ShowCarbon(IAtom atom, IAtomContainer container, RendererModel model)
        {
            var massNumber = atom.MassNumber;
            if (massNumber != null)
            {
                try
                {
                    var expectedMassNumber = CDK.IsotopeFactory.GetMajorIsotope(atom.Symbol).MassNumber;
                    if (massNumber != expectedMassNumber) return true;
                }
                catch (IOException e)
                {
                    Trace.TraceWarning(e.Message);
                }
            }
            return base.ShowCarbon(atom, container, model);
        }
    }
}
