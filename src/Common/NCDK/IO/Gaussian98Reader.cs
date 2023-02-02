/* Copyright (C) 2002-2003  Bradley A. Smith <yeldar@home.com>
 *  Copyright (C) 2003-2007  Egon Willighagen <egonw@users.sf.net>
 *  Copyright (C) 2003-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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
using NCDK.Common.Util;
using NCDK.IO.Formats;
using NCDK.IO.Setting;
using NCDK.Numerics;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// A reader for Gaussian98 output. 
    /// <para>Gaussian 98 is a quantum chemistry program
    /// by Gaussian, Inc. (<see href="http://www.gaussian.com/">http://www.gaussian.com/</see>).
    /// </para>
    /// <para>Molecular coordinates, energies, and normal coordinates of vibrations are
    /// read. Each set of coordinates is added to the ChemFile in the order they are
    /// found. Energies and vibrations are associated with the previously read set
    /// of coordinates.
    /// </para>
    /// <para>
    /// This reader was developed from a small set of example output files, and
    /// therefore, is not guaranteed to properly read all Gaussian98 output. If you
    /// have problems, please contact the author of this code, not the developers of
    /// Gaussian98.</para>
    /// </summary>
    // @author Bradley A. Smith <yeldar@home.com>
    // @author Egon Willighagen
    // @author Christoph Steinbeck
    // @cdk.module io
    // @cdk.iooptions
    public class Gaussian98Reader : DefaultChemObjectReader
    {
        private TextReader input;

        private int atomCount = 0;
        private string lastRoute = "";

        /// <summary>
        /// Customizable setting
        /// </summary>
        private BooleanIOSetting readOptimizedStructureOnly;

        public Gaussian98Reader(Stream input)
                : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => Gaussian98Format.Instance;

        /// <summary>
        /// Create an Gaussian98 output reader.
        /// </summary>
        /// <param name="input">source of Gaussian98 data</param>
        public Gaussian98Reader(TextReader input)
        {
            this.input = input;
            InitIOSettings();
        }

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            CustomizeJob();

            if (obj is IChemFile file)
            {
                try
                {
                    file = ReadChemFile(file);
                }
                catch (IOException exception)
                {
                    throw new CDKException("Error while reading file: " + exception.ToString(), exception);
                }
                return (T)file;
            }
            else
            {
                throw new CDKException($"Reading of a {obj.GetType().Name} is not supported.");
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

        /// <summary>
        /// Read the Gaussian98 output.
        /// </summary>
        /// <param name="chemFile"></param>
        /// <returns>a ChemFile with the coordinates, energies, and vibrations.</returns>
        private IChemFile ReadChemFile(IChemFile chemFile)
        {
            IChemSequence sequence = chemFile.Builder.NewChemSequence();
            IChemModel model = null;
            string line = input.ReadLine();
            string levelOfTheory;
            string description;
            int modelCounter = 0;

            // Find first set of coordinates by skipping all before "Standard orientation"
            while (line != null)
            {
                if (line.Contains("Standard orientation:"))
                {

                    // Found a set of coordinates
                    model = chemFile.Builder.NewChemModel();
                    ReadCoordinates(model);
                    break;
                }
                line = input.ReadLine();
            }
            if (model != null)
            {
                // Read all other data
                line = input.ReadLine().Trim();
                while (line != null)
                {
                    if (line.IndexOf('#') == 0)
                    {
                        // Found the route section
                        // Memorizing this for the description of the chemmodel
                        lastRoute = line;
                        modelCounter = 0;

                    }
                    else if (line.Contains("Standard orientation:"))
                    {

                        // Found a set of coordinates
                        // Add current frame to file and create a new one.
                        if (!readOptimizedStructureOnly.IsSet)
                        {
                            sequence.Add(model);
                        }
                        else
                        {
                            Trace.TraceInformation("Skipping frame, because I was told to do");
                        }
                        FrameRead();
                        model = chemFile.Builder.NewChemModel();
                        modelCounter++;
                        ReadCoordinates(model);
                    }
                    else if (line.Contains("SCF Done:"))
                    {

                        // Found an energy
                        model.SetProperty(CDKPropertyName.Remark, line.Trim());
                    }
                    else if (line.Contains("Harmonic frequencies"))
                    {
                        // Found a set of vibrations
                        // ReadFrequencies(frame);
                    }
                    else if (line.Contains("Total atomic charges"))
                    {
                        ReadPartialCharges(model);
                    }
                    else if (line.Contains("Magnetic shielding"))
                    {
                        // Found NMR data
                        ReadNMRData(model, line);
                    }
                    else if (line.Contains("GINC"))
                    {
                        // Found calculation level of theory
                        levelOfTheory = ParseLevelOfTheory(line);
                        Debug.WriteLine($"Level of Theory for this model: {levelOfTheory}");
                        description = lastRoute + ", model no. " + modelCounter;
                        model.SetProperty(CDKPropertyName.Description, description);
                    }
                    else
                    {
                    }
                    line = input.ReadLine();
                }

                // Add last frame to file
                sequence.Add(model);
                FrameRead();
            }
            chemFile.Add(sequence);

            return chemFile;
        }

        /// <summary>
        /// Reads a set of coordinates into ChemFrame.
        /// </summary>
        /// <param name="model"></param>
        private void ReadCoordinates(IChemModel model)
        {
            var moleculeSet = model.Builder.NewAtomContainerSet();
            IAtomContainer molecule = model.Builder.NewAtomContainer();
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
                int atomicNumber;
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
                    throw new CDKException("Error while reading coordinates: expected integer.");
                }
                token.NextToken();

                // ignore third token
                double x;
                double y;
                double z;
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    x = token.NumberValue;
                }
                else
                {
                    throw new IOException("Error reading x coordinate");
                }
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    y = token.NumberValue;
                }
                else
                {
                    throw new IOException("Error reading y coordinate");
                }
                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    z = token.NumberValue;
                }
                else
                {
                    throw new IOException("Error reading z coordinate");
                }
                string symbol = "Du";
                symbol = PeriodicTable.GetSymbol(atomicNumber);
                IAtom atom = model.Builder.NewAtom(symbol);
                atom.Point3D = new Vector3(x, y, z);
                molecule.Atoms.Add(atom);
            }
            
            // this is the place where we store the atomcount to be used as a
            // counter in the nmr reading
            atomCount = molecule.Atoms.Count;
            moleculeSet.Add(molecule);
            model.MoleculeSet = moleculeSet;
        }

        /// <summary>
        /// Reads partial atomic charges and add the to the given ChemModel.
        /// </summary>
        /// <param name="model"></param>
        private void ReadPartialCharges(IChemModel model)
        {
            Trace.TraceInformation("Reading partial atomic charges");
            var moleculeSet = model.MoleculeSet;
            IAtomContainer molecule = moleculeSet[0];
            string line = input.ReadLine();
            // skip first line after "Total atomic charges"
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

                    tokenizer.NextToken();
                    // ignore the symbol

                    double charge;
                    if (tokenizer.NextToken() == StreamTokenizer.TTypeNumber)
                    {
                        charge = tokenizer.NumberValue;
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
    
        /// <summary>
        /// Reads NMR nuclear shieldings.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="labelLine"></param>
        private void ReadNMRData(IChemModel model, string labelLine)
        {
            var containers = ChemModelManipulator.GetAllAtomContainers(model).ToReadOnlyList();
            if (containers.Count == 0)
            {
                // nothing to store the results into
                return;
            } // otherwise insert in the first AC

            IAtomContainer ac = containers[0];
            // Determine label for properties
            string label;
            if (labelLine.Contains("Diamagnetic"))
            {
                label = "Diamagnetic Magnetic shielding (Isotropic)";
            }
            else if (labelLine.Contains("Paramagnetic"))
            {
                label = "Paramagnetic Magnetic shielding (Isotropic)";
            }
            else
            {
                label = "Magnetic shielding (Isotropic)";
            }
            int atomIndex = 0;
            for (int i = 0; i < atomCount; ++i)
            {
                try
                {
                    string line = input.ReadLine().Trim();
                    while (!line.Contains("Isotropic"))
                    {
                        if (line == null)
                        {
                            return;
                        }
                        line = input.ReadLine().Trim();
                    }
                    var st1 = Strings.Tokenize(line).GetEnumerator();

                    // Find Isotropic label
                    while (st1.MoveNext())
                    {
                        if (string.Equals(st1.Current, "Isotropic", StringComparison.Ordinal))
                        {
                            break;
                        }
                    }

                    // Find Isotropic value
                    while (st1.MoveNext())
                    {
                        if (string.Equals(st1.Current, "=", StringComparison.Ordinal)) break;
                    }
                    st1.MoveNext();
                    double shielding = double.Parse(st1.Current, NumberFormatInfo.InvariantInfo);
                    Trace.TraceInformation("Type of shielding: " + label);
                    ac.Atoms[atomIndex].SetProperty(CDKPropertyName.IsotropicShielding, shielding);
                    ++atomIndex;
                }
                catch (Exception exc)
                {
                    if (!(exc is IOException || exc is FormatException))
                        throw;
                    Debug.WriteLine("failed to read line from gaussian98 file where I expected one.");
                }
            }
        }

        /// <summary>
        /// Select the theory and basis set from the first archive line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string ParseLevelOfTheory(string line)
        {
            var summary = new StringBuilder();
            summary.Append(line);
            try
            {
                do
                {
                    line = input.ReadLine().Trim();
                    summary.Append(line);
                } while (!(line.IndexOf('@') >= 0));
            }
            catch (Exception exc)
            {
                Debug.WriteLine("syntax problem while parsing summary of g98 section: ");
                Debug.WriteLine(exc);
            }
            Debug.WriteLine("ParseLoT(): " + summary.ToString());
            var tokens1 = Strings.Tokenize(summary.ToString(), '\\');

            // Must contain at least 6 tokens
            if (tokens1.Count < 6)
            {
                return null;
            }

            return tokens1[4] + "/" + tokens1[5];
        }

        private void InitIOSettings()
        {
            readOptimizedStructureOnly = Add(new BooleanIOSetting("ReadOptimizedStructureOnly",
                    Importance.Low, "Should I only read the optimized structure from a geometry optimization?",
                    "false"));
        }

        private void CustomizeJob()
        {
            ProcessIOSettingQuestion(readOptimizedStructureOnly);
        }
    }
}
