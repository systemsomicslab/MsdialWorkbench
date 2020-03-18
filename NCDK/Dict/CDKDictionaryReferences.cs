/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

namespace NCDK.Dict
{
    /// <summary>
    /// This class transforms implicit references to dictionary of CDK
    /// objects into explicit references.
    /// </summary>
    /// <remarks>
    /// The syntax of the property names used is as follows:
    /// org.openscience.cdk.dict:self or
    /// org.openscience.cdk.dict:field:'fieldname', where fieldname
    /// indicates a field for this object. The name may be appended
    /// by :'number' to allow for more than one reference.
    ///</remarks>
    // @author     Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.created    2003-08-06
    // @cdk.keyword    dictionary, implicit CDK references
    // @cdk.module     dict
    public static class CDKDictionaryReferences
    {
        private const string prefix = DictionaryDatabase.DictRefPropertyName;

        public static void MakeReferencesExplicit(IChemObject o)
        {
            if (o == null)
            {
                throw new System.ArgumentNullException(nameof(o));
            }

            if (o is IAtom)
            {
                MakeReferencesExplicitForAtom((IAtom)o);
            }
            else if (o is IBond)
            {
                MakeReferencesExplicitForBond((IBond)o);
            }
            else if (o is IChemModel)
            {
                MakeReferencesExplicitForChemModel((IChemModel)o);
            }
            else if (o is IIsotope)
            {
                MakeReferencesExplicitForIsotope((IIsotope)o);
            }
            else if (o is IElement)
            {
                MakeReferencesExplicitForElement((IElement)o);
            }
            else if (o is IAtomContainer)
            {
                MakeReferencesExplicitForMolecule((IAtomContainer)o);
            }
            else if (o is IReaction)
            {
                MakeReferencesExplicitForReaction((IReaction)o);
            }
        }

        private static void MakeReferencesExplicitForAtom(IAtom atom)
        {
            int selfCounter = 0;
            atom.SetProperty(prefix + ":self:" + selfCounter++, "chemical:atom");

            MakeReferencesExplicitForElement(atom);
        }

        private static void MakeReferencesExplicitForBond(IBond bond)
        {
            int selfCounter = 0;
            bond.SetProperty(prefix + ":self:" + selfCounter++, "chemical:covalentBond");
            bond.SetProperty(prefix + ":field:order", "chemical:bondOrder");
        }

        private static void MakeReferencesExplicitForChemModel(IChemModel model)
        { // NOPMD
          // nothing to do
        }

        private static void MakeReferencesExplicitForElement(IElement element)
        {
            int selfCounter = 0;
            element.SetProperty(prefix + ":field:symbol", "chemical:atomSymbol");
            element.SetProperty(prefix + ":field:atomicNumber", "chemical:atomicNumber");

            switch (element.AtomicNumber)
            {
                case AtomicNumbers.C:
                    element.SetProperty(prefix + ":self:" + selfCounter++, "element:carbon");
                    break;
                case AtomicNumbers.N:
                    element.SetProperty(prefix + ":self:" + selfCounter++, "element:nitrogen");
                    break;
                case AtomicNumbers.O:
                    element.SetProperty(prefix + ":self:" + selfCounter++, "element:oxygen");
                    break;
                case AtomicNumbers.H:
                    element.SetProperty(prefix + ":self:" + selfCounter++, "element:hydrogen");
                    break;
                case AtomicNumbers.S:
                    element.SetProperty(prefix + ":self:" + selfCounter++, "element:sulphur");
                    break;
                case AtomicNumbers.P:
                    element.SetProperty(prefix + ":self:" + selfCounter++, "element:phosphorus");
                    break;
                default:
                    break;
            }
        }

        private static void MakeReferencesExplicitForIsotope(IIsotope isotope)
        {
            int selfCounter = 0;
            isotope.SetProperty(prefix + ":self:" + selfCounter++, "chemical:isotope");
        }

        private static void MakeReferencesExplicitForMolecule(IAtomContainer molecule)
        {
            int selfCounter = 0;
            molecule.SetProperty(prefix + ":self:" + selfCounter++, "chemical:molecularEntity");
            // remark: this is not strictly true... the Compendium includes the ion
            // pair, which normally would not considered a CDK molecule
        }

        private static void MakeReferencesExplicitForReaction(IReaction reaction)
        {
            int selfCounter = 0;
            reaction.SetProperty(prefix + ":self:" + selfCounter++, "reaction:reactionStep");
        }
    }
}
