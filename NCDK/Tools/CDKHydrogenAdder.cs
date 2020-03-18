/* Copyright (C) 2007  Egon Willighagen
 *               2009  Mark Rijnbeek <markr@ebi.ac.uk>
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

using NCDK.Config;
using System;
using System.Linq;

namespace NCDK.Tools
{
    public interface IHydrogenAdder
    {
        /// <summary>
        /// Sets implicit hydrogen counts for all atoms in the given <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">The molecule to which H's will be added</param>
        /// <exception cref="CDKException">If insufficient information is present</exception>
        // @cdk.keyword hydrogens, adding
        void AddImplicitHydrogens(IAtomContainer container);

        /// <summary>
        /// Sets the implicit hydrogen count for the indicated IAtom in the given <see cref="IAtomContainer"/>.
        /// If the atom type is "X", then the atom is assigned zero implicit hydrogens.
        /// </summary>
        /// <param name="container">The molecule to which H's will be added</param>
        /// <param name="atom">IAtom to set the implicit hydrogen count for</param>
        /// <exception cref="CDKException">if insufficient information is present</exception>
        void AddImplicitHydrogens(IAtomContainer container, IAtom atom);
    }

    /// <summary>
    /// Adds implicit hydrogens based on atom type definitions. The class assumes
    /// that CDK atom types are already detected. 
    /// </summary>
    /// <example>
    /// A full code example is:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.CDKHydrogenAdder_Example.cs+1"]/*' />
    /// If you want to add the hydrogens to a specific atom only,
    /// use this example:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.CDKHydrogenAdder_Example.cs+2"]/*' />
    /// </example>
    // @author     egonw
    // @cdk.module valencycheck
    public class CDKHydrogenAdder
        : IHydrogenAdder
    {
        private static readonly AtomTypeFactory atomTypeList = CDK.CdkAtomTypeFactory;

        private CDKHydrogenAdder()
        {
        }

        private static readonly CDKHydrogenAdder instance = new CDKHydrogenAdder();

        public static CDKHydrogenAdder GetInstance() => instance;
        
        /// <inheritdoc/>
        public void AddImplicitHydrogens(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
            {
                if (!(atom is IPseudoAtom))
                {
                    AddImplicitHydrogens(container, atom);
                }
            }
        }

        /// <inheritdoc/>
        public void AddImplicitHydrogens(IAtomContainer container, IAtom atom)
        {
            if (atom.AtomTypeName == null)
                throw new CDKException("IAtom is not typed! " + atom.Symbol);

            if (string.Equals("X", atom.AtomTypeName, StringComparison.Ordinal))
            {
                if (atom.ImplicitHydrogenCount == null)
                    atom.ImplicitHydrogenCount = 0;
                return;
            }

            var type = atomTypeList.GetAtomType(atom.AtomTypeName);
            if (type == null)
                throw new CDKException("Atom type is not a recognized CDK atom type: " + atom.AtomTypeName);

            if (type.FormalNeighbourCount == null)
                throw new CDKException($"Atom type is too general; cannot decide the number of implicit hydrogen to add for: {atom.AtomTypeName}");

            // very simply counting: each missing explicit neighbor is a missing hydrogen
            atom.ImplicitHydrogenCount = type.FormalNeighbourCount - container.GetConnectedBonds(atom).Count();
        }
    }
}
