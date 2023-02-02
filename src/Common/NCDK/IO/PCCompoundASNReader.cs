/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.IO.Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// Reads an object from ASN formated input for PubChem Compound entries. The following
    /// bits are supported: atoms.aid, atoms.element, bonds.aid1, bonds.aid2. Additionally,
    /// it extracts the InChI and canonical SMILES properties.
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @cdk.keyword file format, PubChem Compound ASN
    public class PCCompoundASNReader : DefaultChemObjectReader
    {
        private TextReader input;
        IAtomContainer molecule = null;
        Dictionary<string, IAtom> atomIDs = null;

        /// <summary>
        /// Construct a new reader from a Reader type object.
        /// </summary>
        /// <param name="input">reader from which input is read</param>
        public PCCompoundASNReader(TextReader input)
        {
            this.input = input;
        }

        public PCCompoundASNReader(Stream input)
            : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => PubChemASNFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
#if !DEBUG
                try
                {
#endif
                    return (T)ReadChemFile((IChemFile)obj);
#if !DEBUG
                }
                catch (IOException e)
                {
                    throw new CDKException("An IO Exception occurred while reading the file.", e);
                }
                catch (CDKException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new CDKException("An error occurred.", e);
                }
#endif
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile objects.");
            }
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Dispose();
                }

                input = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
