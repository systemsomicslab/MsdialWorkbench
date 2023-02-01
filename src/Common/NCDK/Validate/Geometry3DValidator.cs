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

using NCDK.Numerics;

namespace NCDK.Validate
{
    /// <summary>
    /// Validates the 3D geometry of the model.
    /// </summary>
    // @cdk.module  extra
    // @cdk.created 2006-05-11
    public class Geometry3DValidator : AbstractValidator
    {
        public Geometry3DValidator() { }

        // assumes 1 unit in the coordinate system is one angstrom
        public override ValidationReport ValidateBond(IBond subject)
        {
            var report = new ValidationReport();
            // only consider two atom bonds
            if (subject.Atoms.Count == 2)
            {
                var distance = Vector3.Distance(subject.Begin.Point3D.Value, subject.End.Point3D.Value);
                if (distance > 3.0)
                { // should really depend on the elements
                    var badBondLengthError = new ValidationTest(subject,
                            "Bond length cannot exceed 3 Angstroms.",
                            "A bond length typically is between 0.5 and 3.0 Angstroms.");
                    report.Errors.Add(badBondLengthError);
                }
            }
            return report;
        }
    }
}

