/* Copyright (C) 2007  Ola Spjuth <ospjuth@users.sf.net>
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

using NCDK.LibIO.MD;
using System.Xml.Linq;
using static NCDK.LibIO.CML.CMLElement;

namespace NCDK.LibIO.CML
{
    /// <summary>
    /// Customize persistence of MDMolecule by adding support for residues and charge groups.
    /// </summary>
    // @author ola
    // @cdk.module libiomd
    public class MDMoleculeCustomizer : ICMLCustomizer
    {
        internal static readonly XNamespace NS_MD = "http://www.bioclipse.net/mdmolecule/";

        /// <summary>
        /// No customization for bonds.
        /// </summary>
        /// <param name="bond"></param>
        /// <param name="nodeToAdd"></param>
        public void Customize(IBond bond, object nodeToAdd)
        {
            // nothing to do
        }

        /// <summary>
        /// Customize Atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="nodeToAdd"></param>
        public void Customize(IAtom atom, object nodeToAdd)
        {
            // nothing to do
        }

        /// <summary>
        /// Customize Molecule.
        /// </summary>
        /// <param name="molecule"></param>
        /// <param name="nodeToAdd"></param>
        public void Customize(IAtomContainer molecule, object nodeToAdd)
        {
            if (!(nodeToAdd is CMLMolecule))
                throw new CDKException("NodeToAdd must be of type nu.xom.Element!");

            //The nodeToAdd
            CMLMolecule molToCustomize = (CMLMolecule)nodeToAdd;

            if ((molecule is MDMolecule))
            {
                MDMolecule mdmol = (MDMolecule)molecule;
                molToCustomize.Convention = "md:mdMolecule";
                molToCustomize.SetAttributeValue(XNamespace.Xmlns + "md", NS_MD.NamespaceName);

                //Residues
                if (mdmol.GetResidues().Count > 0)
                {
                    foreach (var residue in mdmol.GetResidues())
                    {
                        int number = residue.GetNumber();

                        CMLMolecule resMol = new CMLMolecule
                        {
                            DictRef = "md:residue",
                            Title = residue.Name
                        };

                        //Append resNo
                        CMLScalar residueNumber = new CMLScalar(number);
                        residueNumber.SetAttributeValue(Attribute_dictRef, "md:resNumber");
                        resMol.Add(residueNumber);

                        // prefix for residue atom id
                        string rprefix = "r" + number;
                        //Append atoms
                        CMLAtomArray ar = new CMLAtomArray();
                        for (int i = 0; i < residue.Atoms.Count; i++)
                        {
                            CMLAtom cmlAtom = new CMLAtom
                            {
                                //                        Console.Out.WriteLine("atom ID: "+ residue.Atoms[i].Id);
                                //                        cmlAtom.AddAttribute(new Attribute("ref", residue.Atoms[i].Id));
                                // the next thing is better, but  exception
                                //
                                // setRef to keep consistent usage
                                // setId to satisfy Jumbo 54. need for all atoms to have id
                                Ref = residue.Atoms[i].Id,
                                Id = rprefix + "_" + residue.Atoms[i].Id
                            };
                            ar.Add(cmlAtom);
                        }
                        resMol.Add(ar);

                        molToCustomize.Add(resMol);
                    }
                }

                //Chargegroups
                if (mdmol.GetChargeGroups().Count > 0)
                {
                    foreach (var chargeGroup in mdmol.GetChargeGroups())
                    {
                        int number = chargeGroup.GetNumber();

                        //FIXME: persist the ChargeGroup
                        CMLMolecule cgMol = new CMLMolecule
                        {
                            DictRef = "md:chargeGroup"
                        };
                        // etc: add name, refs to atoms etc

                        //Append chgrpNo
                        CMLScalar cgNo = new CMLScalar(number)
                        {
                            DictRef = "md:cgNumber"
                        };
                        cgMol.Add(cgNo);

                        // prefix for residue atom id
                        string cprefix = "cg" + number;

                        //Append atoms from chargeGroup as it is an AC
                        CMLAtomArray ar = new CMLAtomArray();
                        for (int i = 0; i < chargeGroup.Atoms.Count; i++)
                        {
                            CMLAtom cmlAtom = new CMLAtom
                            {
                                // setRef to keep consistent usage
                                // setId to satisfy Jumbo 5.4 need for all atoms to have id
                                Ref = chargeGroup.Atoms[i].Id,
                                Id = cprefix + "_" + chargeGroup.Atoms[i].Id
                            };

                            //Append switching atom?
                            if (chargeGroup.Atoms[i].Equals(chargeGroup.GetSwitchingAtom()))
                            {
                                CMLScalar scalar = new CMLScalar
                                {
                                    DictRef = "md:switchingAtom"
                                };
                                cmlAtom.Add(scalar);
                            }
                            ar.Add(cmlAtom);
                        }
                        cgMol.Add(ar);

                        molToCustomize.Add(cgMol);
                    }
                }
            }
        }
    }
}