#endregion

        // private procedures

        private IChemFile ReadChemFile(IChemFile file)
        {
            var chemSequence = file.Builder.NewChemSequence();
            var chemModel = file.Builder.NewChemModel();
            var moleculeSet = file.Builder.NewAtomContainerSet();
            molecule = file.Builder.NewAtomContainer();
            atomIDs = new Dictionary<string, IAtom>();

            string line = input.ReadLine();
            while (line != null)
            {
                if (line.IndexOf('{') != -1)
                {
                    ProcessBlock(line);
                }
                else
                {
                    Trace.TraceWarning("Skipping non-block: " + line);
                }
                line = input.ReadLine();
            }
            moleculeSet.Add(molecule);
            chemModel.MoleculeSet = moleculeSet;
            chemSequence.Add(chemModel);
            file.Add(chemSequence);
            return file;
        }

        private void ProcessBlock(string line)
        {
            string command = GetCommand(line);
            if (string.Equals(command, "atoms", StringComparison.Ordinal))
            {
                // parse frame by frame
                Debug.WriteLine("ASN atoms found");
                ProcessAtomBlock();
            }
            else if (string.Equals(command, "bonds", StringComparison.Ordinal))
            {
                // ok, that fine
                Debug.WriteLine("ASN bonds found");
                ProcessBondBlock();
            }
            else if (string.Equals(command, "props", StringComparison.Ordinal))
            {
                // ok, that fine
                Debug.WriteLine("ASN props found");
                ProcessPropsBlock();
            }
            else if (string.Equals(command, "PC-Compound ::=", StringComparison.Ordinal))
            {
                // ok, that fine
                Debug.WriteLine("ASN PC-Compound found");
            }
            else
            {
                Trace.TraceWarning("Skipping block: " + command);
                SkipBlock();
            }
        }

        private void ProcessPropsBlock()
        {
            string line = input.ReadLine();
            while (line != null)
            {
                if (line.IndexOf('{') != -1)
                {
                    ProcessPropsBlockBlock();
                }
                else if (line.IndexOf('}') != -1)
                {
                    return;
                }
                else
                {
                    Trace.TraceWarning("Skipping non-block: " + line);
                }
                line = input.ReadLine();
            }
        }

        private void ProcessPropsBlockBlock()
        {
            var line = input.ReadLine();
            var urn = new URN();
            while (line != null)
            {
                if (line.Contains("urn"))
                {
                    urn = ExtractURN();
                }
                else if (line.Contains("value"))
                {
                    Debug.WriteLine($"Found a prop value line: {line}");
                    if (line.Contains(" sval"))
                    {
                        Debug.WriteLine($"Label: {urn.Label}");
                        Debug.WriteLine($"Name: {urn.Name}");
                        switch (urn.Label)
                        {
                            case "InChI":
                                {
                                    string value = GetQuotedValue(line.Substring(line.IndexOf("value sval", StringComparison.Ordinal) + 10));
                                    molecule.SetProperty(CDKPropertyName.InChI, value);
                                }
                                break;
                            case "SMILES":
                                if ("Canonical".Equals(urn.Name, StringComparison.Ordinal))
                                {
                                    string value = GetQuotedValue(line.Substring(line.IndexOf("value sval", StringComparison.Ordinal) + 10));
                                    molecule.SetProperty(CDKPropertyName.SMILES, value);
                                }
                                break;
                        }
                    }
                }
                else if (line.IndexOf('}') != -1)
                {
                    return;
                }
                else
                {
                    Trace.TraceWarning("Skipping non-block: " + line);
                }
                line = input.ReadLine();
            }
        }

        private URN ExtractURN()
        {
            var urn = new URN();
            var line = input.ReadLine();
            while (line != null)
            {
                int n;
                if ((n = line.IndexOf("name", StringComparison.Ordinal)) != -1)
                    urn.Name = GetQuotedValue(line.Substring(n + 4));
                else if ((n = line.IndexOf("label", StringComparison.Ordinal)) != -1)
                    urn.Label = GetQuotedValue(line.Substring(n + 5));
                else if (line.IndexOf('}') != -1 && line.IndexOf('\"') == -1)
                {
                    // ok, don't return if it also has a "
                    return urn;
                }
                else
                {
                    Trace.TraceWarning("Ignoring URN statement: " + line);
                }
                line = input.ReadLine();
            }
            return urn;
        }

        private void ProcessAtomBlock()
        {
            string line = input.ReadLine();
            while (line != null)
            {
                if (line.IndexOf('{') != -1)
                {
                    ProcessAtomBlockBlock(line);
                }
                else if (line.IndexOf('}') != -1)
                {
                    return;
                }
                else
                {
                    Trace.TraceWarning("Skipping non-block: " + line);
                }
                line = input.ReadLine();
            }
        }

        private void ProcessBondBlock()
        {
            string line = input.ReadLine();
            var newBondInfo = new NewBondInfo();
            while (line != null)
            {
                if (line.IndexOf('{') != -1)
                {
                    ProcessBondBlockBlock(line, newBondInfo);
                }
                else if (line.IndexOf('}') != -1)
                {
                    break;
                }
                else
                {
                    Trace.TraceWarning("Skipping non-block: " + line);
                }
                line = input.ReadLine();
            }
            foreach (var info in newBondInfo.IndexToAtoms)
            {
                SetBondAtoms(info.Key, info.Value[0], info.Value[1]);
            }
        }

        private IAtom GetAtom(int i)
        {
            if (molecule.Atoms.Count <= i)
            {
                molecule.Atoms.Add(molecule.Builder.NewAtom());
            }
            return molecule.Atoms[i];
        }

        private void SetBondAtoms(int i, IAtom atom1, IAtom atom2)
        {
            if (molecule.Bonds.Count <= i)
            {
                molecule.Bonds.Add(molecule.Builder.NewBond(atom1, atom2));
            }
            else
            {
                molecule.Bonds[i].Atoms[0] = atom1;
                molecule.Bonds[i].Atoms[1] = atom2;
            }
        }

        private void ProcessAtomBlockBlock(string line)
        {
            var command = GetCommand(line);
            if (string.Equals(command, "aid", StringComparison.Ordinal))
            {
                // assume this is the first block in the atom block
                Debug.WriteLine("ASN atoms aid found");
                ProcessAtomAIDs();
            }
            else if (string.Equals(command, "element", StringComparison.Ordinal))
            {
                // assume this is the first block in the atom block
                Debug.WriteLine("ASN atoms element found");
                ProcessAtomElements();
            }
            else
            {
                Trace.TraceWarning("Skipping atom block block: " + command);
                SkipBlock();
            }
        }

        private void ProcessBondBlockBlock(string line, NewBondInfo newBondInfo)
        {
            var command = GetCommand(line);
            if (string.Equals(command, "aid1", StringComparison.Ordinal))
            {
                // assume this is the first block in the atom block
                Debug.WriteLine("ASN bonds aid1 found");
                ProcessBondAtomIDs(0, newBondInfo);
            }
            else if (string.Equals(command, "aid2", StringComparison.Ordinal))
            {
                // assume this is the first block in the atom block
                Debug.WriteLine("ASN bonds aid2 found");
                ProcessBondAtomIDs(1, newBondInfo);
            }
            else
            {
                Trace.TraceWarning("Skipping atom block block: " + command);
                SkipBlock();
            }
        }

        private void ProcessAtomAIDs()
        {
            var line = input.ReadLine();
            int atomIndex = 0;
            while (line != null)
            {
                if (line.IndexOf('}') != -1)
                {
                    // done
                    return;
                }
                else
                {
                    IAtom atom = GetAtom(atomIndex);
                    string id = GetValue(line);
                    atom.Id = id;
                    atomIDs[id] = atom;
                    atomIndex++;
                }
                line = input.ReadLine();
            }
        }

        private void ProcessBondAtomIDs(int pos, NewBondInfo newBondInfo)
        {
            var line = input.ReadLine();
            int bondIndex = 0;
            while (line != null)
            {
                if (line.IndexOf('}') != -1)
                {
                    // done
                    return;
                }
                else
                {
                    var id = GetValue(line);
                    var atom = (IAtom)atomIDs[id];
                    if (atom == null)
                        throw new CDKException($"File is corrupt: atom ID does not exist {id}");
                    newBondInfo.Set(bondIndex, pos, atom);
                    bondIndex++;
                }
                line = input.ReadLine();
            }
        }

        class NewBondInfo
        {
            public Dictionary<int, IAtom[]> IndexToAtoms = new Dictionary<int, IAtom[]>();

            public void Set(int bondIndex, int position, IAtom atom)
            {
                if (!IndexToAtoms.ContainsKey(bondIndex))
                    IndexToAtoms.Add(bondIndex, new IAtom[2]);
                IndexToAtoms[bondIndex][position] = atom;
            }
        }

        private void ProcessAtomElements()
        {
            var line = input.ReadLine();
            int atomIndex = 0;
            while (line != null)
            {
                if (line.IndexOf('}') != -1)
                {
                    // done
                    return;
                }
                else
                {
                    var atom = GetAtom(atomIndex);
                    atom.Symbol = ToSymbol(GetValue(line));
                    atomIndex++;
                }
                line = input.ReadLine();
            }
        }

        private static string ToSymbol(string value)
        {
            if (value.Length == 1)
                return value.ToUpperInvariant();
            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);
        }

        private void SkipBlock()
        {
            var line = input.ReadLine();
            int openBrackets = 0;
            while (line != null)
            {
                //            Debug.WriteLine($"SkipBlock: line={line}");
                if (line.IndexOf('{') != -1)
                {
                    openBrackets++;
                }
                //            Debug.WriteLine($" #open brackets: {openBrackets}");
                if (line.IndexOf('}') != -1)
                {
                    if (openBrackets == 0)
                        return;
                    openBrackets--;
                }
                line = input.ReadLine();
            }
        }

        private static string GetCommand(string line)
        {
            var buffer = new StringBuilder();
            int i = 0;
            bool foundBracket = false;
            while (i < line.Length && !foundBracket)
            {
                var currentChar = line[i];
                if (currentChar == '{')
                {
                    foundBracket = true;
                }
                else
                {
                    buffer.Append(currentChar);
                }
                i++;
            }
            return foundBracket ? buffer.ToString().Trim() : null;
        }

        private static string GetValue(string line)
        {
            var buffer = new StringBuilder();
            int i = 0;
            bool foundComma = false;
            bool preWS = true;
            while (i < line.Length && !foundComma)
            {
                var currentChar = line[i];
                if (char.IsWhiteSpace(currentChar))
                {
                    if (!preWS)
                        buffer.Append(currentChar);
                }
                else if (currentChar == ',')
                {
                    foundComma = true;
                }
                else
                {
                    buffer.Append(currentChar);
                    preWS = false;
                }
                i++;
            }
            return buffer.ToString();
        }

        private string GetQuotedValue(string line)
        {
            var buffer = new StringBuilder();
            int i = 0;
            //        Debug.WriteLine($"QV line: {line}");
            bool startQuoteFound = false;
            while (line != null)
            {
                while (i < line.Length)
                {
                    char currentChar = line[i];
                    if (currentChar == '"')
                    {
                        if (startQuoteFound)
                        {
                            return buffer.ToString();
                        }
                        else
                        {
                            startQuoteFound = true;
                        }
                    }
                    else if (startQuoteFound)
                    {
                        buffer.Append(currentChar);
                    }
                    i++;
                }
                line = input.ReadLine();
                i = 0;
            }
            return null;
        }

        struct URN
        {
            public string Name { get; set; }
            public string Label { get; set; }
        }
    }
}
