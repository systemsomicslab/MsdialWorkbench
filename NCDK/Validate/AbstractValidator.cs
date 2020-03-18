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
    /// Abstract validator that does nothing but provide all the methods that the ValidatorInterface requires.
    /// </summary>
    // @cdk.module extra
    // @author   Egon Willighagen
    // @cdk.created  2004-03-27
    // @cdk.require java1.4+
    public class AbstractValidator : IValidator
    {
        public virtual ValidationReport ValidateChemObject(IChemObject subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateAtom(IAtom subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateAtomContainer(IAtomContainer subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateAtomType(IAtomType subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateBond(IBond subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateChemFile(IChemFile subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateChemModel(IChemModel subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateChemSequence(IChemSequence subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateCrystal(ICrystal subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateElectronContainer(IElectronContainer subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateElement(IElement subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateIsotope(IIsotope subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateMolecule(IAtomContainer subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateReaction(IReaction subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateMoleculeSet(IChemObjectSet<IAtomContainer> subject)
        {
            var report = new ValidationReport();
            return report;
        }

        public virtual ValidationReport ValidateReactionSet(IReactionSet subject)
        {
            var report = new ValidationReport();
            return report;
        }
    }
}
