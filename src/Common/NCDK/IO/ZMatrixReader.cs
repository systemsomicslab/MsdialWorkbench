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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Primitives;
using NCDK.Geometries;
using NCDK.IO.Formats;
using System;
using System.Globalization;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// It reads Z matrices like in Gaussian input files. It seems that it cannot
    /// handle Z matrices where values are given via a stringID for which the value
    /// is given later.
    /// </summary>
    // @cdk.module extra
    // @cdk.iooptions
    // @cdk.keyword file format, Z-matrix
    public class ZMatrixReader : DefaultChemObjectReader
    {
        private TextReader input;

        /// <summary>
        /// Constructs a <see cref="ZMatrixReader"/> from a Reader that contains the
        /// data to be parsed.
        /// </summary>
        /// <param name="input">Reader containing the data to read</param>
        public ZMatrixReader(TextReader input)
        {
            this.input = input;
        }

        public ZMatrixReader(Stream input)
                : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => ZMatrixFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        ///  Returns a <see cref="IChemObject"/> of type object bye reading from
        ///  the input.
        ///
        ///  The function supports only reading of <see cref="IChemFile"/>'s.
        /// </summary>
        /// <param name="obj">IChemObject that types the class to return.</param>
        /// <exception cref="CDKException">when a <see cref="IChemObject"/> is requested that cannot be read.</exception>
        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
                return (T)ReadChemFile((IChemFile)obj);
            else
                throw new CDKException("Only ChemFile objects can be read.");
        }

        /// <summary>
        ///  Private method that actually parses the input to read a <see cref="IChemFile"/> object.
        /// </summary>
        /// <param name="file">the file to read from</param>
        /// <returns>A ChemFile containing the data parsed from input.</returns>
        private IChemFile ReadChemFile(IChemFile file)
        {
            IChemSequence chemSequence = file.Builder.NewChemSequence();

            int number_of_atoms;
            try
            {
                string line = input.ReadLine();
                while (line.StartsWithChar('#'))
                    line = input.ReadLine();
                
                // while (input.Ready() && line != null) {
                //        Debug.WriteLine("lauf");
                // parse frame by frame
                string token = Strings.Tokenize(line, '\t', ' ', ',', ';')[0];
                number_of_atoms = int.Parse(token, NumberFormatInfo.InvariantInfo);
                string info = input.ReadLine();

                IChemModel chemModel = file.Builder.NewChemModel();
                var setOfMolecules = file.Builder.NewAtomContainerSet();

                IAtomContainer m = file.Builder.NewAtomContainer();
                m.Title = info;

                string[] types = new string[number_of_atoms];
                double[] d = new double[number_of_atoms];
                int[] d_atom = new int[number_of_atoms]; // Distances
                double[] a = new double[number_of_atoms];
                int[] a_atom = new int[number_of_atoms]; // Angles
                double[] da = new double[number_of_atoms];
                int[] da_atom = new int[number_of_atoms]; // Diederangles
                                                          //Vector3[] pos = new Vector3[number_of_atoms]; // calculated positions

                int i = 0;
                while (i < number_of_atoms)
                {
                    line = input.ReadLine();
                    //          Debug.WriteLine("line:\""+line+"\"");
                    if (line == null) break;
                    if (line.StartsWithChar('#'))
                    {
                        // skip comment in file
                    }
                    else
                    {
                        d[i] = 0d;
                        d_atom[i] = -1;
                        a[i] = 0d;
                        a_atom[i] = -1;
                        da[i] = 0d;
                        da_atom[i] = -1;

                        var tokens = Strings.Tokenize(line, '\t', ' ', ',', ';');
                        int fields = int.Parse(tokens[0], NumberFormatInfo.InvariantInfo);

                        if (fields < Math.Min(i * 2 + 1, 7))
                        {
                            // this is an error but cannot throw exception
                        }
                        else if (i == 0)
                        {
                            types[i] = tokens[1];
                            i++;
                        }
                        else if (i == 1)
                        {
                            types[i] = tokens[1];
                            d_atom[i] = int.Parse(tokens[2], NumberFormatInfo.InvariantInfo) - 1;
                            d[i] = double.Parse(tokens[3], NumberFormatInfo.InvariantInfo);
                            i++;
                        }
                        else if (i == 2)
                        {
                            types[i] = tokens[1];
                            d_atom[i] = int.Parse(tokens[2], NumberFormatInfo.InvariantInfo) - 1;
                            d[i] = double.Parse(tokens[3], NumberFormatInfo.InvariantInfo);
                            a_atom[i] = int.Parse(tokens[4], NumberFormatInfo.InvariantInfo) - 1;
                            a[i] = double.Parse(tokens[5], NumberFormatInfo.InvariantInfo);
                            i++;
                        }
                        else
                        {
                            types[i] = tokens[1];
                            d_atom[i] = int.Parse(tokens[2], NumberFormatInfo.InvariantInfo) - 1;
                            d[i] = double.Parse(tokens[3], NumberFormatInfo.InvariantInfo);
                            a_atom[i] = int.Parse(tokens[4], NumberFormatInfo.InvariantInfo) - 1;
                            a[i] = double.Parse(tokens[5], NumberFormatInfo.InvariantInfo);
                            da_atom[i] = int.Parse(tokens[6], NumberFormatInfo.InvariantInfo) - 1;
                            da[i] = double.Parse(tokens[7], NumberFormatInfo.InvariantInfo);
                            i++;
                        }
                    }
                }

                // calculate cartesian coordinates
                var cartCoords = ZMatrixTools.ZMatrixToCartesian(d, d_atom, a, a_atom, da, da_atom);

                for (i = 0; i < number_of_atoms; i++)
                {
                    m.Atoms.Add(file.Builder.NewAtom(types[i], cartCoords[i]));
                }

                //        Debug.WriteLine("molecule:"+m);

                setOfMolecules.Add(m);
                chemModel.MoleculeSet = setOfMolecules;
                chemSequence.Add(chemModel);
                line = input.ReadLine();
                file.Add(chemSequence);
            }
            catch (IOException)
            {
                // should make some noise now
                file = null;
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
