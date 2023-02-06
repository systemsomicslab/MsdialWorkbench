/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License as published by the Free
 * Software Foundation; either version 2.1 of the License, or (at your option)
 * any later version. All we ask is that proper credit is given for our work,
 * which includes - but is not limited to - adding the above copyright notice to
 * the beginning of your source code files, and to any copyright notice that you
 * may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.IO.Formats;
using NCDK.Smiles;
using System;
using System.Diagnostics;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// Reader for MoSS output files <token>cdk-cite-BOR2002</token> which present the results
    /// of a substructure mining study. These files look like:
    /// <pre>
    /// id,description,nodes,edges,s_abs,s_rel,c_abs,c_rel
    /// 1,S-c:c:c:c:c:c,7,6,491,5.055081,5,1.7421603
    /// 2,S-c:c:c:c:c,6,5,493,5.0756717,5,1.7421603
    /// </pre>
    /// <note type="caution">
    /// The output contains substructures, not full molecules,
    /// even though they are read as such right now.
    /// </note>
    /// </summary>
    // @cdk.module  smiles
    // @cdk.iooptions
    // @cdk.keyword MoSS
    public class MoSSOutputReader : DefaultChemObjectReader
    {
        private TextReader input;

        /// <summary>
        /// Create a reader for MoSS output files from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="input">source of CIF data</param>
        public MoSSOutputReader(TextReader input)
        {
            this.input = input;
        }

        /// <summary>
        /// Create a reader for MoSS output files from an <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">source of CIF data</param>
        public MoSSOutputReader(Stream input)
                : this(new StreamReader(input))
        { }

        /// <inheritdoc/>    
        public override IResourceFormat Format => MoSSOutputFormat.Instance;

        /// <inheritdoc/>
        public override bool Accepts(Type type)
        {
            if (typeof(IChemObjectSet<IAtomContainer>).IsAssignableFrom(type)) return true;
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// Read a <see cref="IChemObjectSet{T}"/> from the input source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">object an <see cref="IChemObjectSet{T}"/> into which the data is stored.</param>
        /// <returns>the content in a <see cref="IChemObjectSet{T}"/> object</returns>
        public override T Read<T>(T obj)
        {
            if (obj is IChemObjectSet<IAtomContainer> cf)
            {
                try
                {
                    cf = ReadAtomContainerSet(cf);
                }
                catch (IOException)
                {
                    Trace.TraceError("Input/Output error while reading from input.");
                }
                return (T)cf;
            }
            else if (obj is IChemFile chemFile)
            {
                IChemSequence chemSeq = obj.Builder.NewChemSequence();
                IChemModel chemModel = obj.Builder.NewChemModel();
                IChemObjectSet<IAtomContainer> molSet = obj.Builder.NewAtomContainerSet();
                try
                {
                    molSet = ReadAtomContainerSet(molSet);
                }
                catch (IOException)
                {
                    Trace.TraceError("Input/Output error while reading from input.");
                }
                chemModel.MoleculeSet = molSet;
                chemSeq.Add(chemModel);
                chemFile.Add(chemSeq);
                return (T)chemFile;
            }
            else
            {
                throw new CDKException("Only supported is reading of IAtomContainerSet.");
            }
        }

        /// <summary>
        /// Read the file content into a <see cref="IAtomContainerSet"/>.
        /// </summary>
        /// <param name="molSet">an <see cref="IAtomContainerSet"/> to store the structures</param>
        /// <returns>the <see cref="IAtomContainerSet"/> containing the molecules read in</returns>
        /// <exception cref="IOException">if there is an error during reading</exception>
        private IChemObjectSet<IAtomContainer> ReadAtomContainerSet(IChemObjectSet<IAtomContainer> molSet)
        {
            SmilesParser parser = new SmilesParser(molSet.Builder, false);
            string line = input.ReadLine();
            line = input.ReadLine(); // skip the first line
            while (line != null)
            {
                string[] cols = line.Split(',');
                try
                {
                    var mol = parser.ParseSmiles(cols[1]);
                    mol.SetProperty("focusSupport", cols[5]);
                    mol.SetProperty("complementSupport", cols[7]);
                    mol.SetProperty("atomCount", cols[2]);
                    mol.SetProperty("bondCount", cols[3]);
                    molSet.Add(mol);
                }
                catch (InvalidSmilesException exception)
                {
                    Trace.TraceError($"Skipping invalid SMILES: {cols[1]}");
                    Debug.WriteLine(exception);
                }
                line = input.ReadLine();
            }
            return molSet;
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
