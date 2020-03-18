/* Copyright (C) 2004-2008  Egon Willighagen <egonw@users.sf.net>
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
using NCDK.Graphs.Rebond;
using NCDK.IO.Formats;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace NCDK.IO
{
    /// <summary>
    /// Reads an frames from a PMP formated input.
    /// Both compilation and use of this class requires Java 1.4.
    /// </summary>
    // @cdk.module  io
    // @cdk.iooptions
    // @cdk.keyword file format, Polymorph Predictor (tm)
    // @author E.L. Willighagen
    // @cdk.require java1.4+
    public class PMPReader : DefaultChemObjectReader
    {
        private const string PMP_ZORDER = "ZOrder";
        private const string PMP_ID = "Id";

        private TextReader input;

        /* Keep a copy of the PMP model */
        private IAtomContainer modelStructure;
        private IChemObject chemObject;
        /* Keep an index of PMP id -> AtomCountainer id */
        private readonly Dictionary<int, int> atomids = new Dictionary<int, int>();
        private readonly Dictionary<int, int> atomGivenIds = new Dictionary<int, int>();
        private Dictionary<int, int> bondids = new Dictionary<int, int>();
        private Dictionary<int, int> bondAtomOnes = new Dictionary<int, int>();
        private Dictionary<int, int> bondAtomTwos = new Dictionary<int, int>();
        private Dictionary<int, double> bondOrders = new Dictionary<int, double>();

        /* Often used patterns */
        Regex objHeader;
        Regex objCommand;
        Regex atomTypePattern;

        int lineNumber;
        int bondCounter = 0;
        private RebondTool rebonder;

        /// <summary>
        /// construct a new reader from a Reader type object
        /// <param name="input">reader from which input is read</param>
        /// </summary>
        public PMPReader(TextReader input)
        {
            this.input = input;
            this.lineNumber = 0;

            /* compile patterns */
            objHeader = new Regex(".*\\((\\d+)\\s(\\w+)$", RegexOptions.Compiled);
            objCommand = new Regex(".*\\(A\\s([CFDIO])\\s(\\w+)\\s+\"?(.*?)\"?\\)$", RegexOptions.Compiled);
            atomTypePattern = new Regex("^(\\d+)\\s+(\\w+)$", RegexOptions.Compiled);

            rebonder = new RebondTool(2.0, 0.5, 0.5);
        }

        public PMPReader(Stream input)
                : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => PMPFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type))
                return true;
            return false;
        }

        /// <summary>
        /// reads the content from a PMP input. It can only return a
        /// IChemObject of type ChemFile
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

        private string ReadLine()
        {
            string line = input.ReadLine();
            lineNumber = lineNumber + 1;
            Debug.WriteLine("LINE (" + lineNumber + "): ", line);
            return line;
        }

        /// <summary>
        ///  Private method that actually parses the input to read a ChemFile
        ///  object.
        ///
        ///  Each PMP frame is stored as a Crystal in a ChemModel. The PMP
        ///  file is stored as a ChemSequence of ChemModels.
        /// </summary>
        /// <returns>A ChemFile containing the data parsed from input.</returns>
        private IChemFile ReadChemFile(IChemFile chemFile)
        {
            IChemSequence chemSequence;
            IChemModel chemModel;
            ICrystal crystal;

            try
            {
                string line = ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("%%Header Start", StringComparison.Ordinal))
                    {
                        // parse Header section
                        while (line != null && !(line.StartsWith("%%Header End", StringComparison.Ordinal)))
                        {
                            if (line.StartsWith("%%Version Number", StringComparison.Ordinal))
                            {
                                string version = ReadLine().Trim();
                                if (!string.Equals(version, "3.00", StringComparison.Ordinal))
                                {
                                    Trace.TraceError("The PMPReader only supports PMP files with version 3.00");
                                    return null;
                                }
                            }
                            line = ReadLine();
                        }
                    }
                    else if (line.StartsWith("%%Model Start", StringComparison.Ordinal))
                    {
                        // parse Model section
                        modelStructure = chemFile.Builder.NewAtomContainer();
                        while (line != null && !(line.StartsWith("%%Model End", StringComparison.Ordinal)))
                        {
                            var objHeaderMatcher = objHeader.Match(line);
                            if (objHeaderMatcher.Success)
                            {
                                var obj = objHeaderMatcher.Groups[2].Value;
                                ConstructObject(chemFile.Builder, obj);
                                var id = int.Parse(objHeaderMatcher.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                                // Debug.WriteLine(object + " id: " + id);
                                line = ReadLine();
                                while (line != null && !(string.Equals(line.Trim(), ")", StringComparison.Ordinal)))
                                {
                                    // parse object command (or new object header)
                                    var objCommandMatcher = objCommand.Match(line);
                                    objHeaderMatcher = objHeader.Match(line);
                                    if (objHeaderMatcher.Success)
                                    {
                                        // ok, forget about nesting and hope for the best
                                        obj = objHeaderMatcher.Groups[2].Value;
                                        id = int.Parse(objHeaderMatcher.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                                        ConstructObject(chemFile.Builder, obj);
                                    }
                                    else if (objCommandMatcher.Success)
                                    {
                                        var format = objCommandMatcher.Groups[1].Value;
                                        var command = objCommandMatcher.Groups[2].Value;
                                        var field = objCommandMatcher.Groups[3].Value;

                                        ProcessModelCommand(obj, command, format, field);
                                    }
                                    else
                                    {
                                        Trace.TraceWarning("Skipping line: " + line);
                                    }
                                    line = ReadLine();
                                }
                                if (chemObject is IAtom)
                                {
                                    atomids[id] = modelStructure.Atoms.Count;
                                    atomGivenIds[int.Parse(chemObject.GetProperty<string>(PMP_ID), NumberFormatInfo.InvariantInfo)] = id;
                                    modelStructure.Atoms.Add((IAtom)chemObject);
                                }
                                else if (chemObject is IBond) {
                                    // ignored: bonds may be defined before their
                                    // atoms so their handling is deferred until the
                                    // end of the model
                                } 
                                else
                                {
                                    Trace.TraceError("chemObject is not initialized or of bad class type");
                                }
                            }
                            line = ReadLine();
                        }
                        Trace.Assert(line != null);
                        if (line.StartsWith("%%Model End", StringComparison.Ordinal))
                        {
                            // during the Model Start, all bonds are cached as PMP files might
                            // define bonds *before* the involved atoms :(
                            // the next lines dump the cache into the atom container

                            int bondsFound = bondids.Count;
                            Debug.WriteLine($"Found #bonds: {bondsFound}");
                            Debug.WriteLine($"#atom ones: {bondAtomOnes.Count}");
                            Debug.WriteLine($"#atom twos: {bondAtomTwos.Count}");
                            Debug.WriteLine($"#orders: {bondOrders.Count}");
                            foreach (var index in bondids.Keys)
                            {
                                if (!bondOrders.TryGetValue(index, out double order))
                                    order = 1;
                                Debug.WriteLine($"index: {index}");
                                Debug.WriteLine($"ones: {bondAtomOnes[index]}");
                                var atom1 = modelStructure.Atoms[atomids[bondAtomOnes[index]]];
                                var atom2 = modelStructure.Atoms[atomids[bondAtomTwos[index]]];
                                var bond = modelStructure.Builder.NewBond(atom1, atom2);
                                if (order == 1.0)
                                {
                                    bond.Order = BondOrder.Single;
                                }
                                else if (order == 2.0)
                                {
                                    bond.Order = BondOrder.Double;
                                }
                                else if (order == 3.0)
                                {
                                    bond.Order = BondOrder.Triple;
                                }
                                else if (order == 4.0)
                                {
                                    bond.Order = BondOrder.Quadruple;
                                }
                                modelStructure.Bonds.Add(bond);
                            }
                        }
                    }
                    else if (line.StartsWith("%%Traj Start", StringComparison.Ordinal))
                    {
                        chemSequence = chemFile.Builder.NewChemSequence();
                        double energyFragment = 0.0;
                        double energyTotal = 0.0;
                        int Z = 1;
                        while (line != null && !(line.StartsWith("%%Traj End", StringComparison.Ordinal)))
                        {
                            if (line.StartsWith("%%Start Frame", StringComparison.Ordinal))
                            {
                                chemModel = chemFile.Builder.NewChemModel();
                                crystal = chemFile.Builder.NewCrystal();
                                while (line != null && !(line.StartsWith("%%End Frame", StringComparison.Ordinal)))
                                {
                                    // process frame data
                                    if (line.StartsWith("%%Atom Coords", StringComparison.Ordinal))
                                    {
                                        // calculate Z: as it is not explicitely given, try to derive it from the
                                        // energy per fragment and the total energy
                                        if (energyFragment != 0.0 && energyTotal != 0.0)
                                        {
                                            Z = (int)Math.Round(energyTotal / energyFragment);
                                            Debug.WriteLine($"Z derived from energies: {Z}");
                                        }
                                        // add atomC as atoms to crystal
                                        int expatoms = modelStructure.Atoms.Count;
                                        for (int molCount = 1; molCount <= Z; molCount++)
                                        {
                                            IAtomContainer clone = modelStructure.Builder.NewAtomContainer();
                                            for (int i = 0; i < expatoms; i++)
                                            {
                                                line = ReadLine();
                                                IAtom a = clone.Builder.NewAtom();
                                                var st = Strings.Tokenize(line, ' ');
                                                a.Point3D = new Vector3(double.Parse(st[0], NumberFormatInfo.InvariantInfo), double.Parse(st[1], NumberFormatInfo.InvariantInfo), double.Parse(st[2], NumberFormatInfo.InvariantInfo));
                                                a.CovalentRadius = 0.6;
                                                IAtom modelAtom = modelStructure.Atoms[atomids[atomGivenIds[i + 1]]];
                                                a.Symbol = modelAtom.Symbol;
                                                clone.Atoms.Add(a);
                                            }
                                            rebonder.Rebond(clone);
                                            crystal.Add(clone);
                                        }
                                    }
                                    else if (line.StartsWith("%%E/Frag", StringComparison.Ordinal))
                                    {
                                        line = ReadLine().Trim();
                                        energyFragment = double.Parse(line, NumberFormatInfo.InvariantInfo);
                                    }
                                    else if (line.StartsWith("%%Tot E", StringComparison.Ordinal))
                                    {
                                        line = ReadLine().Trim();
                                        energyTotal = double.Parse(line, NumberFormatInfo.InvariantInfo);
                                    }
                                    else if (line.StartsWith("%%Lat Vects", StringComparison.Ordinal))
                                    {
                                        line = ReadLine();
                                        IList<string> st;
                                        st = Strings.Tokenize(line, ' ');
                                        crystal.A = new Vector3(double.Parse(st[0], NumberFormatInfo.InvariantInfo), double.Parse(st[1], NumberFormatInfo.InvariantInfo), double.Parse(st[2], NumberFormatInfo.InvariantInfo));
                                        line = ReadLine();
                                        st = Strings.Tokenize(line, ' ');
                                        crystal.B = new Vector3(double.Parse(st[0], NumberFormatInfo.InvariantInfo), double.Parse(st[1], NumberFormatInfo.InvariantInfo), double.Parse(st[2], NumberFormatInfo.InvariantInfo));
                                        line = ReadLine();
                                        st = Strings.Tokenize(line, ' ');
                                        crystal.C = new Vector3(double.Parse(st[0], NumberFormatInfo.InvariantInfo), double.Parse(st[1], NumberFormatInfo.InvariantInfo), double.Parse(st[2], NumberFormatInfo.InvariantInfo));
                                    }
                                    else if (line.StartsWith("%%Space Group", StringComparison.Ordinal))
                                    {
                                        line = ReadLine().Trim();
                                        
                                        // standardize space group name. See Crystal.SetSpaceGroup()
                                        if (string.Equals("P 21 21 21 (1)", line, StringComparison.Ordinal))
                                        {
                                            crystal.SpaceGroup = "P 2_1 2_1 2_1";
                                        }
                                        else
                                        {
                                            crystal.SpaceGroup = "P1";
                                        }
                                    }
                                    line = ReadLine();
                                }
                                chemModel.Crystal = crystal;
                                chemSequence.Add(chemModel);
                            }
                            line = ReadLine();
                        }
                        chemFile.Add(chemSequence);
                    }
                    // else disregard line

                    // read next line
                    line = ReadLine();
                }
            }
            catch (IOException e)
            {
                Trace.TraceError($"An IOException happened: {e.Message}");
                Debug.WriteLine(e);
                chemFile = null;
            }
            catch (CDKException e)
            {
                Trace.TraceError($"An CDKException happened: {e.Message}");
                Debug.WriteLine(e);
                chemFile = null;
            }

            return chemFile;
        }

        private void ProcessModelCommand(string obj, string command, string format, string field)
        {
            Debug.WriteLine(obj + "->" + command + " (" + format + "): " + field);
            if (string.Equals("Model", obj, StringComparison.Ordinal))
            {
                Trace.TraceWarning("Unkown PMP Model command: " + command);
            }
            else if (string.Equals("Atom", obj, StringComparison.Ordinal))
            {
                if (string.Equals("ACL", command, StringComparison.Ordinal))
                {
                    var atomTypeMatcher = atomTypePattern.Match(field);
                    if (atomTypeMatcher.Success)
                    {
                        int atomicnum = int.Parse(atomTypeMatcher.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                        string type = atomTypeMatcher.Groups[2].Value;
                        ((IAtom)chemObject).AtomicNumber = atomicnum;
                        ((IAtom)chemObject).Symbol = type;
                    }
                    else
                    {
                        Trace.TraceError("Incorrectly formated field value: " + field + ".");
                    }
                }
                else if (string.Equals("Charge", command, StringComparison.Ordinal))
                {
                    try
                    {
                        double charge = double.Parse(field, NumberFormatInfo.InvariantInfo);
                        ((IAtom)chemObject).Charge = charge;
                    }
                    catch (FormatException)
                    {
                        Trace.TraceError("Incorrectly formated float field: " + field + ".");
                    }
                }
                else if (string.Equals("CMAPPINGS", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("FFType", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("Id", command, StringComparison.Ordinal))
                {
                    // ok, should take this into account too
                    chemObject.SetProperty(PMP_ID, field);
                }
                else if (string.Equals("Mass", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("XYZ", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("ZOrder", command, StringComparison.Ordinal))
                {
                    // ok, should take this into account too
                    chemObject.SetProperty(PMP_ZORDER, field);
                }
                else
                {
                    Trace.TraceWarning("Unkown PMP Atom command: " + command);
                }
            }
            else if (string.Equals("Bond", obj, StringComparison.Ordinal))
            {
                if (string.Equals("Atom1", command, StringComparison.Ordinal))
                {
                    int atomid = int.Parse(field, NumberFormatInfo.InvariantInfo);
                    // this assumes that the atoms involved in this bond are
                    // already added, which seems the case in the PMP files
                    bondAtomOnes[bondCounter] = atomid;
                }
                else if (string.Equals("Atom2", command, StringComparison.Ordinal))
                {
                    int atomid = int.Parse(field, NumberFormatInfo.InvariantInfo);
                    // this assumes that the atoms involved in this bond are
                    // already added, which seems the case in the PMP files
                    Debug.WriteLine($"atomids: {atomids}");
                    Debug.WriteLine($"atomid: {atomid}");
                    bondAtomTwos[bondCounter] = atomid;
                }
                else if (string.Equals("Order", command, StringComparison.Ordinal))
                {
                    double order = double.Parse(field, NumberFormatInfo.InvariantInfo);
                    bondOrders[bondCounter] = order;
                }
                else if (string.Equals("Id", command, StringComparison.Ordinal))
                {
                    int bondid = int.Parse(field, NumberFormatInfo.InvariantInfo);
                    bondids[bondCounter] = bondid;
                }
                else if (string.Equals("Label", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("3DGridOrigin", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("3DGridMatrix", command, StringComparison.Ordinal))
                {
                }
                else if (string.Equals("3DGridDivision", command, StringComparison.Ordinal))
                {
                }
                else
                {
                    Trace.TraceWarning("Unkown PMP Bond command: " + command);
                }
            }
            else
            {
                Trace.TraceWarning("Unkown PMP object: " + obj);
            }
        }

        private void ConstructObject(IChemObjectBuilder builder, string obj)
        {
            if (string.Equals("Atom", obj, StringComparison.Ordinal))
            {
                chemObject = builder.NewAtom("C");
            }
            else if (string.Equals("Bond", obj, StringComparison.Ordinal))
            {
                bondCounter++;
                chemObject = builder.NewBond();
            }
            else if (string.Equals("Model", obj, StringComparison.Ordinal))
            {
                modelStructure = builder.NewAtomContainer();
            }
            else
            {
                Trace.TraceError($"Cannot construct PMP object type: {obj}");
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
