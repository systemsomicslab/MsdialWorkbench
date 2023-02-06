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

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using NCDK.Numerics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// Reads an object from XYZ formated input.
    /// 
    /// <para>This class is based on Dan Gezelter's XYZReader from Jmol</para>
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @cdk.keyword file format, XYZ
    public class XYZReader : DefaultChemObjectReader
    {
        private TextReader input;

        /// <summary>
        /// Construct a new reader from a Reader type object.
        /// </summary>
        /// <param name="input">reader from which input is read</param>
        public XYZReader(TextReader input)
        {
            this.input = input;
        }

        public XYZReader(Stream input)
            : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => XYZFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type))
                return true;
            return false;
        }

        /// <summary>
        /// reads the content from a XYZ input. It can only return a
        /// <see cref="IChemObject"/>  of type <see cref="IChemFile"/> 
        /// </summary>
        /// <param name="obj">class must be of type ChemFile</param>
        /// <seealso cref="IChemFile"/>
        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                return (T)ReadChemFile((IChemFile)obj);
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile objects.");
            }
        }

        // private procedures

        /// <summary>
        ///  Private method that actually parses the input to read a <see cref="IChemFile"/>  object.
        /// </summary>
        /// <returns>A ChemFile containing the data parsed from input.</returns>
        private IChemFile ReadChemFile(IChemFile file)
        {
            IChemSequence chemSequence = file.Builder.NewChemSequence();

            int number_of_atoms = 0;

            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    // parse frame by frame
                    string token = line.Split('\t', ' ', ',', ';')[0];
                    number_of_atoms = int.Parse(token, NumberFormatInfo.InvariantInfo);
                    string info = input.ReadLine();

                    IChemModel chemModel = file.Builder.NewChemModel();
                    var setOfMolecules = file.Builder.NewAtomContainerSet();

                    IAtomContainer m = file.Builder.NewAtomContainer();
                    m.Title = info;

                    for (int i = 0; i < number_of_atoms; i++)
                    {
                        line = input.ReadLine();
                        if (line == null) break;
                        if (line.StartsWithChar('#') && line.Length > 1)
                        {
                            var comment = m.GetProperty(CDKPropertyName.Comment, "");
                            comment = comment + line.Substring(1).Trim();
                            m.SetProperty(CDKPropertyName.Comment, comment);
                            Debug.WriteLine($"Found and set comment: {comment}");
                            i--; // a comment line does not count as an atom
                        }
                        else
                        {
                            double x = 0.0f, y = 0.0f, z = 0.0f;
                            double charge = 0.0f;
                            var tokenizer = Strings.Tokenize(line, '\t', ' ', ',', ';');
                            int fields = tokenizer.Count;

                            if (fields < 4)
                            {
                                // this is an error but cannot throw exception
                            }
                            else
                            {
                                string atomtype = tokenizer[0];
                                x = double.Parse(tokenizer[1], NumberFormatInfo.InvariantInfo);
                                y = double.Parse(tokenizer[2], NumberFormatInfo.InvariantInfo);
                                z = double.Parse(tokenizer[3], NumberFormatInfo.InvariantInfo);
                                if (fields == 8) charge = double.Parse(tokenizer[4], NumberFormatInfo.InvariantInfo);

                                IAtom atom = file.Builder.NewAtom(atomtype, new Vector3(x, y, z));
                                atom.Charge = charge;
                                m.Atoms.Add(atom);
                            }
                        }
                    }

                    setOfMolecules.Add(m);
                    chemModel.MoleculeSet = setOfMolecules;
                    chemSequence.Add(chemModel);
                    line = input.ReadLine();
                }
                file.Add(chemSequence);
            }
            catch (IOException e)
            {
                // should make some noise now
                file = null;
                Trace.TraceError($"Error while reading file: {e.Message}");
                Debug.WriteLine(e);
            }
            return file;
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
