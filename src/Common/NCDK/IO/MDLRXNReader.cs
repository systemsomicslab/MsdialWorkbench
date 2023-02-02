/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// Reads a molecule from an MDL RXN file <token>cdk-cite-DAL92</token>.
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @author     Egon Willighagen
    // @cdk.created    2003-07-24
    // @cdk.keyword    file format, MDL RXN
    [Obsolete("Use V2000 or V3000")]
    public class MDLRXNReader : DefaultChemObjectReader
    {
        TextReader input = null;

        /// <summary>
        /// Constructs a new MDLReader that can read Molecule from a given Reader.
        /// </summary>
        /// <param name="ins">The Reader to read from</param>
        public MDLRXNReader(TextReader ins)
            : this(ins, ChemObjectReaderMode.Relaxed)
        {
        }

        public MDLRXNReader(TextReader ins, ChemObjectReaderMode mode)
        {
            input = ins;
            base.ReaderMode = mode;
        }

        public MDLRXNReader(Stream input)
            : this(input, ChemObjectReaderMode.Relaxed)
        { }

        public MDLRXNReader(Stream input, ChemObjectReaderMode mode)
            : this(new StreamReader(input), mode)
        { }

        public override IResourceFormat Format => MDLRXNV2000Format.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            if (typeof(IChemModel).IsAssignableFrom(type)) return true;
            if (typeof(IReaction).IsAssignableFrom(type)) return true;
            if (typeof(IReactionSet).IsAssignableFrom(type)) return true;
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// Takes an object which subclasses IChemObject, e.g.Molecule, and will read
        /// this (from file, database, internet etc). If the specific implementation
        /// does not support a specific <see cref="IChemObject"/> it will throw an Exception.
        /// </summary>
        /// <param name="obj">The object that subclasses <see cref="IChemObject"/></param>
        /// <returns>The IChemObject read</returns>
        /// <exception cref="CDKException"></exception>
        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                return (T)ReadChemFile((IChemFile)obj);
            }
            else if (obj is IChemModel)
            {
                return (T)ReadChemModel((IChemModel)obj);
            }
            else if (obj is IReactionSet)
            {
                return (T)ReadReactionSet((IReactionSet)obj);
            }
            else if (obj is IReaction)
            {
                return (T)ReadReaction(obj.Builder);
            }
            else
            {
                throw new CDKException("Only supported are Reaction, ReactionSet, ChemModel and ChemFile, and not "
                        + obj.GetType().Name + ".");
            }
        }

        public static bool Accepts(IChemObject o)
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
        /// Read a ChemFile from a file in MDL RDF format.
        /// </summary>
        /// <param name="chemFile">The IChemFile</param>
        /// <returns>The IChemFile that was read from the RDF file.</returns>
        private IChemFile ReadChemFile(IChemFile chemFile)
        {
            IChemSequence chemSequence = chemFile.Builder.NewChemSequence();

            IChemModel chemModel = chemFile.Builder.NewChemModel();
            chemSequence.Add(ReadChemModel(chemModel));
            chemFile.Add(chemSequence);
            return chemFile;
        }

        /// <summary>
        /// Read a IChemModel from a file in MDL RDF format.
        /// </summary>
        /// <param name="chemModel">The IChemModel</param>
        /// <returns>The IChemModel that was read from the RDF file</returns>
        private IChemModel ReadChemModel(IChemModel chemModel)
        {
            IReactionSet setOfReactions = chemModel.ReactionSet;
            if (setOfReactions == null)
            {
                setOfReactions = chemModel.Builder.NewReactionSet();
            }
            chemModel.ReactionSet = ReadReactionSet(setOfReactions);
            return chemModel;
        }

        /// <summary>
       /// Read a IReactionSet from a file in MDL RDF format.
       ///
       /// <param name="setOfReactions">The IReactionSet</param>
       /// <returns>The IReactionSet that was read from the RDF file</returns>
       /// </summary>
        private IReactionSet ReadReactionSet(IReactionSet setOfReactions)
        {

            IReaction r = ReadReaction(setOfReactions.Builder);
            if (r != null)
            {
                setOfReactions.Add(r);
            }

            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    Debug.WriteLine($"line: {line}");
                    // apparently, this is a SDF file, continue with
                    // reading mol files
                    if (string.Equals(line, "$$$$", StringComparison.Ordinal))
                    {
                        r = ReadReaction(setOfReactions.Builder);

                        if (r != null)
                        {
                            setOfReactions.Add(r);
                        }
                    }
                    else
                    {
                        // here the stuff between 'M  END' and '$$$$'
                        if (r != null)
                        {
                            // ok, the first lines should start with '>'
                            string fieldName = null;
                            if (line.StartsWith("> ", StringComparison.Ordinal))
                            {
                                // ok, should extract the field name
                                int index = line.IndexOf('<');
                                if (index != -1)
                                {
                                    int index2 = line.Substring(index).IndexOf('>');
                                    if (index2 != -1)
                                    {
                                        fieldName = line.Substring(index + 1, index2 - 1);
                                    }
                                }
                                // end skip all other lines
                                while ((line = input.ReadLine()) != null && line.StartsWithChar('>'))
                                {
                                    Debug.WriteLine($"data header line: {line}");
                                }
                            }
                            if (line == null)
                            {
                                throw new CDKException("Expecting data line here, but found null!");
                            }
                            string data = line;
                            while ((line = input.ReadLine()) != null && line.Trim().Length > 0)
                            {
                                if (string.Equals(line, "$$$$", StringComparison.Ordinal))
                                {
                                    Trace.TraceError($"Expecting data line here, but found end of molecule: {line}");
                                    break;
                                }
                                Debug.WriteLine($"data line: {line}");
                                data += line;
                                // preserve newlines, unless the line is exactly 80 chars; in that case it
                                // is assumed to continue on the next line. See MDL documentation.
                                if (line.Length < 80) data += "\n";
                            }
                            if (fieldName != null)
                            {
                                Trace.TraceInformation($"fieldName, data: {fieldName}, {data}");
                                r.SetProperty(fieldName, data);
                            }
                        }
                    }
                }
            }
            catch (CDKException)
            {
                throw;
            }
            catch (IOException exception)
            {
                string error = "Error while parsing SDF";
                Trace.TraceError(error);
                Debug.WriteLine(exception);
                throw new CDKException(error, exception);
            }

            return setOfReactions;
        }

        /// <summary>
        /// Read a Reaction from a file in MDL RXN format
        ///
        /// <returns>The Reaction that was read from the MDL file.</returns>
        /// </summary>
        private IReaction ReadReaction(IChemObjectBuilder builder)
        {
            Debug.WriteLine("Reading new reaction");
            int linecount = 0;
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

            int reactantCount = 0;
            int productCount = 0;
            try
            {
                string countsLine = input.ReadLine();
                linecount++;
                if (countsLine == null)
                {
                    return null;
                }
                Debug.WriteLine("Line " + linecount + ": " + countsLine);
                if (countsLine.StartsWith("$$$$", StringComparison.Ordinal))
                {
                    Debug.WriteLine("File is empty, returning empty reaction");
                    return reaction;
                }
                
                // this line contains the number of reactants and products
                var tokens = Strings.Tokenize(countsLine);
                reactantCount = int.Parse(tokens[0], NumberFormatInfo.InvariantInfo);
                Trace.TraceInformation("Expecting " + reactantCount + " reactants in file");
                productCount = int.Parse(tokens[1], NumberFormatInfo.InvariantInfo);
                Trace.TraceInformation("Expecting " + productCount + " products in file");
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

            // now read the reactants
            try
            {
                for (int i = 1; i <= reactantCount; i++)
                {
                    var molFile = new StringBuilder();
                    input.ReadLine(); // announceMDLFileLine
                    string molFileLine = "";
                    do
                    {
                        molFileLine = input.ReadLine();
                        molFile.Append(molFileLine);
                        molFile.Append('\n');
                    } while (!string.Equals(molFileLine, "M  END", StringComparison.Ordinal));

                    // read MDL molfile content
                    MDLReader reader = new MDLReader(new StringReader(molFile.ToString()));
                    IAtomContainer reactant = (IAtomContainer)reader.Read(builder.NewAtomContainer());
                    reader.Close();

                    // add reactant
                    reaction.Reactants.Add(reactant);
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

            // now read the products
            try
            {
                for (int i = 1; i <= productCount; i++)
                {
                    var molFile = new StringBuilder();
                    input.ReadLine(); // string announceMDLFileLine =
                    string molFileLine = "";
                    do
                    {
                        molFileLine = input.ReadLine();
                        molFile.Append(molFileLine);
                        molFile.Append('\n');
                    } while (!string.Equals(molFileLine, "M  END", StringComparison.Ordinal));

                    // read MDL molfile content
                    MDLReader reader = new MDLReader(new StringReader(molFile.ToString()), base.ReaderMode);
                    IAtomContainer product = (IAtomContainer)reader.Read(builder.NewAtomContainer());
                    reader.Close();

                    // add reactant
                    reaction.Products.Add(product);
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
                    throw new CDKException("Error while reading products", exception);
                }
                throw;
            }

            // now try to map things, if wanted
            Trace.TraceInformation("Reading atom-atom mapping from file");
            // distribute all atoms over two AtomContainer's
            IAtomContainer reactingSide = builder.NewAtomContainer();
            foreach (var reactant in reaction.Reactants)
            {
                reactingSide.Add(reactant);
            }
            IAtomContainer producedSide = builder.NewAtomContainer();
            foreach (var reactant in reaction.Products)
            {
                producedSide.Add(reactant);
            }

            // map the atoms
            int mappingCount = 0;
            //        IAtom[] reactantAtoms = reactingSide.GetAtoms();
            //        IAtom[] producedAtoms = producedSide.GetAtoms();
            for (int i = 0; i < reactingSide.Atoms.Count; i++)
            {
                for (int j = 0; j < producedSide.Atoms.Count; j++)
                {
                    IAtom eductAtom = reactingSide.Atoms[i];
                    IAtom productAtom = producedSide.Atoms[j];
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
