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
using NCDK.Numerics;
using NCDK.Geometries;
using NCDK.IO.Formats;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;

namespace NCDK.IO
{
    /// <summary>
    /// This is not a reader for the CIF and mmCIF crystallographic formats.
    /// It is able, however, to extract some content from such files.
    /// It's very ad hoc, not written
    /// using any dictionary. So please complain if something is not working.
    /// In addition, the things it does read are considered experimental.
    /// </summary>
    /// <para>The CIF example on the IUCR website has been tested, as well as Crambin (1CRN)
    /// in the PDB database.</para>
    // @cdk.module io
    // @cdk.keyword file format, CIF
    // @cdk.keyword file format, mmCIF
    // @author  E.L. Willighagen
    // @cdk.created 2003-10-12
    // @cdk.iooptions
    public class CIFReader : DefaultChemObjectReader
    {
        class LineReader
        {
            public TextReader Reader { get; private set; }
            private string nextLine;

            public LineReader(TextReader input)
            {
                this.Reader = input;
                nextLine = this.Reader.ReadLine();
            }

            public string ReadLine()
            {
                if (nextLine == null)
                    return null;
                string line = nextLine;
                nextLine = Reader.ReadLine();
                return line;
            }

            public bool Ready() => nextLine != null;
        }

        private LineReader input;

        private ICrystal crystal = null;
        // cell parameters
        private double a = 0.0;
        private double b = 0.0;
        private double c = 0.0;
        private double alpha = 0.0;
        private double beta = 0.0;
        private double gamma = 0.0;

        /// <summary>
        /// Create an CIF like file reader.
        /// </summary>
        /// <param name="input">source of CIF data</param>
        public CIFReader(TextReader input)
        {
            this.input = new LineReader(input);
        }

