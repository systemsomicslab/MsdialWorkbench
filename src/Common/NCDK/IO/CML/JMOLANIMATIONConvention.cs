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
    // @author Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.module io
    public class JMOLANIMATIONConvention : CMLCoreModule
    {
        private const int Unknown = -1;
        private const int ENERGY = 1;

        private int current;
        private string frame_energy;

        public JMOLANIMATIONConvention(IChemFile chemFile)
            : base(chemFile)
        {
            current = Unknown;
        }

        public JMOLANIMATIONConvention(ICMLModule conv)
            : base(conv)
        { }

        public override void StartElement(CMLStack xpath, XElement element)
        {
            string name = element.Name.LocalName;
            if (string.Equals(name, "list", StringComparison.Ordinal))
            {
                Debug.WriteLine("Oke, JMOLANIMATION seems to be kicked in :)");
                //            cdo.StartObject("Animation");
                CurrentChemSequence = CurrentChemFile.Builder.NewChemSequence();
                base.StartElement(xpath, element);
            }
            else if (string.Equals(name, "molecule", StringComparison.Ordinal))
            {
                //            cdo.StartObject("Frame");
                CurrentChemModel = CurrentChemFile.Builder.NewChemModel();
                Debug.WriteLine("New frame being parsed.");
                base.StartElement(xpath, element);
            }
            else if (string.Equals(name, "float", StringComparison.Ordinal))
            {
                bool isEnergy = false;
                Debug.WriteLine("FLOAT found!");
                foreach (var att in element.Attributes())
                {
                    Debug.WriteLine($" att: {att.Name.LocalName} -> {att.Value}");
                    if (att.Name.LocalName.Equals("title", StringComparison.Ordinal) && att.Value.Equals("FRAME_ENERGY", StringComparison.Ordinal))
                    {
                        isEnergy = true;
                    }
                }
                if (isEnergy)
                {
                    // oke, this is the frames energy!
                    current = ENERGY;
                }
                else
                {
                    base.StartElement(xpath, element);
                }
            }
            else
            {
                base.StartElement(xpath, element);
            }
        }

        public override void EndElement(CMLStack xpath, XElement element)
        {
            string name = element.Name.LocalName;
            if (current == ENERGY)
            {
                //            cdo.SetObjectProperty("Frame", "energy", frame_energy);
                // + " " + units);
                // FIXME: does not have a ChemFileCDO equivalent
                current = Unknown;
                frame_energy = "";
            }
            else if (string.Equals(name, "list", StringComparison.Ordinal))
            {
                base.EndElement(xpath, element);
                //            cdo.EndObject("Animation");
                CurrentChemFile.Add(CurrentChemSequence);
            }
            else if (string.Equals(name, "molecule", StringComparison.Ordinal))
            {
                base.EndElement(xpath, element);
                //            cdo.EndObject("Frame");
                // nothing done in the CD upon this event
            }
            else
            {
                base.EndElement(xpath, element);
            }
        }

        public override void CharacterData(CMLStack xpath, XElement element)
        {
            if (current == ENERGY)
            {
                frame_energy = element.Value;
            }
            else
            {
                base.CharacterData(xpath, element);
            }
        }
    }
}
