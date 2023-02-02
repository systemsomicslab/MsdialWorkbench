/* Copyright (C) 2005-2006  Ideaconsult Ltd.
 *               2012       Egon Willighagen <egonw@users.sf.net>
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
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// Reads MOPAC output, extracts several electronic parameters and assigns them as a molecule
    /// properties.
    /// </summary>
    /// <remarks>
    /// Parameters: "NO. OF FILLED LEVELS",    "TOTAL ENERGY","FINAL HEAT OF FORMATION",
    /// "IONIZATION POTENTIAL", "ELECTRONIC ENERGY","CORE-CORE REPULSION","MOLECULAR WEIGHT".
    /// Doesn't update structure coordinates ! (TODO fix)
    /// </remarks>
    // @author      Nina Jeliazkova <nina@acad.bg>
    // @cdk.module  io
    public class Mopac7Reader : DefaultChemObjectReader
    {
        TextReader input = null;
        private static readonly IReadOnlyList<string> parameters = new[] {"NO. OF FILLED LEVELS", "TOTAL ENERGY", "FINAL HEAT OF FORMATION",
            "IONIZATION POTENTIAL", "ELECTRONIC ENERGY", "CORE-CORE REPULSION", "MOLECULAR WEIGHT", "EHOMO", "ELUMO"};
        private static readonly IReadOnlyList<string> units = new[] { "", "EV", "KJ", "", "EV", "EV", "", "EV", "EV" };
        private const string eigenvalues = "EIGENVALUES";
        private const string filledLevels = "NO. OF FILLED LEVELS";

        /// <summary>
        /// Constructs a new Mopac7reader that can read a molecule from a given <see cref="TextReader"/>.
        /// </summary>
        /// <param name="input">The <see cref="TextReader"/> to read from</param>
        public Mopac7Reader(TextReader input)
        {
            this.input = input;
        }

        /// <summary>
        /// Constructs a new Mopac7reader that can read a molecule from a given <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to read from</param>
        public Mopac7Reader(Stream input)
            : this(new StreamReader(input))
        { }

        // FINAL HEAT OF FORMATION = -32.90826 KCAL = -137.68818 KJ TOTAL ENERGY =
        // -1618.31024 EV ELECTRONIC ENERGY = -6569.42640 EV POINT GROUP: C1
        // CORE-CORE REPULSION = 4951.11615 EV IONIZATION POTENTIAL = 10.76839 NO.
        // OF FILLED LEVELS = 23 MOLECULAR WEIGHT = 122.123

        private static readonly string[] expected_columns = { "NO.", "ATOM", "X", "Y", "Z" };
        public override T Read<T>(T obj)
        {
            var eigenvalues = new StringBuilder();
            if (obj is IAtomContainer container)
            {
                try
                {
                    string line = input.ReadLine();
                    while (line != null)
                    {
                        if (line.IndexOf("****  MAX. NUMBER OF ATOMS ALLOWED", StringComparison.Ordinal) > -1)
                            throw new CDKException(line);
                        if (line.IndexOf("TO CONTINUE CALCULATION SPECIFY \"GEO-OK\"", StringComparison.Ordinal) > -1)
                            throw new CDKException(line);
                        if (string.Equals("CARTESIAN COORDINATES", line.Trim(), StringComparison.Ordinal))
                        {
                            IAtomContainer atomcontainer = ((IAtomContainer)obj);
                            input.ReadLine(); //reads blank line
                            line = input.ReadLine();

                            var columns = Strings.Tokenize(line.Trim(), ' ');
                            int okCols = 0;
                            if (columns.Count == expected_columns.Length)
                                for (int i = 0; i < expected_columns.Length; i++)
                                    okCols += (string.Equals(columns[i], expected_columns[i], StringComparison.Ordinal)) ? 1 : 0;

                            if (okCols < expected_columns.Length)
                                continue;

                            input.ReadLine(); //reads blank line
                            int atomIndex = 0;
                            while (line.Trim().Length != 0)
                            {
                                line = input.ReadLine();
                                var tokens = Strings.Tokenize(line);
                                int token = 0;

                                IAtom atom = null;
                                double[] point3d = new double[3];
                                foreach (var tokenStr in tokens)
                                {
                                    switch (token)
                                    {
                                        case 0:
                                            atomIndex = int.Parse(tokenStr, NumberFormatInfo.InvariantInfo) - 1;
                                            if (atomIndex < atomcontainer.Atoms.Count)
                                            {
                                                atom = atomcontainer.Atoms[atomIndex];
                                            }
                                            else
                                                atom = null;
                                            break;
                                        case 1:
                                            if ((atom != null) && (!string.Equals(tokenStr, atom.Symbol, StringComparison.Ordinal)))
                                                atom = null;
                                            break;
                                        case 2:
                                            point3d[0] = double.Parse(tokenStr, NumberFormatInfo.InvariantInfo);
                                            break;
                                        case 3:
                                            point3d[1] = double.Parse(tokenStr, NumberFormatInfo.InvariantInfo);
                                            break;
                                        case 4:
                                            point3d[2] = double.Parse(tokenStr, NumberFormatInfo.InvariantInfo);
                                            if (atom != null)
                                                atom.Point3D = new Vector3(point3d[0], point3d[1], point3d[2]);
                                            break;
                                    }
                                    token++;
                                    if (atom == null) break;
                                }
                                if ((atom == null) || ((atomIndex + 1) >= atomcontainer.Atoms.Count))
                                    break;
                            }
                        }
                        else if (line.IndexOf(Mopac7Reader.eigenvalues, StringComparison.Ordinal) >= 0)
                        {
                            line = input.ReadLine();
                            line = input.ReadLine();
                            while (line.Trim().Length != 0)
                            {
                                eigenvalues.Append(line);
                                line = input.ReadLine();
                            }
                            container.SetProperty(Mopac7Reader.eigenvalues, eigenvalues.ToString());
                        }
                        else
                            for (int i = 0; i < parameters.Count; i++)
                                if (line.IndexOf(parameters[i], StringComparison.Ordinal) >= 0)
                                {
                                    string value = line.Substring(line.LastIndexOf('=') + 1).Trim();

                                    // v = v.ReplaceAll("EV",""); v =
                                    // v.ReplaceAll("KCAL",""); v =
                                    // v.ReplaceAll("KJ","");
                                    if (!string.IsNullOrEmpty(units[i]))
                                        value = value.Replace(units[i], "").Trim();
                                    int pos = value.IndexOf(' ');
                                    if (pos >= 0) value = value.Substring(0, pos - 1);
                                    container.SetProperty(parameters[i], value.Trim());
                                    break;
                                }
                        line = input.ReadLine();
                    }
                    CalcHomoLumo(container);
                    return (T)container;
                }
                catch (IOException exception)
                {
                    throw new CDKException(exception.Message);
                }
            }
            else
                return default(T);
        }

        private static void CalcHomoLumo(IAtomContainer mol)
        {
            var eigenProp = mol.GetProperty<string>(eigenvalues);
            if (eigenProp == null) return;
            //mol.GetProperties().Remove(eigenvalues);
            var filledLevelsProp = mol.GetProperty<string>(filledLevels);
            //mol.GetProperties().Remove(filledLevels);
            if (filledLevelsProp == null)
                return;
            int nFilledLevels = 0;
            try
            {
                nFilledLevels = int.Parse(filledLevelsProp, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException)
            {
                return;
            }
            var eigenVals = Strings.Tokenize(eigenProp);
            int levelCounter = 0;
            foreach (var eigenVal in eigenVals)
            {
                if (string.IsNullOrWhiteSpace(eigenVal))
                    continue;
                else
                    try
                    {
                        // check if the value is an proper double:
                        double.Parse(eigenVal, NumberFormatInfo.InvariantInfo);
                        levelCounter++;
                        if (levelCounter == nFilledLevels)
                        {
                            mol.SetProperty("EHOMO", eigenVal);
                        }
                        else if (levelCounter == (nFilledLevels + 1))
                        {
                            mol.SetProperty("ELUMO", eigenVal);
                        }
                    }
                    catch (FormatException)
                    {
                        return;
                    }
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

        public override bool Accepts(Type type)
        {
            if (typeof(IAtomContainer).IsAssignableFrom(type)) return true;
            return false;
        }

        public override IResourceFormat Format => MOPAC7Format.Instance;
    }
}

