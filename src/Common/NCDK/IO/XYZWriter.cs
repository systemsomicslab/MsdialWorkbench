/* Copyright (C) 2002  Bradley A. Smith <bradley@baysmith.com>
 *               2002  Miguel Howard
 *               2003-2007  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.IO.Formats;
using NCDK.Numerics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @author  Bradley A. Smith <bradley@baysmith.com>
    // @author  J. Daniel Gezelter
    // @author  Egon Willighagen
    public class XYZWriter : DefaultChemObjectWriter
    {
        private TextWriter writer;

        // new FormatStringBuffer("%-8.6f");
        private static string ToString(double f)
        {
            var s = f.ToString("F6", CultureInfo.InvariantCulture.NumberFormat);
            var len = Math.Max(s.Length, 9);
            return new string(' ', len - s.Length) + s;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="output">the stream to write the XYZ file to.</param>
        public XYZWriter(TextWriter output)
        {
            writer = output;
        }

        public XYZWriter(Stream output)
            : this(new StreamWriter(output))
        { }

        public override IResourceFormat Format => XYZFormat.Instance;


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Dispose();
                }

                writer = null;

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

        public override void Write(IChemObject obj)
        {
            if (obj is IAtomContainer)
            {
                try
                {
                    WriteMolecule((IAtomContainer)obj);
                }
                catch (Exception ex)
                {
                    throw new CDKException("Error while writing XYZ file: " + ex.Message, ex);
                }
            }
            else
            {
                throw new CDKException("XYZWriter only supports output of Molecule classes.");
            }
        }

        /// <summary>
       /// writes a single frame in XYZ format to the Writer.
       /// <param name="mol">the Molecule to write</param>
       /// </summary>
        public void WriteMolecule(IAtomContainer mol)
        {

            string st = "";
            bool writecharge = true;

            try
            {

                string s1 = "" + mol.Atoms.Count;
                writer.Write(s1, 0, s1.Length);
                writer.Write('\n');

                string s2 = null; // FIXME: add some interesting comment
                if (s2 != null)
                {
                    writer.Write(s2, 0, s2.Length);
                }
                writer.Write('\n');

                // Loop through the atoms and write them out:
                foreach (var a in mol.Atoms)
                {
                    st = a.Symbol;

                    Vector3? p3 = a.Point3D;
                    if (p3 != null)
                    {
                        st = st + "\t" + ToString(p3.Value.X) + "\t" + ToString(p3.Value.Y) + "\t" + ToString(p3.Value.Z);
                    }
                    else
                    {
                        st = st + "\t" + ToString(0.0) + "\t" + ToString(0.0) + "\t" + ToString(0.0);
                    }

                    if (writecharge)
                    {
                        double ct = a.Charge ?? 0.0;
                        st = st + "\t" + ct;
                    }

                    writer.Write(st, 0, st.Length);
                    writer.Write('\n');

                }
            }
            catch (IOException e)
            {
                //            throw e;
                Trace.TraceError($"Error while writing file: {e.Message}");
                Debug.WriteLine(e);
            }
        }
    }
}
