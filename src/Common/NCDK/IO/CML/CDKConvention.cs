/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Common.Primitives;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace NCDK.IO.CML
{
    /// <summary>
    /// This is an implementation for the CDK convention.
    /// </summary>
    // @cdk.module io
    // @author egonw
    public class CDKConvention : CMLCoreModule
    {
        private bool isBond;

        public CDKConvention(IChemFile chemFile)
                : base(chemFile)
        { }

        public CDKConvention(ICMLModule conv)
            : base(conv)
        { }

        public override void StartDocument()
        {
            base.StartDocument();
            isBond = false;
        }

        public override void StartElement(CMLStack xpath, XElement element)
        {
            isBond = false;
            if (xpath.ToString().EndsWith("string/", StringComparison.Ordinal))
            {
                var a_buildin = element.Attribute("buildin");
                if (a_buildin != null && a_buildin.Value.Equals("order", StringComparison.Ordinal))
                {
                    isBond = true;
                }
            }
            else
            {
                base.StartElement(xpath, element);
            }
        }

        public override void CharacterData(CMLStack xpath, XElement element)
        {
            string s = element.Value;
            if (isBond)
            {
                Debug.WriteLine($"CharData (bond): {s}");
                var st = Strings.Tokenize(s);
                foreach (var border in st)
                {
                    Debug.WriteLine($"new bond order: {border}");
                    // assume cdk bond object has already started
                    //                cdo.SetObjectProperty("Bond", "order", border);
                    CurrentBond.Order = BondManipulator.CreateBondOrder(double.Parse(border, NumberFormatInfo.InvariantInfo));
                }
            }
            else
            {
                base.CharacterData(xpath, element);
            }
        }
    }
}
