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

namespace NCDK.Validate
{
    /// <summary>
    /// A report on validation of chemical semantics.
    /// </summary>
    public class ValidationReport
    {
        private List<ValidationTest> errors;
        private List<ValidationTest> warnings;
        private List<ValidationTest> oks;
        private List<ValidationTest> cdkErrors;

        /// <summary>
        /// Constructs a new empty <see cref="ValidationReport"/>.
        /// </summary>
        public ValidationReport()
        {
            errors = new List<ValidationTest>();
            warnings = new List<ValidationTest>();
            oks = new List<ValidationTest>();
            cdkErrors = new List<ValidationTest>();
        }

        /// <summary>
        /// Merges the tests with the tests in this ValidationReport.
        /// </summary>
        /// <param name="report"></param>
        public void Add(ValidationReport report)
        {
            errors.AddRange(report.errors);
            warnings.AddRange(report.warnings);
            oks.AddRange(report.oks);
            cdkErrors.AddRange(report.cdkErrors);
        }

        /// <summary>
        /// Validation tests which gives serious errors.
        /// </summary>
        public IList<ValidationTest> Errors => errors;

        /// <summary>
        /// Validation tests which indicate a possible problem.
        /// </summary>
        public IList<ValidationTest> Warnings => warnings;

        /// <summary>
        /// Validation tests which did not find a problem.
        /// </summary>
        public IList<ValidationTest> OKs => oks;

        /// <summary>
        /// CDK problems.
        /// </summary>
        public IList<ValidationTest> CDKErrors => cdkErrors;

        /// <summary>
        /// The number of CDK errors.
        /// </summary>
        public int Count => cdkErrors.Count + errors.Count + warnings.Count + oks.Count;
    }
}
