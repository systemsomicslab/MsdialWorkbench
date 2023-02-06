/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.IO.Formats;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// Reader that extracts information from the IDENT, NAME, ATOMS and BONDS blocks in CTX files.
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    public class CTXReader : DefaultChemObjectReader
    {
        private TextReader input;
        private IChemFile file;

        public CTXReader(TextReader input)
        {
            this.input = input;
            file = null;
        }

        public CTXReader(Stream input)
                : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => CTXFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                file = (IChemFile)obj;
                return (T)ReadChemFile();
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile.");
            }
        }

        private IChemFile ReadChemFile()
        {
            IChemSequence seq = file.Builder.NewChemSequence();
            IChemModel model = file.Builder.NewChemModel();
            var containerSet = file.Builder.NewAtomContainerSet();
            IAtomContainer container = file.Builder.NewAtomContainer();

            int lineNumber = 0;

            try
            {
                string line = input.ReadLine();
                while (line != null)
                {
                    Debug.WriteLine((lineNumber++) + ": ", line);
                    string command = null;
                    if (IsCommand(line))
                    {
                        command = GetCommand(line);
                        int lineCount = GetContentLinesCount(line);
                        if (string.Equals("ATOMS", command, StringComparison.Ordinal))
                        {
                            ProcessAtomsBlock(lineCount, container);
                        }
                        else if (string.Equals("BONDS", command, StringComparison.Ordinal))
                        {
                            ProcessBondsBlock(lineCount, container);
                        }
                        else if (string.Equals("IDENT", command, StringComparison.Ordinal))
                        {
                            ProcessIdentBlock(lineCount, container);
                        }
                        else if (string.Equals("NAME", command, StringComparison.Ordinal))
                        {
                            ProcessNameBlock(lineCount, container);
                        }
                        else
                        {
                            // skip lines
                            Trace.TraceWarning("Dropping block: ", command);
                            for (int i = 0; i < lineCount; i++)
                                input.ReadLine();
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Unexpected content at line: ", lineNumber);
                    }
                    line = input.ReadLine();
                }
                containerSet.Add(container);
                model.MoleculeSet = containerSet;
                seq.Add(model);
                file.Add(seq);
            }
            catch (Exception exception)
            {
                string message = "Error while parsing CTX file: " + exception.Message;
                Trace.TraceError(message);
                Debug.WriteLine(exception);
                throw new CDKException(message, exception);
            }
            return file;
        }

        private void ProcessIdentBlock(int lineCount, IAtomContainer container)
        {
            string identifier = "";
            for (int i = 0; i < lineCount; i++)
            {
                identifier = identifier + input.ReadLine().Trim();
            }
            container.Id = identifier;
        }

        private void ProcessNameBlock(int lineCount, IAtomContainer container)
        {
            string name = "";
            for (int i = 0; i < lineCount; i++)
            {
                name = name + input.ReadLine().Trim();
            }
            container.Title = name;
        }

        private void ProcessAtomsBlock(int lineCount, IAtomContainer container)
        {
            for (int i = 0; i < lineCount; i++)
            {
                string line = input.ReadLine();
                int atomicNumber = int.Parse(line.Substring(7, 3).Trim(), NumberFormatInfo.InvariantInfo);
                IAtom atom = container.Builder.NewAtom();
                atom.AtomicNumber = atomicNumber;
                atom.Symbol = PeriodicTable.GetSymbol(atomicNumber);
                container.Atoms.Add(atom);
            }
        }

        private void ProcessBondsBlock(int lineCount, IAtomContainer container)
        {
            for (int i = 0; i < lineCount; i++)
            {
                string line = input.ReadLine();
                int atom1 = int.Parse(line.Substring(10, 3).Trim(), NumberFormatInfo.InvariantInfo) - 1;
                int atom2 = int.Parse(line.Substring(16, 3).Trim(), NumberFormatInfo.InvariantInfo) - 1;
                if (container.GetBond(container.Atoms[atom1], container.Atoms[atom2]) == null)
                {
                    IBond bond = container.Builder.NewBond(container.Atoms[atom1],
                            container.Atoms[atom2]);
                    int order = int.Parse(line.Substring(23).Trim(), NumberFormatInfo.InvariantInfo);
                    bond.Order = BondManipulator.CreateBondOrder((double)order);
                    container.Bonds.Add(bond);
                } // else: bond already present; CTX store the bonds twice
            }
        }

        private static int GetContentLinesCount(string line)
        {
            return int.Parse(line.Substring(18, 3).Trim(), NumberFormatInfo.InvariantInfo);
        }

        private static string GetCommand(string line)
        {
            return line.Substring(2, 8).Trim();
        }

        private static bool IsCommand(string line)
        {
            return (line.Length > 1 && line[0] == ' ' && line[1] == '/');
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
    }
}
