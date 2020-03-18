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

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// Reads a molecule from an MDL RXN file <token>cdk-cite-DAL92</token>.
    /// This MDL RXN reader uses the MDLV2000 reader to read each mol file
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @author     Egon Willighagen
    // @author        Thomas Kuhn
    // @cdk.created    2003-07-24
    // @cdk.keyword    file format, MDL RXN
    // @cdk.bug        1849923
    public class MDLRXNV2000Reader : DefaultChemObjectReader
    {
        TextReader input = null;

        /// <summary>
        /// Constructs a new MDLReader that can read Molecule from a given Reader.
        /// </summary>
        /// <param name="input">The Reader to read from</param>
        public MDLRXNV2000Reader(TextReader input)
            : this(input, ChemObjectReaderMode.Relaxed)
        { }

        public MDLRXNV2000Reader(TextReader input, ChemObjectReaderMode mode)
        {
            this.input = input;
            base.ReaderMode = mode;
        }

        public MDLRXNV2000Reader(Stream input)
            : this(input, ChemObjectReaderMode.Relaxed)
        { }

        public MDLRXNV2000Reader(Stream input, ChemObjectReaderMode mode)
            : this(new StreamReader(input), mode)
        { }

        public override IResourceFormat Format => MDLRXNV2000Format.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            if (typeof(IChemModel).IsAssignableFrom(type)) return true;
            if (typeof(IReaction).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// Takes an object which subclasses IChemObject, e.g.Molecule, and will read
        /// this (from file, database, Internet etc). If the specific implementation
        /// does not support a specific IChemObject it will throw an Exception.
        /// </summary>
        /// <param name="obj">The object that subclasses <see cref="IChemObject"/></param>
        /// <returns>The <see cref="IChemObject"/> read</returns>
        /// <exception cref="CDKException"></exception>
        public override T Read<T>(T obj)
        {
            if (obj is IReaction)
            {
                return (T)ReadReaction(obj.Builder);
            }
            else if (obj is IReactionSet)
            {
                IReactionSet reactionSet = obj.Builder.NewReactionSet();
                reactionSet.Add(ReadReaction(obj.Builder));
                return (T)reactionSet;
            }
            else if (obj is IChemModel)
            {
                IChemModel model = obj.Builder.NewChemModel();
                IReactionSet reactionSet = obj.Builder.NewReactionSet();
                reactionSet.Add(ReadReaction(obj.Builder));
                model.ReactionSet = reactionSet;
                return (T)model;
            }
            else if (obj is IChemFile)
            {
                IChemFile chemFile = obj.Builder.NewChemFile();
                IChemSequence sequence = obj.Builder.NewChemSequence();
                sequence.Add((IChemModel)Read(obj.Builder.NewChemModel()));
                chemFile.Add(sequence);
                return (T)chemFile;
            }
            else
            {
                throw new CDKException($"Only supported are Reaction and ChemModel, and not {obj.GetType().Name}.");
            }
        }

        public virtual bool Accepts(IChemObject o)
        {
            if (o is IReaction)
            {
                return true;
            }
            else if (o is IChemModel)
            {
                return true;
            }
            else if (o is IChemFile)
            {
                return true;
            }
            else if (o is IReactionSet)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Read a Reaction from a file in MDL RXN format
        /// </summary>
        /// <returns>The Reaction that was read from the MDL file.</returns>
        private IReaction ReadReaction(IChemObjectBuilder builder)
        {
            IReaction reaction = builder.NewReaction();
            try
            {
                input.ReadLine(); // first line should be $RXN
                input.ReadLine(); // second line
                input.ReadLine(); // third line
                input.ReadLine(); // fourth line
            }
            catch (IOException exception)
            {
                Debug.WriteLine(exception);
                throw new CDKException("Error while reading header of RXN file", exception);
            }

            int numReactans = 0;
            int numProducts = 0;
            int agentCount = 0;
            try
            {
                var countsLine = input.ReadLine();

                // this line contains the number of reactants and products
                var tokenizer = Strings.Tokenize(countsLine).GetEnumerator();
                tokenizer.MoveNext();
                numReactans = int.Parse(tokenizer.Current, NumberFormatInfo.InvariantInfo);
                Trace.TraceInformation($"Expecting {numReactans} reactants in file");
                tokenizer.MoveNext();
                numProducts = int.Parse(tokenizer.Current, NumberFormatInfo.InvariantInfo);

                if (tokenizer.MoveNext())
                {
                    agentCount = int.Parse(tokenizer.Current, NumberFormatInfo.InvariantInfo);
                    // ChemAxon extension, technically BIOVIA now support this but
                    // not documented yet
                    if (ReaderMode == ChemObjectReaderMode.Strict && agentCount > 0)
                        throw new CDKException("RXN files uses agent count extension");
                }
                Trace.TraceInformation($"Expecting {numProducts} products in file");
            }
            catch (Exception exception)
            {
                if (exception is IOException | exception is FormatException)
                {
                    Debug.WriteLine(exception);
                    throw new CDKException("Error while counts line of RXN file", exception);
                }
                throw;
            }

            // now read the molecules
            try
            {
                var line = input.ReadLine();
                if (line == null || !line.StartsWith("$MOL", StringComparison.Ordinal))
                {
                    throw new CDKException("Expected $MOL to start, was" + line);
                }

                var components = new List<IAtomContainer>();

                var sb = new StringBuilder();
                while ((line = input.ReadLine()) != null)
                {
                    if (line.StartsWith("$MOL", StringComparison.Ordinal))
                    {
                        ProcessMol(builder.NewAtomContainer(), components, sb);
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(line).Append('\n');
                    }
                }

                // last record
                if (sb.Length > 0)
                    ProcessMol(builder.NewAtomContainer(), components, sb);

                foreach (var component in components.GetRange(0, numReactans))
                {
                    reaction.Reactants.Add(component);
                }
                foreach (var component in components.GetRange(numReactans, numProducts))
                {
                    reaction.Products.Add(component);
                }
                foreach (var component in components.GetRange(numReactans + numProducts, components.Count - (numReactans + numProducts)))
                {
                    reaction.Agents.Add(component);
                }
            }
            catch (CDKException)
            {
                // rethrow exception from MDLReader
                throw;
            }
            catch (Exception exception)
            {
                if (exception is IOException | exception is ArgumentException)
                {
                    Debug.WriteLine(exception);
                    throw new CDKException("Error while reading reactant", exception);
                }
                throw;
            }

            // now try to map things, if wanted
            Trace.TraceInformation("Reading atom-atom mapping from file");
            // distribute all atoms over two AtomContainer's
            var reactingSide = builder.NewAtomContainer();
            foreach (var molecule in reaction.Reactants)
            {
                reactingSide.Add(molecule);
            }
            var producedSide = builder.NewAtomContainer();
            foreach (var molecule in reaction.Products)
            {
                producedSide.Add(molecule);
            }

            // map the atoms
            int mappingCount = 0;
            for (int i = 0; i < reactingSide.Atoms.Count; i++)
            {
                for (int j = 0; j < producedSide.Atoms.Count; j++)
                {
                    var eductAtom = reactingSide.Atoms[i];
                    var productAtom = producedSide.Atoms[j];
                    if (eductAtom.GetProperty<object>(CDKPropertyName.AtomAtomMapping) != null
                            && eductAtom.GetProperty<object>(CDKPropertyName.AtomAtomMapping).Equals(
                                    productAtom.GetProperty<object>(CDKPropertyName.AtomAtomMapping)))
                    {
                        reaction.Mappings.Add(builder.NewMapping(eductAtom, productAtom));
                        mappingCount++;
                        break;
                    }
                }
            }
            Trace.TraceInformation("Mapped atom pairs: " + mappingCount);

            return reaction;
        }

        private void ProcessMol(IAtomContainer mol, List<IAtomContainer> components, StringBuilder sb)
        {
            using (var reader = new MDLV2000Reader(new StringReader(sb.ToString()), this.ReaderMode))
            {
                components.Add(reader.Read(mol));
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
    }
}
