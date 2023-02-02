/* Copyright (C) 2005-2007  The Chemistry Development Kit (CDK) Project
 *                    2014  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Templates;
using System;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Tools
{
    /// <summary>
    /// Class that facilitates building protein structures.
    /// </summary>
    // Building DNA and RNA is done by a complementary class <see cref="NucleicAcidBuilderTool"/> (to be written).
    // @cdk.module pdb
    public static class ProteinBuilderTool
    {
        /// <summary>
        /// Builds a protein by connecting a new amino acid at the N-terminus of the
        /// given strand.
        /// </summary>
        /// <param name="protein">protein to which the strand belongs</param>
        /// <param name="aaToAdd">amino acid to add to the strand of the protein</param>
        /// <param name="strand">strand to which the protein is added</param>
        /// <param name="aaToAddTo"></param>
        public static IBioPolymer AddAminoAcidAtNTerminus(IBioPolymer protein, IAminoAcid aaToAdd, IStrand strand, IAminoAcid aaToAddTo)
        {
            // then add the amino acid
            AddAminoAcid(protein, aaToAdd, strand);
            // Now think about the protein back bone connection
            if (!protein.GetMonomerMap().Any())
            {
                // make the connection between that aminoAcid's C-terminus and the
                // protein's N-terminus
                protein.Bonds.Add(aaToAdd.Builder.NewBond(aaToAddTo.NTerminus,
                        aaToAdd.CTerminus, BondOrder.Single));
            } // else : no current N-terminus, so nothing special to do
            return protein;
        }

        /// <summary>
        /// Builds a protein by connecting a new amino acid at the C-terminus of the
        /// given strand. The acidic oxygen of the added amino acid is removed so that
        /// additional amino acids can be added savely. But this also means that you
        /// might want to add an oxygen at the end of the protein building!
        /// </summary>
        /// <param name="protein">protein to which the strand belongs</param>
        /// <param name="aaToAdd">amino acid to add to the strand of the protein</param>
        /// <param name="strand">strand to which the protein is added</param>
        /// <param name="aaToAddTo"></param>
        public static IBioPolymer AddAminoAcidAtCTerminus(IBioPolymer protein, IAminoAcid aaToAdd, IStrand strand, IAminoAcid aaToAddTo)
        {
            // then add the amino acid
            AddAminoAcid(protein, aaToAdd, strand);
            // Now think about the protein back bone connection
            if (protein.GetMonomerMap().Any() && (aaToAddTo != null))
            {
                // make the connection between that aminoAcid's N-terminus and the
                // protein's C-terminus
                protein.Bonds.Add(aaToAdd.Builder.NewBond(aaToAddTo.CTerminus,
                        aaToAdd.NTerminus, BondOrder.Single));
            } // else : no current C-terminus, so nothing special to do
            return protein;
        }

        /// <summary>
        /// Creates a BioPolymer from a sequence of amino acid as identified by a
        /// the sequence of their one letter codes. It uses the <see cref="IChemObjectBuilder"/> 
        /// to create a data model.
        /// </summary>
        /// <example>
        /// For example:
        /// <code>
        /// IBioPolymer protein = ProteinBuilderTool.CreateProtein("GAGA");
        /// </code>
        /// </example>
        /// <seealso cref="CreateProtein(string)"/>
        public static IBioPolymer CreateProtein(string sequence)
        {
            return CreateProtein(sequence, CDK.Builder);
        }

        /// <summary>
        /// Creates a BioPolymer from a sequence of amino acid as identified by a
        /// the sequence of their one letter codes. It uses the given <see cref="IChemObjectBuilder"/>
        /// to create a data model.
        /// </summary>
        /// <example>
        /// For example:
        /// <code>
        /// IBioPolymer protein = ProteinBuilderTool.CreateProtein("GAGA", Silent.ChemObjectBuilder.Instance);
        /// </code>
        /// </example>
        /// <seealso cref="CreateProtein(string)"/>
        public static IBioPolymer CreateProtein(string sequence, IChemObjectBuilder builder)
        {
            var templates = AminoAcids.MapBySingleCharCode;
            var protein = builder.NewBioPolymer();
            var strand = builder.NewStrand();
            IAminoAcid previousAA = null;
            for (int i = 0; i < sequence.Length; i++)
            {
                string aminoAcidCode = "" + sequence[i];
                Debug.WriteLine($"Adding AA: {aminoAcidCode}");
                if (string.Equals(aminoAcidCode, " ", StringComparison.Ordinal))
                {
                    // fine, just skip spaces
                }
                else
                {
                    IAminoAcid aminoAcid = templates[aminoAcidCode];
                    if (aminoAcid == null)
                    {
                        throw new CDKException("Cannot build sequence! Unknown amino acid: " + aminoAcidCode);
                    }
                    aminoAcid = (IAminoAcid)aminoAcid.Clone();
                    aminoAcid.MonomerName = aminoAcidCode + i;
                    Debug.WriteLine($"protein: {protein}");
                    Debug.WriteLine($"strand: {strand}");
                    AddAminoAcidAtCTerminus(protein, aminoAcid, strand, previousAA);
                    previousAA = aminoAcid;
                }
            }
            // add the last oxygen of the protein
            var oxygen = builder.NewAtom("O");
            // ... to amino acid
            previousAA.Atoms.Add(oxygen);
            var bond = builder.NewBond(oxygen, previousAA.CTerminus, BondOrder.Single);
            previousAA.Bonds.Add(bond);
            // ... and to protein
            protein.AddAtom(oxygen, previousAA, strand);
            protein.Bonds.Add(bond);
            return protein;
        }

        private static IBioPolymer AddAminoAcid(IBioPolymer protein, IAminoAcid aaToAdd, IStrand strand)
        {
            foreach (var atom in aaToAdd.Atoms)
                protein.AddAtom(atom, aaToAdd, strand);
            foreach (var bond in aaToAdd.Bonds)
                protein.Bonds.Add(bond);
            return protein;
        }
    }
}
