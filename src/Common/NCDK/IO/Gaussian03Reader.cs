/* Copyright (C) 2002-2003  Bradley A. Smith <yeldar@home.com>
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

using NCDK.Common.Util;
using NCDK.IO.Formats;
using NCDK.Numerics;
using NCDK.Tools;
using System;
using System.Diagnostics;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// A reader for Gaussian03 output.
    /// Gaussian 03 is a quantum chemistry program
    /// by Gaussian, Inc. (<see href="http://www.gaussian.com/">http://www.gaussian.com/</see>).
    /// <para>
    /// Molecular coordinates, energies, and normal coordinates of
    /// vibrations are read. Each set of coordinates is added to the
    /// ChemFile in the order they are found. Energies and vibrations
    /// are associated with the previously read set of coordinates.
    /// </para>
    /// <para>
    /// This reader was developed from a small set of
    /// example output files, and therefore, is not guaranteed to
    /// properly read all Gaussian03 output. If you have problems,
    /// please contact the author of this code, not the developers
    /// of Gaussian03.
    /// </para>
    /// <para>
    /// This code was adaptated by Jonathan from Gaussian98Reader written by
    /// Bradley, and ported to CDK by Egon.
    /// </para>
    /// </summary>
    // @author Jonathan C. Rienstra-Kiracofe <jrienst@emory.edu>
    // @author Bradley A. Smith <yeldar@home.com>
    // @author Egon Willighagen
    // @cdk.module io
    // @cdk.iooptions
    public class Gaussian03Reader : DefaultChemObjectReader
    {
        private TextReader input;

        public Gaussian03Reader(TextReader reader)
        {
            input = reader;
        }

        public Gaussian03Reader(Stream input)
                : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => Gaussian03Format.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            if (typeof(IChemSequence).IsAssignableFrom(type)) return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            if (obj is IChemSequence)
            {
                return (T)ReadChemSequence((IChemSequence)obj);
            }
            else if (obj is IChemFile)
            {
                return (T)ReadChemFile((IChemFile)obj);
            }
            else
            {
                throw new CDKException($"Object {obj.GetType().Name} is not supported");
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

        private IChemFile ReadChemFile(IChemFile chemFile)
        {
            IChemSequence sequence = ReadChemSequence(chemFile.Builder.NewChemSequence());
            chemFile.Add(sequence);
            return chemFile;
        }

        private IChemSequence ReadChemSequence(IChemSequence sequence)
        {
            IChemModel model = null;

            try
            {
                string line = input.ReadLine();

                // Find first set of coordinates
                while (line != null)
                {
                    if (line.Contains("Standard orientation:"))
                    {

                        // Found a set of coordinates
                        model = sequence.Builder.NewChemModel();
                        try
                        {
                            ReadCoordinates(model);
                        }
                        catch (IOException exception)
                        {
                            throw new CDKException("Error while reading coordinates: " + exception.ToString(), exception);
                        }
                        break;
                    }
                    line = input.ReadLine();
                }
                if (model != null)
                {
                    // Read all other data
                    line = input.ReadLine();
                    while (line != null)
                    {
                        if (line.Contains("Standard orientation:"))
                        {
                            // Found a set of coordinates
                            // Add current frame to file and create a new one.
                            sequence.Add(model);
                            FrameRead();
                            model = sequence.Builder.NewChemModel();
                            ReadCoordinates(model);
                        }
                        else if (line.Contains("SCF Done:"))
                        {
                            // Found an energy
                            model.SetProperty("NCDK.IO.Gaussian03Reaer:SCF Done", line.Trim());
                        }
                        else if (line.Contains("Harmonic frequencies"))
                        {
                            // Found a set of vibrations
                        }
                        else if (line.Contains("Mulliken atomic charges"))
                        {
                            ReadPartialCharges(model);
                        }
                        else if (line.Contains("Magnetic shielding"))
                        {
                            // Found NMR data
                        }
                        else if (line.Contains("GINC"))
                        {
                            // Found calculation level of theory
                            // FIXME: is doing anything with it?
                        }
                        line = input.ReadLine();
                    }

                    // Add current frame to file
                    sequence.Add(model);
                    FrameRead();
                }
            }
            catch (IOException exception)
            {
                throw new CDKException("Error while reading general structure: " + exception.ToString(), exception);
            }
            return sequence;
        }

        /// <summary>
        /// Reads a set of coordinates into ChemModel.
        /// </summary>
        /// <param name="model">the destination ChemModel</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        private void ReadCoordinates(IChemModel model)
        {
            IAtomContainer container = model.Builder.NewAtomContainer();
            string line = input.ReadLine();
            line = input.ReadLine();
            line = input.ReadLine();
            line = input.ReadLine();
            while (true)
            {
                line = input.ReadLine();
                if ((line == null) || (line.Contains("-----")))
                {
                    break;
                }
                int atomicNumber = 0;
                StringReader sr = new StringReader(line);
                StreamTokenizer token = new StreamTokenizer(sr);
                token.NextToken();

                // ignore first token
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    atomicNumber = (int)token.NumberValue;
                    if (atomicNumber == 0)
                    {
                        // Skip dummy atoms. Dummy atoms must be skipped
                        // if frequencies are to be read because Gaussian
                        // does not report dummy atoms in frequencies, and
                        // the number of atoms is used for reading frequencies.
                        continue;
                    }
                }
                else
                {
                    throw new IOException("Error reading coordinates");
                }
                token.NextToken();

                // ignore third token
                double x = 0.0;
                double y = 0.0;
                double z = 0.0;
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    x = token.NumberValue;
                }
                else
                {
                    throw new IOException("Error reading coordinates");
                }
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    y = token.NumberValue;
                }
                else
                {
                    throw new IOException("Error reading coordinates");
                }
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    z = token.NumberValue;
                }
                else
                {
                    throw new IOException("Error reading coordinates");
                }
                string symbol = "Du";
                symbol = PeriodicTable.GetSymbol(atomicNumber);
                IAtom atom = model.Builder.NewAtom(symbol);
                atom.Point3D = new Vector3(x, y, z);
                container.Atoms.Add(atom);
            }
            var moleculeSet = model.Builder.NewAtomContainerSet();
            moleculeSet.Add(model.Builder.NewAtomContainer(container));
            model.MoleculeSet = moleculeSet;
        }

        /// <summary>
        /// Reads partial atomic charges and add the to the given ChemModel.
        /// </summary>
        private void ReadPartialCharges(IChemModel model)
        {
            Trace.TraceInformation("Reading partial atomic charges");
            var moleculeSet = model.MoleculeSet;
            IAtomContainer molecule = moleculeSet[0];
            string line = input.ReadLine(); // skip first line after "Total atomic charges"
            while (true)
            {
                line = input.ReadLine();
                Debug.WriteLine($"Read charge block line: {line}");
                if ((line == null) || line.Contains("Sum of Mulliken charges"))
                {
                    Debug.WriteLine("End of charge block found");
                    break;
                }
                StringReader sr = new StringReader(line);
                StreamTokenizer tokenizer = new StreamTokenizer(sr);
                if (tokenizer.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    int atomCounter = (int)tokenizer.NumberValue;

                    tokenizer.NextToken(); // ignore the symbol

                    double charge = 0.0;
                    if (tokenizer.NextToken() == StreamTokenizer.TTypeNumber)
                    {
                        charge = (double)tokenizer.NumberValue;
                        Debug.WriteLine("Found charge for atom " + atomCounter + ": " + charge);
                    }
                    else
                    {
                        throw new CDKException("Error while reading charge: expected double.");
                    }
                    IAtom atom = molecule.Atoms[atomCounter - 1];
                    atom.Charge = charge;
                }
            }
        }
    }
}
