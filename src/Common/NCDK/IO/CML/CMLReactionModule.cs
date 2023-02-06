/* Copyright (C) 2003-2007  Egon Willighagen <egonw@users.sf.net>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace NCDK.IO.CML
{
    // @author Egon Willighagen <elw38@cam.ac.uk>
    // @cdk.module io
    public class CMLReactionModule : CMLCoreModule
    {
#if DEBUG
        internal string objectType;
#endif

        public CMLReactionModule(IChemFile chemFile)
            : base(chemFile)
        { }

        public CMLReactionModule(ICMLModule conv)
            : base(conv)
        {
            Debug.WriteLine("New CML-Reaction Module!");
        }

        public override void StartElement(CMLStack xpath, XElement element)
        {
            if (string.Equals("reaction", element.Name.LocalName, StringComparison.Ordinal))
            {
                //            cdo.StartObject("Reaction");
                if (CurrentReactionSet == null)
                    CurrentReactionSet = CurrentChemFile.Builder.NewReactionSet();
                CurrentReaction = CurrentChemFile.Builder.NewReaction();
                string id = AttGetValue(element.Attributes(), "id");
                if (id != null) CurrentReaction.Id = id;
                //                cdo.SetObjectProperty("Reaction", "id", id);
            }
            else if (string.Equals("reactionList", element.Name.LocalName, StringComparison.Ordinal))
            {
                //            cdo.StartObject("ReactionSet");
                CurrentReactionSet = CurrentChemFile.Builder.NewReactionSet();
                string id = AttGetValue(element.Attributes(), "id");
                if (id != null) CurrentReactionSet.Id = id;
                //                cdo.SetObjectProperty("reactionList", "id", id);
            }
            else if (string.Equals("reactant", element.Name.LocalName, StringComparison.Ordinal))
            {
                //            cdo.StartObject("Reactant");
                if (CurrentReaction == null)
                {
                    if (CurrentReactionSet == null)
                        CurrentReactionSet = CurrentChemFile.Builder.NewReactionSet();
                    CurrentReaction = CurrentChemFile.Builder.NewReaction();
                }
                CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
#if DEBUG
                objectType = "Reactant";
#endif
                string id = AttGetValue(element.Attributes(), "id");
                if (id != null)
                    CurrentMolecule.Id = id;
                else
                {
                    string ref_ = AttGetValue(element.Attributes(), "ref");
                    if (ref_ != null) CurrentMolecule.Id = ref_;
                }
                //                cdo.SetObjectProperty("Reactant", "id", id);
            }
            else if (string.Equals("product", element.Name.LocalName, StringComparison.Ordinal))
            {
                //            cdo.StartObject("Product");
                if (CurrentReaction == null)
                {
                    if (CurrentReactionSet == null)
                        CurrentReactionSet = CurrentChemFile.Builder.NewReactionSet();
                    CurrentReaction = CurrentChemFile.Builder.NewReaction();
                }
                CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
#if DEBUG
                objectType = "Product";
#endif
                string id = AttGetValue(element.Attributes(), "id");
                if (id != null)
                    CurrentMolecule.Id = id;
                else
                {
                    string ref_ = AttGetValue(element.Attributes(), "ref");
                    if (ref_ != null) CurrentMolecule.Id = ref_;
                }
                //                cdo.SetObjectProperty("Product", "id", id);
            }
            else if (string.Equals("substance", element.Name.LocalName, StringComparison.Ordinal))
            {
                //            cdo.StartObject("Agent");
                if (CurrentReaction == null)
                {
                    if (CurrentReactionSet == null)
                        CurrentReactionSet = CurrentChemFile.Builder.NewReactionSet();
                    CurrentReaction = CurrentChemFile.Builder.NewReaction();
                }
                CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
#if DEBUG
                objectType = "Agent";
#endif
                string id = AttGetValue(element.Attributes(), "id");
                if (id != null)
                    CurrentMolecule.Id = id;
                else
                {
                    string ref_ = AttGetValue(element.Attributes(), "ref");
                    if (ref_ != null) CurrentMolecule.Id = ref_;
                }
                //                cdo.SetObjectProperty("Agent", "id", id);
            }
            else if (string.Equals("molecule", element.Name.LocalName, StringComparison.Ordinal))
            {
                // clear existing molecule data
                base.NewMolecule();
                string id = AttGetValue(element.Attributes(), "id");
                if (id != null)
                {
                    // check for existing molecule of that id
                    IAtomContainer existing = GetMoleculeFromID(CurrentMoleculeSet, id);
                    if (existing != null)
                    {
                        CurrentMolecule = existing;
                    }
                    else
                    {
                        CurrentMolecule.Id = id;
                    }
                }
                else
                {
                    string ref_ = AttGetValue(element.Attributes(), "ref");
                    if (ref_ != null)
                    {
                        IAtomContainer atomC = GetMoleculeFromID(CurrentMoleculeSet, ref_);

                        // if there was no molecule create a new one for the reference. this
                        // happens when the reaction is defined before the molecule set
                        if (atomC == null)
                        {
                            atomC = CurrentChemFile.Builder.NewAtomContainer();
                            atomC.Id = ref_;
                            CurrentMoleculeSet.Add(atomC);
                        }

                        base.CurrentMolecule = atomC;
                    }
                }
            }
            else
            {
                base.StartElement(xpath, element);
            }
        }

        public override void EndElement(CMLStack xpath, XElement element)
        {
            var local = element.Name.LocalName;

            switch (local)
            {
                case "reaction":
                    CurrentReactionSet.Add(CurrentReaction);
                    CurrentChemModel.ReactionSet = CurrentReactionSet;
                    break;
                case "reactionList":
                    CurrentChemModel.ReactionSet = CurrentReactionSet;
                    /* FIXME: this should be when document is closed! */
                    break;
                case "reactant":
                    CurrentReaction.Reactants.Add(CurrentMolecule);
                    break;
                case "product":
                    CurrentReaction.Products.Add(CurrentMolecule);
                    break;
                case "substance":
                    CurrentReaction.Agents.Add(CurrentMolecule);
                    break;
                case "molecule":
                    Debug.WriteLine("Storing Molecule");
                    //if the current molecule exists in the currentMoleculeSet means that is a reference in these.
                    if (CurrentMoleculeSet.GetMultiplier(CurrentMolecule) == -1) base.StoreData();
                    // do nothing else but store atom/bond information
                    break;
                default:
                    base.EndElement(xpath, element);
                    break;
            }                    
        }

        /// <summary>
        /// Get the IAtomContainer contained in a IAtomContainerSet object with a ID.
        /// </summary>
        /// <param name="molSet">Molecules</param>
        /// <param name="id">The ID the look</param>
        /// <returns>The IAtomContainer with the ID</returns>
        private static IAtomContainer GetMoleculeFromID(IEnumerable<IAtomContainer> molSet, string id)
        {
            foreach (var mol in molSet)
            {
                if (string.Equals(mol.Id, id, StringComparison.Ordinal))
                    return mol;
            }
            return null;
        }
    }
}
