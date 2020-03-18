/* Copyright (C) 2003-2008  Egon Willighagen <egonw@users.sf.net>
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

using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Validate
{
    /// <summary>
    /// Engine that performs the validation by traversing the <see cref="IChemObject"/> hierarchy. 
    /// </summary>
    /// <example>
    /// Basic use of the <see cref="ValidatorEngine"/> is:
    /// <code>
    /// ValidatorEngine engine = new ValidatorEngine();
    /// engine.Add(new BasicValidator());
    /// var report = engine.ValidateMolecule(molecule);
    /// </code>
    /// </example>
    // @author   Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.created  2003-08-22
    public class ValidatorEngine 
        : IValidator
    {
        private Dictionary<string, IValidator> validators;

        public ValidatorEngine()
        {
            validators = new Dictionary<string, IValidator>();
        }

        public void Add(IValidator validator)
        {
            Trace.TraceInformation($"Registering validator: {validator.GetType().FullName}");
            string validatorName = validator.GetType().FullName;
            if (validators.ContainsKey(validatorName))
            {
                Trace.TraceWarning("  already registered.");
            }
            else
            {
                validators[validatorName] = validator;
            }
        }

        public void Remove(IValidator validator)
        {
            Trace.TraceInformation("Removing validator: " + validator.GetType().FullName);
            string validatorName = validator.GetType().FullName;
            if (!validators.ContainsKey(validatorName))
            {
                Trace.TraceWarning("  not in list.");
            }
            else
            {
                validators.Remove(validatorName);
            }
        }

        public ValidationReport ValidateAtom(IAtom subject)
        {
            Trace.TraceInformation($"Validating {nameof(IAtom)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateAtom(subject));
            }
            // traverse into super class
            report.Add(ValidateAtomType(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateAtomContainer(IAtomContainer subject)
        {
            Trace.TraceInformation($"Validating {nameof(IAtomContainer)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateAtomContainer(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            foreach (var atom in subject.Atoms)
            {
                report.Add(ValidateAtom(atom));
            }

            foreach (var bond in subject.Bonds)
            {
                report.Add(ValidateBond(bond));
            }
            return report;
        }

        public ValidationReport ValidateAtomType(IAtomType subject)
        {
            Trace.TraceInformation($"Validating {nameof(IAtomType)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateAtomType(subject));
            }
            // traverse into super class
            report.Add(ValidateIsotope(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateBond(IBond subject)
        {
            Trace.TraceInformation($"Validating {nameof(IBond)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateBond(subject));
            }
            // traverse into super class
            report.Add(ValidateElectronContainer(subject));
            // traverse into hierarchy
            foreach (var atom in subject.Atoms)
            {
                report.Add(ValidateAtom(atom));
            }
            return report;
        }

        public ValidationReport ValidateChemFile(IChemFile subject)
        {
            Trace.TraceInformation($"Validating {nameof(IChemFile)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateChemFile(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            foreach (var sequence in subject)
            {
                report.Add(ValidateChemSequence(sequence));
            }
            return report;
        }

        public ValidationReport ValidateChemModel(IChemModel subject)
        {
            Trace.TraceInformation($"Validating {nameof(IChemModel)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateChemModel(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            ICrystal crystal = subject.Crystal;
            if (crystal != null)
            {
                report.Add(ValidateCrystal(crystal));
            }
            IReactionSet reactionSet = subject.ReactionSet;
            if (reactionSet != null)
            {
                report.Add(ValidateReactionSet(reactionSet));
            }
            var moleculeSet = subject.MoleculeSet;
            if (moleculeSet != null)
            {
                report.Add(ValidateMoleculeSet(moleculeSet));
            }
            return report;
        }

        public ValidationReport ValidateChemObject(IChemObject subject)
        {
            Trace.TraceInformation($"Validating {nameof(IChemObject)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateChemObject(subject));
            }
            // traverse into super class
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateChemSequence(IChemSequence subject)
        {
            Trace.TraceInformation($"Validating {nameof(IChemSequence)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateChemSequence(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            foreach (var model in subject)
            {
                report.Add(ValidateChemModel(model));
            }
            return report;
        }

        public ValidationReport ValidateCrystal(ICrystal subject)
        {
            Trace.TraceInformation($"Validating {nameof(ICrystal)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateCrystal(subject));
            }
            // traverse into super class
            report.Add(ValidateAtomContainer(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateElectronContainer(IElectronContainer subject)
        {
            Trace.TraceInformation($"Validating {nameof(IElectronContainer)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateElectronContainer(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateElement(IElement subject)
        {
            Trace.TraceInformation($"Validating {nameof(IElement)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateElement(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateIsotope(IIsotope subject)
        {
            Trace.TraceInformation($"Validating {nameof(IIsotope)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateIsotope(subject));
            }
            // traverse into super class
            report.Add(ValidateElement(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateMolecule(IAtomContainer subject)
        {
            Trace.TraceInformation($"Validating {nameof(IAtomContainer)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateMolecule(subject));
            }
            // traverse into super class
            report.Add(ValidateAtomContainer(subject));
            // traverse into hierarchy
            return report;
        }

        public ValidationReport ValidateReaction(IReaction subject)
        {
            Trace.TraceInformation($"Validating {nameof(IReaction)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateReaction(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            var reactants = subject.Reactants;
            foreach (var reactant in reactants)
            {
                report.Add(ValidateMolecule(reactant));
            }
            var products = subject.Products;
            foreach (var product in products)
            {
                report.Add(ValidateMolecule(product));
            }
            return report;
        }

        public ValidationReport ValidateMoleculeSet(IChemObjectSet<IAtomContainer> subject)
        {
            Trace.TraceInformation($"Validating {nameof(IChemObjectSet<IAtomContainer>)}");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateMoleculeSet(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            foreach (var container in subject)
            {
                report.Add(ValidateMolecule(container));
            }
            return report;
        }

        public ValidationReport ValidateReactionSet(IReactionSet subject)
        {
            Trace.TraceInformation("Validating org.openscience.cdk.ReactionSet");
            var report = new ValidationReport();
            // apply validators
            foreach (var test in validators.Values)
            {
                report.Add(test.ValidateReactionSet(subject));
            }
            // traverse into super class
            report.Add(ValidateChemObject(subject));
            // traverse into hierarchy
            foreach (var reaction in subject)
            {
                report.Add(ValidateReaction(reaction));
            }
            return report;
        }
    }
}
