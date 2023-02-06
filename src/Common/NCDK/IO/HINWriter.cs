/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
using System.Globalization;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// Writer that outputs in the HIN format.
    /// </summary>
    // @author Rajarshi Guha <rajarshi@presidency.com>
    // @cdk.module io
    // @cdk.created 2004-01-27
    // @cdk.iooptions
    public class HINWriter : DefaultChemObjectWriter
    {
        private TextWriter writer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="output">the stream to write the HIN file to.</param>
        public HINWriter(TextWriter output)
        {
            this.writer = output;
        }

        public HINWriter(Stream output)
                : this(new StreamWriter(output))
        { }

        public override IResourceFormat Format => HINFormat.Instance;

        /// <summary>
        /// Flushes the output and closes this object.
        /// </summary>
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
            if (typeof(IEnumerableChemObject<IAtomContainer>).IsAssignableFrom(type)) return true;
            return false;
        }

        public override void Write(IChemObject obj)
        {
            if (obj is IAtomContainer)
            {
                try
                {
                    IChemObjectSet<IAtomContainer> som = obj.Builder.NewAtomContainerSet();
                    som.Add((IAtomContainer)obj);
                    WriteAtomContainer(som);
                }
                catch (Exception ex)
                {
                    if (!(ex is ArgumentException | ex is IOException))
                        throw;
                    throw new CDKException("Error while writing HIN file: " + ex.Message, ex);
                }
            }
            else if (obj is IEnumerableChemObject<IAtomContainer>)
            {
                try
                {
                    WriteAtomContainer((IEnumerableChemObject<IAtomContainer>)obj);
                }
                catch (IOException)
                {
                    //
                }
            }
            else
            {
                throw new CDKException("HINWriter only supports output of Molecule or SetOfMolecule classes.");
            }
        }

        /// <summary>
        /// writes all the molecules supplied in a MoleculeSet class to
        /// a single HIN file. You can also supply a single Molecule object
        /// as well
        /// </summary>
        /// <param name="som">the set of molecules to write</param>
        /// <exception cref="IOException">if there is a problem writing the molecule</exception>
        private void WriteAtomContainer(IEnumerableChemObject<IAtomContainer> som)
        {
            string sym;
            double chrg;

            int molnumber = 0;
            foreach (var mol in som)
            {
                molnumber++;

                try
                {
                    string molname = "mol " + molnumber + " " + mol.Title;

                    writer.Write(molname, 0, molname.Length);
                    writer.Write('\n');

                    // Loop through the atoms and write them out:

                    int i = 0;
                    foreach (var atom in mol.Atoms)
                    {
                        string line = "atom ";

                        sym = atom.Symbol;
                        chrg = atom.Charge.Value;
                        Vector3 point = atom.Point3D.Value;

                        line = line + (i + 1).ToString(NumberFormatInfo.InvariantInfo) + " - " + sym + " ** - " + chrg.ToString(NumberFormatInfo.InvariantInfo) + " "
                                + point.X.ToString(NumberFormatInfo.InvariantInfo) + " " + point.Y.ToString(NumberFormatInfo.InvariantInfo) + " "
                                + point.Z.ToString(NumberFormatInfo.InvariantInfo) + " ";

                        string abuf = "";
                        int ncon = 0;
                        foreach (var bond in mol.Bonds)
                        {
                            if (bond.Contains(atom))
                            {
                                // current atom is in the bond so lets get the connected atom
                                IAtom connectedAtom = bond.GetOther(atom);
                                BondOrder bondOrder = bond.Order;
                                int serial;
                                string bondType = "";

                                // get the serial no for this atom
                                serial = mol.Atoms.IndexOf(connectedAtom);

                                if (bondOrder == BondOrder.Single)
                                    bondType = "s";
                                else if (bondOrder == BondOrder.Double)
                                    bondType = "d";
                                else if (bondOrder == BondOrder.Triple)
                                    bondType = "t";
                                else if (bond.IsAromatic) bondType = "a";
                                abuf = abuf + (serial + 1).ToString(NumberFormatInfo.InvariantInfo) + " " + bondType + " ";
                                ncon++;
                            }
                        }
                        line = line + " " + ncon.ToString(NumberFormatInfo.InvariantInfo) + " " + abuf;
                        writer.Write(line, 0, line.Length);
                        writer.Write('\n');
                        i++;
                    }
                    string buf = "endmol " + molnumber;
                    writer.Write(buf, 0, buf.Length);
                    writer.Write('\n');
                }
                catch (IOException)
                {
                    throw;
                }
            }
        }
    }
}
