/* Copyright (C) 2003-2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
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
    /// Class that implements the new MDL mol format introduced in August 2002.
    /// The overall syntax is compatible with the old format, but I consider
    /// the format completely different, and thus implemented a separate Reader
    /// for it.
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @author  Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.created 2003-10-05
    // @cdk.keyword MDL V3000
    public class MDLRXNV3000Reader : DefaultChemObjectReader
    {
        TextReader input = null;

        public MDLRXNV3000Reader(TextReader input)
           : this(input, ChemObjectReaderMode.Relaxed)
        { }

        public MDLRXNV3000Reader(TextReader input, ChemObjectReaderMode mode)
        {
            this.input = input;
            InitIOSettings();
            base.ReaderMode = mode;
        }

        public MDLRXNV3000Reader(Stream input)
            : this(input, ChemObjectReaderMode.Relaxed)
        { }

        public MDLRXNV3000Reader(Stream input, ChemObjectReaderMode mode)
           : this(new StreamReader(input), mode)
        { }

        public override IResourceFormat Format => MDLRXNV3000Format.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemModel).IsAssignableFrom(type))
                return true;
            if (typeof(IReaction).IsAssignableFrom(type))
                return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            if (obj is IReaction)
            {
                return (T)ReadReaction(obj.Builder);
            }
            else if (obj is IChemModel)
            {
                IChemModel model = obj.Builder.NewChemModel();
                IReactionSet reactionSet = obj.Builder.NewReactionSet();
                reactionSet.Add(ReadReaction(obj.Builder));
                model.ReactionSet = reactionSet;
                return (T)model;
            }
            else
            {
                throw new CDKException($"Only supported are Reaction and ChemModel, and not {obj.GetType().Name}.");
            }
        }

        /// <summary>
        /// Reads the command on this line. If the line is continued on the next, that
        /// part is added.
        /// </summary>
        /// <returns>the command on this line.</returns>
        private string ReadCommand()
        {
            string line = ReadLine();
            if (line.StartsWith("M  V30 ", StringComparison.Ordinal))
            {
                string command = line.Substring(7);
                if (command.EndsWithChar('-'))
                {
                    command = command.Substring(0, command.Length - 1);
                    command += ReadCommand();
                }
                return command;
            }
            else
            {
                throw new CDKException("Could not read MDL file: unexpected line: " + line);
            }
        }

        private string ReadLine()
        {
            string line = null;
            try
            {
                line = input.ReadLine();
                Debug.WriteLine($"read line: {line}");
            }
            catch (Exception exception)
            {
                string error = "Unexpected error while reading file: " + exception.Message;
                Trace.TraceError(error);
                Debug.WriteLine(exception);
                throw new CDKException(error, exception);
            }
            return line;
        }

        private IReaction ReadReaction(IChemObjectBuilder builder)
        {
            IReaction reaction = builder.NewReaction();
            ReadLine(); // first line should be $RXN
            ReadLine(); // second line
            ReadLine(); // third line
            ReadLine(); // fourth line

            int reactantCount = 0;
            int productCount = 0;
            bool foundCOUNTS = false;
            while (!foundCOUNTS)
            {
                string command = ReadCommand();
                if (command.StartsWith("COUNTS", StringComparison.Ordinal))
                {
                    var tokenizer = Strings.Tokenize(command);
                    try
                    {
                        reactantCount = int.Parse(tokenizer[1], NumberFormatInfo.InvariantInfo);
                        Trace.TraceInformation($"Expecting {reactantCount} reactants in file");
                        productCount = int.Parse(tokenizer[2], NumberFormatInfo.InvariantInfo);
                        Trace.TraceInformation($"Expecting {productCount } products in file");
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception);
                        throw new CDKException("Error while counts line of RXN file", exception);
                    }
                    foundCOUNTS = true;
                }
                else
                {
                    Trace.TraceWarning("Waiting for COUNTS line, but found: " + command);
                }
            }

            // now read the reactants
            for (int i = 1; i <= reactantCount; i++)
            {
                var molFile = new StringBuilder();
                string announceMDLFileLine = ReadCommand();
                if (!string.Equals(announceMDLFileLine, "BEGIN REACTANT", StringComparison.Ordinal))
                {
                    string error = "Excepted start of reactant, but found: " + announceMDLFileLine;
                    Trace.TraceError(error);
                    throw new CDKException(error);
                }
                string molFileLine = "";
                while (!molFileLine.EndsWith("END REACTANT", StringComparison.Ordinal))
                {
                    molFileLine = ReadLine();
                    molFile.Append(molFileLine);
                    molFile.Append('\n');
                };

                try
                {
                    // read MDL molfile content
                    MDLV3000Reader reader = new MDLV3000Reader(new StringReader(molFile.ToString()), base.ReaderMode);
                    IAtomContainer reactant = (IAtomContainer)reader.Read(builder.NewAtomContainer());
                    reader.Close();

                    // add reactant
                    reaction.Reactants.Add(reactant);
                }
                catch (Exception exception)
                {
                    if (!(exception is ArgumentException ||
                        exception is CDKException ||
                        exception is IOException))
                        throw;
                    string error = "Error while reading reactant: " + exception.Message;
                    Trace.TraceError(error);
                    Debug.WriteLine(exception);
                    throw new CDKException(error, exception);
                }
            }

            // now read the products
            for (int i = 1; i <= productCount; i++)
            {
                var molFile = new StringBuilder();
                string announceMDLFileLine = ReadCommand();
                if (!string.Equals(announceMDLFileLine, "BEGIN PRODUCT", StringComparison.Ordinal))
                {
                    string error = "Excepted start of product, but found: " + announceMDLFileLine;
                    Trace.TraceError(error);
                    throw new CDKException(error);
                }
                string molFileLine = "";
                while (!molFileLine.EndsWith("END PRODUCT", StringComparison.Ordinal))
                {
                    molFileLine = ReadLine();
                    molFile.Append(molFileLine);
                    molFile.Append('\n');
                };

                try
                {
                    // read MDL molfile content
                    MDLV3000Reader reader = new MDLV3000Reader(new StringReader(molFile.ToString()));
                    IAtomContainer product = (IAtomContainer)reader.Read(builder.NewAtomContainer());
                    reader.Close();

                    // add product
                    reaction.Products.Add(product);
                }
                catch (Exception exception)
                {
                    if (!(exception is ArgumentException ||
                        exception is CDKException ||
                        exception is IOException))
                        throw;
                    string error = "Error while reading product: " + exception.Message;
                    Trace.TraceError(error);
                    Debug.WriteLine(exception);
                    throw new CDKException(error, exception);
                }
            }

            return reaction;
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
            return false;
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

        private static void InitIOSettings() { }
    }
}
