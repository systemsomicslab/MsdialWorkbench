/* Copyright (C) 2001-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.IO.Iterator;
using NCDK.Smiles;
using System;
using System.Diagnostics;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// This Reader reads files which has one SMILES string on each
    /// line.
    /// </summary>
    /// <remarks>
    /// The format is given as below:
    /// <para>
    /// COC ethoxy ethane
    /// </para>
    /// Thus first the SMILES, and then after the first space (or tab) on the line a title
    /// that is stored as <see cref="CDKPropertyName.Title"/>. For legacy comparability the
    /// title is also placed in a "SMIdbNAME" property. If a line is invalid an empty
    /// molecule is inserted into the container set. The molecule with have the prop
    /// <see cref="EnumerableSMILESReader.BadSmilesInput"/> set to the input line that
    /// could not be read. 
    ///
    /// <para>For each line a molecule is generated, and multiple Molecules are
    /// read as MoleculeSet.</para>
    /// </remarks>
    /// <seealso cref="EnumerableSMILESReader"/>
    // @cdk.module  smiles
    // @cdk.iooptions
    // @cdk.keyword file format, SMILES
    public class SMILESReader
        : DefaultChemObjectReader
    {
        private readonly TextReader input;
        private SmilesParser sp;

        /// <summary>
        /// Construct a new reader from a Reader and a specified builder object.
        ///
        /// <param name="input">The Reader object from which to read structures</param>
        /// </summary>
        public SMILESReader(TextReader input)
        {
            this.input = input;
        }

        public SMILESReader(Stream input)
            : this(new StreamReader(input))
        {
        }

        public override IResourceFormat Format => SMILESFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemObjectSet<IAtomContainer>).IsAssignableFrom(type))
                return true;
            if (typeof(IChemFile).IsAssignableFrom(type))
                return true;
            return false;
        }

        /// <summary>
        /// Reads the content from a XYZ input. It can only return a
        /// <see cref="IChemObject"/> of type <see cref="IChemFile"/>.
        /// </summary>
        /// <param name="obj">class must be of type ChemFile</param>
        /// <seealso cref="IChemFile"/>
        public override T Read<T>(T obj)
        {
            sp = new SmilesParser(obj.Builder);

            switch (obj)
            {
                case IChemObjectSet<IAtomContainer> set:
                    return (T)ReadAtomContainerSet(set);
                case IChemFile file:
                    var sequence = file.Builder.NewChemSequence();
                    var chemModel = file.Builder.NewChemModel();
                    chemModel.MoleculeSet = ReadAtomContainerSet(file.Builder.NewAtomContainerSet());
                    sequence.Add(chemModel);
                    file.Add(sequence);
                    return (T)file;
                default:
                    throw new CDKException("Only supported is reading of MoleculeSet objects.");
            }
        }

        // private procedures

        /// <summary>
        /// Private method that actually parses the input to read a ChemFile
        /// object.
        /// </summary>
        /// <param name="som">The set of molecules that came from the file</param>
        /// <returns>A ChemFile containing the data parsed from input.</returns>
        private IChemObjectSet<IAtomContainer> ReadAtomContainerSet(IChemObjectSet<IAtomContainer> som)
        {
            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    line = line.Trim();
                    Debug.WriteLine($"Line: {line}");

                    string name = Suffix(line);

                    try
                    {
                        var molecule = sp.ParseSmiles(line);
                        molecule.SetProperty("SMIdbNAME", name);
                        som.Add(molecule);
                    }
                    catch (CDKException exception)
                    {
                        Trace.TraceWarning("This SMILES could not be parsed: ", line);
                        Trace.TraceWarning("Because of: ", exception.Message);
                        Debug.WriteLine(exception);
                        var empty = som.Builder.NewAtomContainer();
                        empty.SetProperty(EnumerableSMILESReader.BadSmilesInput, line);
                        som.Add(empty);
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError($"Error while reading SMILES line: {exception.Message}");
                Debug.WriteLine(exception);
            }
            return som;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (input != null)
                        input.Dispose();
                }
                
                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        /// <summary>
        /// Obtain the suffix after a line containing SMILES. The suffix follows
        /// any ' ' or '\t' termination characters.
        /// </summary>
        /// <param name="line">input line</param>
        /// <returns>the suffix - or an empty line</returns>
        private static string Suffix(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == ' ' || c == '\t')
                    return line.Substring(i + 1);
            }
            return null;
        }
    }
}
