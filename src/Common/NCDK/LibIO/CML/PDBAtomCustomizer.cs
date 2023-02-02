/*  Copyright (C) 2005-2007  The Chemistry Development Kit (CDK) project
 *                     2013  Egon Willighagen <egonw@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Xml.Linq;

namespace NCDK.LibIO.CML
{
    /// <summary>
    /// <see cref="ICMLCustomizer"/> for the libio-cml <see cref="Convertor"/> to be able to export details for <see cref="IPDBAtom"/>'s.
    /// </summary>
    // @author        egonw
    // @cdk.created   2005-05-04
    // @cdk.module pdbcml
    // @cdk.require java1.5+
    public class PDBAtomCustomizer : ICMLCustomizer
    {
        public void Customize(IAtom atom, object nodeToAdd)
        {
            if (!(nodeToAdd is XElement))
                throw new CDKException("NodeToAdd must be of type nu.xom.Element!");

            XElement element = (XElement)nodeToAdd;
            if (atom is IPDBAtom pdbAtom)
            {
                if (HasContent(pdbAtom.AltLoc))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:altLoc"
                    };
                    scalar.Add(pdbAtom.AltLoc);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.ChainID))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:chainID"
                    };
                    scalar.Add(pdbAtom.ChainID);
                    element.Add(scalar);
                }

                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:hetAtom"
                    };
                    scalar.Add("" + pdbAtom.HetAtom);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.ICode))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:iCode"
                    };
                    scalar.Add(pdbAtom.ICode);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.Name))
                {
                    var scalar = new CMLLabel
                    {
                        DictRef = "pdb:name"
                    };
                    scalar.Add(pdbAtom.Name);
                    element.Add(scalar);
                }

                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:oxt"
                    };
                    scalar.Add("" + pdbAtom.Oxt);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.Record))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:record"
                    };
                    scalar.Add(pdbAtom.Record);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.ResName))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:resName"
                    };
                    scalar.Add(pdbAtom.ResName);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.ResSeq))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:resSeq"
                    };
                    scalar.Add(pdbAtom.ResSeq);
                    element.Add(scalar);
                }

                if (HasContent(pdbAtom.SegID))
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:segID"
                    };
                    scalar.Add(pdbAtom.SegID);
                    element.Add(scalar);
                }

                if (pdbAtom.Serial != 0)
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:serial"
                    };
                    scalar.Add("" + pdbAtom.Serial);
                    element.Add(scalar);
                }

                if (pdbAtom.TempFactor != -1.0)
                {
                    var scalar = new CMLScalar
                    {
                        DictRef = "pdb:tempFactor"
                    };
                    scalar.Add("" + pdbAtom.TempFactor);
                    element.Add(scalar);
                }

                element.SetAttributeValue("occupancy", "" + pdbAtom.Occupancy);

                // remove isotope info
                element.SetAttributeValue("isotopeNumber", null);
            }
        }

        private static bool HasContent(string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public void Customize(IAtomContainer molecule, object nodeToAdd)
        {
            // nothing to do at this moment
        }

        public void Customize(IBond bond, object nodeToAdd)
        {
            // nothing to do at this moment
        }
    }
}
