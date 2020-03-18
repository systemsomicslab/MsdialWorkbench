/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
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

using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace NCDK.IO.CML
{
    /// <summary>
    /// Implementation of the MDLMol Covention for CML.
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class MDLMolConvention : CMLCoreModule
    {
        public MDLMolConvention(IChemFile chemFile)
            : base(chemFile)
        { }

        public MDLMolConvention(ICMLModule conv)
            : base(conv)
        { }

        public override void StartDocument()
        {
            base.StartDocument();
            //        cdo.StartObject("Frame");
            CurrentChemModel = CurrentChemFile.Builder.NewChemModel();
        }

        public override void StartElement(CMLStack xpath, XElement element)
        {
            Debug.WriteLine("MDLMol element: name");
            base.StartElement(xpath, element);
        }

        public override void CharacterData(CMLStack xpath, XElement element)
        {
            string s = element.Value.Trim();
            if (xpath.ToString().EndsWith("string/", StringComparison.Ordinal) && Builtin.Equals("stereo", StringComparison.Ordinal))
            {
                StereoGiven = true;
                switch (s.Trim())
                {
                    case "W":
                        Debug.WriteLine("CML W stereo found");
                        BondStereo.Add("1");
                        break;
                    case "H":
                        Debug.WriteLine("CML H stereo found");
                        BondStereo.Add("6");
                        break;
                }
            }
            else
            {
                base.CharacterData(xpath, element);
            }
        }
    }
}
