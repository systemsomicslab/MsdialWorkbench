/* Copyright (C) 2005-2007  Martin Eklund <martin.eklund@farmbio.uu.se>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Dict;
using NCDK.IO;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NCDK.Templates
{
    /// <summary>
    /// Tool that provides templates for the (natural) amino acids.
    /// </summary>
    // @author      Martin Eklund <martin.eklund@farmbio.uu.se>
    // @cdk.module  pdb
    // @cdk.keyword templates
    // @cdk.keyword amino acids, stuctures
    // @cdk.created 2005-02-08
    public static class AminoAcids
    {
        public const string ResidueNameKey = "residueName";
        public const string ResidueNameShortKey = "residueNameShort";
        public const string NoAtomsKey = "noOfAtoms";
        public const string NoBoundsKey = "noOfBonds";
        public const string IdKey = "id";

        private static readonly IAminoAcid[] proteinogenics;
        private static readonly Dictionary<string, IAminoAcid> singleLetterCodeMap;
        private static readonly Dictionary<string, IAminoAcid> threeLetterCodeMap;
        private static readonly Dictionary<string, string> singleLetterToThreeLetter;
        private static readonly Dictionary<string, string> threeLetterToSingleLetter;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static AminoAcids()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            // Create set of AtomContainers
            proteinogenics = new IAminoAcid[20];

            #region Create proteinogenics
            {
                IChemFile list = CDK.Builder.NewChemFile();
                using (var reader = new CMLReader(ResourceLoader.GetAsStream("NCDK.Templates.Data.list_aminoacids.cml")))
                {
                    try
                    {
                        list = (IChemFile)reader.Read(list);
                        var containersList = ChemFileManipulator.GetAllAtomContainers(list);
                        int counter = 0;
                        foreach (var ac in containersList)
                        {
                            Debug.WriteLine($"Adding AA: {ac}");
                            // convert into an AminoAcid
                            var aminoAcid = CDK.Builder.NewAminoAcid();
                            foreach (var next in ac.GetProperties().Keys)
                            {
                                Debug.WriteLine("Prop: " + next.ToString());
                                if (next is DictRef dictRef)
                                {
                                    // Debug.WriteLine("DictRef type: " + dictRef.Type}");
                                    if (string.Equals(dictRef.Type, "pdb:residueName", StringComparison.Ordinal))
                                    {
                                        aminoAcid.SetProperty(ResidueNameKey, ac.GetProperty<string>(next).ToUpperInvariant());
                                        aminoAcid.MonomerName = ac.GetProperty<string>(next);
                                    }
                                    else if (string.Equals(dictRef.Type, "pdb:oneLetterCode", StringComparison.Ordinal))
                                    {
                                        aminoAcid.SetProperty(ResidueNameShortKey, ac.GetProperty<string>(next));
                                    }
                                    else if (string.Equals(dictRef.Type, "pdb:id", StringComparison.Ordinal))
                                    {
                                        aminoAcid.SetProperty(IdKey, ac.GetProperty<string>(next));
                                        Debug.WriteLine($"Set AA ID to: {ac.GetProperty<string>(next)}");
                                    }
                                    else
                                    {
                                        Trace.TraceError("Cannot deal with dictRef!");
                                    }
                                }
                            }
                            foreach (var atom in ac.Atoms)
                            {
                                string dictRef = atom.GetProperty<string>("org.openscience.cdk.dict");
                                switch (dictRef)
                                {
                                    case "pdb:nTerminus":
                                        aminoAcid.AddNTerminus(atom);
                                        break;
                                    case "pdb:cTerminus":
                                        aminoAcid.AddCTerminus(atom);
                                        break;
                                    default:
                                        aminoAcid.Atoms.Add(atom);
                                        break;
                                }
                            }
                            foreach (var bond in ac.Bonds)
                            {
                                aminoAcid.Bonds.Add(bond);
                            }
                            AminoAcidManipulator.RemoveAcidicOxygen(aminoAcid);
                            aminoAcid.SetProperty(NoAtomsKey, "" + aminoAcid.Atoms.Count);
                            aminoAcid.SetProperty(NoBoundsKey, "" + aminoAcid.Bonds.Count);
                            if (counter < proteinogenics.Length)
                            {
                                proteinogenics[counter] = aminoAcid;
                            }
                            else
                            {
                                Trace.TraceError("Could not store AminoAcid! Array too short!");
                            }
                            counter++;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (exception is CDKException | exception is IOException)
                        {
                            Trace.TraceError($"Failed reading file: {exception.Message}");
                            Debug.WriteLine(exception);
                        }
                        else
                            throw;
                    }
                }
            }
            #endregion

            int count = proteinogenics.Length;
            singleLetterCodeMap = new Dictionary<string, IAminoAcid>(count);
            threeLetterCodeMap = new Dictionary<string, IAminoAcid>(count);
            singleLetterToThreeLetter = new Dictionary<string, string>(count);
            threeLetterToSingleLetter = new Dictionary<string, string>(count);

            foreach (IAminoAcid aa in proteinogenics)
            {
                var single = aa.GetProperty<string>(ResidueNameShortKey);
                var three = aa.GetProperty<string>(ResidueNameKey);
                singleLetterCodeMap[single] = aa;
                threeLetterCodeMap[three] = aa;
                singleLetterToThreeLetter[single] = three;
                threeLetterToSingleLetter[three] = single;
            }
        }

        /// <summary>
        /// Proteinogenic amino acid list.
        /// </summary>
        public static IReadOnlyList<IAminoAcid> Proteinogenics => proteinogenics;

        /// <summary>
        /// Map where the key is one of G, A, V, L, I, S, T, C, M, D,
        /// N, E, Q, R, K, H, F, Y, W and P.
        /// </summary>
        public static IReadOnlyDictionary<string, IAminoAcid> MapBySingleCharCode => singleLetterCodeMap;

        /// <summary>
        /// Map where the key is one of GLY, ALA, VAL, LEU, ILE, SER,
        /// THR, CYS, MET, ASP, ASN, GLU, GLN, ARG, LYS, HIS, PHE, TYR, TRP AND PRO.
        /// </summary>
        public static IReadOnlyDictionary<string, IAminoAcid> MapByThreeLetterCode => threeLetterCodeMap;

        /// <summary>
        /// Get amino acid from string.
        /// </summary>
        /// <param name="code">Single or three letter code</param>
        /// <returns>The amino acid</returns>
        public static IAminoAcid FromString(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            IAminoAcid aminoAcid;
            switch (code.Length)
            {
                case 1:
                    if (MapBySingleCharCode.TryGetValue(code, out aminoAcid))
                        return aminoAcid;
                    break;
                case 3:
                    if (MapByThreeLetterCode.TryGetValue(code, out aminoAcid))
                        return aminoAcid;
                    break;
                default:
                    break;
            }

            throw new ArgumentException($"Unknown code {code}.", nameof(code));
        }

        /// <summary>
        /// Returns the one letter code of an amino acid given a three letter code.
        /// For example, it will return "V" when "Val" was passed.
        /// </summary>
        public static string ConvertThreeLetterCodeToOneLetterCode(string threeLetterCode)
        {
            if (!threeLetterToSingleLetter.TryGetValue(threeLetterCode, out string single))
                return null;
            return single;
        }

        /// <summary>
        /// Returns the three letter code of an amino acid given a one letter code.
        /// For example, it will return "Val" when "V" was passed.
        /// </summary>
        public static string ConvertOneLetterCodeToThreeLetterCode(string oneLetterCode)
        {
            if (!singleLetterToThreeLetter.TryGetValue(oneLetterCode, out string three))
                return null;
            return three;
        }
    }
}