        public CIFReader(Stream input)
                : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => CIFFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type))
                return true;
            return false;
        }

        /// <summary>
        /// Read a ChemFile from input.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns>the content in a ChemFile object</returns>
        public override T Read<T>(T obj)
        {
            switch (obj)
            {
                case IChemFile cf:
                    try
                    {
                        cf = ReadChemFile(cf);
                    }
                    catch (IOException)
                    {
                        Trace.TraceError("Input/Output error while reading from input.");
                    }
                    return (T)cf;
                default:
                    throw new CDKException("Only supported is reading of ChemFile.");
            }
        }

        /// <summary>
        /// Read the ShelX from input. Each ShelX document is expected to contain one crystal structure.
        /// </summary>
        /// <returns>a ChemFile with the coordinates, charges, vectors, etc.</returns>
        private IChemFile ReadChemFile(IChemFile file)
        {
            var seq = file.Builder.NewChemSequence();
            var model = file.Builder.NewChemModel();
            crystal = file.Builder.NewCrystal();

            var line = input.ReadLine();
            bool end_found = false;
            while (input.Ready() && line != null && !end_found)
            {
                if (line.Length == 0)
                {
                    Debug.WriteLine("Skipping empty line");
                    // skip empty lines
                }
                else if (line[0] == '#')
                {
                    Trace.TraceWarning($"Skipping comment: {line}");
                    // skip comment lines
                }
                else if (!(line[0] == '_' || line.StartsWith("loop_", StringComparison.Ordinal)))
                {
                    Trace.TraceWarning($"Skipping unrecognized line: {line}");
                    // skip line
                }
                else
                {
                    /* determine CIF command */
                    string command = "";
                    var spaceIndex = line.IndexOf(' ');
                    if (spaceIndex != -1)
                    {
                        // everything upto space is command
                        try
                        {
                            command = line.Substring(0, spaceIndex);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // disregard this line
                            break;
                        }
                    }
                    else
                    {
                        // complete line is command
                        command = line;
                    }

                    Debug.WriteLine($"command: {command}");
                    if (command.StartsWith("_cell", StringComparison.Ordinal))
                    {
                        ProcessCellParameter(command, line);
                    }
                    else if (string.Equals(command, "loop_", StringComparison.Ordinal))
                    {
                        line = ProcessLoopBlock();
                        continue;
                    }
                    else if (string.Equals(command, "_symmetry_space_group_name_H-M", StringComparison.Ordinal))
                    {
                        string value = line.Substring(29).Trim();
                        crystal.SpaceGroup = value;
                    }
                    else
                    {
                        // skip command
                        Trace.TraceWarning($"Skipping command: {command}");
                        line = input.ReadLine();
                        if (line != null && line.StartsWithChar(';'))
                        {
                            Debug.WriteLine("Skipping block content");
                            line = input.ReadLine();
                            if (line != null)
                                line = line.Trim();
                            while ((line = input.ReadLine()) != null
                                && !line.StartsWith(";"))
                            {
                                Debug.WriteLine($"Skipping block line: {line}");
                            }
                        }
                    }
                }
                line = input.ReadLine();
            }
            Trace.TraceInformation($"Adding crystal to file with #atoms: {crystal.Atoms.Count}");
            model.Crystal = crystal;
            seq.Add(model);
            file.Add(seq);
            return file;
        }

        private void ProcessCellParameter(string command, string line)
        {
            command = command.Substring(6); // skip the "_cell." part
            if (string.Equals(command, "length_a", StringComparison.Ordinal))
            {
                var value = line.Substring(14).Trim();
                a = ParseIntoDouble(value);
                PossiblySetCellParams(a, b, c, alpha, beta, gamma);
            }
            else if (string.Equals(command, "length_b", StringComparison.Ordinal))
            {
                var value = line.Substring(14).Trim();
                b = ParseIntoDouble(value);
                PossiblySetCellParams(a, b, c, alpha, beta, gamma);
            }
            else if (string.Equals(command, "length_c", StringComparison.Ordinal))
            {
                var value = line.Substring(14).Trim();
                c = ParseIntoDouble(value);
                PossiblySetCellParams(a, b, c, alpha, beta, gamma);
            }
            else if (string.Equals(command, "angle_alpha", StringComparison.Ordinal))
            {
                var value = line.Substring(17).Trim();
                alpha = ParseIntoDouble(value);
                PossiblySetCellParams(a, b, c, alpha, beta, gamma);
            }
            else if (string.Equals(command, "angle_beta", StringComparison.Ordinal))
            {
                var value = line.Substring(16).Trim();
                beta = ParseIntoDouble(value);
                PossiblySetCellParams(a, b, c, alpha, beta, gamma);
            }
            else if (string.Equals(command, "angle_gamma", StringComparison.Ordinal))
            {
                var value = line.Substring(17).Trim();
                gamma = ParseIntoDouble(value);
                PossiblySetCellParams(a, b, c, alpha, beta, gamma);
            }
        }

        private void PossiblySetCellParams(double a, double b, double c, double alpha, double beta, double gamma)
        {
            if (a != 0.0 && b != 0.0 && c != 0.0 && alpha != 0.0 && beta != 0.0 && gamma != 0.0)
            {
                Trace.TraceInformation("Found and set crystal cell parameters");
                var axes = CrystalGeometryTools.NotionalToCartesian(a, b, c, alpha, beta, gamma);

                crystal.A = axes[0];
                crystal.B = axes[1];
                crystal.C = axes[2];
            }
        }

        private string ProcessLoopBlock()
        {
            string line = input.ReadLine().Trim();
            if (line.StartsWith("_atom", StringComparison.Ordinal))
            {
                Trace.TraceInformation("Found atom loop block");
                return ProcessAtomLoopBlock(line);
            }
            else
            {
                Trace.TraceWarning("Skipping loop block");
                return SkipLoop(line);
            }
        }

        private string SkipLoop(string line)
        {
            // skip everything until the end of the loop body
            if (line != null)
                line = line.Trim();
            // First, skip the loop_ data name list:
            while (line != null && line.Length > 0 && line[0] == '_')
            {
                line = input.ReadLine();
                if (line != null)
                    line = line.Trim();
            }
            return SkipLoopBody(line);
        }

        private string SkipLoopBody(String line)
        {
            // Then, skip every line that looks like starting with a CIF value:
            while (line != null && line.Length > 0 &&
                    line[0] != '#' &&
                    line[0] != '_' &&
                    !line.StartsWith("loop_", StringComparison.Ordinal) &&
                    !line.StartsWith("data_", StringComparison.Ordinal))
            {
                line = input.ReadLine();
                if (line != null)
                    line = line.Trim();
            }
            return line;
        }

        private string ProcessAtomLoopBlock(string firstLine)
        {
            int atomLabel = -1; // -1 means not found in this block
            int atomSymbol = -1;
            int atomFractX = -1;
            int atomFractY = -1;
            int atomFractZ = -1;
            int atomRealX = -1;
            int atomRealY = -1;
            int atomRealZ = -1;
            string line = firstLine.Trim();
            int headerCount = 0;
            bool hasParsableInformation = false;
            while (line != null && line[0] == '_')
            {
                headerCount++;
                if (line.Equals("_atom_site_label", StringComparison.Ordinal) || line.Equals("_atom_site_label_atom_id", StringComparison.Ordinal))
                {
                    atomLabel = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"label found in col: {atomLabel}");
                }
                else if (line.StartsWith("_atom_site_fract_x", StringComparison.Ordinal))
                {
                    atomFractX = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"frac x found in col: {atomFractX}");
                }
                else if (line.StartsWith("_atom_site_fract_y", StringComparison.Ordinal))
                {
                    atomFractY = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"frac y found in col: {atomFractY}");
                }
                else if (line.StartsWith("_atom_site_fract_z", StringComparison.Ordinal))
                {
                    atomFractZ = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"frac z found in col: {atomFractZ}");
                }
                else if (string.Equals(line, "_atom_site.Cartn_x", StringComparison.Ordinal))
                {
                    atomRealX = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"cart x found in col: {atomRealX}");
                }
                else if (string.Equals(line, "_atom_site.Cartn_y", StringComparison.Ordinal))
                {
                    atomRealY = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"cart y found in col: {atomRealY}");
                }
                else if (string.Equals(line, "_atom_site.Cartn_z", StringComparison.Ordinal))
                {
                    atomRealZ = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"cart z found in col: {atomRealZ}");
                }
                else if (string.Equals(line, "_atom_site.type_symbol", StringComparison.Ordinal))
                {
                    atomSymbol = headerCount;
                    hasParsableInformation = true;
                    Trace.TraceInformation($"type_symbol found in col: {atomSymbol}");
                }
                else
                {
                    Trace.TraceWarning($"Ignoring atom loop block field: {line}");
                }
                line = input.ReadLine().Trim();
            }
            if (!hasParsableInformation)
            {
                Trace.TraceInformation("No parsable info found");
                return SkipLoopBody(line);
            }
            else
            {
                // now that headers are parsed, read the data
                while (line != null && line.Length > 0
                    && line[0] != '#'
                    && line[0] != '_'
                    && !line.StartsWith("loop_", StringComparison.Ordinal)
                    && !line.StartsWith("data_", StringComparison.Ordinal))
                {
                    Debug.WriteLine("new row");
                    var tokenizer = Strings.Tokenize(line);
                    if (tokenizer.Count < headerCount)
                    {
                        Trace.TraceWarning("Column count mismatch; assuming continued on next line");
                        Debug.WriteLine($"Found #expected, #found:{headerCount}, {tokenizer.Count}");
                        tokenizer = Strings.Tokenize(line + input.ReadLine());
                    }
                    int colIndex = 0;
                    // process one row
                    var atom = crystal.Builder.NewAtom("C");
                    var frac = new Vector3();
                    var real = new Vector3();
                    bool hasFractional = false;
                    bool hasCartesian = false;
                    foreach (var field in tokenizer)
                    {
                        colIndex++;
                        Debug.WriteLine($"Parsing col,token: {colIndex}={field}");
                        if (colIndex == atomLabel)
                        {
                            if (atomSymbol == -1)
                            {
                                // no atom symbol found, use label
                                string element = ExtractFirstLetters(field);
                                atom.Symbol = element;
                            }
                            atom.Id = field;
                        }
                        else if (colIndex == atomFractX)
                        {
                            hasFractional = true;
                            frac.X = ParseIntoDouble(field);
                        }
                        else if (colIndex == atomFractY)
                        {
                            hasFractional = true;
                            frac.Y = ParseIntoDouble(field);
                        }
                        else if (colIndex == atomFractZ)
                        {
                            hasFractional = true;
                            frac.Z = ParseIntoDouble(field);
                        }
                        else if (colIndex == atomSymbol)
                        {
                            atom.Symbol = field;
                        }
                        else if (colIndex == atomRealX)
                        {
                            hasCartesian = true;
                            Debug.WriteLine($"Adding x3: {ParseIntoDouble(field)}");
                            real.X = ParseIntoDouble(field);
                        }
                        else if (colIndex == atomRealY)
                        {
                            hasCartesian = true;
                            Debug.WriteLine($"Adding y3: {ParseIntoDouble(field)}");
                            real.Y = ParseIntoDouble(field);
                        }
                        else if (colIndex == atomRealZ)
                        {
                            hasCartesian = true;
                            Debug.WriteLine($"Adding x3: {ParseIntoDouble(field)}");
                            real.Z = ParseIntoDouble(field);
                        }
                    }
                    if (hasCartesian)
                    {
                        Vector3 a = crystal.A;
                        Vector3 b = crystal.B;
                        Vector3 c = crystal.C;
                        frac = CrystalGeometryTools.CartesianToFractional(a, b, c, real);
                        atom.FractionalPoint3D = frac;
                    }
                    if (hasFractional)
                    {
                        atom.FractionalPoint3D = frac;
                    }
                    Debug.WriteLine($"Adding atom: {atom}");
                    crystal.Atoms.Add(atom);

                    // look up next row
                    line = input.ReadLine();
                    if (line != null)
                        line = line.Trim();
                }
            }
            return line;
        }

        /// <summary>
        /// Process double in the format: '.071(1)'.
        /// </summary>
        private static double ParseIntoDouble(string value)
        {
            double returnVal = 0.0;
            if (value[0] == '.')
                value = "0" + value;
            int bracketIndex = value.IndexOf('(');
            if (bracketIndex != -1)
            {
                value = value.Substring(0, bracketIndex);
            }
            try
            {
                returnVal = double.Parse(value, NumberFormatInfo.InvariantInfo);
            }
            catch (Exception)
            {
                Trace.TraceError($"Could not parse double string: {value}");
            }
            return returnVal;
        }

        private static string ExtractFirstLetters(string value)
        {
            var result = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    break;
                }
                else
                {
                    result.Append(value[i]);
                }
            }
            return result.ToString();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Reader.Dispose();
                }

                input = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion
    }
}
