/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.Smiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NCDK.IO.Iterator
{
    /// <summary>
    /// Iterating SMILES file reader. It allows to iterate over all molecules
    /// in the SMILES file, without being read into memory all. Suitable
    /// for very large SMILES files. These SMILES files are expected to have one
    /// molecule on each line. If a line could not be parsed and empty molecule is
    /// returned and the property <see cref="BadSmilesInput"/> is set to the attempted
    /// input. The error is also logged.
    /// 
    /// <para>For parsing each SMILES it still uses the normal <see cref="SMILESReader"/>.</para>
    /// </summary>
    /// <seealso cref="SMILESReader"/>
    // @cdk.module smiles
    // @cdk.iooptions
    // @author     Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.created    2004-12-16
    // @cdk.keyword    file format, SMILES
    public class EnumerableSMILESReader : DefaultEnumerableChemObjectReader<IAtomContainer>
    {
        private TextReader input;
        private SmilesParser sp = null;
        private readonly IChemObjectBuilder builder;

        /// <summary>Store the problem input as a property.</summary>
        public const string BadSmilesInput = "bad.smiles.input";

        public EnumerableSMILESReader(TextReader input)
            : this(input, CDK.Builder)
        {
        }

        public EnumerableSMILESReader(Stream input)
            : this(input, CDK.Builder)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="EnumerableSMILESReader"/> that can read molecule from a given reader.
        /// </summary>
        /// <param name="input">The Reader to read from</param>
        /// <param name="builder">The builder to use</param>
        public EnumerableSMILESReader(TextReader input, IChemObjectBuilder builder)
        {
            sp = new SmilesParser(builder);
            this.input = input;
            this.builder = builder;
        }

        /// <summary>
        /// Constructs a new <see cref="EnumerableSMILESReader"/> that can read Molecule from a given <see cref="Stream"/>  and <see cref="IChemObjectBuilder"/> .
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="builder">The builder</param>
        public EnumerableSMILESReader(Stream input, IChemObjectBuilder builder)
           : this(new StreamReader(input), builder)
        {
        }

        /// <summary>
        /// Get the format for this reader.
        /// </summary>
        /// <returns>An instance of <see cref="SMILESFormat"/></returns>
        public override IResourceFormat Format => SMILESFormat.Instance;

        /// <summary>
        /// Checks whether there is another molecule to read.
        /// </summary>
        /// <returns>true if there are molecules to read, false otherwise</returns>
        public override IEnumerator<IAtomContainer> GetEnumerator()
        {
            string line;
            // now try to parse the next Molecule
            while ((line = input.ReadLine()) != null)
            {
                IAtomContainer nextMolecule;
                try
                {
                    string suffix = Suffix(line);
                    nextMolecule = ReadSmiles(line);
                    nextMolecule.Title = suffix;
                }
                catch (Exception exception)
                {
                    Trace.TraceError($"Unexpected problem: {exception.Message}");
                    Debug.WriteLine(exception);
                    yield break;
                }
                yield return nextMolecule;
            }
            yield break;
        }

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
                char c = line[i];
                if (c == ' ' || c == '\t') return line.Substring(i + 1);
            }
            return "";
        }

        /// <summary>
        /// Read the SMILES given in the input line - or return an empty container.
        /// </summary>
        /// <param name="line">input line</param>
        /// <returns>the read container (or an empty one)</returns>
        private IAtomContainer ReadSmiles(string line)
        {
            try
            {
                return sp.ParseSmiles(line);
            }
            catch (CDKException e)
            {
                Trace.TraceError("Error while reading the SMILES from: " + line + ", ", e);
                IAtomContainer empty = builder.NewAtomContainer();
                empty.SetProperty(BadSmilesInput, line);
                return empty;
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

        public override void Remove()
        {
            throw new NotSupportedException();
        }
    }
}
