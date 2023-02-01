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
    /// This Validator tests the internal data structures, and tries to detect inconsistencies in it.
    /// </summary>
    // @author   Egon Willighagen
    // @cdk.created  2003-08-22
    public class CDKValidator : AbstractValidator
    {
        public CDKValidator() { }

        public override ValidationReport ValidateChemFile(IChemFile subject)
        {
            return ValidateChemFileNulls(subject);
        }

        public override ValidationReport ValidateChemSequence(IChemSequence subject)
        {
            return ValidateChemSequenceNulls(subject);
        }

        private static ValidationReport ValidateChemFileNulls(IChemFile chemFile)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest hasNulls = new ValidationTest(chemFile, "ChemFile contains a null ChemSequence.");
            for (int i = 0; i < chemFile.Count; i++)
            { // DIRTY !!!! FIXME !!!!!
              // but it does not seem to work on 1.4.2 otherwise....
                if (chemFile[i] == null)
                {
                    report.Errors.Add(hasNulls);
                }
                else
                {
                    report.OKs.Add(hasNulls);
                }
            }
            return report;
        }

        private static ValidationReport ValidateChemSequenceNulls(IChemSequence sequence)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest hasNulls = new ValidationTest(sequence, "ChemSequence contains a null ChemModel.");
            for (int i = 0; i < sequence.Count; i++)
            { // DIRTY !!!! FIXME !!!!!
              // but it does not seem to work on 1.4.2 otherwise....
                if (sequence[i] == null)
                {
                    report.Errors.Add(hasNulls);
                }
                else
                {
                    report.OKs.Add(hasNulls);
                }
            }
            return report;
        }
    }
}
