/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
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
using NCDK.Geometries;
using NCDK.IO.Formats;
using NCDK.Maths;
using NCDK.Numerics;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// A reader for ShelX output (RES) files. It does not read all information.
    /// The list of fields that is read: REM, END, CELL, SPGR.
    /// In additions atoms are read.
    /// <para>A reader for ShelX files. It currently supports ShelXL.</para>
    /// </summary>
    /// <remarks>
    /// The ShelXL format is described on the net:
    /// <see href="http://www.msg.ucsf.edu/local/programs/shelxl/ch_07.html">http://www.msg.ucsf.edu/local/programs/shelxl/ch_07.html</see>.
    /// </remarks>
    // @cdk.module io
    // @cdk.iooptions
    // @cdk.keyword file format, ShelXL
    // @author E.L. Willighagen
    public class ShelXReader : DefaultChemObjectReader
    {
        private TextReader input;

        /// <summary>
        /// Create an ShelX file reader.
        /// </summary>
        /// <param name="input">source of ShelX data</param>
        public ShelXReader(TextReader input)
        {
            this.input = input;
        }

        public ShelXReader(Stream input)
            : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => ShelXFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            if (typeof(ICrystal).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// Read a <see cref="IChemFile"/> from input.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns> the content in a <see cref="IChemFile"/> object</returns>
        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                try
                {
                    return (T)ReadChemFile((IChemFile)obj);
                }
                catch (IOException e)
                {
                    Trace.TraceError($"Input/Output error while reading from input: {e.Message}");
                    throw new CDKException(e.Message, e);
                }
            }
            else if (obj is ICrystal)
            {
                try
                {
                    return (T)ReadCrystal((ICrystal)obj);
                }
                catch (IOException e)
                {
                    Trace.TraceError($"Input/Output error while reading from input: {e.Message}");
                    throw new CDKException(e.Message, e);
                }
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile.");
            }
        }

        /// <summary>
        /// Read the ShelX from input. Each ShelX document is expected to contain one crystal structure.
        /// </summary>
        /// <param name="file"></param>
        /// <returns>a ChemFile with the coordinates, charges, vectors, etc.</returns>
        private IChemFile ReadChemFile(IChemFile file)
        {
            IChemSequence seq = file.Builder.NewChemSequence();
            IChemModel model = file.Builder.NewChemModel();
            ICrystal crystal = ReadCrystal(file.Builder.NewCrystal());
            model.Crystal = crystal;
            seq.Add(model);
            file.Add(seq);
            return file;
        }

        private ICrystal ReadCrystal(ICrystal crystal)
        {
            string line = input.ReadLine();
            bool end_found = false;
            while (line != null && !end_found)
            {
                /* is line continued? */
                if (line.Length > 0 && line.Substring(line.Length - 1).Equals("=", StringComparison.Ordinal))
                {
                    /* yes, line is continued */
                    line = line + input.ReadLine();
                }

                /* determine ShelX command */
                string command;
                try
                {
                    command = line.Substring(0, 4);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // disregard this line
                    break;
                }

                Debug.WriteLine($"command: {command}");
                var u_command = command.ToUpperInvariant();
                if (u_command.StartsWith("REM", StringComparison.Ordinal))
                {
                    /* line is comment, disregard */

                    /* 7.1 Crystal data and general instructions */
                }
                else if (u_command.StartsWith("END", StringComparison.Ordinal))
                {
                    end_found = true;
                }
                else switch (u_command)
                    {
                        case "TITL":
                            break;
                        case "CELL":
                            {
                                // example: CELL 1.54184 23.56421 7.13203 18.68928 90.0000
                                // 109.3799 90.0000 CELL 1.54184 7.11174 21.71704 30.95857
                                // 90.000 90.000 90.000
                                var st = Strings.Tokenize(line);
                                //st[0]; // string command_again
                                //st[1]; // string wavelength
                                string sa = st[2];
                                string sb = st[3];
                                string sc = st[4];
                                string salpha = st[5];
                                string sbeta = st[6];
                                string sgamma = st[7];
                                Debug.WriteLine($"a: {sa}");
                                Debug.WriteLine($"b: {sb}");
                                Debug.WriteLine($"c: {sc}");
                                Debug.WriteLine($"alpha: {salpha}");
                                Debug.WriteLine($"beta : {sbeta}");
                                Debug.WriteLine($"gamma: {sgamma}");

                                double a = FortranFormat.Atof(sa);
                                double b = FortranFormat.Atof(sb);
                                double c = FortranFormat.Atof(sc);
                                double alpha = FortranFormat.Atof(salpha);
                                double beta = FortranFormat.Atof(sbeta);
                                double gamma = FortranFormat.Atof(sgamma);

                                Vector3[] axes = CrystalGeometryTools.NotionalToCartesian(a, b, c, alpha, beta, gamma);

                                crystal.A = axes[0];
                                crystal.B = axes[1];
                                crystal.C = axes[2];
                            }
                            break;
                        case "ZERR":
                        case "LATT":
                        case "SYMM":
                        case "SFAC":
                        case "DISP":
                        case "UNIT":
                        case "LAUE":
                        case "REM ":
                        case "MORE":
                        case "TIME":
                        /* 7.2 Reflection data input */
                        case "HKLF":
                        case "OMIT":
                        case "SHEL":
                        case "BASF":
                        case "TWIN":
                        case "EXTI":
                        case "SWAT":
                        case "HOPE":
                        case "MERG":
                        /* 7.3 Atom list and least-squares constraints */
                        case "SPEC":
                        case "RESI":
                        case "MOVE":
                        case "ANIS":
                        case "AFIX":
                        case "HFIX":
                        case "FRAG":
                        case "FEND":
                        case "EXYZ":
                        //case "EXTI":
                        case "EADP":
                        case "EQIV":
                        /* 7.4 The connectivity list */
                        case "CONN":
                        case "PART":
                        case "BIND":
                        case "FREE":
                        /* 7.5 Least-squares restraints */
                        case "DFIX":
                        case "DANG":
                        case "BUMP":
                        case "SAME":
                        case "SADI":
                        case "CHIV":
                        case "FLAT":
                        case "DELU":
                        case "SIMU":
                        case "DEFS":
                        case "ISOR":
                        case "NCSY":
                        case "SUMP":
                        /* 7.6 Least-squares organization */
                        case "L.S.":
                        case "CGLS":
                        case "BLOC":
                        case "DAMP":
                        case "STIR":
                        case "WGHT":
                        case "FVAR":
                        /* 7.7 Lists and tables */
                        case "BOND":
                        case "CONF":
                        case "MPLA":
                        case "RTAB":
                        case "HTAB":
                        case "LIST":
                        case "ACTA":
                        case "SIZE":
                        case "TEMP":
                        case "WPDB":
                        /* 7.8 Fouriers, peak search and lineprinter plots */
                        case "FMAP":
                        case "GRID":
                        case "PLAN":
                            break;
                        case "MOLE":
                            /* NOT DOCUMENTED BUT USED BY PLATON */
                            break;
                        case "SPGR":
                            {
                                // Line added by PLATON stating the spacegroup
                                var st = Strings.Tokenize(line);
                                //st[0]; // string command_again
                                string spacegroup = st[1];
                                crystal.SpaceGroup = spacegroup;
                            }
                            break;
                        case "    ":
                            {
                                Debug.WriteLine($"Disrgarding line assumed to be added by PLATON: {line}");

                                /* All other is atom */
                            }
                            break;
                        default:
                            {
                                //Debug.WriteLine($"Assumed to contain an atom: {line}");

                                // this line gives an atom, because all lines not starting with
                                // a ShelX command is an atom (that sucks!)
                                var st = Strings.Tokenize(line);
                                string atype = st[0];
                                //st[1]; // string scatt_factor
                                string sa = st[2];
                                string sb = st[3];
                                string sc = st[4];
                                // skip the rest

                                if (char.IsDigit(atype[1]))
                                {
                                    // atom type has a one letter code
                                    atype = atype.Substring(0, 1);
                                }
                                else
                                {
                                    var sb2 = new StringBuilder();
                                    sb2.Append(atype[1]);
                                    atype = atype.Substring(0, 1) + sb2.ToString().ToLowerInvariant();
                                }

                                double[] frac = new double[3];
                                frac[0] = FortranFormat.Atof(sa); // fractional coordinates
                                frac[1] = FortranFormat.Atof(sb);
                                frac[2] = FortranFormat.Atof(sc);
                                Debug.WriteLine("fa,fb,fc: " + frac[0] + ", " + frac[1] + ", " + frac[2]);

                                if (string.Equals(atype, "Q", StringComparison.OrdinalIgnoreCase))
                                {
                                    // ingore atoms named Q
                                }
                                else
                                {
                                    Trace.TraceInformation("Adding atom: " + atype + ", " + frac[0] + ", " + frac[1] + ", " + frac[2]);
                                    IAtom atom = crystal.Builder.NewAtom(atype);
                                    atom.FractionalPoint3D = new Vector3(frac[0], frac[1], frac[2]);
                                    crystal.Atoms.Add(atom);
                                    Debug.WriteLine($"Atom added: {atom}");
                                }
                            }
                            break;
                    }
                line = input.ReadLine();
            }
            return crystal;
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
