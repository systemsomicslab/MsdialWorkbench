/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

namespace NCDK.Validate
{
    /// <summary>
    /// Interface that Validators need to implement to be used in validation.
    /// </summary>
    // @author   Egon Willighagen
    // @cdk.created  2003-03-28
    public interface IValidator
    {
        ValidationReport ValidateAtom(IAtom subject);

        ValidationReport ValidateAtomContainer(IAtomContainer subject);

        ValidationReport ValidateAtomType(IAtomType subject);

        ValidationReport ValidateBond(IBond subject);

        ValidationReport ValidateChemFile(IChemFile subject);

        ValidationReport ValidateChemModel(IChemModel subject);

        ValidationReport ValidateChemObject(IChemObject o);

        ValidationReport ValidateChemSequence(IChemSequence subject);

        ValidationReport ValidateCrystal(ICrystal subject);

        ValidationReport ValidateElectronContainer(IElectronContainer subject);

        ValidationReport ValidateElement(IElement subject);

        ValidationReport ValidateIsotope(IIsotope subject);

        ValidationReport ValidateMolecule(IAtomContainer subject);

        ValidationReport ValidateReaction(IReaction subject);

        ValidationReport ValidateMoleculeSet(IChemObjectSet<IAtomContainer> subject);

        ValidationReport ValidateReactionSet(IReactionSet subject);
    }
}
